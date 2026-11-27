using System.Text.Json.Serialization;

namespace ConviTEC.Modelo
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum EstadoPedido
    {
        Pendiente,   // recién creado, todavía se está procesando
        Reservado,   // todas las sedes reservaron lo pedido
        Confirmado,  // inventarios descontados, espera la entrega
        Entregado,
        Cancelado,   // falló una reserva o una sede no respondió
        Rechazado    // no se aceptó (destino inalcanzable, producto inexistente, pedido vacío)
    }

    // Una línea del pedido: de qué sede sale cada cantidad
    public class DetallePedido
    {
        public string CodigoProducto { get; set; } = "";
        public int Cantidad { get; set; }
        public decimal PrecioUnitario { get; set; }
        public string SedeOrigen { get; set; } = "";

        [JsonIgnore]
        public decimal Subtotal => Cantidad * PrecioUnitario;
    }

    public class Pedido
    {
        public string Id { get; set; } = "";
        public DateTime Fecha { get; set; }
        public EstadoPedido Estado { get; set; }
        public string Destino { get; set; } = "";

        // Por qué se canceló o rechazó (vacío si todo salió bien)
        public string Motivo { get; set; } = "";

        public DetallePedido[] Detalle { get; set; } = [];

        // Se llenan al entregar
        public int TiempoTraslado { get; set; }
        public DateTime? FechaEntrega { get; set; }

        public decimal Monto
        {
            get
            {
                decimal total = 0;
                foreach (DetallePedido linea in Detalle)
                    total += linea.Subtotal;
                return total;
            }
        }
    }
}