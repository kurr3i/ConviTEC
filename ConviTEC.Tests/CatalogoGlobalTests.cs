using ConviTEC.Modelo;
using ConviTEC.Servicios;

namespace ConviTEC.Tests
{
    public class CatalogoGlobalTests
    {
        // P1 con 10 unidades y P2 sin existencias
        private static InventarioService Crear()
        {
            var inventario = new InventarioService();
            inventario.RegistrarProducto(new Producto { Codigo = "P1", Nombre = "Mesa", Categoria = "Muebles", Precio = 100 });
            inventario.RegistrarProducto(new Producto { Codigo = "P2", Nombre = "Silla", Precio = 40 });
            inventario.Agregar("P1", 10);
            return inventario;
        }

        private static ItemCantidad[] Items(string codigo, int cantidad)
        {
            return [new ItemCantidad { CodigoProducto = codigo, Cantidad = cantidad }];
        }

        [Fact]
        public void Construir_IncluyeTodosLosProductosConSusDatos()
        {
            ProductoGlobal[] catalogo = CatalogoGlobal.Construir(Crear(), new InventarioGlobal(), "ALA", ["ALA", "SJO"]);

            Assert.Equal(2, catalogo.Length);
            Assert.Equal("Mesa", catalogo[0].Nombre);
            Assert.Equal("Muebles", catalogo[0].Categoria);
            Assert.Equal(100m, catalogo[0].Precio);
            Assert.Equal(2, catalogo[0].Existencias.Length);
        }

        [Fact]
        public void Construir_ExistenciasPropias_RestanLoReservado()
        {
            var inventario = Crear();
            inventario.Reservar("PED-1", Items("P1", 4));

            ProductoGlobal[] catalogo = CatalogoGlobal.Construir(inventario, new InventarioGlobal(), "ALA", ["ALA"]);

            Assert.Equal("ALA", catalogo[0].Existencias[0].Sede);
            Assert.Equal(6, catalogo[0].Existencias[0].Cantidad);
            Assert.Equal(0, catalogo[1].Existencias[0].Cantidad);
        }

        [Fact]
        public void Construir_SedeSinCopiaEsNull_SedeConCopiaUsaLaCopia()
        {
            var global = new InventarioGlobal();
            global.Actualizar("SJO", Items("P1", 30));

            ProductoGlobal[] catalogo = CatalogoGlobal.Construir(Crear(), global, "ALA", ["ALA", "SJO", "LIM"]);

            Assert.Equal(30, catalogo[0].Existencias[1].Cantidad);
            Assert.Equal(0, catalogo[1].Existencias[1].Cantidad);   // SJO ya publicó, pero sin sillas
            Assert.Null(catalogo[0].Existencias[2].Cantidad);       // LIM todavía no ha publicado
        }
    }
}