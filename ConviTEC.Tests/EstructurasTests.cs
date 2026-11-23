using ConviTEC.Estructuras;

namespace ConviTEC.Tests
{
    public class GrafoMatrizTests
    {
        [Fact]
        public void GrafoNuevo_EstaVacio()
        {
            var grafo = new GrafoMatriz();

            Assert.Equal(0, grafo.CantidadVertices);
            Assert.Equal(0, grafo.CantidadAristas);
            Assert.Equal(-1, grafo.ObtenerIndice("Heredia"));
            Assert.Empty(grafo.ObtenerNombres());
        }

        [Fact]
        public void AgregarVertice_Repetido_NoDuplica()
        {
            var grafo = new GrafoMatriz();

            int primero = grafo.AgregarVertice("Heredia");
            int segundo = grafo.AgregarVertice("Heredia");

            Assert.Equal(primero, segundo);
            Assert.Equal(1, grafo.CantidadVertices);
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData(null)]
        public void AgregarVertice_NombreVacio_Lanza(string? nombre)
        {
            var grafo = new GrafoMatriz();

            Assert.Throws<ArgumentException>(() => grafo.AgregarVertice(nombre!));
        }

        [Fact]
        public void AgregarArista_EsSimetrica()
        {
            var grafo = new GrafoMatriz();

            Assert.True(grafo.AgregarArista("Cartago", "Paraíso", 15));

            int c = grafo.ObtenerIndice("Cartago");
            int p = grafo.ObtenerIndice("Paraíso");
            Assert.Equal(15, grafo.ObtenerPeso(c, p));
            Assert.Equal(15, grafo.ObtenerPeso(p, c));
            Assert.Equal(1, grafo.CantidadAristas);
        }

        [Fact]
        public void SinRutaDirecta_DevuelveSinArista()
        {
            var grafo = new GrafoMatriz();
            grafo.AgregarArista("Cartago", "Paraíso", 15);
            grafo.AgregarArista("Paraíso", "Turrialba", 45);

            int c = grafo.ObtenerIndice("Cartago");
            int t = grafo.ObtenerIndice("Turrialba");

            Assert.Equal(GrafoMatriz.SinArista, grafo.ObtenerPeso(c, t));
            Assert.Equal(GrafoMatriz.SinArista, grafo.ObtenerPeso(c, c));
        }

        [Fact]
        public void AgregarArista_Duplicada_ConservaMenorTiempo()
        {
            var grafo = new GrafoMatriz();
            grafo.AgregarArista("Cartago", "Paraíso", 30);

            Assert.False(grafo.AgregarArista("Paraíso", "Cartago", 15));
            Assert.False(grafo.AgregarArista("Cartago", "Paraíso", 50));

            int c = grafo.ObtenerIndice("Cartago");
            int p = grafo.ObtenerIndice("Paraíso");
            Assert.Equal(15, grafo.ObtenerPeso(c, p));
            Assert.Equal(15, grafo.ObtenerPeso(p, c));
            Assert.Equal(1, grafo.CantidadAristas);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-5)]
        public void AgregarArista_TiempoNoPositivo_Lanza(int minutos)
        {
            var grafo = new GrafoMatriz();

            Assert.Throws<ArgumentException>(() => grafo.AgregarArista("Cartago", "Paraíso", minutos));
            Assert.Equal(0, grafo.CantidadVertices);
        }

        [Fact]
        public void AgregarArista_Lazo_LanzaYNoCreaVertice()
        {
            var grafo = new GrafoMatriz();

            Assert.Throws<ArgumentException>(() => grafo.AgregarArista("Cartago", "Cartago", 10));
            Assert.Equal(0, grafo.CantidadVertices);
        }

        [Fact]
        public void Crecer_ConservaVerticesYAristas()
        {
            var grafo = new GrafoMatriz(capacidadInicial: 2);

            for (int k = 0; k < 19; k++)
                grafo.AgregarArista($"V{k}", $"V{k + 1}", k + 1);

            Assert.Equal(20, grafo.CantidadVertices);
            Assert.Equal(19, grafo.CantidadAristas);
            for (int k = 0; k < 19; k++)
            {
                int a = grafo.ObtenerIndice($"V{k}");
                int b = grafo.ObtenerIndice($"V{k + 1}");
                Assert.Equal(k + 1, grafo.ObtenerPeso(a, b));
                Assert.Equal(k + 1, grafo.ObtenerPeso(b, a));
            }
            Assert.Equal("V19", grafo.ObtenerNombre(19));
        }

        [Theory]
        [InlineData("Alajuela (Sede)", "alajuela")]
        [InlineData("San José (Sede)", "san jose")]
        [InlineData("  Limón  ", "limon")]
        [InlineData("SAN   JOSÉ", "san jose")]
        [InlineData("Paraíso", "paraiso")]
        [InlineData("Guápiles (sede)", "guapiles")]
        [InlineData("   ", "")]
        public void Normalizar_QuitaSedeTildesYMayusculas(string entrada, string esperado)
        {
            Assert.Equal(esperado, GrafoMatriz.Normalizar(entrada));
        }

