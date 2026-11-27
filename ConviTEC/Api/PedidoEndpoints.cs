using ConviTEC.Modelo;
using ConviTEC.Servicios;

namespace ConviTEC.Api
{
    // Cuerpo de POST /api/pedidos:
    // { "destino": "Cartago", "items": [ { "codigoProducto": "P001", "cantidad": 2 } ] }
    public record NuevoPedido(string? Destino, ItemCantidad[]? Items);

    public static class PedidoEndpoints
    {
        public static void MapPedidos(this WebApplication app, PedidoService pedidos)
        {
            // Siempre responde 200 con "aceptado" y un "mensaje" claro (confirmación o motivo del rechazo)
            app.MapPost("/api/pedidos", async (NuevoPedido cuerpo) =>
            {
                Pedido pedido = await pedidos.RegistrarAsync(cuerpo.Destino ?? "", cuerpo.Items ?? []);
                bool aceptado = pedido.Estado == EstadoPedido.Confirmado;

                return Results.Ok(new
                {
                    aceptado,
                    mensaje = aceptado ? $"Pedido {pedido.Id} confirmado." : pedido.Motivo,
                    pedido
                });
            });

            // Historial de la sede
            app.MapGet("/api/pedidos", () => Results.Ok(pedidos.ObtenerPedidos()));

            // Confirmados que esperan entrega
            app.MapGet("/api/pedidos/pendientes",
                () => Results.Ok(pedidos.ObtenerPorEstado(EstadoPedido.Confirmado)));
        }
    }
}