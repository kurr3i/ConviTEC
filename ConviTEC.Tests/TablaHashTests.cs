using ConviTEC.Algoritmos;
using ConviTEC.Estructuras;

namespace ConviTEC.Tests
{
    public class TablaHashTests
    {
        [Fact]
        public void TablaNueva_EstaVacia()
        {
            var tabla = new TablaHash<string, int>();

            Assert.True(tabla.EstaVacia);
            Assert.Equal(0, tabla.Cantidad);
            Assert.Empty(tabla.Claves());
            Assert.Empty(tabla.Valores());
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-4)]
        public void CapacidadInicialInvalida_Lanza(int capacidad)
        {
            Assert.Throws<ArgumentException>(() => new TablaHash<string, int>(capacidad));
        }

        [Fact]
        public void Insertar_YObtener_DevuelveElValor()
        {
            var tabla = new TablaHash<string, string>();

            Assert.True(tabla.Insertar("P001", "Café"));
            Assert.True(tabla.Insertar("P002", "Leche"));

            Assert.Equal(2, tabla.Cantidad);
            Assert.Equal("Café", tabla.Obtener("P001"));
            Assert.Equal("Leche", tabla.Obtener("P002"));
        }

        [Fact]
        public void Insertar_ClaveRepetida_DevuelveFalseYNoSobrescribe()
        {
            var tabla = new TablaHash<string, string>();
            tabla.Insertar("P001", "Café");

            Assert.False(tabla.Insertar("P001", "Otro"));
            Assert.Equal(1, tabla.Cantidad);
            Assert.Equal("Café", tabla.Obtener("P001"));
        }

        [Fact]
        public void ClaveNull_Lanza()
        {
            var tabla = new TablaHash<string, int>();

            Assert.Throws<ArgumentNullException>(() => tabla.Insertar(null!, 1));
            Assert.Throws<ArgumentNullException>(() => tabla.Contiene(null!));
            Assert.Throws<ArgumentNullException>(() => tabla.Eliminar(null!));
            Assert.Throws<ArgumentNullException>(() => tabla.Actualizar(null!, 1));
            Assert.Throws<ArgumentNullException>(() => tabla.IntentarObtener(null!, out _));
        }

        [Fact]
        public void Contiene_ExistenteEInexistente()
        {
            var tabla = new TablaHash<string, int>();
            tabla.Insertar("P001", 10);

            Assert.True(tabla.Contiene("P001"));
            Assert.False(tabla.Contiene("P999"));
        }

        [Fact]
        public void IntentarObtener_ExistenteEInexistente()
        {
            var tabla = new TablaHash<string, int>();
            tabla.Insertar("P001", 10);

            Assert.True(tabla.IntentarObtener("P001", out int valor));
            Assert.Equal(10, valor);
            Assert.False(tabla.IntentarObtener("P999", out _));
        }

        [Fact]
        public void Obtener_Inexistente_Lanza()
        {
            var tabla = new TablaHash<string, int>();

            Assert.Throws<KeyNotFoundException>(() => tabla.Obtener("P001"));
        }

        [Fact]
        public void Actualizar_Existente_CambiaElValor()
        {
            var tabla = new TablaHash<string, int>();
            tabla.Insertar("P001", 10);

            Assert.True(tabla.Actualizar("P001", 25));
            Assert.Equal(25, tabla.Obtener("P001"));
            Assert.Equal(1, tabla.Cantidad);
        }

        [Fact]
        public void Actualizar_Inexistente_DevuelveFalseYNoLaCrea()
        {
            var tabla = new TablaHash<string, int>();

            Assert.False(tabla.Actualizar("P001", 25));
            Assert.False(tabla.Contiene("P001"));
        }

        [Fact]
        public void Colisiones_TodasLasClavesSeEncuentran()
        {
            var tabla = new TablaHash<int, string>(16);
            tabla.Insertar(1, "uno");
            tabla.Insertar(17, "diecisiete");
            tabla.Insertar(33, "treinta y tres");

            Assert.Equal(3, tabla.Cantidad);
            Assert.Equal("uno", tabla.Obtener(1));
            Assert.Equal("diecisiete", tabla.Obtener(17));
            Assert.Equal("treinta y tres", tabla.Obtener(33));
            Assert.False(tabla.Contiene(49));
        }

        [Fact]
        public void Eliminar_PrimeroDeLaCadena()
        {
            var tabla = CrearTablaConColisiones();

            Assert.True(tabla.Eliminar(33));
            Assert.False(tabla.Contiene(33));
            Assert.True(tabla.Contiene(17));
            Assert.True(tabla.Contiene(1));
            Assert.Equal(2, tabla.Cantidad);
        }

