using ConviTEC.Estructuras;
using ConviTEC.Persistencia;
using ConviTEC.Servicios;

namespace ConviTEC.Tests
{
    public class CargaGrafoTests
    {
        [Fact]
        public void CargarGrafoDelProyecto_TieneLasRutasDelEnunciado()
        {
            GrafoMatriz grafo = Repositorio.CargarGrafo(RutaGrafoDelProyecto());

            Assert.Equal(13, grafo.CantidadVertices);
            Assert.Equal(15, grafo.CantidadAristas);

            int alajuela = grafo.ObtenerIndice("Alajuela (Sede)");
            int heredia = grafo.ObtenerIndice("Heredia");
            Assert.Equal(20, grafo.ObtenerPeso(alajuela, heredia));
            Assert.Equal(20, grafo.ObtenerPeso(heredia, alajuela));
        }

        [Fact]
        public void CargarGrafoDelProyecto_ReconoceLasCuatroSedes()
        {
            GrafoMatriz grafo = Repositorio.CargarGrafo(RutaGrafoDelProyecto());

            foreach (string sede in new[] { "Liberia", "Alajuela", "San José", "Limón" })
            {
                int indice = grafo.ObtenerIndice(sede);
                Assert.NotEqual(-1, indice);
                Assert.True(grafo.EsSede(indice));
            }

            int sedes = 0;
            for (int i = 0; i < grafo.CantidadVertices; i++)
                if (grafo.EsSede(i)) sedes++;
            Assert.Equal(4, sedes);
        }

        [Fact]
        public void CargarGrafoDelProyecto_LiberiaSoloConectaConCanas()
        {
            GrafoMatriz grafo = Repositorio.CargarGrafo(RutaGrafoDelProyecto());
            int liberia = grafo.ObtenerIndice("Liberia");
            int canas = grafo.ObtenerIndice("Cañas");

            for (int j = 0; j < grafo.CantidadVertices; j++)
            {
                int esperado = j == canas ? 65 : GrafoMatriz.SinArista;
                Assert.Equal(esperado, grafo.ObtenerPeso(liberia, j));
            }
        }

        [Fact]
        public void CargarGrafo_ObjetoConPropiedadRutas_SeAcepta()
        {
            string archivo = CrearArchivo("""
                { "Rutas": [ { "origen": "Cartago", "destino": "Paraíso", "ruta": "R09", "tiempo": 15 } ] }
                """);
            try
            {
                GrafoMatriz grafo = Repositorio.CargarGrafo(archivo);
                Assert.Equal(2, grafo.CantidadVertices);
                Assert.Equal(1, grafo.CantidadAristas);
            }
            finally { File.Delete(archivo); }
        }

        [Fact]
        public void CargarGrafo_RutaDuplicada_ConservaMenorTiempo()
        {
            string archivo = CrearArchivo("""
                [
                  { "origen": "Cartago", "destino": "Paraíso", "ruta": "R09", "tiempo": 30 },
                  { "origen": "Paraíso", "destino": "Cartago", "ruta": "R99", "tiempo": 15 }
                ]
                """);
            try
            {
                GrafoMatriz grafo = Repositorio.CargarGrafo(archivo);
                Assert.Equal(1, grafo.CantidadAristas);
                Assert.Equal(15, grafo.ObtenerPeso(0, 1));
            }
            finally { File.Delete(archivo); }
        }

        [Fact]
        public void CargarGrafo_ArchivoInexistente_Lanza()
        {
            string archivo = Path.Combine(Path.GetTempPath(), $"no-existe-{Guid.NewGuid()}.json");

            Assert.Throws<FileNotFoundException>(() => Repositorio.CargarGrafo(archivo));
        }

