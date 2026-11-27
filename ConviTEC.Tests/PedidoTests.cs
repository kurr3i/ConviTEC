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
                
    }
}