using ConviTEC.Estructuras;
using ConviTEC.Modelo;
using ConviTEC.Servicios;

namespace ConviTEC.Tests
{
    public class PedidoTests
    {
        // Dos sedes sin RabbitMQ: ALA (la que registra) y SJO (la que responde).
        // Los envíos son funciones que llaman directamente al inventario de SJO.
        private class Escenario
        {
            public readonly InventarioService Local = new();
            public readonly InventarioService Remoto = new();
            public readonly InventarioGlobal Global = new();
            public readonly PedidoService Pedidos;

            public bool SedeCaida;       // simula que SJO no responde
            public int Confirmaciones;   // ConfirmarReserva enviadas
            public int Liberaciones;     // LiberarReserva enviadas

            // stockGlobal es lo que ALA CREE que tiene SJO (puede estar desactualizado)
            public Escenario(int stockLocal, int stockRemoto, int? stockGlobal = null)
            {
                Preparar(Local, stockLocal);
                Preparar(Remoto, stockRemoto);

                var grafo = new GrafoMatriz();
                grafo.AgregarArista("Alajuela (Sede)", "San José (Sede)", 30);
                grafo.AgregarArista("San José (Sede)", "Cartago", 25);
                grafo.AgregarArista("Liberia (Sede)", "Cañas", 65);

                Global.Actualizar("SJO", [new ItemCantidad { CodigoProducto = "P1", Cantidad = stockGlobal ?? stockRemoto }]);

                Pedidos = new PedidoService(Local, new DistribucionService(grafo, "Alajuela"), Global, "ALA",
                    solicitar: SolicitarAsync, enviar: EnviarAsync);
            }

            private static void Preparar(InventarioService inventario, int stock)
            {
                inventario.RegistrarProducto(new Producto { Codigo = "P1", Nombre = "Mesa", Precio = 100 });
                if (stock > 0)
                    inventario.Agregar("P1", stock);
            }

            private Task<Mensaje> SolicitarAsync(Mensaje solicitud)
            {
                if (SedeCaida)
                    throw new TimeoutException("La sede SJO no respondió en 10 segundos.");

                SolicitudReserva contenido = solicitud.ObtenerContenido<SolicitudReserva>()!;
                ItemCantidad[] reservados = Remoto.Reservar(contenido.IdPedido, contenido.Items);
                var respuesta = new RespuestaReserva
                {
                    IdPedido = contenido.IdPedido,
                    Aceptada = reservados.Length > 0,
                    Reservados = reservados
                };
                return Task.FromResult(Mensaje.Crear(TipoMensaje.RespuestaReserva, "SJO", "ALA",
                                                     respuesta, solicitud.IdCorrelacion));
            }

            private Task EnviarAsync(Mensaje mensaje)
            {
                if (mensaje.Tipo == TipoMensaje.ConfirmarReserva)
                {
                    Confirmaciones++;
                    Remoto.Confirmar(mensaje.ObtenerContenido<ConfirmarReserva>()!.IdPedido);
                }
                else if (mensaje.Tipo == TipoMensaje.LiberarReserva)
                {
                    Liberaciones++;
                    Remoto.Liberar(mensaje.ObtenerContenido<LiberarReserva>()!.IdPedido);
                }
                return Task.CompletedTask;
            }
        }

        private static ItemCantidad[] Items(int cantidad)
        {
            return [new ItemCantidad { CodigoProducto = "P1", Cantidad = cantidad }];
        }
        [Fact]
        public async Task Exitoso_SoloConInventarioPropio()
        {
            var e = new Escenario(stockLocal: 10, stockRemoto: 10);

            Pedido pedido = await e.Pedidos.RegistrarAsync("Cartago", Items(3));

            Assert.Equal(EstadoPedido.Confirmado, pedido.Estado);
            Assert.Equal("Cartago", pedido.Destino);
            Assert.Equal(7, e.Local.Consultar("P1")!.Total);
            Assert.Equal(10, e.Remoto.Consultar("P1")!.Total);
            Assert.Equal(0, e.Confirmaciones);
            Assert.Equal(300m, pedido.Monto);
        }
        [Fact]
        public async Task Exitoso_ComplementaConOtraSede()
        {
            var e = new Escenario(stockLocal: 2, stockRemoto: 10);

            Pedido pedido = await e.Pedidos.RegistrarAsync("Cartago", Items(5));

            Assert.Equal(EstadoPedido.Confirmado, pedido.Estado);
            Assert.Equal(0, e.Local.Consultar("P1")!.Total);
            Assert.Equal(7, e.Remoto.Consultar("P1")!.Total);
            Assert.Equal(1, e.Confirmaciones);
            Assert.Equal(2, pedido.Detalle.Length);
            Assert.Equal("ALA", pedido.Detalle[0].SedeOrigen);
            Assert.Equal("SJO", pedido.Detalle[1].SedeOrigen);
            Assert.Equal(500m, pedido.Monto);
        }

