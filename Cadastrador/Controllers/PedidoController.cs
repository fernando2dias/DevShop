using Compartilhado;
using Compartilhado.Model;
using Microsoft.AspNetCore.Mvc;

namespace Cadastrador.Controllers;

[Route("api/[controller]")]
public class PedidoController : ControllerBase
{
    [HttpPost]
    public async Task PostAsync([FromBody] Pedido pedido)
    {
        pedido.Id = Guid.NewGuid().ToString();
        pedido.DataDeCriacao = DateTime.Now;

        await pedido.SalvarAsync();

        Console.WriteLine($"Pedido salvo com sucesso: id {pedido.Id}");
    }
}
