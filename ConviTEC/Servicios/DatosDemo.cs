using ConviTEC.Modelo;

namespace ConviTEC.Servicios
{
    // Catálogo y existencias de ejemplo para poder probar pedidos entre sedes.
    // TEMPORAL: se reemplaza cuando el Repositorio cargue productos.json e inventario.json.
    // El catálogo es igual en todas las sedes; las cantidades cambian por sede
    // (hay productos que una sede no tiene, para que haya que pedirlos a otra).
    public static class DatosDemo
    {
        public static void Cargar(InventarioService inventario, string codigoSede)
        {
            Producto[] productos =
            [
                new Producto { Codigo = "P001", Nombre = "Mesa de comedor", Descripcion = "Madera, 6 puestos", Categoria = "Muebles", Precio = 85000 },
                new Producto { Codigo = "P002", Nombre = "Silla ergonómica", Descripcion = "Con soporte lumbar", Categoria = "Muebles", Precio = 45000 },
                new Producto { Codigo = "P003", Nombre = "Lámpara LED", Descripcion = "Luz cálida, 12 W", Categoria = "Iluminación", Precio = 9500 },
                new Producto { Codigo = "P004", Nombre = "Taladro eléctrico", Descripcion = "650 W con maletín", Categoria = "Herramientas", Precio = 38000 },
                new Producto { Codigo = "P005", Nombre = "Juego de destornilladores", Descripcion = "24 piezas", Categoria = "Herramientas", Precio = 12500 },
                new Producto { Codigo = "P006", Nombre = "Estante metálico", Descripcion = "5 niveles", Categoria = "Almacenaje", Precio = 27000 }
            ];

            // Una cantidad por producto, en el mismo orden
            int[] existencias = codigoSede switch
            {
                "LIB" => new[] { 10, 0, 5, 20, 0, 8 },
                "ALA" => new[] { 4, 12, 0, 6, 15, 0 },
                "SJO" => new[] { 0, 8, 30, 0, 10, 12 },
                "LIM" => new[] { 7, 0, 0, 14, 3, 6 },
                _ => new[] { 5, 5, 5, 5, 5, 5 }
            };

            for (int i = 0; i < productos.Length; i++)
            {
                inventario.RegistrarProducto(productos[i]);
                if (existencias[i] > 0)
                    inventario.Agregar(productos[i].Codigo, existencias[i]);
            }
        }
    }
}