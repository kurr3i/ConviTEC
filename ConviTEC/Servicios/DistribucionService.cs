using ConviTEC.Algoritmos;
using ConviTEC.Estructuras;

namespace ConviTEC.Servicios
{
    public class DistribucionService
    {
        private readonly GrafoMatriz _grafo;
        private readonly int _sede;

        public DistribucionService(GrafoMatriz grafo, string nombreSede)
        {
            _grafo = grafo ?? throw new ArgumentNullException(nameof(grafo));
            _sede = grafo.ObtenerIndice(nombreSede);
            if (_sede == -1)
                throw new ArgumentException($"La sede '{nombreSede}' no está en el grafo de distribución.");
        }

        public string Sede => _grafo.ObtenerNombre(_sede);

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
