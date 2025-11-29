using System.Text;
using Newtonsoft.Json;
using PaymentAPI_Publisher.Core.Interfaces;
using RabbitMQ.Client;
using Shared;

namespace PaymentAPI_Publisher.Infrastructure;

public class RabbitMQPublisher : IRabbitMQPublisher
{
    private IConnection _connection;

    private const string QueueName = "payment_queue";
    public async Task ConnectAsync()
    {
        ConnectionFactory factory = new ConnectionFactory()
        {
            HostName = "localhost",
            UserName = "guest",
            Password = "guest"
        };

        _connection = await factory.CreateConnectionAsync();
    }


    public async Task Publish(PaymentRequest paymentRequest)
    {
        // Kanal (Tünel) Oluşturma: Bağlantı üzerinden hafif bir kanal açıyoruz.
        await using var channel = await _connection.CreateChannelAsync();

        
        Dictionary<string, object> dict = new Dictionary<string, object>();
        dict.Add("x-dead-letter-exchange", "payment_dlx");


        // A. KUYRUK GARANTİSİ (DURABILITY)
        // Eğer bu kuyruk yoksa oluştur, varsa olduğu gibi kullan.
        // durable: true -> RabbitMQ resetlense bile kuyruk silinmesin.
        await channel.QueueDeclareAsync(
            queue: QueueName,
            durable: true, // => durable True yaparak kuyrugun silinmesini engelliyoruz 
            exclusive: false,
            autoDelete: false,
            arguments: dict!);


        // B. MESAJ ETİKETLEME (PERSISTENCE)
        // Mesajın özelliklerini (metadata) oluşturuyoruz.
        var props = new BasicProperties();

        // Mesajı diske kaydet! (RAM'de tutma sadece)
        props.Persistent = true; // => Persistent : True yaparak mesajında kalıcılığını saglıyoruz


        // C. VERİ DÖNÜŞÜMÜ (SERIALIZATION)
        // C# Nesnesi -> JSON String -> Byte Dizisi

        // Mesajı Önce JSON formatına sonrada byte dizisine çeviriyoruz 
        var jsonString = JsonConvert.SerializeObject(paymentRequest);
        var body = Encoding.UTF8.GetBytes(jsonString);


        // D. GÖNDERME (PUBLISH)
        // exchange: "" -> Varsayılan değişim (direkt kuyruğa atar) : Simdilik exchangeler ile ilgilenmiyoruz 
        // routingKey: "payment_queue" -> Hangi kuyruğa gidecek?
        await channel.BasicPublishAsync(
            exchange: "", // default exchange ile ugrasiyoruz 
            routingKey: QueueName,
            mandatory: false,
            basicProperties: props,
            body: body);

        // Konsola bilgi verelim ki çalıştığını görelim
        Console.WriteLine($" [x] Kuyruğa Gönderildi: Sipariş No {paymentRequest.OrderId}");
    }
}