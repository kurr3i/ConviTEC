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
}
