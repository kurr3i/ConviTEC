using ConviTEC.Estructuras;

namespace ConviTEC.Tests
{
    public class ArbolAVLTests
    {
        [Fact]
        public void ArbolNuevo_EstaVacio()
        {
            var arbol = new ArbolAVL<string, int>();

            Assert.True(arbol.EstaVacia);
            Assert.Equal(0, arbol.Cantidad);
            Assert.Equal(0, arbol.Altura);
            Assert.Empty(arbol.ValoresEnOrden());
            Assert.Empty(arbol.ClavesEnOrden());
        }

        [Fact]
        public void Insertar_YObtener_DevuelveElValor()
        {
            var arbol = new ArbolAVL<string, string>();

            Assert.True(arbol.Insertar("P001", "Café"));
            Assert.True(arbol.Insertar("P002", "Leche"));

            Assert.Equal(2, arbol.Cantidad);
            Assert.Equal("Café", arbol.Obtener("P001"));
            Assert.Equal("Leche", arbol.Obtener("P002"));
        }

        [Fact]
        public void Insertar_ClaveRepetida_DevuelveFalseYNoSobrescribe()
        {
            var arbol = new ArbolAVL<string, string>();
            arbol.Insertar("P001", "Café");

            Assert.False(arbol.Insertar("P001", "Otro"));
            Assert.Equal(1, arbol.Cantidad);
            Assert.Equal("Café", arbol.Obtener("P001"));
        }

        [Fact]
        public void ClaveNull_Lanza()
        {
            var arbol = new ArbolAVL<string, int>();

            Assert.Throws<ArgumentNullException>(() => arbol.Insertar(null!, 1));
            Assert.Throws<ArgumentNullException>(() => arbol.Contiene(null!));
            Assert.Throws<ArgumentNullException>(() => arbol.Eliminar(null!));
            Assert.Throws<ArgumentNullException>(() => arbol.Actualizar(null!, 1));
        }

        [Fact]
        public void Contiene_ExistenteEInexistente()
        {
            var arbol = new ArbolAVL<int, string>();
            arbol.Insertar(10, "diez");

            Assert.True(arbol.Contiene(10));
            Assert.False(arbol.Contiene(99));
        }

        [Fact]
        public void IntentarObtener_Inexistente_DevuelveFalse()
        {
            var arbol = new ArbolAVL<int, string>();
            arbol.Insertar(10, "diez");

            Assert.False(arbol.IntentarObtener(99, out _));
        }

        [Fact]
        public void IntentarObtener_Existente_DevuelveTrueYElValor()
        {
            var arbol = new ArbolAVL<int, string>();
            arbol.Insertar(10, "diez");

            Assert.True(arbol.IntentarObtener(10, out string valor));
            Assert.Equal("diez", valor);
        }

        [Fact]
        public void Obtener_Inexistente_Lanza()
        {
            var arbol = new ArbolAVL<int, string>();

            Assert.Throws<KeyNotFoundException>(() => arbol.Obtener(5));
        }

        [Fact]
        public void Actualizar_Existente_CambiaElValor()
        {
            var arbol = new ArbolAVL<string, int>();
            arbol.Insertar("P001", 10);

            Assert.True(arbol.Actualizar("P001", 25));
            Assert.Equal(25, arbol.Obtener("P001"));
            Assert.Equal(1, arbol.Cantidad);
        }

        [Fact]
        public void Actualizar_Inexistente_DevuelveFalse()
        {
            var arbol = new ArbolAVL<string, int>();

            Assert.False(arbol.Actualizar("P001", 25));
            Assert.True(arbol.EstaVacia);
        }

        [Fact]
        public void ValoresEnOrden_SalenOrdenadosPorClave()
        {
            var arbol = new ArbolAVL<int, string>();
            int[] claves = { 50, 20, 80, 10, 30, 90, 60 };
            foreach (int clave in claves)
                arbol.Insertar(clave, $"v{clave}");

            Assert.Equal(new[] { 10, 20, 30, 50, 60, 80, 90 }, arbol.ClavesEnOrden());
            Assert.Equal(new[] { "v10", "v20", "v30", "v50", "v60", "v80", "v90" }, arbol.ValoresEnOrden());
        }

        [Fact]
        public void RotacionSimple_InsertarEnOrdenAscendente()
        {
            var arbol = new ArbolAVL<int, int>();
            arbol.Insertar(10, 0);
            arbol.Insertar(20, 0);
            arbol.Insertar(30, 0);

            Assert.Equal(2, arbol.Altura);
        }

        [Fact]
        public void RotacionSimple_InsertarEnOrdenDescendente()
        {
            var arbol = new ArbolAVL<int, int>();
            arbol.Insertar(30, 0);
            arbol.Insertar(20, 0);
            arbol.Insertar(10, 0);

            Assert.Equal(2, arbol.Altura);
        }

