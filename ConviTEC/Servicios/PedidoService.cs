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

    }
}