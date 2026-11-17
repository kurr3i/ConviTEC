using System.Text;
using System.Text.Json;
using RabbitMQ.Client;
using ConviTEC.Modelo;
using RabbitMQ.Client.Events;
 
namespace ConviTEC.Mensajeria
{
    // Único punto de contacto con RabbitMQ. El resto del sistema solo usa esta clase.
    // Se encarga de declarar el exchange y las colas, publicar y consumir mensajes.
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
        public static string NombreCola(string sede)
        {
                return $"sede.{sede}";
        }

        // Registra un manejador para un tipo de mensaje. Solo se puede registrar uno por tipo.
        public void Suscribir(TipoMensaje tipo, Func<Mensaje, Task> manejador)
        {
            _manejadores[(int)tipo] = manejador;
        }

        // Abre la conexión, declara el exchange y las colas de TODAS las sedes, y empieza a escuchar la cola propia.
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

        private bool EsSedeConocida(string codigo)
        {
            foreach (string sede in _todasLasSedes)
            {
                if (sede == codigo) return true;
            }
            return false;
        }            
        
 
        // Cierra la conexión y libera los recursos. No se puede volver a usar.
        public async ValueTask DisposeAsync()
        {
            if (_canalConsumir != null) await _canalConsumir.DisposeAsync();
            if (_canalPublicar != null) await _canalPublicar.DisposeAsync();
            if (_conexion != null) await _conexion.DisposeAsync();
            _bloqueoPublicar.Dispose();
        }
    }
}