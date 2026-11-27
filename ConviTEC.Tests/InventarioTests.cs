using ConviTEC.Modelo;
using ConviTEC.Servicios;

namespace ConviTEC.Tests
{
    public class InventarioTests
    {
        // P1 con 10 unidades y P2 con 5
        private static InventarioService Crear()
        {
            var inventario = new InventarioService();
            inventario.RegistrarProducto(new Producto { Codigo = "P1", Nombre = "Mesa", Precio = 100 });
            inventario.RegistrarProducto(new Producto { Codigo = "P2", Nombre = "Silla", Precio = 40 });
            inventario.Agregar("P1", 10);
            inventario.Agregar("P2", 5);
            return inventario;
        }

        private static ItemCantidad[] Items(string codigo, int cantidad)
        {
            return [new ItemCantidad { CodigoProducto = codigo, Cantidad = cantidad }];
        }

        [Fact]
        public void Agregar_SumaAlTotal()
        {
            var inventario = Crear();
            inventario.Agregar("P1", 5);

            Assert.Equal(15, inventario.Consultar("P1")!.Total);
        }

        [Fact]
        public void Agregar_CantidadInvalidaOProductoInexistente_Lanza()
        {
            var inventario = Crear();

            Assert.Throws<ArgumentException>(() => inventario.Agregar("P1", 0));
            Assert.Throws<ArgumentException>(() => inventario.Agregar("P1", -3));
            Assert.Throws<ArgumentException>(() => inventario.Agregar("XX", 1));
        }

        [Fact]
        public void RegistrarProducto_CodigoDuplicadoOVacio_Lanza()
        {
            var inventario = Crear();

            Assert.Throws<ArgumentException>(() => inventario.RegistrarProducto(new Producto { Codigo = "p1" }));
            Assert.Throws<ArgumentException>(() => inventario.RegistrarProducto(new Producto { Codigo = "  " }));
        }

        [Fact]
        public void Consultar_ProductoInexistente_DevuelveNull()
        {
            Assert.Null(Crear().Consultar("XX"));
        }
    }
}