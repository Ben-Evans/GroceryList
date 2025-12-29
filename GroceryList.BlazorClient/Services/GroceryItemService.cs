namespace GroceryList.BlazorClient.Services;

/*public interface IGroceryItemService
{
    public Task FetchData();
}

public class GroceryItemService : IGroceryItemService
{
    private readonly IOfflineManagerService _offlineService;
    private GroceryListDto _groceryListDto;

    public GroceryItemService(IOfflineManagerService offlineService)
    {
        _offlineService = offlineService;
    }

    public async Task FetchData()
    {
        GroceryListDto = await HttpClient.GetFromJsonAsync<GroceryListDto>(ApiEndpointPaths.GetGroceryList);

        // TODO: Replace with separate API call
        AllKnownGroceryItems = GroceryListDto.GroceryItems.Select(x => x.Name).Distinct().ToArray();
    }
}*/
