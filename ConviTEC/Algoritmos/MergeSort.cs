namespace ConviTEC.Algoritmos
{
    public static class MergeSort
    {
        // Ordena un arreglo dado, según un metodo de comparasión, no devuelve nada ya que trabaja sobre el arreglo dado.
        public static void Ordenar<T>(T[] arreglo, Comparison<T> comparar)
        {
            if (arreglo == null) // Si el arreglo es nulo lanza una excepción
                throw new ArgumentNullException(nameof(arreglo));
            if (comparar == null) // Si el argumento de comparación es nulo lanza una excepción
                throw new ArgumentNullException(nameof(comparar));

            if (arreglo.Length < 2) // Si el arreglo solo tiene un elemento o está vacío, termina la ejecución
                return;

            T[] auxiliar = new T[arreglo.Length]; // Se crea un arreglo auxiliar que se reutiliza en todas las mezclas.
            Dividir(arreglo, auxiliar, 0, arreglo.Length - 1, comparar); // Llama al método recursivo con todo el rango: desde la posición 0 hasta la última.
        }
        // NOTA: llamar al metodo se vería algo así: MergeSort.Ordenar(pedidos, (a, b) => a.Rendimiento.CompareTo(b.Rendimiento));
        // Se le da el arreglo a ordenar y el criterio de comparación entre elementos.


        // Metodo que emplea la lógica de dividir: ordena solo el pedazo del arreglo que va de izquierda a derecha (incluyendo ambos). Se trabaja sobre el mismo arreglo marcando con indices, no se crean nuevos entre mitades. 
        private static void Dividir<T>(T[] arreglo, T[] auxiliar, int izquierda, int derecha, Comparison<T> comparar)
        {
            if (izquierda >= derecha) // El pedazo tiene un elemento o ninguno. Ya está ordenado.
                return;

            int medio = izquierda + (derecha - izquierda) / 2; // Se cálcula la mitad evitando el desbordamiento, es lo mismo que (izquierda + derecha) / 2

            Dividir(arreglo, auxiliar, izquierda, medio, comparar); // Llamada recursiva para ordenar la mitad izquierda
            Dividir(arreglo, auxiliar, medio + 1, derecha, comparar); // Llamada recursiva para ordenar la mitad derecha
            Mezclar(arreglo, auxiliar, izquierda, medio, derecha, comparar); // junta ambas
        }

        // Metodo que mezcla dos mitades de un arreglo ya ordenadas
        private static void Mezclar<T>(T[] arreglo, T[] auxiliar, int izquierda, int medio, int derecha, Comparison<T> comparar)
        {
            for (int k = izquierda; k <= derecha; k++) // Se copia el pedazo al auxiliar
                auxiliar[k] = arreglo[k]; 

            int i = izquierda;  // Recorre la mitad izquierda (en el auxiliar)
            int j = medio + 1; // Recorre la mitad derecha (en el auxiliar)
            int posicion = izquierda; // Dónde se escribe en el arreglo original

            while (i <= medio && j <= derecha) // Ambas mitades deben tener elementos
            {
                if (comparar(auxiliar[i], auxiliar[j]) <= 0) // Si el de la izquierda va primero o empatan. Con <= gana la izquierda en empates: eso hace que sea estable.
                {
                    arreglo[posicion] = auxiliar[i]; // Escribe el elemento de la izquierda.
                    i++; // Avanza al siguiente elemento de la izquierda.
                }
                else
                {
                    arreglo[posicion] = auxiliar[j]; // Sino el de la derecha va primero.
                    j++;
                }
                posicion++; // Avanza a la siguiente casilla libre
            }

            while (i <= medio) // Mientras queden elementos en la izquierda se copian uno por uno al arreglo
            {
                arreglo[posicion] = auxiliar[i];
                i++;
                posicion++;
            }

            while (j <= derecha) // Mientras queden elementos en la derecha se copian uno por uno al arreglo
            {
                arreglo[posicion] = auxiliar[j];
                j++;
                posicion++;
            }
        }
    }
}