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

        // Clave: el código en mayúsculas, así la búsqueda no distingue mayúsculas de minúsculas
        private readonly ArbolAVL<string, Producto> _productos = new();
        private readonly ArbolAVL<string, ItemInventario> _items = new();
        private Reserva[] _reservas = new Reserva[8];
        private int _cantidadReservas;

        // Los manejadores de mensajes y la API usan el servicio al mismo tiempo
        private readonly object _bloqueo = new();


        // ---------- Búsquedas ----------

        private static string Clave(string? codigo)
        {
            return Limpiar(codigo).ToUpperInvariant();
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
                if (_productos.Contiene(Clave(codigo)))
                    throw new ArgumentException($"Ya existe un producto con el código '{codigo}'.");

                var copia = new Producto
                {
                    Codigo = codigo,
                    Nombre = producto.Nombre,
                    Descripcion = producto.Descripcion,
                    Categoria = producto.Categoria,
                    Precio = producto.Precio
                };
                _productos.Insertar(Clave(codigo), copia);
                _items.Insertar(Clave(codigo), new ItemInventario { Codigo = codigo });
            }
        }

        // Null si no existe
        public Producto? ObtenerProducto(string codigo)
        {
            lock (_bloqueo)
            {
                return _productos.IntentarObtener(Clave(codigo), out Producto producto)
                    ? CopiarProducto(producto)
                    : null;
            }
        }

        public Producto[] ObtenerProductos()
        {
            lock (_bloqueo)
            {
                // Salen ordenados por código
                Producto[] productos = _productos.ValoresEnOrden();
                Producto[] copia = new Producto[productos.Length];
                for (int i = 0; i < productos.Length; i++)
                    copia[i] = CopiarProducto(productos[i]);
                return copia;
            }
        }
                // ---------- Existencias ----------

        // Incorpora unidades al inventario de un producto registrado
        public ItemInventario Agregar(string codigo, int cantidad)
        {
            if (cantidad <= 0)
                throw new ArgumentException($"La cantidad a agregar debe ser mayor que cero (se recibió {cantidad}).");

            lock (_bloqueo)
            {
                if (!_items.IntentarObtener(Clave(codigo), out ItemInventario item))
                    throw new ArgumentException($"El producto '{codigo}' no está registrado en el catálogo.");

                item.Total += cantidad;
                return CopiarItem(item);
            }
        }

        // Null si el producto no existe
        public ItemInventario? Consultar(string codigo)
        {
            lock (_bloqueo)
            {
                return _items.IntentarObtener(Clave(codigo), out ItemInventario item)
                    ? CopiarItem(item)
                    : null;
            }
        }

        public ItemInventario[] ObtenerTodos()
        {
            lock (_bloqueo)
            {
                ItemInventario[] items = _items.ValoresEnOrden();
                ItemInventario[] copia = new ItemInventario[items.Length];
                for (int i = 0; i < items.Length; i++)
                    copia[i] = CopiarItem(items[i]);
                return copia;
            }
        }
                // Lo que se comparte con las otras sedes: unidades disponibles por producto
        public ItemCantidad[] ObtenerDisponibles()
        {
            lock (_bloqueo)
            {
                ItemInventario[] items = _items.ValoresEnOrden();
                ItemCantidad[] disponibles = new ItemCantidad[items.Length];
                for (int i = 0; i < items.Length; i++)
                {
                    disponibles[i] = new ItemCantidad
                    {
                        CodigoProducto = items[i].Codigo,
                        Cantidad = items[i].Disponible
                    };
                }
                return disponibles;
            }
        }

        // Descuento directo (venta en la propia sede). No toca lo reservado por otros pedidos.
        public void Descontar(string codigo, int cantidad)
        {
            if (cantidad <= 0)
                throw new ArgumentException($"La cantidad a descontar debe ser mayor que cero (se recibió {cantidad}).");

            lock (_bloqueo)
            {
                if (!_items.IntentarObtener(Clave(codigo), out ItemInventario item))
                    throw new ArgumentException($"El producto '{codigo}' no existe.");

                if (item.Disponible < cantidad)
                    throw new InvalidOperationException(
                        $"No hay existencias suficientes de '{item.Codigo}': disponible {item.Disponible}, solicitado {cantidad}.");

                item.Total -= cantidad;
            }
        }
                // ---------- Reservas por pedido ----------

        // Aparta lo que haya (hasta lo pedido) y devuelve lo que REALMENTE se apartó.
        // Productos inexistentes o sin existencias simplemente no aparecen en el resultado.
        // Si el pedido ya tenía una reserva (mensaje duplicado) devuelve la misma, sin apartar de nuevo.
        public ItemCantidad[] Reservar(string idPedido, ItemCantidad[] solicitados)
        {
            string id = Limpiar(idPedido);
            if (id.Length == 0)
                throw new ArgumentException("El pedido debe tener un identificador para reservar.");

            lock (_bloqueo)
            {
                int existente = BuscarReserva(id);
                if (existente != -1)
                    return CopiarItems(_reservas[existente].Items);

                ItemCantidad[] pedidos = solicitados ?? [];
                ItemCantidad[] reservados = new ItemCantidad[pedidos.Length];
                int n = 0;

                foreach (ItemCantidad pedido in pedidos)
                {
                    if (pedido == null || pedido.Cantidad <= 0)
                        continue;

                    if (!_items.IntentarObtener(Clave(pedido.CodigoProducto), out ItemInventario item))
                        continue;

                    int cantidad = Math.Min(pedido.Cantidad, item.Disponible);
                    if (cantidad <= 0)
                        continue;

                    item.Reservado += cantidad;
                    reservados[n++] = new ItemCantidad { CodigoProducto = item.Codigo, Cantidad = cantidad };
                }

                ItemCantidad[] resultado = Arreglos.Copiar(reservados, n);
                Arreglos.Agregar(ref _reservas, ref _cantidadReservas,
                    new Reserva { IdPedido = id, Items = resultado, Estado = EstadoReserva.Activa });

                return CopiarItems(resultado);
            }
        }
                // Descuenta de verdad lo reservado. True si quedó confirmada (también si ya lo estaba).
        // False si no hay reserva o ya fue liberada.
        public bool Confirmar(string idPedido)
        {
            lock (_bloqueo)
            {
                int pos = BuscarReserva(Limpiar(idPedido));
                if (pos == -1)
                    return false;

                Reserva reserva = _reservas[pos];
                if (reserva.Estado == EstadoReserva.Confirmada)
                    return true;
                if (reserva.Estado == EstadoReserva.Liberada)
                    return false;

                foreach (ItemCantidad linea in reserva.Items)
                {
                    ItemInventario item = _items.Obtener(Clave(linea.CodigoProducto));
                    item.Reservado -= linea.Cantidad;
                    item.Total -= linea.Cantidad;
                }
                reserva.Estado = EstadoReserva.Confirmada;
                return true;
            }
        }

        // Devuelve lo reservado al disponible. True si quedó liberada (también si ya lo estaba).
        // False si no hay reserva o ya fue confirmada (lo confirmado no se puede liberar).
        public bool Liberar(string idPedido)
        {
            lock (_bloqueo)
            {
                int pos = BuscarReserva(Limpiar(idPedido));
                if (pos == -1)
                    return false;

                Reserva reserva = _reservas[pos];
                if (reserva.Estado == EstadoReserva.Liberada)
                    return true;
                if (reserva.Estado == EstadoReserva.Confirmada)
                    return false;

                foreach (ItemCantidad linea in reserva.Items)
                    _items.Obtener(Clave(linea.CodigoProducto)).Reservado -= linea.Cantidad;

                reserva.Estado = EstadoReserva.Liberada;
                return true;
            }
        }
                // Une las líneas con el mismo código y descarta las de cantidad <= 0
        public static ItemCantidad[] Consolidar(ItemCantidad[]? items)
        {
            if (items == null)
                return [];

            ItemCantidad[] resultado = new ItemCantidad[items.Length];
            int n = 0;

            foreach (ItemCantidad item in items)
            {
                if (item == null || item.Cantidad <= 0)
                    continue;

                string codigo = Limpiar(item.CodigoProducto);
                if (codigo.Length == 0)
                    continue;

                int pos = -1;
                for (int i = 0; i < n; i++)
                {
                    if (string.Equals(resultado[i].CodigoProducto, codigo, StringComparison.OrdinalIgnoreCase))
                    {
                        pos = i;
                        break;
                    }
                }

                if (pos == -1)
                    resultado[n++] = new ItemCantidad { CodigoProducto = codigo, Cantidad = item.Cantidad };
                else
                    resultado[pos].Cantidad += item.Cantidad;
            }

            return Arreglos.Copiar(resultado, n);
        }
    }
}