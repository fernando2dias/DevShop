using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Amazon.Lambda.Core;
using Amazon.Lambda.SQSEvents;
using Compartilhado;
using Compartilhado.Model;
using System.Text.Json;

[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace Reservador;

public class Function
{
    private readonly AmazonDynamoDBClient _client;
    private readonly string _tableName = "estoque";

    public Function()
    {
        _client = new AmazonDynamoDBClient();
    }

    public async Task FunctionHandler(SQSEvent evnt, ILambdaContext context)
    {
        // validar se veio mais de uma mensagem... se vier mais de uma ela deve retornar a fila
        if (evnt.Records.Count > 1) throw new InvalidOperationException("Somente uma mensagem pode ser tratada por vez");

        var message = evnt.Records.FirstOrDefault();

        // tratando se a mensagem for nula
        if (message == null) return;

        await ProcessMessageAsync(message, context);
    }

    private async Task ProcessMessageAsync(SQSEvent.SQSMessage message, ILambdaContext context)
    {
        var pedido = JsonSerializer.Deserialize<Pedido>(message.Body);
        pedido.Status = StatusDoPedido.Reservado;

        foreach (var produto in pedido.Produtos)
        {
            try
            {
                await BaixarEstoque(produto.Id, produto.Quantidade);
                produto.Reservado = true;
                context.Logger.LogInformation($"Produto baixado do estoque {produto.Id} - {produto.Nome}");
            }
            catch (ConditionalCheckFailedException)
            {
                pedido.JustificativaDeCancelamento = $"Produto indisponivel no estoque {produto.Id} {produto.Nome}";
                pedido.Cancelado = true;
                context.Logger.LogInformation($"Erro: {pedido.JustificativaDeCancelamento}");
                break;
            }

        }
        if (pedido.Cancelado)
        {
            foreach (var produto in pedido.Produtos)
            {
                if (produto.Reservado)
                {
                    await DevolverAoEstoque(produto.Id, produto.Quantidade);
                }
            }

            await AmazonUtil.EnviarParaFila(EnumFilasSNS.falha, pedido);
            await pedido.SalvarAsync();
        }
        else
        {
            await AmazonUtil.EnviarParaFila(EnumFilasSQS.reservado, pedido);
            await pedido.SalvarAsync();
        }
    }

    private Task DevolverAoEstoque(string id, int quantidade)
    {
        throw new NotImplementedException();
    }

    private Task BaixarEstoque(string id, int quantidade)
    {
        throw new NotImplementedException();
    }
}