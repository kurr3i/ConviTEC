namespace ConviTEC.Estructuras
{
    // Utilidades mínimas sobre arreglos nativos mientras llega ListaDinamica<T>.
    // Cuando esa estructura exista, se reemplazan las llamadas a esta clase por ella.
    public static class Arreglos
    {
        // Agrega al final; si el arreglo está lleno lo duplica
        public static void Agregar<T>(ref T[] arreglo, ref int cantidad, T elemento)
        {
            if (cantidad == arreglo.Length)
                Crecer(ref arreglo);
            arreglo[cantidad++] = elemento;
        }

        // Arreglo nuevo con solo los primeros "cantidad" elementos
        public static T[] Copiar<T>(T[] arreglo, int cantidad)
        {
            T[] copia = new T[cantidad];
            for (int i = 0; i < cantidad; i++)
                copia[i] = arreglo[i];
            return copia;
        }

        private static void Crecer<T>(ref T[] arreglo)
        {
            T[] nuevo = new T[arreglo.Length == 0 ? 4 : arreglo.Length * 2];
            for (int i = 0; i < arreglo.Length; i++)
                nuevo[i] = arreglo[i];
            arreglo = nuevo;
        }
    }
}