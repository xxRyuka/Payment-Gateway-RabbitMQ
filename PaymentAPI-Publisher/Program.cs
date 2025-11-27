// See https://aka.ms/new-console-template for more information

using PaymentAPI_Publisher.Core.Interfaces;
using PaymentAPI_Publisher.Infrastructure;
using Shared;

Console.WriteLine("Hello, World!");

IRabbitMQPublisher publisher = new RabbitMQPublisher();
await publisher.ConnectAsync();


PaymentRequest paymentRequest = new()
{
    Amount = 15,
    CardNumber = "15",
    CreatedAt = DateTime.Now,
    OrderId = 2
};
await publisher.Publish(paymentRequest);