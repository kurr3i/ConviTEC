using ConviTEC.Estructuras;

namespace ConviTEC.Tests
{
    public class ListaDinamicaTests
    {
        [Fact]
        public void ListaNueva_EstaVacia()
        {
            var lista = new ListaDinamica<int>();

            Assert.True(lista.EstaVacia);
            Assert.Equal(0, lista.Cantidad);
            Assert.Empty(lista.ComoArreglo());
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-5)]
        public void CapacidadInicialInvalida_Lanza(int capacidad)
        {
            Assert.Throws<ArgumentException>(() => new ListaDinamica<int>(capacidad));
        }

        [Fact]
        public void Agregar_ConservaElOrden()
        {
            var lista = new ListaDinamica<string>();
            lista.Agregar("A");
            lista.Agregar("B");
            lista.Agregar("C");

            Assert.Equal(3, lista.Cantidad);
            Assert.False(lista.EstaVacia);
            Assert.Equal("A", lista.Obtener(0));
            Assert.Equal("B", lista.Obtener(1));
            Assert.Equal("C", lista.Obtener(2));
        }

        [Fact]
        public void Crecer_NoPierdeElementos()
        {
            var lista = new ListaDinamica<int>(2);
            for (int i = 0; i < 100; i++)
                lista.Agregar(i);

            Assert.Equal(100, lista.Cantidad);
            for (int i = 0; i < 100; i++)
                Assert.Equal(i, lista.Obtener(i));
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(3)]
        [InlineData(10)]
        public void IndiceFueraDeRango_Lanza(int indice)
        {
            var lista = new ListaDinamica<int>();
            lista.Agregar(1);
            lista.Agregar(2);
            lista.Agregar(3);

            Assert.Throws<ArgumentOutOfRangeException>(() => lista.Obtener(indice));
            Assert.Throws<ArgumentOutOfRangeException>(() => lista.Reemplazar(indice, 99));
            Assert.Throws<ArgumentOutOfRangeException>(() => lista.EliminarEn(indice));
        }

        [Fact]
        public void ListaVacia_ObtenerPosicionCero_Lanza()
        {
            var lista = new ListaDinamica<int>();

            Assert.Throws<ArgumentOutOfRangeException>(() => lista.Obtener(0));
        }

        [Fact]
        public void Reemplazar_CambiaSoloEsaPosicion()
        {
            var lista = new ListaDinamica<string>();
            lista.Agregar("A");
            lista.Agregar("B");
            lista.Agregar("C");

            lista.Reemplazar(1, "X");

            Assert.Equal(new[] { "A", "X", "C" }, lista.ComoArreglo());
        }

        [Fact]
        public void EliminarEn_Inicio_CorreLosDemas()
        {
            var lista = CrearLista("A", "B", "C", "D");

            lista.EliminarEn(0);

            Assert.Equal(new[] { "B", "C", "D" }, lista.ComoArreglo());
        }

        [Fact]
        public void EliminarEn_Medio_CorreLosDemas()
        {
            var lista = CrearLista("A", "B", "C", "D");

            lista.EliminarEn(1);

            Assert.Equal(new[] { "A", "C", "D" }, lista.ComoArreglo());
        }

        [Fact]
        public void EliminarEn_Final_QuitaElUltimo()
        {
            var lista = CrearLista("A", "B", "C", "D");

            lista.EliminarEn(3);

            Assert.Equal(new[] { "A", "B", "C" }, lista.ComoArreglo());
        }

        [Fact]
        public void EliminarEn_ElUnico_QuedaVacia()
        {
            var lista = CrearLista("A");

            lista.EliminarEn(0);

            Assert.True(lista.EstaVacia);
        }

        [Fact]
        public void BuscarIndice_Existente_DevuelveSuPosicion()
        {
            var lista = CrearLista("A", "B", "C");

            Assert.Equal(2, lista.BuscarIndice(x => x == "C"));
        }

        [Fact]
        public void BuscarIndice_Inexistente_DevuelveMenosUno()
        {
            var lista = CrearLista("A", "B", "C");

            Assert.Equal(-1, lista.BuscarIndice(x => x == "Z"));
        }

        [Fact]
        public void BuscarIndice_ListaVacia_DevuelveMenosUno()
        {
            var lista = new ListaDinamica<string>();

            Assert.Equal(-1, lista.BuscarIndice(x => x == "A"));
        }

        [Fact]
        public void BuscarIndice_CondicionNull_Lanza()
        {
            var lista = CrearLista("A");

            Assert.Throws<ArgumentNullException>(() => lista.BuscarIndice(null!));
        }

        [Fact]
        public void EliminarPrimero_Existente_DevuelveTrueYLoQuita()
        {
            var lista = CrearLista("A", "B", "C");

            Assert.True(lista.EliminarPrimero(x => x == "B"));
            Assert.Equal(new[] { "A", "C" }, lista.ComoArreglo());
        }

        [Fact]
        public void EliminarPrimero_Inexistente_DevuelveFalseYNoCambia()
        {
            var lista = CrearLista("A", "B", "C");

            Assert.False(lista.EliminarPrimero(x => x == "Z"));
            Assert.Equal(new[] { "A", "B", "C" }, lista.ComoArreglo());
        }

        [Fact]
        public void EliminarPrimero_ConDuplicados_QuitaSoloElPrimero()
        {
            var lista = CrearLista("A", "B", "A", "C");

            lista.EliminarPrimero(x => x == "A");

            Assert.Equal(new[] { "B", "A", "C" }, lista.ComoArreglo());
        }

        [Fact]
        public void Limpiar_DejaLaListaVaciaYUsable()
        {
            var lista = CrearLista("A", "B", "C");

            lista.Limpiar();

            Assert.True(lista.EstaVacia);
            Assert.Empty(lista.ComoArreglo());

            lista.Agregar("D");
            Assert.Equal("D", lista.Obtener(0));
        }

        [Fact]
        public void ComoArreglo_DevuelveUnaCopia()
        {
            var lista = CrearLista("A", "B");

            string[] copia = lista.ComoArreglo();
            copia[0] = "Z";

            Assert.Equal("A", lista.Obtener(0));
        }

        [Fact]
        public void ComoArreglo_TieneElTamanoExacto()
        {
            var lista = new ListaDinamica<int>(8);
            lista.Agregar(1);
            lista.Agregar(2);
            lista.Agregar(3);

            Assert.Equal(3, lista.ComoArreglo().Length);
        }

        private static ListaDinamica<string> CrearLista(params string[] elementos)
        {
            var lista = new ListaDinamica<string>();
            foreach (string elemento in elementos)
                lista.Agregar(elemento);
            return lista;
        }
    }
}