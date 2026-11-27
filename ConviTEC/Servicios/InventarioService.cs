using ConviTEC.Estructuras;
using ConviTEC.Modelo;

namespace ConviTEC.Servicios
{
    // Catálogo e inventario de ESTA sede. Solo esta sede modifica sus existencias:
    // las demás piden por mensajes (reservar, confirmar, liberar).
    //
    // Reservar NO descuenta: aparta unidades (sube Reservado). Solo Confirmar descuenta de verdad.
    // Confirmar y Liberar son idempotentes: un mensaje duplicado no cambia nada.
    //
    // Por dentro usa arreglos nativos con búsqueda lineal. Cuando existan TablaHash y ArbolAVL,
    // solo hay que cambiar los métodos privados Buscar* y los campos.
    public class InventarioService
    {
        private enum EstadoReserva { Activa, Confirmada, Liberada }

        private class Reserva
        {
            public string IdPedido = "";
            public ItemCantidad[] Items = [];
            public EstadoReserva Estado;
        }

        private Producto[] _productos = new Producto[8];
        private int _cantidadProductos;
        private ItemInventario[] _items = new ItemInventario[8];
        private int _cantidadItems;
        private Reserva[] _reservas = new Reserva[8];
        private int _cantidadReservas;

        // Los manejadores de mensajes y la API usan el servicio al mismo tiempo
        private readonly object _bloqueo = new();

        // ---------- Búsquedas (lo único que cambia al usar TablaHash o AVL) ----------

        private int BuscarProducto(string codigo)
        {
            for (int i = 0; i < _cantidadProductos; i++)
            {
                if (string.Equals(_productos[i].Codigo, codigo, StringComparison.OrdinalIgnoreCase))
                    return i;
            }
            return -1;
        }

        private int BuscarItem(string codigo)
        {
            for (int i = 0; i < _cantidadItems; i++)
            {
                if (string.Equals(_items[i].Codigo, codigo, StringComparison.OrdinalIgnoreCase))
                    return i;
            }
            return -1;
        }

        private int BuscarReserva(string idPedido)
        {
            for (int i = 0; i < _cantidadReservas; i++)
            {
                if (_reservas[i].IdPedido == idPedido)
                    return i;
            }
            return -1;
        }

        // ---------- Auxiliares ----------

        private static string Limpiar(string? texto)
        {
            return (texto ?? "").Trim();
        }

        private static Producto CopiarProducto(Producto p)
        {
            return new Producto
            {
                Codigo = p.Codigo,
                Nombre = p.Nombre,
                Descripcion = p.Descripcion,
                Categoria = p.Categoria,
                Precio = p.Precio
            };
        }

        private static ItemInventario CopiarItem(ItemInventario i)
        {
            return new ItemInventario { Codigo = i.Codigo, Total = i.Total, Reservado = i.Reservado };
        }

        private static ItemCantidad[] CopiarItems(ItemCantidad[] items)
        {
            ItemCantidad[] copia = new ItemCantidad[items.Length];
            for (int i = 0; i < items.Length; i++)
                copia[i] = new ItemCantidad { CodigoProducto = items[i].CodigoProducto, Cantidad = items[i].Cantidad };
            return copia;
            
        }
                // ---------- Catálogo ----------

        // Registra el producto y le crea su inventario en cero
        public void RegistrarProducto(Producto producto)
        {
            if (producto == null)
                throw new ArgumentNullException(nameof(producto));

            string codigo = Limpiar(producto.Codigo);
            if (codigo.Length == 0)
                throw new ArgumentException("El código del producto no puede estar vacío.");
            if (producto.Precio < 0)
                throw new ArgumentException("El precio del producto no puede ser negativo.");

            lock (_bloqueo)
            {
                if (BuscarProducto(codigo) != -1)
                    throw new ArgumentException($"Ya existe un producto con el código '{codigo}'.");

                var copia = new Producto
                {
                    Codigo = codigo,
                    Nombre = producto.Nombre,
                    Descripcion = producto.Descripcion,
                    Categoria = producto.Categoria,
                    Precio = producto.Precio
                };
                Arreglos.Agregar(ref _productos, ref _cantidadProductos, copia);
                Arreglos.Agregar(ref _items, ref _cantidadItems, new ItemInventario { Codigo = codigo });
            }
        }

        // Null si no existe
        public Producto? ObtenerProducto(string codigo)
        {
            lock (_bloqueo)
            {
                int pos = BuscarProducto(Limpiar(codigo));
                return pos == -1 ? null : CopiarProducto(_productos[pos]);
            }
        }

        public Producto[] ObtenerProductos()
        {
            lock (_bloqueo)
            {
                Producto[] copia = new Producto[_cantidadProductos];
                for (int i = 0; i < _cantidadProductos; i++)
                    copia[i] = CopiarProducto(_productos[i]);
                return copia;
            }
        }
    }
}