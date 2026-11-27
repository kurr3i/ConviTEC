using ConviTEC.Modelo;

namespace ConviTEC.Servicios
{
    // Existencias disponibles de un producto en una sede.
    // Cantidad es null si todavía no se sabe (esa sede aún no ha publicado su inventario).
    public class ExistenciaSede
    {
        public string Sede { get; set; } = "";
        public int? Cantidad { get; set; }
    }

    public class ProductoGlobal
    {
        public string Codigo { get; set; } = "";
        public string Nombre { get; set; } = "";
        public string Descripcion { get; set; } = "";
        public string Categoria { get; set; } = "";
        public decimal Precio { get; set; }
        public ExistenciaSede[] Existencias { get; set; } = [];
    }

    // Catálogo de toda la red: el catálogo local con las existencias de cada sede.
    // Las propias salen del inventario; las demás, de la última copia recibida por mensajes.
    public static class CatalogoGlobal
    {
        public static ProductoGlobal[] Construir(InventarioService inventario, InventarioGlobal global,
                                                 string codigoSede, string[] sedes)
        {
            Producto[] productos = inventario.ObtenerProductos();
            ProductoGlobal[] resultado = new ProductoGlobal[productos.Length];

            for (int i = 0; i < productos.Length; i++)
            {
                Producto producto = productos[i];
                ExistenciaSede[] existencias = new ExistenciaSede[sedes.Length];

                for (int j = 0; j < sedes.Length; j++)
                {
                    int? cantidad;
                    if (sedes[j] == codigoSede)
                        cantidad = inventario.Consultar(producto.Codigo)?.Disponible ?? 0;
                    else if (global.Conoce(sedes[j]))
                        cantidad = global.Disponible(sedes[j], producto.Codigo);
                    else
                        cantidad = null;

                    existencias[j] = new ExistenciaSede { Sede = sedes[j], Cantidad = cantidad };
                }

                resultado[i] = new ProductoGlobal
                {
                    Codigo = producto.Codigo,
                    Nombre = producto.Nombre,
                    Descripcion = producto.Descripcion,
                    Categoria = producto.Categoria,
                    Precio = producto.Precio,
                    Existencias = existencias
                };
            }
            return resultado;
        }
    }
}