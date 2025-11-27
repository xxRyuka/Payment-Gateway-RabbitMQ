namespace Shared;

// Bu sadece veri taşıyan bir obje (DTO - Data Transfer Object)
public class PaymentRequest
{
    public int OrderId { get; set; }
    public string CardNumber { get; set; }
    public decimal Amount { get; set; }
    public DateTime CreatedAt { get; set; }
}