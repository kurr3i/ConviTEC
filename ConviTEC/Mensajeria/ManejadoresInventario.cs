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
            // MANEJADORES: los siguientes commits agregan uno por uno encima de esta línea
        }
    }
}
