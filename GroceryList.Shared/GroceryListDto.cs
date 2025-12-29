namespace GroceryList.Shared;

public class GroceryListDto
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public List<GroceryItemDto> GroceryItems { get; set; } = new();
}
