using ConviTEC.Estructuras;
using ConviTEC.Modelo;

namespace ConviTEC.Servicios
{
    // Registra pedidos de varios productos que pueden salir de varias sedes.
    //
    // Flujo de RegistrarAsync:
    //   1. Valida el pedido y que el destino tenga ruta desde esta sede.
    //   2. Aparta lo que haya en el inventario propio.
    //   3. Lo que falte lo pide a las otras sedes (la que tenga más existencias según la copia global).
    //   4. Si algo falla (sin stock, sede caída, sin respuesta a tiempo) se cancela TODO:
    //      se libera lo propio y se envía LiberarReserva a cada sede contactada.
    //   5. Si todo sale bien se confirma en todas las sedes y el pedido queda pendiente de entrega.
    //
    // Recibe los envíos como funciones para poder probarlo sin RabbitMQ.
    public class PedidoService
    {
        // Lo que hay que pedirle a una sede concreta
        private class Asignacion
        {
            public string Sede = "";
            public ListaDinamica<ItemCantidad> Items = new();
        }

        private readonly InventarioService _inventario;
        private readonly DistribucionService _distribucion;
        private readonly InventarioGlobal _global;
        private readonly string _codigoSede;
        private readonly Func<Mensaje, Task<Mensaje>> _solicitar;
        private readonly Func<Mensaje, Task> _enviar;
        private readonly Func<Task>? _alCambiarInventario;

        // Historial en orden de registro
        private readonly ListaDinamica<Pedido> _pedidos = new();
        private readonly object _bloqueo = new();

        // Lo escucha quien genera las ventas
        public event Action<Pedido>? PedidoEntregado;

        // solicitar: envía una solicitud y espera la respuesta (en producción, bus.SolicitarAsync con 10 s)
        // enviar: envía un mensaje sin esperar respuesta
        // alCambiarInventario: se llama cuando cambia el inventario propio (para sincronizar con las sedes)
        public PedidoService(InventarioService inventario, DistribucionService distribucion,
                             InventarioGlobal global, string codigoSede,
                             Func<Mensaje, Task<Mensaje>> solicitar, Func<Mensaje, Task> enviar,
                             Func<Task>? alCambiarInventario = null)
        {
            _inventario = inventario ?? throw new ArgumentNullException(nameof(inventario));
            _distribucion = distribucion ?? throw new ArgumentNullException(nameof(distribucion));
            _global = global ?? throw new ArgumentNullException(nameof(global));
            _codigoSede = codigoSede;
            _solicitar = solicitar ?? throw new ArgumentNullException(nameof(solicitar));
            _enviar = enviar ?? throw new ArgumentNullException(nameof(enviar));
            _alCambiarInventario = alCambiarInventario;
        }

        // ---------- Auxiliares ----------

        private void GuardarPedido(Pedido pedido)
        {
            lock (_bloqueo)
                _pedidos.Agregar(pedido);
        }

        private static Pedido Rechazar(Pedido pedido, string motivo)
        {
            pedido.Estado = EstadoPedido.Rechazado;
            pedido.Motivo = motivo;
            return pedido;
        }

        public Pedido[] ObtenerPedidos()
        {
            lock (_bloqueo)
                return _pedidos.ComoArreglo();
        }

        public Pedido? ObtenerPedido(string id)
        {
            lock (_bloqueo)
            {
                int pos = _pedidos.BuscarIndice(p => p.Id == id);
                return pos == -1 ? null : _pedidos.Obtener(pos);
            }
        }

        public Pedido[] ObtenerPorEstado(EstadoPedido estado)
        {
            ListaDinamica<Pedido> filtrados = new();
            foreach (Pedido pedido in ObtenerPedidos())
            {
                if (pedido.Estado == estado)
                    filtrados.Agregar(pedido);
            }
            return filtrados.ComoArreglo();
        }

        // Para DistribucionService: pedidos confirmados, el más viejo primero
        public EntregaPendiente[] ObtenerPendientesParaEntrega()
        {
            Pedido[] confirmados = ObtenerPorEstado(EstadoPedido.Confirmado);
            EntregaPendiente[] pendientes = new EntregaPendiente[confirmados.Length];
            for (int i = 0; i < confirmados.Length; i++)
                pendientes[i] = new EntregaPendiente { IdPedido = confirmados[i].Id, Destino = confirmados[i].Destino };
            return pendientes;
        }

