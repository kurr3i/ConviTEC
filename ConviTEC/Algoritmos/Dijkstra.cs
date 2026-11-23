using ConviTEC.Estructuras;

namespace ConviTEC.Algoritmos
{
    public static class Dijkstra
    {
        public const int Infinito = int.MaxValue;

        public static ResultadoDijkstra Calcular(GrafoMatriz grafo, string origen)
        {
            int indice = grafo.ObtenerIndice(origen);
            if (indice == -1)
                throw new ArgumentException($"La ubicación '{origen}' no existe en la red de distribución.");
            return Calcular(grafo, indice);
        }

        public static ResultadoDijkstra Calcular(GrafoMatriz grafo, int origen)
        {
            int n = grafo.CantidadVertices;
            if (origen < 0 || origen >= n)
                throw new ArgumentOutOfRangeException(nameof(origen),
                    $"El origen {origen} no corresponde a ningún vértice (hay {n}).");

            int[] distancias = new int[n];
            int[] previos = new int[n];
            bool[] visitados = new bool[n];

            for (int i = 0; i < n; i++)
            {
                distancias[i] = Infinito;
                previos[i] = -1;
            }
            distancias[origen] = 0;

            var cola = new ColaPrioridad<int>();
            cola.Encolar(origen, 0);

            while (!cola.EstaVacia)
            {
                int actual = cola.Desencolar(out int distanciaActual);

                // Un vértice puede quedar varias veces en la cola; solo sirve la primera vez que sale
                if (visitados[actual])
                    continue;
                visitados[actual] = true;

                for (int vecino = 0; vecino < n; vecino++)
                {
                    int peso = grafo.ObtenerPeso(actual, vecino);
                    if (peso == GrafoMatriz.SinArista || visitados[vecino])
                        continue;

                    int nuevaDistancia = distanciaActual + peso;
                    if (nuevaDistancia < distancias[vecino])
                    {
                        distancias[vecino] = nuevaDistancia;
                        previos[vecino] = actual;
                        cola.Encolar(vecino, nuevaDistancia);
                    }
                }
            }

            return new ResultadoDijkstra(origen, distancias, previos);
        }
    }

    public class ResultadoDijkstra
    {
        private readonly int[] _distancias;
        private readonly int[] _previos;

        internal ResultadoDijkstra(int origen, int[] distancias, int[] previos)
        {
            Origen = origen;
            _distancias = distancias;
            _previos = previos;
        }

        public int Origen { get; }

        // Dijkstra.Infinito si no se puede llegar
        public int Distancia(int destino)
        {
            VerificarIndice(destino);
            return _distancias[destino];
        }

        public bool EsAlcanzable(int destino)
        {
            return Distancia(destino) != Dijkstra.Infinito;
        }

        // Índices desde el origen hasta el destino. Vacío si no se puede llegar.
        public int[] ReconstruirCamino(int destino)
        {
            if (!EsAlcanzable(destino))
                return [];

            int largo = 0;
            for (int v = destino; v != -1; v = _previos[v])
                largo++;

            // Los previos van del destino hacia atrás, por eso se llena desde el final
            int[] camino = new int[largo];
            int posicion = largo - 1;
            for (int v = destino; v != -1; v = _previos[v])
                camino[posicion--] = v;

            return camino;
        }

        private void VerificarIndice(int indice)
        {
            if (indice < 0 || indice >= _distancias.Length)
                throw new ArgumentOutOfRangeException(nameof(indice),
                    $"El índice {indice} no corresponde a ningún vértice (hay {_distancias.Length}).");
        }
    }
}
