namespace ConviTEC.Estructuras
{
    // Lista sobre un arreglo que duplica su capacidad cuando se llena.
    public class ListaDinamica<T>  // <T> Significa que la clase es génerica, contiene cualquier tipo de dato.
    {
        // CAMPOS

        private const int CapacidadPorDefecto = 8; // La capacidad del la lista por defecto. Const es un valor fijo que no cambia.

        private T[] _elementos;   // Arreglo donde se guardan los datos.

        private int _cantidad;    // Cuántos elementos hay de verdad. Lo válido es de 0 a _cantidad-1.


        // CONSTRUCTOR

        public ListaDinamica(int capacidadInicial = CapacidadPorDefecto) // Parametro opcional, si no se pasa ninguno, vale 8
        {
            if (capacidadInicial < 1) // Si la capacidad es de 0 o negatica, Crecer () no funcionaría correctamente.
                throw new ArgumentException("La capacidad inicial de la lista debe ser al menos 1.");

            _elementos = new T[capacidadInicial];
            _cantidad = 0;
        }

        // PROPIEDADES

        public int Cantidad => _cantidad; // => funciona como get, es decir solo se puede leer.

        public bool EstaVacia => _cantidad == 0;

        // Agrega al final de la lista
        public void Agregar(T elemento)
        {
            if (_cantidad == _elementos.Length) // Si el arreglo está lleno, primero crece y después guarda
                Crecer(); 

            _elementos[_cantidad] = elemento; // Cantidad siempre apunta a la primera posicion libre del arreglo
            _cantidad++; 
        }

        // Crea un arreglo nuevo con el doble de capacidad y copia los elementos del anterior arreglo.
        private void Crecer()
        {
            T[] nuevo = new T[_elementos.Length * 2]; // Se crea el nuevo arreglo (doble capacidad).
            for (int i = 0; i < _cantidad; i++) // Se copian los elementos del arreglo viejo al nuevo.
                nuevo[i] = _elementos[i];
            _elementos = nuevo;
        }
        // NOTA: Buscamos duplicarlo para reducir la complejidad, 
        // ya que si solo aumentamos de a 1, se crea un arreglo cada que se agrega un elemento.
        // O(n) cada que se copia el arreglo, y O(1) si las copias no son tan seguidas.


        // Verificar que el indice sea un dato y no esté vacío.
        private void VerificarIndice(int indice)
        {
            if (indice < 0 || indice >= _cantidad) // Debe estar en el rango de 0 a _cantidad-1.
                throw new ArgumentOutOfRangeException(nameof(indice), $"El índice {indice} no es válido (la lista tiene {_cantidad} elementos)."); // nameof(indice) convierte el indice a texto.
        }

        // Obtener un elemento según el indice.
        public T Obtener(int indice)
        {
            VerificarIndice(indice); // Verifica que el indice sea válido.
            return _elementos[indice]; // Devuelve el elemento en la posición indicada.
        }

        // Remplazar un elemento según el indice.
        public void Reemplazar(int indice, T elemento)
        {
            VerificarIndice(indice);
            _elementos[indice] = elemento;
        }

        // Elimina la posición indicada y corre a la izquierda los que estaban después.
        public void EliminarEn(int indice)
        {
            VerificarIndice(indice); // Verifica que el indice sea válido.

            for (int i = indice; i < _cantidad - 1; i++) // Se mueven los elementos a la izquierda, sobreescribiendo el que se elimina.
                _elementos[i] = _elementos[i + 1];

            _cantidad--; 
            _elementos[_cantidad] = default!; // Suelta la referencia para que el objeto se pueda liberar
        }

        // Índice del primer elemento que cumple la condición, o -1 si ninguno la cumple
        public int BuscarIndice(Predicate<T> condicion) // Predicate<T> es una función que recibe un T elemento y true o false.
        {
            if (condicion == null)
                throw new ArgumentNullException(nameof(condicion)); // La condición no debe ser null.

            for (int i = 0; i < _cantidad; i++) // Se recorre el arreglo.
            {
                if (condicion(_elementos[i]))
                    return i; // Si el elemento cumple la condición, devuelve el índice.
            }
            return -1; // Si no se encpntró un elemento que cumpla la condición devuelve -1.
        }

        // NOTA: Predicate<T> es una función de lenguaje.
        // debe darse como una expresión de tres partes: p => p.Id == "P-001".
        // p es el parámetro, => es la flecha que indica que lo que sigue es el cuerpo de la función
        // y p.Id == "P-001" es la condición que se evalúa para cada elemento de la lista.
        // Hay distintas maneras de escribir la condición, no es la única forma.


        // Elimina el primer elemento que cumpla la condición y devuelve true si se eliminó y false si no se eliminó ninguno.
        public bool EliminarPrimero(Predicate<T> condicion)
        {
            int indice = BuscarIndice(condicion); // Busca el elemento que cumpla la condición.
            if (indice == -1)
                return false; // Si no se encuentra ninguno, devuelve false.

            EliminarEn(indice); // Si se encuentra, lo elimina y devuelve true para indicar que el elemento se eliminó.
            return true;
        }

        // Limpia el arreglo, conservando la capacidad.
        public void Limpiar()
        {
            for (int i = 0; i < _cantidad; i++)
                _elementos[i] = default!; // default cada posición usada.
            _cantidad = 0;
        }

        // Copia solo los elementos válidos (sin el espacio libre). Sirve para JSON y para ordenar.
        public T[] ComoArreglo()
        {
            T[] copia = new T[_cantidad]; // Crea un arreglo con la cantidad de elementos válidos.
            for (int i = 0; i < _cantidad; i++) // Copia los elementos en el nuevo arreglo.
                copia[i] = _elementos[i];
            return copia;
        }
    }
}