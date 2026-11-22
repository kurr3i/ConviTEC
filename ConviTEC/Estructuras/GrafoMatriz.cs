using System.Globalization;
using System.Text;

namespace ConviTEC.Estructuras
{
    // Grafo no dirigido y ponderado con matriz de adyacencia.
    // El peso de cada arista es el tiempo de traslado que hay entre ambas en minutos.
    public class GrafoMatriz
    {
        public const int SinArista = -1;

        private const string SufijoSede = "(Sede)";
        private const int CapacidadPorDefecto = 8;

        private int[,] _matriz;
        private string[] _nombres;   // "San José"
        private string[] _claves;    // "san jose"
        private bool[] _esSede;
        private int _cantidadVertices;
        private int _cantidadAristas;

        public GrafoMatriz(int capacidadInicial = CapacidadPorDefecto)
        {
            if (capacidadInicial < 1)
                throw new ArgumentException("La capacidad inicial del grafo debe ser al menos 1.");

            _matriz = CrearMatrizVacia(capacidadInicial);
            _nombres = new string[capacidadInicial];
            _claves = new string[capacidadInicial];
            _esSede = new bool[capacidadInicial];
        }

        public int CantidadVertices => _cantidadVertices;
        public int CantidadAristas => _cantidadAristas;

        // Si el vértice ya existe devuelve su índice
        public int AgregarVertice(string nombre)
        {
            string clave = Normalizar(nombre);
            if (clave.Length == 0)
                throw new ArgumentException("El nombre de la ubicación no puede estar vacío.");

            string nombreLimpio = QuitarSufijoSede(nombre, out bool eraSede);

            int existente = BuscarClave(clave);
            if (existente != -1)
            {
                if (eraSede)
                    _esSede[existente] = true;
                return existente;
            }

            if (_cantidadVertices == _nombres.Length)
                Crecer();

            _nombres[_cantidadVertices] = nombreLimpio;
            _claves[_cantidadVertices] = clave;
            _esSede[_cantidadVertices] = eraSede;
            return _cantidadVertices++;
        }

        // Devuelve false si la ruta ya existía (se deja la de menor tiempo)
        public bool AgregarArista(string origen, string destino, int minutos)
        {
            if (minutos <= 0)
                throw new ArgumentException(
                    $"El tiempo de traslado debe ser mayor que cero (se recibió {minutos}).");

            if (Normalizar(origen) == Normalizar(destino))
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
            return BuscarClave(Normalizar(nombre));
        }

        public string ObtenerNombre(int indice)
        {
            VerificarIndice(indice);
            return _nombres[indice];
        }

        public bool EsSede(int indice)
        {
            VerificarIndice(indice);
            return _esSede[indice];
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

        // "  San José (Sede) " -> "san jose"
        public static string Normalizar(string nombre)
        {
            if (string.IsNullOrWhiteSpace(nombre))
                return "";

            string sinSufijo = QuitarSufijoSede(nombre, out _);

            // FormD separa la letra de la tilde ("é" -> "e" + "´") y así se puede descartar
            string descompuesto = sinSufijo.Normalize(NormalizationForm.FormD);

            char[] resultado = new char[descompuesto.Length];
            int largo = 0;
            bool espacioPendiente = false;

            foreach (char c in descompuesto)
            {
                if (CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.NonSpacingMark)
                    continue;

                if (char.IsWhiteSpace(c))
                {
                    espacioPendiente = largo > 0;
                    continue;
                }

                if (espacioPendiente)
                {
                    resultado[largo++] = ' ';
                    espacioPendiente = false;
                }
                resultado[largo++] = char.ToLowerInvariant(c);
            }

            return new string(resultado, 0, largo);
        }

        private static string QuitarSufijoSede(string nombre, out bool eraSede)
        {
            string recortado = nombre.Trim();
            eraSede = recortado.EndsWith(SufijoSede, StringComparison.OrdinalIgnoreCase);
            if (eraSede)
                recortado = recortado.Substring(0, recortado.Length - SufijoSede.Length).Trim();
            return recortado;
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
            bool[] nuevasSedes = new bool[nuevaCapacidad];
            for (int i = 0; i < _cantidadVertices; i++)
            {
                nuevosNombres[i] = _nombres[i];
                nuevasClaves[i] = _claves[i];
                nuevasSedes[i] = _esSede[i];
            }

            _matriz = nuevaMatriz;
            _nombres = nuevosNombres;
            _claves = nuevasClaves;
            _esSede = nuevasSedes;
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
