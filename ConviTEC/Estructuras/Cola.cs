namespace ConviTEC.Estructuras
{
    public class Cola<T>
    {
        private const int CapacidadPorDefecto = 8;

        // CAMPOS
        private T[] _elementos; // El arreglo
        private int _inicio; // La posición del frente
        private int _cantidad; // Cuantos elementos hay

        // CONSTRUCTOR
        public Cola(int capacidadInicial = CapacidadPorDefecto)
        {
            if (capacidadInicial < 1)
                throw new ArgumentException("La capacidad inicial de la cola debe ser al menos 1.");

            _elementos = new T[capacidadInicial];
            _inicio = 0;
            _cantidad = 0;
        }

        // PROPIEDADES
        public int Cantidad => _cantidad; // => funciona como get
        public bool EstaVacia => _cantidad == 0;

        // Método para crecer el arreglo cuando se llena
        private void Crecer()
        {
            T[] nuevo = new T[_elementos.Length * 2]; // Se crea una arreglo con el doble de capacidad que el anterior

            for (int k = 0; k < _cantidad; k++)
                nuevo[k] = _elementos[(_inicio + k) % _elementos.Length];
            // NOTA: Se copian los elementos de la cola siguiendo el orden de la cola original
            // empleando [(_inicio + k) % _elementos.Length]
            // Por ejemplo:  para [E | F | C | D] el orden se saca con k = 0 → posición (2+0) % 4 = 2 → C
            // y continúa.

            _elementos = nuevo; // Elementos pasa a ser el arreglo nuevo.
            _inicio = 0; // Y el inicio pasa a ser 0 de nuevo
        }

        // Método para agregar un elemento a la cola
        public void Encolar(T elemento)
        {
            if (_cantidad == _elementos.Length) // Si está llena crece
                Crecer(); 

            int fin = (_inicio + _cantidad) % _elementos.Length; // Se cálcula la primera posición libre detrás del último
            _elementos[fin] = elemento; // Se guarda el elemento.
            _cantidad++;

            // NOTA: Si _inicio = 2 y hay 2 elementos (posiciones 2 y 3),
            // el siguiente va en (2 + 2) % 4 = 0: dio la vuelta.
        }

        // Método para quitar un elemento de la cola
        public T Desencolar()
        {
            if (EstaVacia) // Si está vacía está no se puede desencolar
                throw new InvalidOperationException("La cola está vacía.");

            T frente = _elementos[_inicio]; // Guarda el elemento del frente en una variable para no perderlo.
            _elementos[_inicio] = default!; // Limpia esa posición con default. ! se espera un null.
            _inicio = (_inicio + 1) % _elementos.Length; // Avanza la posición en uno. Con % si estaba en la última pasa a 0.
            _cantidad--;
            return frente; // Se devuelve el elemento.
        }

        // Devuelve el elemento del frente sin sacarlo.
        public T VerFrente()
        {
            if (EstaVacia)
                throw new InvalidOperationException("La cola está vacía.");

            return _elementos[_inicio];
        }

        // Limpia la cola completa
        public void Limpiar()
        {
            for (int k = 0; k < _cantidad; k++) // Emplea la misma lógica del método crecer 
                _elementos[(_inicio + k) % _elementos.Length] = default!; // Pone en default las posiciones usadas. 
            // NOTA: De este modo se reduce la complejidad del método, ya que se recorren solo las posiciones empleadas
            // y no todas las posiciones del arreglo.

            _inicio = 0; // Se reinicia el inicio y la cantidad.
            _cantidad = 0;
        }

        // Devuelve una copia desde el frente hasta el final
        public T[] ComoArreglo()
        {
            T[] copia = new T[_cantidad]; // Se crea una copia del tamaño de la cantidad de elementos.

            for (int k = 0; k < _cantidad; k++) // Se emplea la misma lógica de crecer para visitar los elementos en el orden de la cola.
                copia[k] = _elementos[(_inicio + k) % _elementos.Length]; // Se copian los elementos, no espacios vacios.

            return copia;
        }
    }
}