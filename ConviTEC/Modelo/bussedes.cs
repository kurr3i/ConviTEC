using System.Text;
using System.Text.Json;
using RabbitMQ.Client;
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