namespace ConviTEC.Estructuras
{
    public class TablaHash<TClave, TValor> where TClave : notnull // La clave no puede ser null
    {
        private class Nodo
        {
            // CAMPOS DE LA CLASE NODO
            public TClave Clave;
            public TValor Valor;
            public Nodo? Siguiente; // Apunta al siguiente nodo, puede ser null si ya no hay siguiente nodo.

            // CONSTRUCTOR DE LA CLASE NODO
            public Nodo(TClave clave, TValor valor, Nodo? siguiente)
            {
                Clave = clave;
                Valor = valor;
                Siguiente = siguiente;
            }
        }

        // CONSTANTES DE LA TABLAHASH
        private const int CapacidadPorDefecto = 16;
        private const double FactorCargaMaximo = 0.75;

        // CAMPOS DE LA TABLAHASH
        private Nodo?[] _cubetas;
        private int _cantidad;

        // CONSTRUCTOR DE LA TABLAHASH
        public TablaHash(int capacidadInicial = CapacidadPorDefecto)
        {
            if (capacidadInicial < 1) // La capacidad debe ser de almenos 1
                throw new ArgumentException("La capacidad inicial de la tabla debe ser al menos 1.");

            _cubetas = new Nodo?[capacidadInicial];
            _cantidad = 0;
        }

        // PROPIEDADES DE LA TABLAHAS (SOLO LECTURA)
        public int Cantidad => _cantidad;
        public bool EstaVacia => _cantidad == 0;

        // Convierte la clave en el índice de su cubeta: calcula el hash, le quita el signo y con % lo deja entre 0 y capacidad - 1.
        private static int IndiceDe(TClave clave, int capacidad)
        {
            int hash = clave.GetHashCode() & 0x7FFFFFFF;
            // NOTA: GetHashCode() devuelve un int calculado a partir de su contenido. Es un método del lenguaje.
            // GetHashCode puede devolver números negativos, y un índice negativo haría fallar el arreglo.
            // 0x7FFFFFFF es el número más grande que cabe en un int positivo, escrito en hexadecimal.
            // El & apaga el bit de signo, así que el resultado siempre es positivo o cero.

            return hash % capacidad; // Convierte el hash en un número entre hash y la capacidad - 1.
        }

        // Busca un nodo por su clave a partir de la cubeta en la que está
        private Nodo? BuscarNodo(TClave clave)
        {
            int indice = IndiceDe(clave, _cubetas.Length); // Cálcula la cubeta en la que debería estar el nodo
            
            for (Nodo? nodo = _cubetas[indice]; nodo != null; nodo = nodo.Siguiente) // Empieza en la cabeza de la cadena, sigue mientras el nodo no sea null, y pasa al siguiente nodo
            {
                if (nodo.Clave.Equals(clave)) // Si la clave que se busca es igual a la clave del nodo
                    return nodo; // Se devuelve el nodo
            }
            return null; // Sino se devuelve null
        }

        // Aumenta la capacidad de las _cubetas, copiando los elementos según su indice nuevo.
        private void Redimensionar()
        {
            Nodo?[] nuevas = new Nodo?[_cubetas.Length * 2]; // Se aumenta el doble la capacidad del arreglo (cubetas)

            for (int i = 0; i < _cubetas.Length; i++) // Se copian las cubetas
            {
                Nodo? nodo = _cubetas[i]; // Empieza en la cabeza de la cadena de la cubeta i vieja

                while (nodo != null) // Se repite mientras queden nodos en esa cadena
                {
                    Nodo? siguiente = nodo.Siguiente; // Guarda el siguiente de la cadena vieja.

                    int indice = IndiceDe(nodo.Clave, nuevas.Length); // Cálcula la cubeta del nodo en la tabla nueva
                    
                    // Inserta el nodo al inicio de su cubeta nueva
                    nodo.Siguiente = nuevas[indice]; // El nodo apunta a lo que ya había en esa cubeta nueva
                    nuevas[indice] = nodo; // La cubeta nueva pasa a apuntar al nodo.

                    nodo = siguiente; // Sigue con el nodo.
                }
            }
            // NOTA: No se puede copiar cubeta por cubeta ya que el indice depende de la capacidad
            // Al cambiar la cantidad de cubetas cada clave puede caer en otra cubeta.

            _cubetas = nuevas;
        }

        // Verifica que la clave no sea null
        private static void VerificarClave(TClave clave)
        {
            if (clave is null)
                throw new ArgumentNullException(nameof(clave), "La clave no puede ser null.");
        }

