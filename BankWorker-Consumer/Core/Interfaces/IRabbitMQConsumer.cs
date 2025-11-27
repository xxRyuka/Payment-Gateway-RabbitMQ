namespace BankWorker_Consumer.Core.Interfaces;

public interface IRabbitMQConsumer
{
    Task ConnectAsync();
    Task ConsumeAsync(string queueName);
}