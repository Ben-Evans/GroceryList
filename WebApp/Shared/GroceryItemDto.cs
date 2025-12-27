namespace WebApp.Shared;

public class GroceryItemDto
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;
    public DateTimeOffset DateCreated { get; set; }
    public DateTimeOffset DateModified { get; set; }
    // TODO: Change into => DateTimeOffset? DateIsChecked
    public bool IsChecked { get; set; }
    public string Store { get; set; } = string.Empty;
    public string Brand { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public string QuantityType { get; set; } = string.Empty;
    public bool? HighPriority { get; set; }
}

//public record CheckedGroceryItemDto(Guid Id, bool IsChecked);
