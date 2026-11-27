using ConviTEC.Modelo;
using ConviTEC.Servicios;

namespace ConviTEC.Api
{
    // Cuerpo de POST /api/inventario/agregar
    public record AgregarInventario(string? Codigo, int Cantidad);

    public static class InventarioEndpoints
    {
        // alCambiar: se llama tras modificar el inventario para avisar a las otras sedes
        public static void MapInventario(this WebApplication app, InventarioService inventario, Func<Task> alCambiar)
        {
            // Productos del catálogo
            app.MapGet("/api/catalogo", () => Results.Ok(inventario.ObtenerProductos()));

            // Existencias de esta sede, con los datos del producto
            app.MapGet("/api/inventario", () =>
            {
                ItemInventario[] items = inventario.ObtenerTodos();
                object[] filas = new object[items.Length];

                for (int i = 0; i < items.Length; i++)
                {
                    Producto? producto = inventario.ObtenerProducto(items[i].Codigo);
                    filas[i] = new
                    {
                        codigo = items[i].Codigo,
                        nombre = producto?.Nombre ?? "",
                        categoria = producto?.Categoria ?? "",
                        precio = producto?.Precio ?? 0,
                        total = items[i].Total,
                        reservado = items[i].Reservado,
                        disponible = items[i].Disponible
                    };
                }
                return Results.Ok(filas);
            });

            // Incorporar unidades: { "codigo": "P001", "cantidad": 5 }
            app.MapPost("/api/inventario/agregar", async (AgregarInventario cuerpo) =>
            {
                try
                {
                    ItemInventario item = inventario.Agregar(cuerpo.Codigo ?? "", cuerpo.Cantidad);
                    await alCambiar();
                    return Results.Ok(item);
                }
                catch (ArgumentException ex)
                {
                    return Results.BadRequest(new { mensaje = ex.Message });
                }
            });
        }
    }
}