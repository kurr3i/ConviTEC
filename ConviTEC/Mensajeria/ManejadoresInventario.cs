using ConviTEC.Modelo;
using ConviTEC.Servicios;

namespace ConviTEC.Mensajeria
{
    // Lo que hace ESTA sede cuando otra le escribe sobre inventario.
    // Importante: dentro de un manejador solo se usa EnviarAsync, nunca SolicitarAsync
    // (con prefetch 1 el consumidor quedaría esperando una respuesta que no puede recibir).
    public static class ManejadoresInventario
    {

        // Envía a las demás sedes las existencias disponibles de esta
        public static async Task PublicarInventarioAsync(BusSedes bus, InventarioService inventario,
                                                         string codigoSede, string[] sedes)
        {
            var contenido = new SincronizarInventario
            {
                Sede = codigoSede,
                Inventario = inventario.ObtenerDisponibles()
            };

            foreach (string destino in sedes)
            {
                if (destino == codigoSede)
                    continue;

                try
                {
                    await bus.EnviarAsync(Mensaje.Crear(TipoMensaje.SincronizarInventario, codigoSede, destino, contenido));
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"[{codigoSede}] No se pudo sincronizar el inventario con {destino}: {ex.Message}");
                }
            }
        }

        public static void Registrar(BusSedes bus, InventarioService inventario, InventarioGlobal global,
                                     string codigoSede, string[] sedes)
        {
            // Otra sede pide apartar productos para un pedido suyo
            bus.Suscribir(TipoMensaje.SolicitudReserva, async mensaje =>
            {
                SolicitudReserva? solicitud = mensaje.ObtenerContenido<SolicitudReserva>();
                if (solicitud == null)
                {
                    Console.Error.WriteLine($"[{codigoSede}] SolicitudReserva vacía de {mensaje.Origen}, se descarta.");
                    return;
                }

                ItemCantidad[] reservados = inventario.Reservar(solicitud.IdPedido, solicitud.Items);

                var respuesta = new RespuestaReserva
                {
                    IdPedido = solicitud.IdPedido,
                    Aceptada = reservados.Length > 0,
                    Reservados = reservados,
                    Motivo = reservados.Length > 0
                        ? null
                        : $"La sede {codigoSede} no tiene existencias de los productos solicitados."
                };

                // La respuesta copia el IdCorrelacion de la solicitud para que quien esperaba la reconozca
                await bus.EnviarAsync(Mensaje.Crear(TipoMensaje.RespuestaReserva, codigoSede,
                                                    mensaje.Origen, respuesta, mensaje.IdCorrelacion));

                Console.WriteLine($"[{codigoSede}] Reserva {solicitud.IdPedido} para {mensaje.Origen}: " +
                                  $"{reservados.Length} producto(s) apartado(s).");
                await PublicarInventarioAsync(bus, inventario, codigoSede, sedes);
            });
        }
    }
}
