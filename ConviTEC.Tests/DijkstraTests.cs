using ConviTEC.Algoritmos;
using ConviTEC.Estructuras;
using ConviTEC.Persistencia;
using ConviTEC.Servicios;

namespace ConviTEC.Tests
{
    public class DijkstraTests
    {
        private readonly GrafoMatriz _grafo = Repositorio.CargarGrafo(CargaGrafoTests.RutaGrafoDelProyecto());

        [Theory]
        [InlineData("Alajuela", "Heredia", 20)]
        [InlineData("Alajuela", "San José", 35)]
        [InlineData("Alajuela", "Puntarenas", 80)]   // directa (80) le gana a Orotina (55 + 45)
        [InlineData("Alajuela", "Limón", 215)]       // por Turrialba (215) y no por Guápiles (225)
        [InlineData("San José", "Limón", 180)]
        [InlineData("Limón", "Puntarenas", 295)]
        [InlineData("Liberia", "Cañas", 65)]
        public void DistanciaMinima_RedDelEnunciado(string origen, string destino, int esperado)
        {
            ResultadoDijkstra resultado = Dijkstra.Calcular(_grafo, origen);

            Assert.Equal(esperado, resultado.Distancia(_grafo.ObtenerIndice(destino)));
        }

        [Fact]
        public void Camino_AlajuelaALimon_PasaPorTurrialba()
        {
            ResultadoDijkstra resultado = Dijkstra.Calcular(_grafo, "Alajuela");

            int[] camino = resultado.ReconstruirCamino(_grafo.ObtenerIndice("Limón"));

            Assert.Equal(
                new[] { "Alajuela", "San José", "Cartago", "Paraíso", "Turrialba", "Limón" },
                Nombres(camino));
        }

        [Fact]
        public void Origen_DistanciaCeroYCaminoDeUnSoloVertice()
        {
            int sj = _grafo.ObtenerIndice("San José");
            ResultadoDijkstra resultado = Dijkstra.Calcular(_grafo, sj);

            Assert.Equal(0, resultado.Distancia(sj));
            Assert.Equal(new[] { sj }, resultado.ReconstruirCamino(sj));
        }

        [Fact]
        public void Guanacaste_EstaAisladoDelResto()
        {
            ResultadoDijkstra desdeLiberia = Dijkstra.Calcular(_grafo, "Liberia");
            int heredia = _grafo.ObtenerIndice("Heredia");

            Assert.False(desdeLiberia.EsAlcanzable(heredia));
            Assert.Equal(Dijkstra.Infinito, desdeLiberia.Distancia(heredia));
            Assert.Empty(desdeLiberia.ReconstruirCamino(heredia));

            // Y al revés tampoco se llega
            ResultadoDijkstra desdeAlajuela = Dijkstra.Calcular(_grafo, "Alajuela");
            Assert.False(desdeAlajuela.EsAlcanzable(_grafo.ObtenerIndice("Liberia")));
            Assert.False(desdeAlajuela.EsAlcanzable(_grafo.ObtenerIndice("Cañas")));
        }

        [Fact]
        public void Distancias_SonSimetricas()
        {
            int n = _grafo.CantidadVertices;
            for (int a = 0; a < n; a++)
            {
                ResultadoDijkstra desdeA = Dijkstra.Calcular(_grafo, a);
                for (int b = 0; b < n; b++)
                    Assert.Equal(desdeA.Distancia(b), Dijkstra.Calcular(_grafo, b).Distancia(a));
            }
        }

        [Fact]
        public void RutaIndirecta_MasCortaQueLaDirecta()
        {
            var grafo = new GrafoMatriz();
            grafo.AgregarArista("A", "B", 10);
            grafo.AgregarArista("B", "C", 10);
            grafo.AgregarArista("A", "C", 50);

            ResultadoDijkstra resultado = Dijkstra.Calcular(grafo, "A");
            int c = grafo.ObtenerIndice("C");

            Assert.Equal(20, resultado.Distancia(c));
            Assert.Equal(new[] { "A", "B", "C" }, Nombres(grafo, resultado.ReconstruirCamino(c)));
        }

        // Simula el recorrido de entregas: ir siempre al pendiente más cercano
        // y volver a correr Dijkstra desde donde quedó el vehículo.
        [Fact]
        public void Recalculo_DesdeCadaEntrega()
        {
            string[] pendientes = { "Turrialba", "Cartago", "Heredia" };
            bool[] entregado = new bool[pendientes.Length];
            string actual = "Alajuela";
            string[] orden = new string[pendientes.Length];
            int tiempoTotal = 0;

            for (int paso = 0; paso < pendientes.Length; paso++)
            {
                ResultadoDijkstra resultado = Dijkstra.Calcular(_grafo, actual);
                int mejor = -1;
                for (int k = 0; k < pendientes.Length; k++)
                {
                    if (entregado[k]) continue;
                    int d = resultado.Distancia(_grafo.ObtenerIndice(pendientes[k]));
                    if (mejor == -1 || d < resultado.Distancia(_grafo.ObtenerIndice(pendientes[mejor])))
                        mejor = k;
                }

                tiempoTotal += resultado.Distancia(_grafo.ObtenerIndice(pendientes[mejor]));
                entregado[mejor] = true;
                actual = pendientes[mejor];
                orden[paso] = actual;
            }

            // Alajuela -> Heredia (20) -> Cartago (65) -> Turrialba (60)
            Assert.Equal(new[] { "Heredia", "Cartago", "Turrialba" }, orden);
            Assert.Equal(145, tiempoTotal);
        }

