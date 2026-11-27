using System.Text.Json.Serialization;

namespace ConviTEC.Modelo
{
    // Existencias de un producto en esta sede.
    // Reservado son unidades apartadas para un pedido que aún no se confirma.
    public class ItemInventario
    {
        public string Codigo { get; set; } = "";
        public int Total { get; set; }
        public int Reservado { get; set; }

        [JsonIgnore]
        public int Disponible => Total - Reservado;
    }
}