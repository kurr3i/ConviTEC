namespace ConviTEC.Estructuras
{
    // Min-heap sobre arreglos: sale primero el de menor prioridad.
    // Hijos de i: 2i+1 y 2i+2. Padre de i: (i-1)/2.
    public class ColaPrioridad<T>
    {
        private const int CapacidadPorDefecto = 8;

        private T[] _elementos;
        private int[] _prioridades;
        private int _cantidad;

        public ColaPrioridad(int capacidadInicial = CapacidadPorDefecto)
        {
            if (capacidadInicial < 1)
                throw new ArgumentException("La capacidad inicial de la cola debe ser al menos 1.");

            _elementos = new T[capacidadInicial];
            _prioridades = new int[capacidadInicial];
        }

        public int Cantidad => _cantidad;
        public bool EstaVacia => _cantidad == 0;

        public void Encolar(T elemento, int prioridad)
        {
            if (_cantidad == _elementos.Length)
                Crecer();

            _elementos[_cantidad] = elemento;
            _prioridades[_cantidad] = prioridad;
            Subir(_cantidad);
            _cantidad++;
        }

        public T Desencolar()
        {
            return Desencolar(out _);
        }

        public T Desencolar(out int prioridad)
        {
            if (EstaVacia)
                throw new InvalidOperationException("La cola de prioridad está vacía.");

            T minimo = _elementos[0];
            prioridad = _prioridades[0];

            // El último pasa a la raíz y se baja hasta su lugar
            _cantidad--;
            _elementos[0] = _elementos[_cantidad];
            _prioridades[0] = _prioridades[_cantidad];
            _elementos[_cantidad] = default!;
            Bajar(0);

            return minimo;
        }

        public int VerPrioridadMinima()
        {
            if (EstaVacia)
                throw new InvalidOperationException("La cola de prioridad está vacía.");
            return _prioridades[0];
        }

        private void Subir(int i)
        {
            while (i > 0)
            {
                int padre = (i - 1) / 2;
                if (_prioridades[padre] <= _prioridades[i])
                    break;
                Intercambiar(i, padre);
                i = padre;
            }
        }

        private void Bajar(int i)
        {
            while (true)
            {
                int izquierdo = 2 * i + 1;
                int derecho = 2 * i + 2;
                int menor = i;

                if (izquierdo < _cantidad && _prioridades[izquierdo] < _prioridades[menor])
                    menor = izquierdo;
                if (derecho < _cantidad && _prioridades[derecho] < _prioridades[menor])
                    menor = derecho;

                if (menor == i)
                    break;
                Intercambiar(i, menor);
                i = menor;
            }
        }

        private void Intercambiar(int a, int b)
        {
            (_elementos[a], _elementos[b]) = (_elementos[b], _elementos[a]);
            (_prioridades[a], _prioridades[b]) = (_prioridades[b], _prioridades[a]);
        }

        private void Crecer()
        {
            int nuevaCapacidad = _elementos.Length * 2;
            T[] nuevosElementos = new T[nuevaCapacidad];
            int[] nuevasPrioridades = new int[nuevaCapacidad];

            for (int i = 0; i < _cantidad; i++)
            {
                nuevosElementos[i] = _elementos[i];
                nuevasPrioridades[i] = _prioridades[i];
            }

            _elementos = nuevosElementos;
            _prioridades = nuevasPrioridades;
        }
    }
}