        [Fact]
        public void Eliminar_MedioDeLaCadena()
        {
            var tabla = CrearTablaConColisiones();

            Assert.True(tabla.Eliminar(17));
            Assert.False(tabla.Contiene(17));
            Assert.True(tabla.Contiene(33));
            Assert.True(tabla.Contiene(1));
        }

        [Fact]
        public void Eliminar_FinalDeLaCadena()
        {
            var tabla = CrearTablaConColisiones();

            Assert.True(tabla.Eliminar(1));
            Assert.False(tabla.Contiene(1));
            Assert.True(tabla.Contiene(33));
            Assert.True(tabla.Contiene(17));
        }

        [Fact]
        public void Eliminar_Inexistente_DevuelveFalseYNoCambia()
        {
            var tabla = CrearTablaConColisiones();

            Assert.False(tabla.Eliminar(49));
            Assert.False(tabla.Eliminar(5));
            Assert.Equal(3, tabla.Cantidad);
        }

        [Fact]
        public void Eliminar_ElUnico_QuedaVacia()
        {
            var tabla = new TablaHash<string, int>();
            tabla.Insertar("P001", 1);

            Assert.True(tabla.Eliminar("P001"));
            Assert.True(tabla.EstaVacia);
        }

        [Fact]
        public void ClavesNegativas_FuncionanIgual()
        {
            var tabla = new TablaHash<int, string>();
            tabla.Insertar(-5, "menos cinco");
            tabla.Insertar(int.MinValue, "mínimo");

            Assert.Equal("menos cinco", tabla.Obtener(-5));
            Assert.Equal("mínimo", tabla.Obtener(int.MinValue));
            Assert.True(tabla.Eliminar(int.MinValue));
        }

        [Fact]
        public void Redimensionar_NoPierdeElementos()
        {
            var tabla = new TablaHash<int, int>(2);
            for (int i = 0; i < 1000; i++)
                tabla.Insertar(i, i * 10);

            Assert.Equal(1000, tabla.Cantidad);
            for (int i = 0; i < 1000; i++)
                Assert.Equal(i * 10, tabla.Obtener(i));
        }

        [Fact]
        public void ClavesYValores_ContienenTodoSinRepetir()
        {
            var tabla = new TablaHash<int, string>();
            int[] claves = { 50, 3, 27, 81, 14 };
            foreach (int clave in claves)
                tabla.Insertar(clave, $"v{clave}");

            int[] obtenidas = tabla.Claves();
            MergeSort.Ordenar(obtenidas, (a, b) => a.CompareTo(b));

            string[] valores = tabla.Valores();
            MergeSort.Ordenar(valores, (a, b) => string.CompareOrdinal(a, b));

            Assert.Equal(new[] { 3, 14, 27, 50, 81 }, obtenidas);
            Assert.Equal(new[] { "v14", "v27", "v3", "v50", "v81" }, valores);
        }

        [Fact]
        public void Limpiar_DejaLaTablaVaciaYUsable()
        {
            var tabla = CrearTablaConColisiones();

            tabla.Limpiar();

            Assert.True(tabla.EstaVacia);
            Assert.False(tabla.Contiene(1));
            Assert.Empty(tabla.Claves());

            Assert.True(tabla.Insertar(1, "otra vez"));
            Assert.Equal("otra vez", tabla.Obtener(1));
        }

        [Fact]
        public void OperacionesAleatorias_CoincidenConLaReferencia()
        {
            var tabla = new TablaHash<int, int>(2);
            bool[] presente = new bool[300];
            int cantidad = 0;
            var azar = new Random(21);

            for (int paso = 0; paso < 5000; paso++)
            {
                int clave = azar.Next(300);
                if (azar.Next(3) < 2)
                {
                    bool insertado = tabla.Insertar(clave, clave * 7);
                    Assert.Equal(!presente[clave], insertado);
                    if (insertado) { presente[clave] = true; cantidad++; }
                }
                else
                {
                    bool eliminado = tabla.Eliminar(clave);
                    Assert.Equal(presente[clave], eliminado);
                    if (eliminado) { presente[clave] = false; cantidad--; }
                }
            }

            Assert.Equal(cantidad, tabla.Cantidad);
            for (int clave = 0; clave < 300; clave++)
            {
                Assert.Equal(presente[clave], tabla.Contiene(clave));
                if (presente[clave])
                    Assert.Equal(clave * 7, tabla.Obtener(clave));
            }
        }

        private static TablaHash<int, string> CrearTablaConColisiones()
        {
            var tabla = new TablaHash<int, string>(16);
            tabla.Insertar(1, "v1");
            tabla.Insertar(17, "v17");
            tabla.Insertar(33, "v33");
            return tabla;
        }
    }
}