namespace ConviTEC.Modelo
{
    // Un producto del catálogo. El código es único.
    public class Producto
    {
        public string Codigo { get; set; } = "";
        public string Nombre { get; set; } = "";
        public string Descripcion { get; set; } = "";
        public string Categoria { get; set; } = "";
        public decimal Precio { get; set; }
    }
}