        [Fact]
        public void RotacionDoble_Zigzag()
        {
            var izquierdaDerecha = new ArbolAVL<int, int>();
            izquierdaDerecha.Insertar(30, 0);
            izquierdaDerecha.Insertar(10, 0);
            izquierdaDerecha.Insertar(20, 0);

            var derechaIzquierda = new ArbolAVL<int, int>();
            derechaIzquierda.Insertar(10, 0);
            derechaIzquierda.Insertar(30, 0);
            derechaIzquierda.Insertar(20, 0);

            Assert.Equal(2, izquierdaDerecha.Altura);
            Assert.Equal(2, derechaIzquierda.Altura);
            Assert.Equal(new[] { 10, 20, 30 }, izquierdaDerecha.ClavesEnOrden());
            Assert.Equal(new[] { 10, 20, 30 }, derechaIzquierda.ClavesEnOrden());
        }

        [Fact]
        public void MilClavesOrdenadas_ElArbolSigueBalanceado()
        {
            var arbol = new ArbolAVL<int, int>();
            for (int i = 1; i <= 1000; i++)
                arbol.Insertar(i, i);

            Assert.Equal(1000, arbol.Cantidad);
            Assert.True(arbol.Altura <= 15, $"La altura fue {arbol.Altura}; un AVL de 1000 nodos no pasa de 14.");
        }

        [Fact]
        public void Eliminar_Hoja()
        {
            var arbol = CrearArbol(20, 10, 30);

            Assert.True(arbol.Eliminar(10));
            Assert.Equal(new[] { 20, 30 }, arbol.ClavesEnOrden());
            Assert.False(arbol.Contiene(10));
        }

        [Fact]
        public void Eliminar_NodoConUnHijo()
        {
            var arbol = CrearArbol(20, 10, 30, 40);

            Assert.True(arbol.Eliminar(30));
            Assert.Equal(new[] { 10, 20, 40 }, arbol.ClavesEnOrden());
        }

        [Fact]
        public void Eliminar_NodoConDosHijos()
        {
            var arbol = CrearArbol(50, 20, 70, 60, 80);

            Assert.True(arbol.Eliminar(50));
            Assert.Equal(new[] { 20, 60, 70, 80 }, arbol.ClavesEnOrden());
            Assert.Equal("v60", arbol.Obtener(60));
        }

        [Fact]
        public void Eliminar_LaRaizUnica_QuedaVacio()
        {
            var arbol = CrearArbol(10);

            Assert.True(arbol.Eliminar(10));
            Assert.True(arbol.EstaVacia);
            Assert.Equal(0, arbol.Altura);
        }

        [Fact]
        public void Eliminar_Inexistente_DevuelveFalseYNoCambia()
        {
            var arbol = CrearArbol(20, 10, 30);

            Assert.False(arbol.Eliminar(99));
            Assert.Equal(3, arbol.Cantidad);
        }

        [Fact]
        public void Eliminar_ProvocaRotacion_SigueBalanceado()
        {
            var arbol = CrearArbol(20, 10, 30, 40);

            arbol.Eliminar(10);

            Assert.Equal(2, arbol.Altura);
            Assert.Equal(new[] { 20, 30, 40 }, arbol.ClavesEnOrden());
        }

        [Fact]
        public void OperacionesAleatorias_CoincidenConLaReferencia()
        {
            var arbol = new ArbolAVL<int, int>();
            bool[] presente = new bool[200];
            int cantidad = 0;
            var azar = new Random(42);

            for (int paso = 0; paso < 5000; paso++)
            {
                int clave = azar.Next(200);
                if (azar.Next(3) < 2)
                {
                    bool insertado = arbol.Insertar(clave, clave * 10);
                    Assert.Equal(!presente[clave], insertado);
                    if (insertado) { presente[clave] = true; cantidad++; }
                }
                else
                {
                    bool eliminado = arbol.Eliminar(clave);
                    Assert.Equal(presente[clave], eliminado);
                    if (eliminado) { presente[clave] = false; cantidad--; }
                }
            }

            Assert.Equal(cantidad, arbol.Cantidad);

            int[] claves = arbol.ClavesEnOrden();
            int posicion = 0;
            for (int clave = 0; clave < 200; clave++)
            {
                if (presente[clave])
                {
                    Assert.Equal(clave, claves[posicion]);
                    Assert.Equal(clave * 10, arbol.Obtener(clave));
                    posicion++;
                }
            }
        }

        [Fact]
        public void Limpiar_DejaElArbolVacioYUsable()
        {
            var arbol = CrearArbol(20, 10, 30);

            arbol.Limpiar();

            Assert.True(arbol.EstaVacia);
            Assert.False(arbol.Contiene(20));

            Assert.True(arbol.Insertar(5, "v5"));
            Assert.Equal(1, arbol.Cantidad);
        }

        private static ArbolAVL<int, string> CrearArbol(params int[] claves)
        {
            var arbol = new ArbolAVL<int, string>();
            foreach (int clave in claves)
                arbol.Insertar(clave, $"v{clave}");
            return arbol;
        }
    }
}