namespace ServiceBridge.Domain.Entities;

public class OrderNote : BaseEntity
{
    public Guid OrderId { get; set; }
    public Guid AuthorId { get; set; }
    public string Text { get; set; } = string.Empty;
    public bool IsVisibleToCustomer { get; set; }

    public Order Order { get; set; } = null!;
    public User Author { get; set; } = null!;
}
