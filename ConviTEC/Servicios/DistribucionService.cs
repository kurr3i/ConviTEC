using ConviTEC.Algoritmos;
using ConviTEC.Estructuras;

namespace ConviTEC.Servicios
{
    public class DistribucionService
    {
        private readonly GrafoMatriz _grafo;
        private readonly int _sede;
        private int _ubicacionActual;
        private int _tiempoAcumulado;
        private int _tiempoUltimoRecorrido;

        public DistribucionService(GrafoMatriz grafo, string nombreSede)
        {
            _grafo = grafo ?? throw new ArgumentNullException(nameof(grafo));
            _sede = grafo.ObtenerIndice(nombreSede);
            if (_sede == -1)
                throw new ArgumentException($"La sede '{nombreSede}' no está en el grafo de distribución.");
            _ubicacionActual = _sede;
        }

        public string Sede => _grafo.ObtenerNombre(_sede);
        public string UbicacionActual => _grafo.ObtenerNombre(_ubicacionActual);
        public int TiempoAcumulado => _tiempoAcumulado;
        public int TiempoUltimoRecorrido => _tiempoUltimoRecorrido;

        // Todos los puntos del grafo menos la propia sede
        public string[] ObtenerDestinos()
        {
            string[] destinos = new string[_grafo.CantidadVertices - 1];
            int k = 0;
            for (int i = 0; i < _grafo.CantidadVertices; i++)
            {
                if (i != _sede)
                    destinos[k++] = _grafo.ObtenerNombre(i);
            }
            return destinos;
        }

        // Se valida desde la sede porque cada salida del vehículo parte de ahí
        public ValidacionRuta ValidarDestino(string destino)
        {
            if (string.IsNullOrWhiteSpace(destino))
                return ValidacionRuta.Rechazada("Debe indicar un punto de entrega.");

            int indice = _grafo.ObtenerIndice(destino);
            if (indice == -1)
                return ValidacionRuta.Rechazada($"El destino '{destino.Trim()}' no existe en la red de distribución.");

            string nombre = _grafo.ObtenerNombre(indice);
            if (indice == _sede)
                return ValidacionRuta.Rechazada($"{nombre} es la propia sede, el pedido no requiere entrega.");

            ResultadoDijkstra rutas = Dijkstra.Calcular(_grafo, _sede);
            if (!rutas.EsAlcanzable(indice))
                return ValidacionRuta.Rechazada(
                    $"No hay ruta desde la sede {Sede} hasta {nombre}. Los vehículos de esta sede no pueden llegar a ese destino.");

            return new ValidacionRuta
            {
                Valido = true,
                Destino = nombre,
                TiempoDesdeSede = rutas.Distancia(indice)
            };
        }

        // Solo calcula, no cambia la ubicación. Null si no queda ningún pendiente alcanzable.
        public PropuestaEntrega? CalcularSiguiente(EntregaPendiente[] pendientes)
        {
            if (pendientes == null || pendientes.Length == 0)
                return null;

            ResultadoDijkstra rutas = Dijkstra.Calcular(_grafo, _ubicacionActual);

            // Con "<" en un empate gana el que aparece primero (el pedido más viejo)
            int mejor = -1;
            int mejorTiempo = Dijkstra.Infinito;
            foreach (EntregaPendiente pendiente in pendientes)
            {
                int destino = IndiceDestino(pendiente);
                if (destino == -1 || !rutas.EsAlcanzable(destino))
                    continue;

                if (rutas.Distancia(destino) < mejorTiempo)
                {
                    mejor = destino;
                    mejorTiempo = rutas.Distancia(destino);
                }
            }

            if (mejor == -1)
                return null;

            return new PropuestaEntrega
            {
                Destino = _grafo.ObtenerNombre(mejor),
                Tiempo = mejorTiempo,
                Camino = NombresDelCamino(rutas.ReconstruirCamino(mejor)),
                Pedidos = PedidosConDestino(pendientes, mejor)
            };
        }