        [Fact]
        public async Task SinExistenciasEnLaRed_CancelaYLiberaLoPropio()
        {
            var e = new Escenario(stockLocal: 2, stockRemoto: 1);

            Pedido pedido = await e.Pedidos.RegistrarAsync("Cartago", Items(10));

            Assert.Equal(EstadoPedido.Cancelado, pedido.Estado);
            Assert.Contains("existencias", pedido.Motivo);
            ItemInventario local = e.Local.Consultar("P1")!;
            Assert.Equal(2, local.Total);
            Assert.Equal(0, local.Reservado);
        }

        [Fact]
        public async Task SedeNoResponde_CancelaYAvisaParaLiberar()
        {
            var e = new Escenario(stockLocal: 2, stockRemoto: 10) { SedeCaida = true };

            Pedido pedido = await e.Pedidos.RegistrarAsync("Cartago", Items(5));

            Assert.Equal(EstadoPedido.Cancelado, pedido.Estado);
            Assert.Contains("no respondió", pedido.Motivo);
            Assert.Equal(1, e.Liberaciones);
            Assert.Equal(2, e.Local.Consultar("P1")!.Total);
            Assert.Equal(0, e.Local.Consultar("P1")!.Reservado);
        }

        [Fact]
        public async Task CopiaGlobalDesactualizada_LaSedeResponderParcial_CancelaTodo()
        {
            // ALA cree que SJO tiene 10, pero solo le quedan 1
            var e = new Escenario(stockLocal: 2, stockRemoto: 1, stockGlobal: 10);

            Pedido pedido = await e.Pedidos.RegistrarAsync("Cartago", Items(5));

            Assert.Equal(EstadoPedido.Cancelado, pedido.Estado);
            Assert.Equal(1, e.Liberaciones);
            Assert.Equal(1, e.Remoto.Consultar("P1")!.Total);
            Assert.Equal(0, e.Remoto.Consultar("P1")!.Reservado);
            Assert.Equal(0, e.Local.Consultar("P1")!.Reservado);
        }

        [Fact]
        public async Task DestinoInalcanzable_SeRechazaSinReservar()
        {
            var e = new Escenario(stockLocal: 10, stockRemoto: 10);

            Pedido pedido = await e.Pedidos.RegistrarAsync("Cañas", Items(1));

            Assert.Equal(EstadoPedido.Rechazado, pedido.Estado);
            Assert.Contains("No hay ruta", pedido.Motivo);
            Assert.Equal(0, e.Local.Consultar("P1")!.Reservado);
        }

        [Fact]
        public async Task ProductoInexistenteOPedidoVacio_SeRechaza()
        {
            var e = new Escenario(stockLocal: 10, stockRemoto: 10);

            Pedido inexistente = await e.Pedidos.RegistrarAsync("Cartago",
                [new ItemCantidad { CodigoProducto = "XX", Cantidad = 1 }]);
            Pedido vacio = await e.Pedidos.RegistrarAsync("Cartago", []);

            Assert.Equal(EstadoPedido.Rechazado, inexistente.Estado);
            Assert.Equal(EstadoPedido.Rechazado, vacio.Estado);
        }

        [Fact]
        public async Task PendientesYEntrega_PasanPorLosEstadosEsperados()
        {
            var e = new Escenario(stockLocal: 10, stockRemoto: 10);
            Pedido entregado = null!;
            e.Pedidos.PedidoEntregado += p => entregado = p;

            Pedido pedido = await e.Pedidos.RegistrarAsync("Cartago", Items(1));
            await e.Pedidos.RegistrarAsync("Cañas", Items(1));   // rechazado: no debe quedar pendiente

            EntregaPendiente[] pendientes = e.Pedidos.ObtenerPendientesParaEntrega();
            Assert.Single(pendientes);
            Assert.Equal(pedido.Id, pendientes[0].IdPedido);
            Assert.Equal("Cartago", pendientes[0].Destino);

            e.Pedidos.MarcarEntregados([pedido.Id], 55);

            Assert.Equal(EstadoPedido.Entregado, pedido.Estado);
            Assert.Equal(55, pedido.TiempoTraslado);
            Assert.NotNull(pedido.FechaEntrega);
            Assert.Same(pedido, entregado);
            Assert.Empty(e.Pedidos.ObtenerPendientesParaEntrega());
            Assert.Equal(2, e.Pedidos.ObtenerPedidos().Length);
        }                
    }
}