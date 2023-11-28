using Amazon;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Amazon.DynamoDBv2.DocumentModel;
using Amazon.DynamoDBv2.Model;
using Amazon.SQS;
using Amazon.SQS.Model;
using Compartilhado.Model;
using System.Text.Json;


namespace Compartilhado
{
    public static class AmazonUtil
    {
        public static async Task SalvarAsync(this Pedido pedido)
        {
            var client = new AmazonDynamoDBClient();
            var context = new DynamoDBContext(client);
            await context.SaveAsync(pedido);
        }

        public static T ToObject<T>(this Dictionary<string, AttributeValue> dictionary)
        {
            var client = new AmazonDynamoDBClient(RegionEndpoint.USEast1);
            var context = new DynamoDBContext(client);
            
            var doc = Document.FromAttributeMap(dictionary);
            return context.FromDocument<T>(doc);
        }

        public static async Task EnviarParaFila(EnumFilasSQS fila, Pedido pedido)
        {
            var json = JsonSerializer.Serialize(pedido);
            var client = new AmazonSQSClient();
            var queueUrlResponse = await client.GetQueueUrlAsync(fila.ToString());
            var request = new SendMessageRequest
            {
                QueueUrl = queueUrlResponse.QueueUrl,
                MessageBody = json,
            };
            await client.SendMessageAsync(request);
        }

        public static async Task EnviarParaFila(EnumFilasSNS fila, Pedido pedido)
        {
            //var json = JsonSerializer.Serialize(pedido);
            //var client = new AmazonSQSClient();
            //var queueUrlResponse = await client.GetQueueUrlAsync(fila.ToString());
            //var request = new SendMessageRequest
            //{
            //    QueueUrl = queueUrlResponse.QueueUrl,
            //    MessageBody = json,
            //};
            //await client.SendMessageAsync(request);
            await Task.CompletedTask;
        }
    }
}