        // Lo llama la entrega: pasa los pedidos a Entregado con su tiempo de traslado
        public void MarcarEntregados(string[] idsPedidos, int tiempoTraslado)
        {
            Pedido[] entregados = new Pedido[idsPedidos.Length];
            int n = 0;

            foreach (string id in idsPedidos)
            {
                Pedido? pedido = ObtenerPedido(id);
                if (pedido == null || pedido.Estado != EstadoPedido.Confirmado)
                    continue;

                pedido.Estado = EstadoPedido.Entregado;
                pedido.TiempoTraslado = tiempoTraslado;
                pedido.FechaEntrega = DateTime.UtcNow;
                entregados[n++] = pedido;
            }

            for (int i = 0; i < n; i++)
                PedidoEntregado?.Invoke(entregados[i]);
        }

        // Libera lo propio y avisa a las sedes contactadas (Liberar es idempotente: no hace daño de más)
        private async Task<Pedido> CancelarAsync(Pedido pedido, string motivo, string[] contactadas)
        {
            _inventario.Liberar(pedido.Id);

            foreach (string sede in contactadas)
            {
                try
                {
                    await _enviar(Mensaje.Crear(TipoMensaje.LiberarReserva, _codigoSede, sede,
                                                new LiberarReserva { IdPedido = pedido.Id }));
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"[Pedidos] No se pudo enviar LiberarReserva a {sede}: {ex.Message}");
                }
            }

            pedido.Estado = EstadoPedido.Cancelado;
            pedido.Motivo = motivo;
            await NotificarCambioAsync();
            return pedido;
        }

        private async Task NotificarCambioAsync()
        {
            if (_alCambiarInventario == null)
                return;

            try
            {
                await _alCambiarInventario();
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[Pedidos] No se pudo sincronizar el inventario: {ex.Message}");
            }
        }

        // Lo pedido menos lo ya reservado en la sede propia
        private static ItemCantidad[] Restar(ItemCantidad[] pedidos, ItemCantidad[] reservados)
        {
            ListaDinamica<ItemCantidad> resultado = new();

            foreach (ItemCantidad pedido in pedidos)
            {
                int falta = pedido.Cantidad - CantidadDe(reservados, pedido.CodigoProducto);
                if (falta > 0)
                    resultado.Agregar(new ItemCantidad { CodigoProducto = pedido.CodigoProducto, Cantidad = falta });
            }
            return resultado.ComoArreglo();
        }

        private static int CantidadDe(ItemCantidad[] items, string codigo)
        {
            int total = 0;
            foreach (ItemCantidad item in items)
            {
                if (string.Equals(item.CodigoProducto, codigo, StringComparison.OrdinalIgnoreCase))
                    total += item.Cantidad;
            }
            return total;
        }

        // True si lo reservado cubre todo lo pedido
        private static bool Cubre(ItemCantidad[] reservados, ItemCantidad[] pedidos)
        {
            foreach (ItemCantidad pedido in pedidos)
            {
                if (CantidadDe(reservados, pedido.CodigoProducto) < pedido.Cantidad)
                    return false;
            }
            return true;
        }

        private static void AgregarAsignacion(ListaDinamica<Asignacion> asignaciones,
                                              string sede, string codigoProducto, int unidades)
        {
            int pos = asignaciones.BuscarIndice(a => a.Sede == sede);
            Asignacion asignacion;

            if (pos == -1)
            {
                asignacion = new Asignacion { Sede = sede };
                asignaciones.Agregar(asignacion);
            }
            else
            {
                asignacion = asignaciones.Obtener(pos);
            }

            asignacion.Items.Agregar(new ItemCantidad { CodigoProducto = codigoProducto, Cantidad = unidades });
        }

        // Una línea por producto y sede de origen, con el precio del catálogo
        private DetallePedido[] ConstruirDetalle(ItemCantidad[] locales, ListaDinamica<Asignacion> asignaciones)
        {
            ListaDinamica<DetallePedido> detalle = new();

            foreach (ItemCantidad local in locales)
                detalle.Agregar(NuevaLinea(local.CodigoProducto, local.Cantidad, _codigoSede));

            foreach (Asignacion asignacion in asignaciones.ComoArreglo())
            {
                foreach (ItemCantidad item in asignacion.Items.ComoArreglo())
                    detalle.Agregar(NuevaLinea(item.CodigoProducto, item.Cantidad, asignacion.Sede));
            }

            return detalle.ComoArreglo();
        }

        private DetallePedido NuevaLinea(string codigo, int cantidad, string sedeOrigen)
        {
            Producto? producto = _inventario.ObtenerProducto(codigo);
            return new DetallePedido
            {
                CodigoProducto = codigo,
                Nombre = producto?.Nombre ?? "",
                Cantidad = cantidad,
                PrecioUnitario = producto?.Precio ?? 0,
                SedeOrigen = sedeOrigen
            };
        }        

