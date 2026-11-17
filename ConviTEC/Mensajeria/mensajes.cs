using System.Text.Json;
using System.Text.Json.Serialization;

namespace ConviTEC.Modelo
{
    // Tipos de mensaje que viajan entre sedes.
    // El valor numérico se usa como índice en BusSedes (por eso no se deben reordenar a la ligera).
    public enum TipoMensaje
    {
        SolicitudReserva,
        RespuestaReserva,
        ConfirmarReserva,
        LiberarReserva,
        SincronizarInventario,
        Ping // solo para pruebas de conexión entre sedes
    }

    // El "sobre": todos los mensajes viajan con esta estructura.
    // El contenido específico de cada tipo va como JSON dentro de la propiedad Contenido.
    public class Mensaje
    {
        // Opciones compartidas para serializar y deserializar
        public static readonly JsonSerializerOptions Opciones = new()
        {
            PropertyNameCaseInsensitive = true
        };

        [JsonConverter(typeof(JsonStringEnumConverter))]
        public TipoMensaje Tipo { get; set; }

        // Identifica este mensaje en particular (sirve para detectar duplicados)
        public string IdMensaje { get; set; } = "";

        // Une una respuesta con su solicitud: la respuesta copia el valor de la solicitud
        public string IdCorrelacion { get; set; } = "";

        // Códigos de sede (LIB, ALA, SJO, LIM). El destino también es la routing key.
        public string Origen { get; set; } = "";
        public string Destino { get; set; } = "";

        public DateTime FechaUtc { get; set; }

        // Cuerpo del mensaje en JSON, según el Tipo
        public string Contenido { get; set; } = "";

        // Crea un mensaje nuevo. Si no se indica idCorrelacion, usa el propio IdMensaje
        // (así una solicitud queda identificada y la respuesta puede copiar ese valor).
        public static Mensaje Crear<T>(TipoMensaje tipo, string origen, string destino,
                                       T contenido, string? idCorrelacion = null)
        {
            string id = Guid.NewGuid().ToString();
            return new Mensaje
            {
                Tipo = tipo,
                IdMensaje = id,
                IdCorrelacion = idCorrelacion ?? id,
                Origen = origen,
                Destino = destino,
                FechaUtc = DateTime.UtcNow,
                Contenido = JsonSerializer.Serialize(contenido, Opciones)
            };
        }

        // Convierte el Contenido (JSON) de vuelta al tipo esperado
        public T? ObtenerContenido<T>()
        {
            return JsonSerializer.Deserialize<T>(Contenido, Opciones);
        }
    }
}