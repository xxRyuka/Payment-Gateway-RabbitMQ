using RabbitMQ.Client;
using Shared;

namespace PaymentAPI_Publisher.Core.Interfaces;

public interface IRabbitMQPublisher
{
    // Program.cs sadece bu metodu görecek.
    // Bağlantı detayı yok, kanal ayarı yok. Sadece "Yayınla".
    Task Publish(PaymentRequest paymentRequest);
    
    Task ConnectAsync();
}