// See https://aka.ms/new-console-template for more information

using BankWorker_Consumer.Core.Interfaces;
using BankWorker_Consumer.Infrastructure;

IRabbitMQConsumer consumer = new RabbitMQConsumer(); 

await consumer.ConnectAsync();
string name = "payment_queue";
await consumer.ConsumeAsync(queueName:name);