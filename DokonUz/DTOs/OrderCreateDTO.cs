namespace DokonUz.DTOs
{
     public class OrderCreateDTO
 {
 public int CustomerId { get; set; }
 public string PaymentStatus { get; set; } = "Pending";
 public ICollection<OrderItemCreateDTO>? OrderItems { get; set; }
 }
}