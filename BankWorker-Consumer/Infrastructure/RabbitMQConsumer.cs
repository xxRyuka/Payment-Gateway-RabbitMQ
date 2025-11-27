using System.Globalization;
using System.Text;
using System.Text.Json.Nodes;
using BankWorker_Consumer.Core.Interfaces;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace BankWorker_Consumer.Infrastructure;

public class RabbitMQConsumer : IRabbitMQConsumer
{
    private IConnection _connection;

    public async Task ConnectAsync()
    {
        var fac = new ConnectionFactory()
        {
            HostName = "localhost",
            UserName = "guest",
            Password = "guest",
        };

        _connection = await fac.CreateConnectionAsync();
    }

    public async Task ConsumeAsync(string queueName)
    {
        // burda using kullanmıyoruz cünkü kanalın acık kalması gerekiyor dinlemede olması için 
        var channel = await _connection.CreateChannelAsync();

        await channel.QueueDeclareAsync(
            queue: queueName,
            autoDelete: false,
            durable: true,
            exclusive: false,
            arguments: null);


        
        
    //BasicQos ile rabbitMQ'nun default olan Round-Robin Dispatch modunu
    //Fairy Dispatch ile değiştiriyoruz 
    
    // Yani bu şekilde sıra sıra değilde elindeki iş sayısı (prefetchCount) kadar olabilir en fazla 
    
    // Sıra sıra dagıtılan durumda bir goren 5dk diger görev 25dk sürebilirdi buda yük dengeleme konusunda problemli bir yaklasim 
    
        // --- YENİ EKLENEN KISIM: QoS Ayarı ---
        // prefetchSize: 0 (Mesaj boyutu limiti yok)
        // prefetchCount: 1 (Aynı anda sadece 1 iş ver)
        // global: false (Bu ayar sadece bu kanal için geçerli)
        await channel.BasicQosAsync(prefetchSize: 0, prefetchCount: 1, global: false);


        Console.WriteLine($" @@@ {queueName} Kanalından Mesaj bekleniyor");


        //bir consumer nesnesi olusturuyorum 
        var consumer = new AsyncEventingBasicConsumer(channel);

        consumer.ReceivedAsync += async (model, ea) =>
        {
            //Byte olarak tasınıyordu mesajlar tekrar byte olarak aldım ve alt satırda jsona cevirelim
            var body = ea.Body.ToArray();

            var msg = Encoding.UTF8.GetString(body);

            Console.WriteLine($"  @@@ {queueName}'den alınan mesaj : {msg}");

            // Simüle ettiğimiz için delay koydum
            await Task.Delay(500);
            //
            // // Fairy Dispatch'i denemek için rnd degerlrden olusan delay olsuturalım 
            //
            // await Task.Delay(Random.Shared.Next(1, 15000));
            await channel.BasicAckAsync(deliveryTag: ea.DeliveryTag, multiple: false);
        };


        await channel.BasicConsumeAsync(
            queue: queueName, // Dinlenecek kuyruk 
            autoAck: false, // otomatik onay KAPALI
            consumer: consumer // mesajlar kime teslim edilecek ? => bizim olusturdugumuz EventingBasicConsumer nesnesine iletilecek
        );

        await Task.Delay(-1);
    }
}