using ConviTEC.Estructuras;
using ConviTEC.Modelo;

namespace ConviTEC.Servicios
{
    // Copia local de las existencias DISPONIBLES de las otras sedes.
    // Se actualiza con los mensajes SincronizarInventario, así que puede estar desactualizada:
    // sirve para decidir a quién pedir, pero la sede dueña siempre tiene la última palabra
    // (por eso se le manda una SolicitudReserva y se revisa lo que responde).
    public class InventarioGlobal
    {
        private string[] _sedes = new string[8];
        private ItemCantidad[][] _copias = new ItemCantidad[8][];
        private int _cantidad;
        private readonly object _bloqueo = new();

        public void Actualizar(string sede, ItemCantidad[]? disponibles)
        {
            if (string.IsNullOrWhiteSpace(sede))
                return;

            ItemCantidad[] origen = disponibles ?? [];
            ItemCantidad[] copia = new ItemCantidad[origen.Length];
            for (int i = 0; i < origen.Length; i++)
                copia[i] = new ItemCantidad { CodigoProducto = origen[i].CodigoProducto, Cantidad = origen[i].Cantidad };

            lock (_bloqueo)
            {
                int pos = BuscarSede(sede);
                if (pos != -1)
                {
                    _copias[pos] = copia;
                    return;
                }

                // Las dos listas crecen juntas, por eso se usa un contador auxiliar
                int cantidadSedes = _cantidad;
                Arreglos.Agregar(ref _sedes, ref cantidadSedes, sede);
                Arreglos.Agregar(ref _copias, ref _cantidad, copia);
            }
        }

        private int BuscarSede(string sede)
        {
            for (int i = 0; i < _cantidad; i++)
            {
                if (_sedes[i] == sede)
                    return i;
            }
            return -1;
        }

        private static bool Contiene(string[] sedes, string sede)
        {
            foreach (string s in sedes)
            {
                if (s == sede)
                    return true;
            }
            return false;
        }
                // Unidades disponibles de un producto en esa sede según la última copia (0 si no se sabe)
        public int Disponible(string sede, string codigoProducto)
        {
            lock (_bloqueo)
            {
                int pos = BuscarSede(sede);
                if (pos == -1)
                    return 0;

                int total = 0;
                foreach (ItemCantidad linea in _copias[pos])
                {
                    if (string.Equals(linea.CodigoProducto, codigoProducto, StringComparison.OrdinalIgnoreCase))
                        total += linea.Cantidad;
                }
                return total;
            }
        }

        // La sede (fuera de las excluidas) con más unidades del producto. Null si ninguna tiene.
        public string? SedeConMas(string codigoProducto, string[] excluidas)
        {
            string[] sedes;
            lock (_bloqueo)
                sedes = Arreglos.Copiar(_sedes, _cantidad);

            string? mejor = null;
            int mayor = 0;

            foreach (string sede in sedes)
            {
                if (Contiene(excluidas, sede))
                    continue;

                int disponible = Disponible(sede, codigoProducto);
                if (disponible > mayor)
                {
                    mejor = sede;
                    mayor = disponible;
                }
            }
            return mejor;
        }

    }
}