        // Entrega todos los pedidos del destino más cercano y mueve el vehículo hasta ahí.
        // Null si no hay nada que entregar.
        public ResultadoEntrega? EntregarSiguiente(EntregaPendiente[] pendientes)
        {
            PropuestaEntrega? propuesta = CalcularSiguiente(pendientes);
            if (propuesta == null)
                return null;

            int destino = _grafo.ObtenerIndice(propuesta.Destino);
            _ubicacionActual = destino;
            _tiempoAcumulado += propuesta.Tiempo;

            var resultado = new ResultadoEntrega
            {
                Destino = propuesta.Destino,
                Camino = propuesta.Camino,
                PedidosEntregados = propuesta.Pedidos,
                TiempoTramo = propuesta.Tiempo,
                TiempoTotal = _tiempoAcumulado,
                UbicacionActual = UbicacionActual
            };

            // Si ya no queda nada alcanzable termina el recorrido y la próxima salida parte de la sede
            if (CalcularSiguiente(SinDestino(pendientes, destino)) == null)
            {
                resultado.Finalizado = true;
                _tiempoUltimoRecorrido = _tiempoAcumulado;
                ReiniciarRecorrido();
            }

            return resultado;
        }

        public void ReiniciarRecorrido()
        {
            _ubicacionActual = _sede;
            _tiempoAcumulado = 0;
        }

        private int IndiceDestino(EntregaPendiente pendiente)
        {
            if (pendiente == null)
                return -1;
            return _grafo.ObtenerIndice(pendiente.Destino);
        }

        private string[] PedidosConDestino(EntregaPendiente[] pendientes, int destino)
        {
            int cantidad = 0;
            foreach (EntregaPendiente pendiente in pendientes)
                if (IndiceDestino(pendiente) == destino) cantidad++;

            string[] ids = new string[cantidad];
            int k = 0;
            foreach (EntregaPendiente pendiente in pendientes)
                if (IndiceDestino(pendiente) == destino) ids[k++] = pendiente.IdPedido;
            return ids;
        }

        private EntregaPendiente[] SinDestino(EntregaPendiente[] pendientes, int destino)
        {
            int cantidad = 0;
            foreach (EntregaPendiente pendiente in pendientes)
                if (IndiceDestino(pendiente) != destino) cantidad++;

            EntregaPendiente[] restantes = new EntregaPendiente[cantidad];
            int k = 0;
            foreach (EntregaPendiente pendiente in pendientes)
                if (IndiceDestino(pendiente) != destino) restantes[k++] = pendiente;
            return restantes;
        }

        private string[] NombresDelCamino(int[] camino)
        {
            string[] nombres = new string[camino.Length];
            for (int i = 0; i < camino.Length; i++)
                nombres[i] = _grafo.ObtenerNombre(camino[i]);
            return nombres;
        }
    }

    // Lo mínimo que necesita la distribución de un pedido pendiente
    public class EntregaPendiente
    {
        public string IdPedido { get; set; } = "";
        public string Destino { get; set; } = "";
    }

    public class PropuestaEntrega
    {
        public string Destino { get; set; } = "";
        public int Tiempo { get; set; }
        public string[] Camino { get; set; } = [];
        public string[] Pedidos { get; set; } = [];
    }

    public class ResultadoEntrega
    {
        public string Destino { get; set; } = "";
        public string[] Camino { get; set; } = [];
        public string[] PedidosEntregados { get; set; } = [];
        public int TiempoTramo { get; set; }
        public int TiempoTotal { get; set; }        // también es el tiempo de traslado de estos pedidos
        public string UbicacionActual { get; set; } = "";
        public bool Finalizado { get; set; }
    }

    public class ValidacionRuta
    {
        public bool Valido { get; set; }
        public string Destino { get; set; } = "";   // nombre limpio, para guardarlo en el pedido
        public int TiempoDesdeSede { get; set; }
        public string Motivo { get; set; } = "";

        public static ValidacionRuta Rechazada(string motivo)
        {
            return new ValidacionRuta { Valido = false, Motivo = motivo };
        }
    }
}
