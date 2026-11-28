using ConviTEC.Estructuras;

namespace ConviTEC.Tests
{
    public class ColaTests
    {
        [Fact]
        public void ColaNueva_EstaVacia()
        {
            var cola = new Cola<int>();

            Assert.True(cola.EstaVacia);
            Assert.Equal(0, cola.Cantidad);
            Assert.Empty(cola.ComoArreglo());
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-3)]
        public void CapacidadInicialInvalida_Lanza(int capacidad)
        {
            Assert.Throws<ArgumentException>(() => new Cola<int>(capacidad));
        }

        [Fact]
        public void ColaVacia_DesencolarOVerFrente_Lanza()
        {
            var cola = new Cola<string>();

            Assert.Throws<InvalidOperationException>(() => cola.Desencolar());
            Assert.Throws<InvalidOperationException>(() => cola.VerFrente());
        }

        [Fact]
        public void Desencolar_SaleEnOrdenDeLlegada()
        {
            var cola = CrearCola("A", "B", "C");

            Assert.Equal("A", cola.Desencolar());
            Assert.Equal("B", cola.Desencolar());
            Assert.Equal("C", cola.Desencolar());
            Assert.True(cola.EstaVacia);
        }

        [Fact]
        public void VerFrente_NoSacaElElemento()
        {
            var cola = CrearCola("A", "B");

            Assert.Equal("A", cola.VerFrente());
            Assert.Equal("A", cola.VerFrente());
            Assert.Equal(2, cola.Cantidad);
        }

        [Fact]
        public void DesencolarTodo_DejaLaColaVaciaYUsable()
        {
            var cola = CrearCola("A", "B");
            cola.Desencolar();
            cola.Desencolar();

            Assert.Throws<InvalidOperationException>(() => cola.Desencolar());

            cola.Encolar("C");
            Assert.Equal("C", cola.VerFrente());
            Assert.Equal(1, cola.Cantidad);
        }

        [Fact]
        public void Crecer_NoPierdeElementosNiElOrden()
        {
            var cola = new Cola<int>(2);
            for (int i = 0; i < 100; i++)
                cola.Encolar(i);

            Assert.Equal(100, cola.Cantidad);
            for (int i = 0; i < 100; i++)
                Assert.Equal(i, cola.Desencolar());
        }

        [Fact]
        public void DarLaVuelta_ReutilizaLosEspaciosDelInicio()
        {
            var cola = new Cola<string>(4);
            cola.Encolar("A");
            cola.Encolar("B");
            cola.Encolar("C");
            cola.Desencolar();
            cola.Desencolar();

            cola.Encolar("D");
            cola.Encolar("E");
            cola.Encolar("F");

            Assert.Equal(4, cola.Cantidad);
            Assert.Equal(new[] { "C", "D", "E", "F" }, cola.ComoArreglo());
        }

        [Fact]
        public void CrecerConLaColaPartida_ConservaElOrden()
        {
            var cola = new Cola<string>(4);
            cola.Encolar("A");
            cola.Encolar("B");
            cola.Encolar("C");
            cola.Encolar("D");
            cola.Desencolar();
            cola.Desencolar();
            cola.Encolar("E");
            cola.Encolar("F");

            cola.Encolar("G");

            Assert.Equal(new[] { "C", "D", "E", "F", "G" }, cola.ComoArreglo());
            Assert.Equal("C", cola.Desencolar());
            Assert.Equal("D", cola.Desencolar());
        }

        [Fact]
        public void ComoArreglo_NoModificaLaCola()
        {
            var cola = CrearCola("A", "B", "C");

            string[] copia = cola.ComoArreglo();
            copia[0] = "Z";

            Assert.Equal(3, cola.Cantidad);
            Assert.Equal("A", cola.VerFrente());
        }

        [Fact]
        public void Limpiar_DejaLaColaVaciaYUsable()
        {
            var cola = CrearCola("A", "B", "C");
            cola.Desencolar();
            cola.Encolar("D");

            cola.Limpiar();

            Assert.True(cola.EstaVacia);
            Assert.Empty(cola.ComoArreglo());

            cola.Encolar("E");
            Assert.Equal("E", cola.Desencolar());
        }

        [Fact]
        public void ConDuplicados_SalenTodos()
        {
            var cola = CrearCola("A", "A", "B", "A");

            Assert.Equal(new[] { "A", "A", "B", "A" }, cola.ComoArreglo());
            Assert.Equal(4, cola.Cantidad);
        }

        [Fact]
        public void OperacionesAleatorias_CoincidenConLaReferencia()
        {
            var cola = new Cola<int>(2);
            int[] referencia = new int[5000];
            int frente = 0;
            int fin = 0;
            var azar = new Random(13);

            for (int paso = 0; paso < 5000; paso++)
            {
                if (fin > frente && azar.Next(5) < 2)
                {
                    Assert.Equal(referencia[frente], cola.Desencolar());
                    frente++;
                }
                else
                {
                    int valor = azar.Next(1000);
                    cola.Encolar(valor);
                    referencia[fin] = valor;
                    fin++;
                }

                Assert.Equal(fin - frente, cola.Cantidad);
            }

            while (frente < fin)
            {
                Assert.Equal(referencia[frente], cola.Desencolar());
                frente++;
            }
            Assert.True(cola.EstaVacia);
        }

        private static Cola<string> CrearCola(params string[] elementos)
        {
            var cola = new Cola<string>();
            foreach (string elemento in elementos)
                cola.Encolar(elemento);
            return cola;
        }
    }
}