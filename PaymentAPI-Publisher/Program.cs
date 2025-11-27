// See https://aka.ms/new-console-template for more information

using PaymentAPI_Publisher.Core.Interfaces;
using PaymentAPI_Publisher.Infrastructure;
using Shared;

Console.WriteLine("Hello, World!");

IRabbitMQPublisher publisher = new RabbitMQPublisher();
await publisher.ConnectAsync();


for (int i = 0; i < 1000; i++)
{
    PaymentRequest paymentRequest = new()
    {
        Amount = 15+ Random.Shared.Next(i, 10000),
        CardNumber = "15" + $" {i}",
        CreatedAt = DateTime.Now,
        OrderId = i+1
    };


    await publisher.Publish(paymentRequest);
}