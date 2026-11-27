namespace ConviTEC.Estructuras
{

    // Hay dos tipos genericos: TClave es con lo qué se busca y TValor es lo que se guarda.

    // NOTA: Con where TClave : IComparable<TClave> obligamos a que la clave se pueda
    // comparar (menor, igual o mayor), porque el árbol necesita eso para ordenarse.
    // IComparable<TClave> es el contrato, una interfaz de C#, y where es lo que obliga a TClave a cumplir ese contrato.

    public class ArbolAVL<TClave, TValor> where TClave : IComparable<TClave>
    {
        private class Nodo // Clase interna que representa un nodo del árbol.
        {
            public TClave Clave;
            public TValor Valor;
            public Nodo? Izquierdo; // ? significa que puede ser null.
            public Nodo? Derecho;
            public int Altura; // Cuántos niveles hay desde este nodo hasta su hoja más lejana

            //  CONSTRUCTOR DEL NODO
            public Nodo(TClave clave, TValor valor)
            {
                Clave = clave;
                Valor = valor;
                Altura = AlturaPorDefecto;
            }
        }

        // CAMPOS DEL ÁRBOL
        private const int AlturaPorDefecto = 1; // Altura con la que nace un nodo nuevo (siempre entra como hoja).
        private Nodo? _raiz; // Puede ser null si el árbol está vacío.
        private int _cantidad; // Cuantos elementos hay.

        // PROPIEDADES DEL ÁRBOL
        public int Cantidad => _cantidad; // => funciona como get, es decir solo se puede leer.
        public bool EstaVacia => _cantidad == 0;
        public int Altura => AlturaDe(_raiz);

        // Verifica que la clave no sea null, ya que null no se puede comparar.
        private static void VerificarClave(TClave clave)
        {
            if (clave is null)
                throw new ArgumentNullException(nameof(clave), "La clave no puede ser null.");
        }

        // Busca un nodo por su clave, devuelve null si no lo encuentra.
        private Nodo? BuscarNodo(TClave clave)
        {
            Nodo? actual = _raiz; // Empieza desde la raíz
            while (actual != null)
            {
                int comparacion = clave.CompareTo(actual.Clave); // negativo la clave es menor, positivo la clave es mayor, 0 las claves son iguales.
                if (comparacion == 0) 
                    return actual; // Se devuelve el nodo si se encontró la clave.
                actual = comparacion < 0 ? actual.Izquierdo : actual.Derecho; // If corto: si la clave es menor, actual pasa a ser el  hijo izquierdo, sino pasa a ser el derecho.
            }
            return null; // Si no se encontró la clave, devuelve null.
        }

        // Verifica si el árbol contiene un nodo con la clave espceificada. Devuelve true si lo encuentra, false si no.
        public bool Contiene(TClave clave)
        {
            VerificarClave(clave); // Verifica que la clave no sea null.
            return BuscarNodo(clave) != null; // Se devuelve true si se encontró el nodo, false si no.
        }

        // Intenta obtener el valor asociado a una clave, devuelve true si lo encuentra y false si no.
        public bool IntentarObtener(TClave clave, out TValor valor)
        {
            VerificarClave(clave); // La clave no debe ser null.

            Nodo? nodo = BuscarNodo(clave); // Busca el nodo con la clave especificada.
            if (nodo == null)
            {
                valor = default!; // Se debe rellenar el valor, ya que es out.
                return false; // No se encontró el nodo, se devuelve false. 
            }

            valor = nodo.Valor; // Se encontró el nodo, se devuelve el valor asociado a la clave.
            return true; // Se encontró el nodo, se devuelve true.
        }

        // Devuelve el valor asociado a la clave especificada. Arroja una excepción si no se encuntra la clave.
        public TValor Obtener(TClave clave)
        {
            if (!IntentarObtener(clave, out TValor valor)) // Se intena obtener el valor asociado a la clave. ! invierte el resultado.
                throw new KeyNotFoundException($"No existe ningún elemento con la clave '{clave}'."); // Si no se encuentra la clave se lanza la excepción.
           
            return valor; // Se devuelve el valor asociado a la clave si se encontró.
        }

        // Actualiza el valor asociado a una clave, devuelve true si lo encuentra y actualiza, false si no.
        public bool Actualizar(TClave clave, TValor valor)
        {
            VerificarClave(clave); // clave no debe ser null


            Nodo? nodo = BuscarNodo(clave); // Busca el nodo con la clave especificada.
            if (nodo == null) // Si no se encuentra el nodo, no se actualiza y devuelve false.
                return false;

            nodo.Valor = valor; // Si se encuentra el nodo, se actualiza el valor asociado a la clave y se devuelve true.
            return true;
        }

        // Inserta un nuevo nodo con la clave y valor especificados. Devuelve true si se insertó, false si ya existía la clave.
        public bool Insertar(TClave clave, TValor valor)
        {
            VerificarClave(clave); // Que la clave no sea null

            if (Contiene(clave)) // Que no se repita la clave, si ya existe se devuelve false (no se insertó).
                return false;

            _raiz = Insertar(_raiz, clave, valor); // Se inserta el nodo al árbol con el metodo insertar privado (recursivo) y se actualiza la raíz del árbol.
            _cantidad++; 
            return true; // Se insertó el nodo, se devuelve true.
        }

        // Método recursivo para insertar un nodo. Devuelve la cima del subárbol recibido (puede cambiar por una rotación).
        private Nodo Insertar(Nodo? nodo, TClave clave, TValor valor)
        {
            if (nodo == null) // Caso base: si el nodo es null, se llegó al espacio vacio donde se se crea el nuevo nodo.
                return new Nodo(clave, valor); // Se crea el nodo y se devuelve como nuevo nodo raíz del subárbol.

            if (clave.CompareTo(nodo.Clave) < 0) // Caso recursivo: Se compara la clave con la del nodo actual. 
                nodo.Izquierdo = Insertar(nodo.Izquierdo, clave, valor); // Si la clave es menor, se hace la llamada recursiva al hijo izquierdo.
            else
                nodo.Derecho = Insertar(nodo.Derecho, clave, valor); // Si la clave es mayor, se hace la llamada recursiva al hijo derecho.

            return Balancear(nodo); // Se devuelve el nodo revisado y rotado si hace falta.
        }
        // NOTA: Se llama recursivamente apilando llamadas hasta el caso base, donde se crea el nodo nuevo. Al volver, cada llamada engancha a su hijo,
        // balancea su subárbol y devuelve la cima de ese subárbol a la llamada de arriba. Así hasta la primera, que devuelve la raíz (la nueva, si cambió por una rotación),
        // y se guarda en _raiz.

        // Balancea un nodo y devuelve la cima del subárbol balanceado.
        private Nodo Balancear(Nodo nodo)
        {
            ActualizarAltura(nodo); // Primero actualiza la altura, porque un hijo pudo haber cambiado
            int factor = FactorEquilibrio(nodo); // Calcula el factor

            if (factor > 1) // Si el factor es mayor a 1, el subárbol está desbalanceado a la izquierda.
            {
                if (FactorEquilibrio(nodo.Izquierdo!) < 0) // Si el factor del hijo izquierdo es negativo (Zigzag)
                    nodo.Izquierdo = RotarIzquierda(nodo.Izquierdo!); // Se hace una rotación a la izquierda del hijo izquierdo para la forma zigzag: lo convierte en lineal.

                return RotarDerecha(nodo); // Se hace una rotación a la derecha del nodo en la forma lineal y en zigzag.
            }

            if (factor < -1) // Si el factor es menor a -1, el subárbol está desbalanceado a la derecha.
            {
                if (FactorEquilibrio(nodo.Derecho!) > 0) // Si el factor del hijo derecho es positivo (Zigzag)
                    nodo.Derecho = RotarDerecha(nodo.Derecho!); // Se hace una rotación a la derecha del hijo derecho para la forma zigzag: lo convierte en lineal.

                return RotarIzquierda(nodo); // Se hace una rotación a la izquierda del nodo en la forma lineal y en zigzag.
            }

            return nodo; // Se devuelve el nodo cima del subárbol balanceado, factor entre 1 y -1: ya está balanceado.
        }

        // Devuelve la altura de un nodo, o 0 si es null.static significa que el método no usa nada del árbol.
        private static int AlturaDe(Nodo? nodo)
        {
            return nodo == null ? 0 : nodo.Altura; // Si el nodo es null, devuelve 0, sino devuelve la altura del nodo.
        }

        // Actualiza la altura de un nodo según la altura de sus hijos. static significa que el método no usa nada del árbol.
        private static void ActualizarAltura(Nodo nodo)
        {
            int izquierda = AlturaDe(nodo.Izquierdo); // Se obtiene la altura del hijo izquierdo.
            int derecha = AlturaDe(nodo.Derecho); // Se obtiene la altura del hijo derecho.
            nodo.Altura = 1 + (izquierda > derecha ? izquierda : derecha); // La altura del nodo es 1 más la altura del hijo con mayor altura.
        }

        // Devuelve el factor de equilibrio de un nodo (altura del hijo izquierdo - altura del hijo derecho). static significa que el método no usa nada del árbol.
        private static int FactorEquilibrio(Nodo nodo)
        {
            return AlturaDe(nodo.Izquierdo) - AlturaDe(nodo.Derecho);
        }

        // Realiza una rotación a la derecha y devuelve la nueva cima del subárbol.
        private Nodo RotarDerecha(Nodo y)
        {
            Nodo x = y.Izquierdo!; // x es el que va a subir. ! es por que se sabe que y.Izquierdo no es null.
            Nodo? b = x.Derecho;  // guardas B antes de perderlo

            x.Derecho = y;  // y baja y queda a la derecha de x
            y.Izquierdo = b;  // B pasa a ser el hijo izquierdo de y. Cabe ahí porque es mayor que x y menor que y.

            ActualizarAltura(y); // primero y, porque ahora está abajo
            ActualizarAltura(x); // después x, que depende de la altura de y
            return x;   // x es la nueva cima de esta rama
        }

        // Realiza una rotación a la izquierda y devuelve la nueva cima del subárbol.
        private Nodo RotarIzquierda(Nodo x)
        {
            Nodo y = x.Derecho!; // y es el que va a subir. ! es porque se sabe que x.Derecho no es null.
            Nodo? b = y.Izquierdo; // guardas B antes de perderlo

            y.Izquierdo = x; // x baja y queda a la izquierda de y
            x.Derecho = b;  // B pasa a ser el hijo derecho de x. Cabe ahí porque es menor que y y mayor que x.

            ActualizarAltura(x); // primero x, porque ahora está abajo
            ActualizarAltura(y); // después y, que depende de la altura de x
            return y; // y es la nueva cima de esta rama
        }

        // Elimina un nodo con la clave especificada. Devuelve true si se eliminó, false si no se encontró la clave.
        public bool Eliminar(TClave clave)
        {
            VerificarClave(clave); 

            if (!Contiene(clave)) // Si no se encuentra la clave, devuelve false.
                return false;

            _raiz = Eliminar(_raiz, clave); // Llama al método recursivo para eliminar el nodo y actualiza la raíz del árbol.
            _cantidad--;
            return true;
        }

        // Método recursivo para eliminar un nodo. Devuelve la cima del subárbol recibido (puede cambiar por una rotación).
        private Nodo? Eliminar(Nodo? nodo, TClave clave)
        {
            if (nodo == null) // Espacio vacío: la clave no está en esta rama (no debería pasar, el público ya revisó).
                return null;

            int comparacion = clave.CompareTo(nodo.Clave);

            if (comparacion < 0)
                nodo.Izquierdo = Eliminar(nodo.Izquierdo, clave); // Si es menor se le pide al hijo izquierdo que la elimine, y lo que devuelva se guarda como nuevo hijo izquierdo.

            else if (comparacion > 0)
                nodo.Derecho = Eliminar(nodo.Derecho, clave); // Si es mayor se le pide al hijo derecho que la elimine, y lo que devuelva se guarda como nuevo hijo derecho.

            else // Si es igual, se encontró el nodo a eliminar y hay tres casos: No tiene hijos, tiene un hijo o tiene dos hijos.
            {
                if (nodo.Izquierdo == null) // Si no tiene hijo izquierdo, se devuelve el hijo derecho (que puede ser null o un nodo).
                    return nodo.Derecho;

                if (nodo.Derecho == null) // Si no tiene hijo derecho, se devuelve el hijo izquierdo (que puede ser null o un nodo).
                    return nodo.Izquierdo;

                // Los dos primeros casos se resuelven con los if anteriores. El tercer caso se soluciona a continuación.
                Nodo sucesor = Minimo(nodo.Derecho); // Se busca el nodo con la clave mínima en el subárbol derecho, que es el sucesor del nodo a eliminar.
                nodo.Clave = sucesor.Clave; // Se copia la clave del sucesor al nodo a eliminar.
                nodo.Valor = sucesor.Valor; // Se copia el valor del sucesor al nodo a eliminar.
                nodo.Derecho = Eliminar(nodo.Derecho, sucesor.Clave); // El sucesor queda repetido, entonces se elimina el sucesor del subárbol derecho.
            }

            return Balancear(nodo); // Se devuelve el nodo revisado y rotado si hace falta, para mantener el árbol balanceado.
        }

        // Devuelve el nodo con la clave mínima en el subárbol dado. Se usa para encontrar el sucesor en la eliminación.
        private static Nodo Minimo(Nodo nodo)
        {
            while (nodo.Izquierdo != null)
                nodo = nodo.Izquierdo; // Se sigue bajando por la izquierda hasta encontrar el nodo más a la izquierda, que es el mínimo.
            return nodo;
        }

        // Devuelve un arreglo con los valores del árbol en orden ascendente de claves. Inorden.
        public TValor[] ValoresEnOrden()
        {
            TValor[] resultado = new TValor[_cantidad]; // Arreglo vacio con la cantidad de elementos del árbol.
            LlenarValores(_raiz, resultado, 0); // Llama al método recursivo para llenar el arreglo con los valores del árbol en orden.
            return resultado; 
        }

        // Método recursivo para llenar un arreglo con los valores del árbol en orden. Devuelve la posición siguiente a la última llenada.
        private static int LlenarValores(Nodo? nodo, TValor[] destino, int posicion)
        {
            if (nodo == null) // Caso base: si el nodo es null, se devuelve la posición actual, que no se llenó.
                return posicion;

            posicion = LlenarValores(nodo.Izquierdo, destino, posicion); // 1. Escribe todos los menores a la izquierda, devuelve dónde quedó y lo guarda en posición.
            destino[posicion] = nodo.Valor; // 2. Escribe su valor en la siguiente posición libre.
            posicion++; // Avanza una posición
            return LlenarValores(nodo.Derecho, destino, posicion); // 3. Escribe todos los mayores y devuelve dónde quedó al final.
        }

        // Devuelve un arreglo con las claves del árbol en orden ascendente. Inorden.
        public TClave[] ClavesEnOrden()
        {
            TClave[] resultado = new TClave[_cantidad]; // Arreglo vacio con la cantidad de elementos del árbol.
            LlenarClaves(_raiz, resultado, 0); // Llama al método recursivo para llenar el arreglo con las claves del árbol en orden.
            return resultado;
        }

        // Método recursivo para llenar un arreglo con las claves del árbol en orden. Devuelve la posición siguiente a la última llenada.
        private static int LlenarClaves(Nodo? nodo, TClave[] destino, int posicion)
        {
            if (nodo == null) // Caso base: si el nodo es null, se devuelve la posición actual, que no se llenó.
                return posicion;

            posicion = LlenarClaves(nodo.Izquierdo, destino, posicion); // 1. Escribe todos las menores a la izquierda, devuelve dónde quedó y lo guarda en posición.
            destino[posicion] = nodo.Clave; // 2. Escribe la clave en la siguiente posición libre.
            posicion++; // Avanza una posición
            return LlenarClaves(nodo.Derecho, destino, posicion); // 3. Escribe todas los mayores y devuelve dónde quedó al final.
        }

        public void Limpiar()
        {
            _raiz = null; // Suelta el árbol. Garbage colector elimina lo que ya no se puede alcanzar
            _cantidad = 0; 
        }
    }
}