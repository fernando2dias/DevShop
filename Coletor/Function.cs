using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Amazon.DynamoDBv2.Model;
using Amazon.Lambda.Core;
using Amazon.Lambda.DynamoDBEvents;
using Compartilhado;
using Compartilhado.Model;

[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace Coletor;

public class Function
{
    public async Task FunctionHandler(DynamoDBEvent dynamoEvent, ILambdaContext context)
    {
        //context.Logger.LogInformation($"Beginning to process {dynamoEvent.Records.Count} records...");

        foreach (var record in dynamoEvent.Records)
        {
            // context.Logger.LogInformation($"Event ID: {record.EventID}");
            // context.Logger.LogInformation($"Event Name: {record.EventName}");

            if (record.EventName == "INSERT")
            {

                var pedido = record.Dynamodb.NewImage.ToObject<Pedido>();
                pedido.Status = StatusDoPedido.Coletado;
                
                try
                {
                    await ProcessarValorDoPedido(pedido);
                    await AmazonUtil.EnviarParaFila(EnumFilasSQS.pedido, pedido);
                    context.Logger.LogLine($"Sucesso na coleta do pedido: {pedido.Id}");
                    //Adicionar na fila pedido
                }
                catch (Exception e)
                {
                    context.Logger.LogLine($"Erro: {e.Message}");
                    pedido.JustificativaDeCancelamento = e.Message;
                    pedido.Cancelado = true;
                    await AmazonUtil.EnviarParaFila(EnumFilasSNS.falha, pedido);
                    //Adicionar a fila de falha
                }

               //salvar o pedido
               await pedido.SalvarAsync();
            }
        }

        //  context.Logger.LogInformation("Stream processing complete.");
    }

    private async Task ProcessarValorDoPedido(Pedido pedido)
    {
        foreach (var produto in pedido.Produtos)
        {
            var produtoDoEstoque = await ObterProdutoDoDynamoDBAsync(produto.Id);
            if (produtoDoEstoque == null)
            {
                throw new InvalidOperationException($"Produto não encontrado na tabela estoque. {produto.Id}");
            }

            produto.Valor = produtoDoEstoque.Valor;
            produto.Nome = produtoDoEstoque.Nome;
        }

        var valorTotal = pedido.Produtos.Sum(x => x.Valor * x.Quantidade);
        if (pedido.ValorTotal != 0 && pedido.ValorTotal != valorTotal)
        {
            throw new InvalidOperationException($"O valor esperado do pedido é de R$ {pedido.ValorTotal}");
        }

        pedido.ValorTotal = valorTotal;
    }

    private async Task<Produto> ObterProdutoDoDynamoDBAsync(string id)
    {
        var client = new AmazonDynamoDBClient();
        var request = new QueryRequest
        {
            TableName = "estoque",
            KeyConditionExpression = "Id = :v_id",
            ExpressionAttributeValues = new Dictionary<string, AttributeValue>
            {
                {
                 ":v_id", new AttributeValue
                 {
                     S = id
                 }
                }
            }
        };

        var response = await client.QueryAsync(request);
        var item = response.Items.FirstOrDefault();

        if (item == null)  return null; 

        return item.ToObject<Produto>();     
    }
}