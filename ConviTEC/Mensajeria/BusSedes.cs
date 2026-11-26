using System.Text;
using System.Text.Json;
using ConviTEC.Modelo;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace ConviTEC.Mensajeria
{
    // Único punto de contacto con RabbitMQ. El resto del sistema solo usa esta clase.
    //
    // Uso:
    //   var bus = new BusSedes("ALA", sedes, host, usuario, clave);
    //   bus.Suscribir(TipoMensaje.Ping, async m => { ... });   // ANTES de iniciar
    //   await bus.IniciarAsync();
    //   await bus.EnviarAsync(Mensaje.Crear(...));             // enviar sin esperar respuesta
    //   var resp = await bus.SolicitarAsync(solicitud, TimeSpan.FromSeconds(10)); // esperar respuesta
    //
    // IMPORTANTE: no llamar a SolicitarAsync desde dentro de un manejador de Suscribir.
    // Los manejadores corren en el consumidor (un mensaje a la vez), así que esperar ahí una
    // respuesta bloquea la llegada de esa misma respuesta y siempre termina en tiempo agotado.
    // Se llama desde servicios o endpoints REST, que corren en otros hilos.
    public class BusSedes : IAsyncDisposable
    {
        public const string NombreExchange = "convitec.sedes";

        private readonly string _codigoSede;
        private readonly string[] _todasLasSedes;
        private readonly ConnectionFactory _factory;

        private IConnection? _conexion;
        private IChannel? _canalPublicar;
        private IChannel? _canalConsumir;

        // Evita que dos hilos publiquen a la vez por el mismo canal
        private readonly SemaphoreSlim _bloqueoPublicar = new(1, 1);

        // Un manejador por tipo de mensaje. Es un arreglo indexado por el valor del enum
        // (no se usa Dictionary porque las colecciones deben ser propias o arreglos nativos).
        private readonly Func<Mensaje, Task>?[] _manejadores =
            new Func<Mensaje, Task>?[Enum.GetValues<TipoMensaje>().Length];

        // ---------- Solicitudes que esperan respuesta ----------
        // Una solicitud enviada con SolicitarAsync queda aquí hasta que llegue su respuesta
        // (mismo IdCorrelacion) o venza el tiempo límite.
        // Es un arreglo con búsqueda lineal: hay muy pocas solicitudes a la vez, así que
        // alcanza. Cuando la TablaHash del equipo esté lista, se puede reemplazar por ella
        // indexando por IdCorrelacion.
        private class SolicitudPendiente
        {
            public string IdCorrelacion = "";
            public readonly TaskCompletionSource<Mensaje> Respuesta =
                new(TaskCreationOptions.RunContinuationsAsynchronously);
        }

        private SolicitudPendiente?[] _pendientes = new SolicitudPendiente?[16];
        private int _cantidadPendientes = 0;
        private readonly object _bloqueoPendientes = new();

        public BusSedes(string codigoSede, string[] todasLasSedes,
                        string host, string usuario, string clave)
        {
            _codigoSede = codigoSede;
            _todasLasSedes = todasLasSedes;
            _factory = new ConnectionFactory
            {
                HostName = host,
                UserName = usuario,
                Password = clave,
                ClientProvidedName = $"convitec-{codigoSede}" // aparece en el panel de RabbitMQ
            };
        }

        public static string NombreCola(string sede) => $"sede.{sede}";

        // Registra qué hacer cuando llega un mensaje de cierto tipo.
        // Debe llamarse antes de IniciarAsync para no perder mensajes que ya estén esperando.
        public void Suscribir(TipoMensaje tipo, Func<Mensaje, Task> manejador)
        {
            _manejadores[(int)tipo] = manejador;
        }

        // Abre la conexión, declara el exchange y las colas de TODAS las sedes,
        // y empieza a escuchar la cola propia.
        public async Task IniciarAsync()
        {
            _conexion = await _factory.CreateConnectionAsync();
            _canalPublicar = await _conexion.CreateChannelAsync();
            _canalConsumir = await _conexion.CreateChannelAsync();

            await _canalPublicar.ExchangeDeclareAsync(NombreExchange, ExchangeType.Direct, durable: true);

            // Se declaran las cuatro colas para que un mensaje no se pierda si la sede
            // destino todavía no ha arrancado nunca. Declarar es idempotente.
            foreach (string sede in _todasLasSedes)
            {
                string cola = NombreCola(sede);
                await _canalPublicar.QueueDeclareAsync(cola, durable: true, exclusive: false, autoDelete: false);
                await _canalPublicar.QueueBindAsync(cola, NombreExchange, sede);
            }

            // Un mensaje a la vez: el siguiente llega cuando se confirme el actual
            await _canalConsumir.BasicQosAsync(prefetchSize: 0, prefetchCount: 1, global: false);

            var consumidor = new AsyncEventingBasicConsumer(_canalConsumir);
            consumidor.ReceivedAsync += AlRecibirAsync;
            await _canalConsumir.BasicConsumeAsync(NombreCola(_codigoSede), autoAck: false, consumidor);

            Console.WriteLine($"[BusSedes] {_codigoSede} conectada. Escuchando en {NombreCola(_codigoSede)}.");
        }

        // Envía un mensaje a la sede indicada en mensaje.Destino (sin esperar respuesta)
        public async Task EnviarAsync(Mensaje mensaje)
        {
            if (_canalPublicar == null)
                throw new InvalidOperationException("BusSedes no se ha iniciado. Llame a IniciarAsync primero.");

            if (!EsSedeConocida(mensaje.Destino))
                throw new ArgumentException($"Sede destino desconocida: '{mensaje.Destino}'.");

            string json = JsonSerializer.Serialize(mensaje, Mensaje.Opciones);
            byte[] cuerpo = Encoding.UTF8.GetBytes(json);
            var propiedades = new BasicProperties { Persistent = true, ContentType = "application/json" };

            await _bloqueoPublicar.WaitAsync();
            try
            {
                await _canalPublicar.BasicPublishAsync(NombreExchange, mensaje.Destino, false, propiedades, cuerpo);
            }
            finally
            {
                _bloqueoPublicar.Release();
            }
        }

        // Envía una solicitud y espera la respuesta que traiga el mismo IdCorrelacion.
        // Lanza TimeoutException si la sede no responde dentro del tiempo límite.
        // El mensaje queda en la cola de la sede destino aunque se agote el tiempo: si esa sede
        // revive después, lo procesará y su respuesta llegará tarde (se descarta, ver AlRecibirAsync).
        // Por eso quien llama debe tratar el TimeoutException como "cancelar el pedido y
        // enviar LiberarReserva a las sedes involucradas".
        public async Task<Mensaje> SolicitarAsync(Mensaje solicitud, TimeSpan limite)
        {
            var pendiente = RegistrarPendiente(solicitud.IdCorrelacion);
            try
            {
                await EnviarAsync(solicitud);
                return await pendiente.Respuesta.Task.WaitAsync(limite);
            }
            catch (TimeoutException)
            {
                throw new TimeoutException(
                    $"La sede {solicitud.Destino} no respondió en {limite.TotalSeconds:0} segundos.");
            }
            finally
            {
                QuitarPendiente(solicitud.IdCorrelacion);
            }
        }

        // Se ejecuta cada vez que llega un mensaje a la cola propia
        private async Task AlRecibirAsync(object sender, BasicDeliverEventArgs ea)
        {
            try
            {
                string json = Encoding.UTF8.GetString(ea.Body.ToArray());
                Mensaje? mensaje = JsonSerializer.Deserialize<Mensaje>(json, Mensaje.Opciones);

                if (mensaje == null)
                {
                    Console.Error.WriteLine("[BusSedes] Mensaje vacío o malformado, se descarta.");
                }
                else if (EsRespuesta(mensaje.Tipo))
                {
                    // Una respuesta no va a un manejador: despierta a quien la estaba esperando
                    var pendiente = QuitarPendiente(mensaje.IdCorrelacion);
                    if (pendiente != null)
                        pendiente.Respuesta.TrySetResult(mensaje);
                    else
                        Console.Error.WriteLine(
                            $"[BusSedes] Respuesta tardía o desconocida ({mensaje.IdCorrelacion}) de {mensaje.Origen}, se descarta.");
                }
                else
                {
                    var manejador = _manejadores[(int)mensaje.Tipo];
                    if (manejador != null)
                        await manejador(mensaje);
                    else
                        Console.Error.WriteLine($"[BusSedes] No hay manejador para {mensaje.Tipo}, se descarta.");
                }

                // Confirmar: ya se procesó, RabbitMQ puede borrarlo de la cola
                await _canalConsumir!.BasicAckAsync(ea.DeliveryTag, multiple: false);
            }
            catch (Exception ex)
            {
                // Mensaje que no se pudo procesar: se descarta sin reencolar para no entrar
                // en un ciclo infinito. Mejora futura: cola de mensajes fallidos.
                Console.Error.WriteLine($"[BusSedes] Error procesando mensaje: {ex.Message}");
                await _canalConsumir!.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: false);
            }
        }

        // Tipos que son respuesta a una solicitud (se emparejan por IdCorrelacion)
        private static bool EsRespuesta(TipoMensaje tipo)
        {
            return tipo == TipoMensaje.RespuestaReserva;
        }

        private SolicitudPendiente RegistrarPendiente(string idCorrelacion)
        {
            var pendiente = new SolicitudPendiente { IdCorrelacion = idCorrelacion };
            lock (_bloqueoPendientes)
            {
                if (_cantidadPendientes == _pendientes.Length)
                {
                    var ampliado = new SolicitudPendiente?[_pendientes.Length * 2];
                    for (int i = 0; i < _cantidadPendientes; i++)
                        ampliado[i] = _pendientes[i];
                    _pendientes = ampliado;
                }
                _pendientes[_cantidadPendientes++] = pendiente;
            }
            return pendiente;
        }

        // Quita y devuelve la solicitud pendiente con ese id (null si no existe)
        private SolicitudPendiente? QuitarPendiente(string idCorrelacion)
        {
            lock (_bloqueoPendientes)
            {
                for (int i = 0; i < _cantidadPendientes; i++)
                {
                    if (_pendientes[i]!.IdCorrelacion == idCorrelacion)
                    {
                        var encontrada = _pendientes[i];
                        _pendientes[i] = _pendientes[_cantidadPendientes - 1];
                        _pendientes[_cantidadPendientes - 1] = null;
                        _cantidadPendientes--;
                        return encontrada;
                    }
                }
            }
            return null;
        }

        private bool EsSedeConocida(string codigo)
        {
            foreach (string sede in _todasLasSedes)
            {
                if (sede == codigo) return true;
            }
            return false;
        }

        public async ValueTask DisposeAsync()
        {
            if (_canalConsumir != null) await _canalConsumir.DisposeAsync();
            if (_canalPublicar != null) await _canalPublicar.DisposeAsync();
            if (_conexion != null) await _conexion.DisposeAsync();
            _bloqueoPublicar.Dispose();
        }
    }
}