        [Fact]
        public void ConYSinSufijoSede_SonElMismoVertice()
        {
            var grafo = new GrafoMatriz();
            grafo.AgregarArista("Alajuela (Sede)", "Heredia", 20);
            grafo.AgregarArista("Alajuela", "Orotina", 55);

            int a = grafo.ObtenerIndice("Alajuela (Sede)");

            Assert.Equal(3, grafo.CantidadVertices);
            Assert.Equal(a, grafo.ObtenerIndice("alajuela"));
            Assert.Equal("Alajuela", grafo.ObtenerNombre(a));
            Assert.True(grafo.EsSede(a));
            Assert.False(grafo.EsSede(grafo.ObtenerIndice("Heredia")));
        }

        [Fact]
        public void SeMarcaComoSede_AunquePrimeroAparezcaSinSufijo()
        {
            var grafo = new GrafoMatriz();
            grafo.AgregarArista("Cartago", "San José", 45);
            grafo.AgregarArista("San José (Sede)", "Heredia", 20);

            int sj = grafo.ObtenerIndice("San Jose");

            Assert.True(grafo.EsSede(sj));
            Assert.Equal("San José", grafo.ObtenerNombre(sj));
        }

        [Fact]
        public void Lazo_ConNombresDistintosDelMismoLugar_Lanza()
        {
            var grafo = new GrafoMatriz();

            Assert.Throws<ArgumentException>(() => grafo.AgregarArista("Limón (Sede)", "limon", 10));
        }

        [Fact]
        public void IndiceFueraDeRango_Lanza()
        {
            var grafo = new GrafoMatriz();
            grafo.AgregarVertice("Heredia");

            Assert.Throws<ArgumentOutOfRangeException>(() => grafo.ObtenerNombre(1));
            Assert.Throws<ArgumentOutOfRangeException>(() => grafo.ObtenerNombre(-1));
            Assert.Throws<ArgumentOutOfRangeException>(() => grafo.ObtenerPeso(0, 5));
        }
    }

    public class ColaPrioridadTests
    {
        [Fact]
        public void ColaNueva_EstaVacia()
        {
            var cola = new ColaPrioridad<string>();

            Assert.True(cola.EstaVacia);
            Assert.Equal(0, cola.Cantidad);
        }

        [Fact]
        public void ColaVacia_DesencolarOVerMinimo_Lanza()
        {
            var cola = new ColaPrioridad<string>();

            Assert.Throws<InvalidOperationException>(() => cola.Desencolar());
            Assert.Throws<InvalidOperationException>(() => cola.VerPrioridadMinima());
        }

        [Fact]
        public void Desencolar_SaleDeMenorAMayorPrioridad()
        {
            var cola = new ColaPrioridad<string>();
            int[] prioridades = { 5, 3, 8, 1, 9, 2 };
            foreach (int p in prioridades)
                cola.Encolar($"e{p}", p);

            Assert.Equal(1, cola.VerPrioridadMinima());

            int[] esperado = { 1, 2, 3, 5, 8, 9 };
            foreach (int p in esperado)
            {
                Assert.Equal($"e{p}", cola.Desencolar(out int prioridad));
                Assert.Equal(p, prioridad);
            }
            Assert.True(cola.EstaVacia);
        }

        [Fact]
        public void PrioridadesRepetidas_NoSePierdeNinguna()
        {
            var cola = new ColaPrioridad<int>();
            cola.Encolar(10, 4);
            cola.Encolar(20, 4);
            cola.Encolar(30, 1);
            cola.Encolar(40, 4);

            Assert.Equal(30, cola.Desencolar());

            int restantes = 0;
            while (!cola.EstaVacia)
            {
                cola.Desencolar(out int prioridad);
                Assert.Equal(4, prioridad);
                restantes++;
            }
            Assert.Equal(3, restantes);
        }

        [Fact]
        public void Crecer_MantieneElOrden()
        {
            var cola = new ColaPrioridad<int>(capacidadInicial: 1);
            for (int i = 100; i >= 1; i--)
                cola.Encolar(i, i);

            Assert.Equal(100, cola.Cantidad);
            for (int i = 1; i <= 100; i++)
                Assert.Equal(i, cola.Desencolar());
        }

        [Fact]
        public void EncolarYDesencolarIntercalado()
        {
            var cola = new ColaPrioridad<string>();
            cola.Encolar("b", 20);
            cola.Encolar("a", 10);
            Assert.Equal("a", cola.Desencolar());

            cola.Encolar("c", 5);
            cola.Encolar("d", 30);
            Assert.Equal("c", cola.Desencolar());
            Assert.Equal("b", cola.Desencolar());
            Assert.Equal("d", cola.Desencolar());
            Assert.True(cola.EstaVacia);
        }
    }
}
