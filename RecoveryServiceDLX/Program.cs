using RecoveryServiceDLX.Infrastucture;

DeadLetterService dlq =  new DeadLetterService();

await dlq.ConnectAsync();
await dlq.ProcessMessageAsync();