using System.Text.Json;
using ConviTEC.Estructuras;
using ConviTEC.Servicios;

namespace ConviTEC.Persistencia
{
    public class Repositorio
    {
        private static readonly JsonSerializerOptions OpcionesJson = new()
        {
            PropertyNameCaseInsensitive = true,
            WriteIndented = true
        };

        public static void GuardarRecorrido(string rutaArchivo, EstadoRecorrido estado)
        {
            string? carpeta = Path.GetDirectoryName(rutaArchivo);
            if (!string.IsNullOrEmpty(carpeta))
                Directory.CreateDirectory(carpeta);

            // Primero a un temporal y después se reemplaza, así un cierre a medias no daña el archivo
            string temporal = rutaArchivo + ".tmp";
            File.WriteAllText(temporal, JsonSerializer.Serialize(estado, OpcionesJson));
            File.Move(temporal, rutaArchivo, overwrite: true);
        }

        // Null si todavía no hay un recorrido guardado
        public static EstadoRecorrido? CargarRecorrido(string rutaArchivo)
        {
            if (!File.Exists(rutaArchivo))
                return null;

            try
            {
                return JsonSerializer.Deserialize<EstadoRecorrido>(File.ReadAllText(rutaArchivo), OpcionesJson)
                       ?? throw new InvalidDataException("El archivo del recorrido está vacío.");
            }
            catch (JsonException ex)
            {
                throw new InvalidDataException($"El archivo del recorrido está dañado: {ex.Message}", ex);
            }
        }

        // Acepta [ {...} ] o { "rutas": [ {...} ] }
        public static GrafoMatriz CargarGrafo(string rutaArchivo)
        {
            if (!File.Exists(rutaArchivo))
                throw new FileNotFoundException($"No se encontró el archivo del grafo: {rutaArchivo}", rutaArchivo);

            RutaArchivo?[] rutas = LeerRutas(rutaArchivo);
            if (rutas.Length == 0)
                throw new InvalidDataException("El archivo del grafo no contiene rutas.");

            var grafo = new GrafoMatriz();

            for (int k = 0; k < rutas.Length; k++)
            {
                RutaArchivo? ruta = rutas[k];
                string id = ruta == null || string.IsNullOrWhiteSpace(ruta.Ruta) ? $"#{k + 1}" : ruta.Ruta;

                if (ruta == null || string.IsNullOrWhiteSpace(ruta.Origen) || string.IsNullOrWhiteSpace(ruta.Destino))
                    throw new InvalidDataException($"Ruta {id}: falta el origen o el destino.");

                try
                {
                    bool esNueva = grafo.AgregarArista(ruta.Origen, ruta.Destino, ruta.Tiempo);
                    if (!esNueva)
                    {
                        int i = grafo.ObtenerIndice(ruta.Origen);
                        int j = grafo.ObtenerIndice(ruta.Destino);
                        Console.WriteLine($"[Grafo] Ruta {id} repetida entre {ruta.Origen} y {ruta.Destino}; " +
                                          $"se conserva el menor tiempo ({grafo.ObtenerPeso(i, j)} min).");
                    }
                }
                catch (ArgumentException ex)
                {
                    throw new InvalidDataException($"Ruta {id}: {ex.Message}", ex);
                }
            }

            return grafo;
        }

        private static RutaArchivo?[] LeerRutas(string rutaArchivo)
        {
            try
            {
                using JsonDocument documento = JsonDocument.Parse(File.ReadAllText(rutaArchivo));
                JsonElement lista = documento.RootElement;

                if (lista.ValueKind == JsonValueKind.Object)
                    lista = BuscarPropiedad(lista, "rutas");

                if (lista.ValueKind != JsonValueKind.Array)
                    throw new InvalidDataException(
                        "El archivo del grafo debe contener una lista de rutas o un objeto con la propiedad \"rutas\".");

                return lista.Deserialize<RutaArchivo?[]>(OpcionesJson) ?? [];
            }
            catch (JsonException ex)
            {
                throw new InvalidDataException($"El archivo del grafo no es un JSON válido: {ex.Message}", ex);
            }
        }

        private static JsonElement BuscarPropiedad(JsonElement objeto, string nombre)
        {
            foreach (JsonProperty propiedad in objeto.EnumerateObject())
            {
                if (string.Equals(propiedad.Name, nombre, StringComparison.OrdinalIgnoreCase))
                    return propiedad.Value;
            }
            return default;
        }

        private class RutaArchivo
        {
            public string Origen { get; set; } = "";
            public string Destino { get; set; } = "";
            public string Ruta { get; set; } = "";
            public int Tiempo { get; set; }
        }
    }
}