        // Inserta un nodo en el arreglo
        public bool Insertar(TClave clave, TValor valor)
        {
            VerificarClave(clave); // Se valida que la clave no sea null

            if (BuscarNodo(clave) != null) // Se valida que no exista otro nodo con dicha clave
                return false; // Devuelve false pues no se insertó el elemento

            if (_cantidad + 1 > _cubetas.Length * FactorCargaMaximo)  // Si se agrega uno más, el factor superaría 0,75 se redimenciona primero
                Redimensionar();

            int indice = IndiceDe(clave, _cubetas.Length); // Calcula el indice de la clave

            _cubetas[indice] = new Nodo(clave, valor, _cubetas[indice]); // Inserta el nodo al inicio de la cadena

            _cantidad++;

            return true; // Devuelve true pues se logró insertar el elemento
        }

        // Elimina un nodo del arreglo  y devuelve true o false si se elimino o no
        public bool Eliminar(TClave clave)
        {
            VerificarClave(clave); // Se verifica que no sea null

            int indice = IndiceDe(clave, _cubetas.Length); // Se cálcula el indice del nodo que se va a eliminar según la clave 

            Nodo? anterior = null; 

            Nodo? actual = _cubetas[indice];

            while (actual != null) 
            {
                if (actual.Clave.Equals(clave)) // Si se encuentra el nodo con la clave
                {
                    if (anterior == null) // Si es el primero de la cadena
                        _cubetas[indice] = actual.Siguiente; // La cubeta apunta al siguiente
                    else // Si es uno del medio o el del final
                        anterior.Siguiente = actual.Siguiente; // El anterior apunta ahora al siguiente del actual.

                    _cantidad--;
                    return true;
                }

                anterior = actual; // Si no se ha encontrado, se avanza el anterior

                actual = actual.Siguiente; // Y se avanza el actual
            }

            return false;
        }

        // Método para obtener todas las claves de la tabla. No sigue un orden especifico.
        public TClave[] Claves()
        {
            TClave[] resultado = new TClave[_cantidad];

            int posicion = 0;

            for (int i = 0; i < _cubetas.Length; i++)
            {
                for (Nodo? nodo = _cubetas[i]; nodo != null; nodo = nodo.Siguiente) // Empieza en la cabeza de la cadena, sigue mientras el nodo no sea null, y pasa al siguiente nodo
                {
                    resultado[posicion] = nodo.Clave; // Inserta la clave en la posición correspondiente
                    posicion++; // Y avanza de posición
                }
            }
            return resultado; // Devuelve el arreglo con las claves
        }

        // Método para obtener todos los valores de la tabla. No sigue un orden especifico.
        public TValor[] Valores()
        {
            TValor[] resultado = new TValor[_cantidad];

            int posicion = 0;

            for (int i = 0; i < _cubetas.Length; i++)
            {
                for (Nodo? nodo = _cubetas[i]; nodo != null; nodo = nodo.Siguiente) // Empieza en la cabeza de la cadena, sigue mientras el nodo no sea null, y pasa al siguiente nodo
                {
                    resultado[posicion] = nodo.Valor; // Inserta el valor en la posición correspondiente
                    posicion++; // Y avanza de posición
                }
            }
            return resultado; // Devuelve el arreglo con los valores
        }

        // Pone cada cubeta en null
        public void Limpiar()
        {
            for (int i = 0; i < _cubetas.Length; i++)
                _cubetas[i] = null;
            _cantidad = 0;
        }

        // Intenta obtener un valor según su clave
        public bool IntentarObtener(TClave clave, out TValor valor)
        {
            VerificarClave(clave); // Valida que la clave no sea null

            Nodo? nodo = BuscarNodo(clave); // Intenta buscar el nodo
             
            if (nodo == null) // Si no lo encuentra 
            {
                valor = default!; // En el out se debe asignar algo, entonces se asigna el valor default.
                return false; // Se devuelve false pues no se encontro
            }

            valor = nodo.Valor; // Si lo encuentra en el out se asigna el valor del nodo
            return true; // Y se devuelve true
        }

        // Se obtiene un valor según su clave
        public TValor Obtener(TClave clave)
        {
            if (!IntentarObtener(clave, out TValor valor)) // Si no se encuentra el elemento se lanza una excepción
                throw new KeyNotFoundException($"No existe ningún elemento con la clave '{clave}'.");
            return valor; // Si se encuentra se devuelve el valor
        }

        // Responde si existe una clave en la tabla
        public bool Contiene(TClave clave)
        {
            VerificarClave(clave); // Se verifica que la clave no sea null
            return BuscarNodo(clave) != null; // Se devuelve false si no se encontró o true si sí 
        }

        // Cambia el valor de un nodo en la tabla, devuelve true si se cambio, false sino
        public bool Actualizar(TClave clave, TValor valor)
        {
            VerificarClave(clave); // Se verifica que la clave no sea null

            Nodo? nodo = BuscarNodo(clave); // Busca el nodo

            if (nodo == null) // Si no lo encuentra devuelve false
                return false;

            nodo.Valor = valor; // Si lo encuentra cambia el valor y devuelve true
            return true;
        }
    }
}