        [Fact]
        public void OrigenInexistente_Lanza()
        {
            Assert.Throws<ArgumentException>(() => Dijkstra.Calcular(_grafo, "Nicoya"));
            Assert.Throws<ArgumentOutOfRangeException>(() => Dijkstra.Calcular(_grafo, 99));
            Assert.Throws<ArgumentOutOfRangeException>(() => Dijkstra.Calcular(_grafo, -1));
        }

        [Fact]
        public void GrafoVacio_Lanza()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => Dijkstra.Calcular(new GrafoMatriz(), 0));
        }

        [Fact]
        public void DestinoFueraDeRango_Lanza()
        {
            ResultadoDijkstra resultado = Dijkstra.Calcular(_grafo, "Alajuela");

            Assert.Throws<ArgumentOutOfRangeException>(() => resultado.Distancia(99));
            Assert.Throws<ArgumentOutOfRangeException>(() => resultado.ReconstruirCamino(-1));
        }

        private string[] Nombres(int[] camino) => Nombres(_grafo, camino);

        private static string[] Nombres(GrafoMatriz grafo, int[] camino)
        {
            string[] nombres = new string[camino.Length];
            for (int i = 0; i < camino.Length; i++)
                nombres[i] = grafo.ObtenerNombre(camino[i]);
            return nombres;
        }
    }

    public class DistribucionServiceTests
    {
        private readonly GrafoMatriz _grafo =
            Repositorio.CargarGrafo(CargaGrafoTests.RutaGrafoDelProyecto());

        [Fact]
        public void Constructor_SedeInexistente_Lanza()
        {
            Assert.Throws<ArgumentException>(() => new DistribucionService(_grafo, "Nicoya"));
            Assert.Throws<ArgumentNullException>(() => new DistribucionService(null!, "Alajuela"));
        }

        [Fact]
        public void ObtenerDestinos_TodosMenosLaPropiaSede()
        {
            var servicio = new DistribucionService(_grafo, "Alajuela");

            string[] destinos = servicio.ObtenerDestinos();

            Assert.Equal(12, destinos.Length);
            Assert.DoesNotContain("Alajuela", destinos);
            Assert.Contains("San José", destinos);
            Assert.Contains("Cañas", destinos);   // se muestra para que el rechazo se pueda probar
        }

        [Theory]
        [InlineData("Heredia", "Heredia", 20)]
        [InlineData("heredia", "Heredia", 20)]
        [InlineData("San José (Sede)", "San José", 35)]
        [InlineData("Limon", "Limón", 215)]
        public void ValidarDestino_Alcanzable(string destino, string nombreLimpio, int tiempo)
        {
            var servicio = new DistribucionService(_grafo, "Alajuela");

            ValidacionRuta validacion = servicio.ValidarDestino(destino);

            Assert.True(validacion.Valido);
            Assert.Equal(nombreLimpio, validacion.Destino);
            Assert.Equal(tiempo, validacion.TiempoDesdeSede);
        }

        [Theory]
        [InlineData("Cañas", "No hay ruta")]
        [InlineData("Liberia", "No hay ruta")]
        [InlineData("Nicoya", "no existe")]
        [InlineData("", "Debe indicar")]
        [InlineData("Alajuela (Sede)", "propia sede")]
        public void ValidarDestino_Rechazado(string destino, string fragmentoMotivo)
        {
            var servicio = new DistribucionService(_grafo, "Alajuela");

            ValidacionRuta validacion = servicio.ValidarDestino(destino);

            Assert.False(validacion.Valido);
            Assert.Contains(fragmentoMotivo, validacion.Motivo);
        }

        [Fact]
        public void ValidarDestino_DesdeLiberia_SoloLlegaACanas()
        {
            var servicio = new DistribucionService(_grafo, "Liberia");

            Assert.True(servicio.ValidarDestino("Cañas").Valido);
            Assert.Equal(65, servicio.ValidarDestino("Cañas").TiempoDesdeSede);
            Assert.False(servicio.ValidarDestino("Heredia").Valido);
            Assert.False(servicio.ValidarDestino("San José").Valido);
        }
    }
}