        [Theory]
        [InlineData("[]", "no contiene rutas")]
        [InlineData("{ \"otra\": 1 }", "lista de rutas")]
        [InlineData("[ { \"origen\": \"Cartago\" ", "no es un JSON válido")]
        [InlineData("[ { \"origen\": \"Cartago\", \"destino\": \"\", \"ruta\": \"R09\", \"tiempo\": 15 } ]", "R09: falta")]
        [InlineData("[ { \"origen\": \"Cartago\", \"destino\": \"Paraíso\", \"ruta\": \"R09\", \"tiempo\": 0 } ]", "R09: El tiempo")]
        [InlineData("[ { \"origen\": \"Cartago\", \"destino\": \"Paraíso\", \"ruta\": \"R09\" } ]", "R09: El tiempo")]
        [InlineData("[ { \"origen\": \"Limón (Sede)\", \"destino\": \"Limon\", \"ruta\": \"R77\", \"tiempo\": 5 } ]", "R77: Una ruta no puede")]
        [InlineData("[ null ]", "#1: falta")]
        public void CargarGrafo_ArchivoInvalido_LanzaConMotivo(string contenido, string fragmentoMensaje)
        {
            string archivo = CrearArchivo(contenido);
            try
            {
                var ex = Assert.Throws<InvalidDataException>(() => Repositorio.CargarGrafo(archivo));
                Assert.Contains(fragmentoMensaje, ex.Message);
            }
            finally { File.Delete(archivo); }
        }

        private static string CrearArchivo(string contenido)
        {
            string archivo = Path.Combine(Path.GetTempPath(), $"grafo-prueba-{Guid.NewGuid()}.json");
            File.WriteAllText(archivo, contenido);
            return archivo;
        }

        // Busca data/grafo.json subiendo desde la carpeta bin
        internal static string RutaGrafoDelProyecto()
        {
            var dir = new DirectoryInfo(AppContext.BaseDirectory);
            while (dir != null)
            {
                string candidato = Path.Combine(dir.FullName, "data", "grafo.json");
                if (File.Exists(candidato))
                    return candidato;
                dir = dir.Parent;
            }
            throw new FileNotFoundException("No se encontró data/grafo.json en el repositorio.");
        }
    }

    public class RecorridoPersistenciaTests
    {
        private readonly GrafoMatriz _grafo =
            Repositorio.CargarGrafo(CargaGrafoTests.RutaGrafoDelProyecto());

        [Fact]
        public void GuardarYReiniciar_RecuperaElRecorrido()
        {
            string carpeta = Path.Combine(Path.GetTempPath(), $"recorrido-{Guid.NewGuid()}");
            string archivo = Path.Combine(carpeta, "recorrido.json");
            try
            {
                var antes = new DistribucionService(_grafo, "Alajuela");
                antes.EntregarSiguiente(new[]
                {
                    new EntregaPendiente { IdPedido = "P1", Destino = "Heredia" },
                    new EntregaPendiente { IdPedido = "P2", Destino = "Cartago" }
                });
                Repositorio.GuardarRecorrido(archivo, antes.ObtenerEstado());

                // Simula cerrar la aplicación y volver a abrirla
                var despues = new DistribucionService(_grafo, "Alajuela");
                despues.RestaurarEstado(Repositorio.CargarRecorrido(archivo)!);

                Assert.Equal("Heredia", despues.UbicacionActual);
                Assert.Equal(20, despues.TiempoAcumulado);
            }
            finally
            {
                if (Directory.Exists(carpeta)) Directory.Delete(carpeta, recursive: true);
            }
        }

        [Fact]
        public void CargarRecorrido_SinArchivo_DevuelveNull()
        {
            string archivo = Path.Combine(Path.GetTempPath(), $"no-existe-{Guid.NewGuid()}.json");

            Assert.Null(Repositorio.CargarRecorrido(archivo));
        }

        [Fact]
        public void CargarRecorrido_ArchivoDanado_Lanza()
        {
            string archivo = Path.Combine(Path.GetTempPath(), $"recorrido-{Guid.NewGuid()}.json");
            File.WriteAllText(archivo, "{ \"ubicacionActual\": ");
            try
            {
                Assert.Throws<InvalidDataException>(() => Repositorio.CargarRecorrido(archivo));
            }
            finally { File.Delete(archivo); }
        }

        [Theory]
        [InlineData("Cañas", 0)]       // existe, pero Alajuela no llega
        [InlineData("Nicoya", 0)]      // no existe
        [InlineData("Heredia", -5)]    // tiempo inválido
        public void RestaurarEstado_Invalido_Lanza(string ubicacion, int tiempo)
        {
            var servicio = new DistribucionService(_grafo, "Alajuela");
            var estado = new EstadoRecorrido { UbicacionActual = ubicacion, TiempoAcumulado = tiempo };

            Assert.Throws<ArgumentException>(() => servicio.RestaurarEstado(estado));
            Assert.Equal("Alajuela", servicio.UbicacionActual);
        }
    }
}
