namespace ConviTEC.Estructuras
{
    // Grafo no dirigido y ponderado con matriz de adyacencia.
    // El peso de cada arista es el tiempo de traslado que hay entre ambas en minutos.
    public class GrafoMatriz
    {
        public const int SinArista = -1;

        private const int CapacidadPorDefecto = 8;

        private int[,] _matriz;
        private string[] _nombres;
        private string[] _claves;
        private int _cantidadVertices;
        private int _cantidadAristas;

        public GrafoMatriz(int capacidadInicial = CapacidadPorDefecto)
        {
            if (capacidadInicial < 1)
                throw new ArgumentException("La capacidad inicial del grafo debe ser al menos 1.");

            _matriz = CrearMatrizVacia(capacidadInicial);
            _nombres = new string[capacidadInicial];
            _claves = new string[capacidadInicial];
        }

        public int CantidadVertices => _cantidadVertices;
        public int CantidadAristas => _cantidadAristas;

        // Si el vértice ya existe devuelve su índice
        public int AgregarVertice(string nombre)
        {
            string clave = ObtenerClave(nombre);
            if (clave.Length == 0)
                throw new ArgumentException("El nombre de la ubicación no puede estar vacío.");

            int existente = BuscarClave(clave);
            if (existente != -1)
                return existente;

            if (_cantidadVertices == _nombres.Length)
                Crecer();

            _nombres[_cantidadVertices] = nombre.Trim();
            _claves[_cantidadVertices] = clave;
            return _cantidadVertices++;
        }

        // Devuelve false si la ruta ya existía (se deja la de menor tiempo)
        public bool AgregarArista(string origen, string destino, int minutos)
        {
            if (minutos <= 0)
                throw new ArgumentException(
                    $"El tiempo de traslado debe ser mayor que cero (se recibió {minutos}).");

            if (ObtenerClave(origen) == ObtenerClave(destino))
                throw new ArgumentException($"Una ruta no puede ir de '{origen}' a sí misma.");

            int i = AgregarVertice(origen);
            int j = AgregarVertice(destino);

            if (_matriz[i, j] != SinArista)
            {
                if (minutos < _matriz[i, j])
                {
                    _matriz[i, j] = minutos;
                    _matriz[j, i] = minutos;
                }
                return false;
            }

            _matriz[i, j] = minutos;
            _matriz[j, i] = minutos;
            _cantidadAristas++;
            return true;
        }

        // -1 si no existe
        public int ObtenerIndice(string nombre)
        {
            if (string.IsNullOrWhiteSpace(nombre))
                return -1;
            return BuscarClave(ObtenerClave(nombre));
        }

        public string ObtenerNombre(int indice)
        {
            VerificarIndice(indice);
            return _nombres[indice];
        }

        public int ObtenerPeso(int i, int j)
        {
            VerificarIndice(i);
            VerificarIndice(j);
            return _matriz[i, j];
        }

        public string[] ObtenerNombres()
        {
            string[] copia = new string[_cantidadVertices];
            for (int i = 0; i < _cantidadVertices; i++)
                copia[i] = _nombres[i];
            return copia;
        }

        private static string ObtenerClave(string nombre)
        {
            return nombre == null ? "" : nombre.Trim();
        }

        private int BuscarClave(string clave)
        {
            for (int i = 0; i < _cantidadVertices; i++)
            {
                if (_claves[i] == clave)
                    return i;
            }
            return -1;
        }

        private void VerificarIndice(int indice)
        {
            if (indice < 0 || indice >= _cantidadVertices)
                throw new ArgumentOutOfRangeException(nameof(indice),
                    $"El índice {indice} no corresponde a ningún vértice (hay {_cantidadVertices}).");
        }

        private void Crecer()
        {
            int nuevaCapacidad = _nombres.Length * 2;
            int[,] nuevaMatriz = CrearMatrizVacia(nuevaCapacidad);

            for (int i = 0; i < _cantidadVertices; i++)
                for (int j = 0; j < _cantidadVertices; j++)
                    nuevaMatriz[i, j] = _matriz[i, j];

            string[] nuevosNombres = new string[nuevaCapacidad];
            string[] nuevasClaves = new string[nuevaCapacidad];
            for (int i = 0; i < _cantidadVertices; i++)
            {
                nuevosNombres[i] = _nombres[i];
                nuevasClaves[i] = _claves[i];
            }

            _matriz = nuevaMatriz;
            _nombres = nuevosNombres;
            _claves = nuevasClaves;
        }

        private static int[,] CrearMatrizVacia(int tamano)
        {
            int[,] matriz = new int[tamano, tamano];
            for (int i = 0; i < tamano; i++)
                for (int j = 0; j < tamano; j++)
                    matriz[i, j] = SinArista;
            return matriz;
        }
    }
}
