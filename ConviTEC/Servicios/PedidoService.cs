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
            public ItemCantidad[] Items = [];
            public int Cantidad;
        }

        private readonly InventarioService _inventario;
        private readonly DistribucionService _distribucion;
        private readonly InventarioGlobal _global;
        private readonly string _codigoSede;
        private readonly Func<Mensaje, Task<Mensaje>> _solicitar;
        private readonly Func<Mensaje, Task> _enviar;
        private readonly Func<Task>? _alCambiarInventario;

        private Pedido[] _pedidos = new Pedido[16];
        private int _cantidadPedidos;
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
                Arreglos.Agregar(ref _pedidos, ref _cantidadPedidos, pedido);
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
                return Arreglos.Copiar(_pedidos, _cantidadPedidos);
        }

        public Pedido? ObtenerPedido(string id)
        {
            foreach (Pedido pedido in ObtenerPedidos())
            {
                if (pedido.Id == id)
                    return pedido;
            }
            return null;
        }

        public Pedido[] ObtenerPorEstado(EstadoPedido estado)
        {
            Pedido[] todos = ObtenerPedidos();
            Pedido[] filtrados = new Pedido[todos.Length];
            int n = 0;
            foreach (Pedido pedido in todos)
            {
                if (pedido.Estado == estado)
                    filtrados[n++] = pedido;
            }
            return Arreglos.Copiar(filtrados, n);
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
        private async Task<Pedido> CancelarAsync(Pedido pedido, string motivo, string[] contactadas, int cantidad)
        {
            _inventario.Liberar(pedido.Id);

            for (int i = 0; i < cantidad; i++)
            {
                try
                {
                    await _enviar(Mensaje.Crear(TipoMensaje.LiberarReserva, _codigoSede, contactadas[i],
                                                new LiberarReserva { IdPedido = pedido.Id }));
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"[Pedidos] No se pudo enviar LiberarReserva a {contactadas[i]}: {ex.Message}");
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
            ItemCantidad[] resultado = new ItemCantidad[pedidos.Length];
            int n = 0;

            foreach (ItemCantidad pedido in pedidos)
            {
                int falta = pedido.Cantidad - CantidadDe(reservados, pedido.CodigoProducto);
                if (falta > 0)
                    resultado[n++] = new ItemCantidad { CodigoProducto = pedido.CodigoProducto, Cantidad = falta };
            }
            return Arreglos.Copiar(resultado, n);
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
        private static bool Cubre(ItemCantidad[] reservados, ItemCantidad[] pedidos, int cantidadPedidos)
        {
            for (int i = 0; i < cantidadPedidos; i++)
            {
                if (CantidadDe(reservados, pedidos[i].CodigoProducto) < pedidos[i].Cantidad)
                    return false;
            }
            return true;
        }

        private static void AgregarAsignacion(ref Asignacion[] asignaciones, ref int cantidad,
                                              string sede, string codigoProducto, int unidades)
        {
            Asignacion? existente = null;
            for (int i = 0; i < cantidad; i++)
            {
                if (asignaciones[i].Sede == sede)
                {
                    existente = asignaciones[i];
                    break;
                }
            }

            if (existente == null)
            {
                existente = new Asignacion { Sede = sede };
                Arreglos.Agregar(ref asignaciones, ref cantidad, existente);
            }

            Arreglos.Agregar(ref existente.Items, ref existente.Cantidad,
                             new ItemCantidad { CodigoProducto = codigoProducto, Cantidad = unidades });
        }
        // Una línea por producto y sede de origen, con el precio del catálogo
        private DetallePedido[] ConstruirDetalle(ItemCantidad[] locales, Asignacion[] asignaciones, int cantidadAsignaciones)
        {
            DetallePedido[] detalle = new DetallePedido[4];
            int n = 0;

            foreach (ItemCantidad local in locales)
                Arreglos.Agregar(ref detalle, ref n, NuevaLinea(local.CodigoProducto, local.Cantidad, _codigoSede));

            for (int i = 0; i < cantidadAsignaciones; i++)
            {
                Asignacion asignacion = asignaciones[i];
                for (int j = 0; j < asignacion.Cantidad; j++)
                    Arreglos.Agregar(ref detalle, ref n,
                        NuevaLinea(asignacion.Items[j].CodigoProducto, asignacion.Items[j].Cantidad, asignacion.Sede));
            }

            return Arreglos.Copiar(detalle, n);
        }

        private DetallePedido NuevaLinea(string codigo, int cantidad, string sedeOrigen)
        {
            return new DetallePedido
            {
                CodigoProducto = codigo,
                Cantidad = cantidad,
                PrecioUnitario = _inventario.ObtenerProducto(codigo)?.Precio ?? 0,
                SedeOrigen = sedeOrigen
            };
        }        

    }
}