        public async Task<Pedido> RegistrarAsync(string destino, ItemCantidad[] solicitados)
        {
            var pedido = new Pedido
            {
                Id = $"{_codigoSede}-{Guid.NewGuid().ToString("N")[..8].ToUpperInvariant()}",
                Fecha = DateTime.UtcNow,
                Estado = EstadoPedido.Pendiente
            };
            GuardarPedido(pedido);

            // 1. Validaciones que no necesitan comunicarse con nadie
            ItemCantidad[] items = InventarioService.Consolidar(solicitados);
            if (items.Length == 0)
                return Rechazar(pedido, "El pedido debe incluir al menos un producto con cantidad mayor que cero.");

            foreach (ItemCantidad item in items)
            {
                if (_inventario.ObtenerProducto(item.CodigoProducto) == null)
                    return Rechazar(pedido, $"El producto '{item.CodigoProducto}' no existe en el catálogo.");
            }

            ValidacionRuta ruta = _distribucion.ValidarDestino(destino);
            if (!ruta.Valido)
                return Rechazar(pedido, ruta.Motivo);
            pedido.Destino = ruta.Destino;

            // 2. Primero lo propio
            ItemCantidad[] locales = _inventario.Reservar(pedido.Id, items);
            ItemCantidad[] faltantes = Restar(items, locales);

            // 3. Repartir lo que falta entre las otras sedes, siempre empezando por la que más tiene
            ListaDinamica<Asignacion> asignaciones = new();

            foreach (ItemCantidad faltante in faltantes)
            {
                int restante = faltante.Cantidad;
                ListaDinamica<string> excluidas = new();
                excluidas.Agregar(_codigoSede);

                while (restante > 0)
                {
                    string? sede = _global.SedeConMas(faltante.CodigoProducto, excluidas.ComoArreglo());
                    if (sede == null)
                        return await CancelarAsync(pedido,
                            $"No hay existencias suficientes de '{faltante.CodigoProducto}' en la red de sedes.", []);

                    int toma = Math.Min(_global.Disponible(sede, faltante.CodigoProducto), restante);
                    AgregarAsignacion(asignaciones, sede, faltante.CodigoProducto, toma);
                    restante -= toma;
                    excluidas.Agregar(sede);
                }
            }

            // 4. Pedir a cada sede. Cualquier fallo cancela todo; a las sedes contactadas se les avisa
            // aunque no hayan respondido, porque podrían haber reservado cuando ya no esperábamos.
            ListaDinamica<string> contactadas = new();

            foreach (Asignacion asignacion in asignaciones.ComoArreglo())
            {
                contactadas.Agregar(asignacion.Sede);

                Mensaje solicitud = Mensaje.Crear(TipoMensaje.SolicitudReserva, _codigoSede, asignacion.Sede,
                    new SolicitudReserva
                    {
                        IdPedido = pedido.Id,
                        Items = asignacion.Items.ComoArreglo()
                    });

                RespuestaReserva? respuesta;
                try
                {
                    Mensaje recibido = await _solicitar(solicitud);
                    respuesta = recibido.ObtenerContenido<RespuestaReserva>();
                }
                catch (TimeoutException ex)
                {
                    return await CancelarAsync(pedido, ex.Message, contactadas.ComoArreglo());
                }
                catch (Exception ex)
                {
                    return await CancelarAsync(pedido,
                        $"Error de comunicación con la sede {asignacion.Sede}: {ex.Message}",
                        contactadas.ComoArreglo());
                }

                if (respuesta == null || !Cubre(respuesta.Reservados ?? [], asignacion.Items.ComoArreglo()))
                {
                    string detalle = string.IsNullOrWhiteSpace(respuesta?.Motivo)
                        ? "no pudo reservar todo lo solicitado"
                        : respuesta.Motivo;
                    return await CancelarAsync(pedido, $"La sede {asignacion.Sede}: {detalle}",
                                               contactadas.ComoArreglo());
                }
            }

            // 5. Todo reservado: confirmar en todas partes
            pedido.Estado = EstadoPedido.Reservado;
            pedido.Detalle = ConstruirDetalle(locales, asignaciones);

            foreach (string sedeContactada in contactadas.ComoArreglo())
            {
                try
                {
                    await _enviar(Mensaje.Crear(TipoMensaje.ConfirmarReserva, _codigoSede, sedeContactada,
                                                new ConfirmarReserva { IdPedido = pedido.Id }));
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"[Pedidos] No se pudo enviar ConfirmarReserva a {sedeContactada}: {ex.Message}");
                }
            }

            _inventario.Confirmar(pedido.Id);
            pedido.Estado = EstadoPedido.Confirmado;
            await NotificarCambioAsync();
            return pedido;
        }
    }
}