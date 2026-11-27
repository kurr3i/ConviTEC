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

                [Fact]
        public void Reservar_NoBajaElTotalPeroSiElDisponible()
        {
            var inventario = Crear();

            ItemCantidad[] reservados = inventario.Reservar("PED-1", Items("P1", 4));

            Assert.Equal(4, reservados[0].Cantidad);
            ItemInventario item = inventario.Consultar("P1")!;
            Assert.Equal(10, item.Total);
            Assert.Equal(4, item.Reservado);
            Assert.Equal(6, item.Disponible);
        }

        [Fact]
        public void Reservar_MasDeLoDisponible_ReservaSoloLoQueHay()
        {
            var inventario = Crear();

            ItemCantidad[] reservados = inventario.Reservar("PED-1", Items("P2", 20));

            Assert.Single(reservados);
            Assert.Equal(5, reservados[0].Cantidad);
            Assert.Equal(0, inventario.Consultar("P2")!.Disponible);
        }

        [Fact]
        public void Reservar_ProductoInexistenteOSinStock_NoApareceEnElResultado()
        {
            var inventario = Crear();
            inventario.Reservar("PED-1", Items("P2", 5));

            Assert.Empty(inventario.Reservar("PED-2", Items("XX", 1)));
            Assert.Empty(inventario.Reservar("PED-3", Items("P2", 1)));
        }

        [Fact]
        public void Reservar_MismoPedidoDosVeces_NoApartaDeNuevo()
        {
            var inventario = Crear();

            inventario.Reservar("PED-1", Items("P1", 4));
            ItemCantidad[] segunda = inventario.Reservar("PED-1", Items("P1", 4));

            Assert.Equal(4, segunda[0].Cantidad);
            Assert.Equal(4, inventario.Consultar("P1")!.Reservado);
        }

        [Fact]
        public void Reservar_SinIdentificador_Lanza()
        {
            Assert.Throws<ArgumentException>(() => Crear().Reservar("", Items("P1", 1)));
        }
                [Fact]
        public void Confirmar_DescuentaElTotalYLimpiaLoReservado()
        {
            var inventario = Crear();
            inventario.Reservar("PED-1", Items("P1", 4));

            Assert.True(inventario.Confirmar("PED-1"));

            ItemInventario item = inventario.Consultar("P1")!;
            Assert.Equal(6, item.Total);
            Assert.Equal(0, item.Reservado);
        }

        [Fact]
        public void Confirmar_DosVeces_NoDescuentaDeNuevo()
        {
            var inventario = Crear();
            inventario.Reservar("PED-1", Items("P1", 4));

            inventario.Confirmar("PED-1");
            Assert.True(inventario.Confirmar("PED-1"));

            Assert.Equal(6, inventario.Consultar("P1")!.Total);
        }

        [Fact]
        public void Confirmar_SinReserva_DevuelveFalse()
        {
            Assert.False(Crear().Confirmar("NO-EXISTE"));
        }

        [Fact]
        public void Liberar_DevuelveLoReservadoYNoTocaElTotal()
        {
            var inventario = Crear();
            inventario.Reservar("PED-1", Items("P1", 4));

            Assert.True(inventario.Liberar("PED-1"));

            ItemInventario item = inventario.Consultar("P1")!;
            Assert.Equal(10, item.Total);
            Assert.Equal(0, item.Reservado);
        }

        [Fact]
        public void Liberar_DosVeces_NoDevuelveDeMas()
        {
            var inventario = Crear();
            inventario.Reservar("PED-1", Items("P1", 4));
            inventario.Reservar("PED-2", Items("P1", 2));

            inventario.Liberar("PED-1");
            Assert.True(inventario.Liberar("PED-1"));

            Assert.Equal(2, inventario.Consultar("P1")!.Reservado);
        }

        [Fact]
        public void LiberarUnPedidoConfirmado_YConfirmarUnoLiberado_DevuelvenFalse()
        {
            var inventario = Crear();
            inventario.Reservar("PED-1", Items("P1", 4));
            inventario.Reservar("PED-2", Items("P1", 2));

            inventario.Confirmar("PED-1");
            inventario.Liberar("PED-2");

            Assert.False(inventario.Liberar("PED-1"));
            Assert.False(inventario.Confirmar("PED-2"));
            Assert.Equal(6, inventario.Consultar("P1")!.Total);
            Assert.Equal(0, inventario.Consultar("P1")!.Reservado);
        }
                [Fact]
        public void Descontar_BajaElTotal()
        {
            var inventario = Crear();
            inventario.Descontar("P1", 3);

            Assert.Equal(7, inventario.Consultar("P1")!.Total);
        }

        [Fact]
        public void Descontar_RespetaLoReservado_Y_ValidaEntradas()
        {
            var inventario = Crear();
            inventario.Reservar("PED-1", Items("P1", 8));

            Assert.Throws<InvalidOperationException>(() => inventario.Descontar("P1", 3));
            Assert.Throws<ArgumentException>(() => inventario.Descontar("P1", 0));
            Assert.Throws<ArgumentException>(() => inventario.Descontar("XX", 1));
        }

        [Fact]
        public void ObtenerDisponibles_DescuentaLoReservado()
        {
            var inventario = Crear();
            inventario.Reservar("PED-1", Items("P1", 4));

            ItemCantidad[] disponibles = inventario.ObtenerDisponibles();

            Assert.Equal(2, disponibles.Length);
            Assert.Equal(6, disponibles[0].Cantidad);
            Assert.Equal(5, disponibles[1].Cantidad);
        }

        [Fact]
        public void Consolidar_UneCodigosRepetidosYDescartaInvalidos()
        {
            ItemCantidad[] entrada =
            [
                new ItemCantidad { CodigoProducto = "P1", Cantidad = 2 },
                new ItemCantidad { CodigoProducto = " p1 ", Cantidad = 3 },
                new ItemCantidad { CodigoProducto = "P2", Cantidad = 0 },
                new ItemCantidad { CodigoProducto = "", Cantidad = 4 }
            ];

            ItemCantidad[] resultado = InventarioService.Consolidar(entrada);

            Assert.Single(resultado);
            Assert.Equal("P1", resultado[0].CodigoProducto);
            Assert.Equal(5, resultado[0].Cantidad);
            Assert.Empty(InventarioService.Consolidar(null));
        }


    }
}