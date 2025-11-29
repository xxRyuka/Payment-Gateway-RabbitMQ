using System.Text;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace RecoveryServiceDLX.Infrastucture;

public class DeadLetterService
{
    private IConnection _connection;

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


    public async Task ProcessMessageAsync()
    {
        var channel = await _connection.CreateChannelAsync();

        await channel.QueueDeclareAsync(
            queue: "payment_dlq",
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null
        );


        // 3. QoS Ayarı : fairy Dispatching 
        await channel.BasicQosAsync(0, 1, false);
        Console.WriteLine("  Recovery Service: Hatalı ödemeler bekleniyor...");


        var consumer = new AsyncEventingBasicConsumer(channel);

        consumer.ReceivedAsync += async (model, ea) =>
        {
            var body = ea.Body.ToArray();
            var msg = Encoding.UTF8.GetString(body);

            Console.WriteLine($"{msg}  inceleme altında ");

            // =========================================================================
            // 🔍 MASTERCLASS BÖLÜMÜ: OTOPSİ (X-DEATH HEADER ANALİZİ)
            // =========================================================================
            // Bir mesaj DLQ'ya düştüğünde RabbitMQ ona "x-death" adında bir rapor ekler.
            // Bu raporda mesajın kaç kere öldüğü, neden öldüğü yazar.

            long failedCount = 0;

            if (ea.BasicProperties.Headers != null && ea.BasicProperties.Headers.ContainsKey("x-death"))
            {
                // x-death bir listedir (Mesaj birden fazla DLX'ten geçmiş olabilir)
                var deaths = (List<object>)ea.BasicProperties.Headers["x-death"]!;
                // En son gerçekleşen ölüm olayı listenin başındadır [0]
                var lastDeath = (Dictionary<string, object>)deaths[0];

                // 'count' değeri RabbitMQ sürümüne göre int veya long olabilir.
                // Convert.ToInt64 en güvenli yöntemdir.
                if (lastDeath.ContainsKey("count"))
                {
                    failedCount = Convert.ToInt64(lastDeath["count"]);
                }

                var reason = Encoding.UTF8.GetString((byte[])lastDeath["reason"]); // Örn: rejected
                var queue = Encoding.UTF8.GetString((byte[])lastDeath["queue"]); // Örn: payment_queue,

                Console.WriteLine($"     -> Geldiği Yer: {queue}");
                Console.WriteLine($"     -> Ölüm Sebebi: {reason}");
                Console.WriteLine($"     -> Hata Sayacı: {failedCount}");
            }
            // =========================================================================
            // 🧠 KARAR MEKANİZMASI (RETRY POLICY)
            // =========================================================================

            // KURAL: Eğer mesaj 3 kereden fazla hata aldıysa, artık uğraşma.
            if (failedCount >= 3)
            {
                Console.WriteLine(" [X] Mesaj 3 kez denendi ve başarısız oldu. İMHA EDİLİYOR.");

                //  Gerçek hayatta burada Veritabanındaki 'Logs' tablosuna kayıt atılır.
                // Insert into ErrorLogs (Msg, Reason) values (...)

                // Mesajı DLQ kuyruğundan SİL (Onayla). Artık sistemden tamamen çıkar.
                // Nack yapmıyoruz cünkü NACK yaparsak ya kuyruga gidecek yada tekrardan DLQ'ya gelecek loopa girme durumu olacak.
                // ack ile onaylayip kuyruktan cıkartıyoruz 
                await channel.BasicAckAsync(ea.DeliveryTag, false);
            }
            else
            {
                Console.WriteLine($" [R] Mesaj {failedCount}. kez hata almış. Tekrar deneniyor... ♻️");

                // SENARYO: Tekrar Ana Kuyruğa Gönder (Re-Publish)
                // Burada mesajı ana kuyruğa "Yeni Bir Mesajmış Gibi" gönderiyoruz.
                // Bu yüzden ana worker (BankWorker) onu tekrar alıp işleyecek.
                
                var props = new BasicProperties();
                props.Persistent = true;

                if (ea.BasicProperties.Headers != null && ea.BasicProperties.Headers.ContainsKey("x-death"))
                {
                    props.Headers = (Dictionary<string,object>)ea!.BasicProperties!.Headers!;
                }
                await channel.BasicPublishAsync(
                    exchange: "", // Default Exchange
                    routingKey: "payment_queue", // Hedef: Ana Kuyruk
                    mandatory: false,
                    basicProperties: props, // Özellikleri sıfırla (x-death temizlenmiş olur ama RabbitMQ onu yine takip eder)
                    body: body);



                // ESKİ MESAJI SİL
                // Yenisini gönderdiğimiz için, DLQ'daki bu eski/hatalı kopyayı silebiliriz.
                await channel.BasicAckAsync(ea.DeliveryTag, false);

                Console.WriteLine(" [√] Ana kuyruğa başarıyla transfer edildi.");

                // Biraz bekle ki konsolda görelim (Simülasyon)
                await Task.Delay(2000);
            }

        };
        
            await channel.BasicConsumeAsync("payment_dlq", false, consumer);
            await Task.Delay(-1);

    }
}