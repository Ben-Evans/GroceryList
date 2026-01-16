namespace GroceryList.Shared;

public static class ApiEndpointPaths
{
    public const string GetGroceryList = "/api/grocerylist";

    public const string AddGroceryItem = "/api/groceryitem";
    public const string UpdateGroceryItem = "/api/groceryitem";
    public const string GroceryItemUpdateChecked = "/api/groceryitems/{0}/ischecked";
    public const string DeleteGroceryItem = "/api/groceryitem/{0}"; // groceryItemId
    public const string DeleteGroceryItem2 = "/api/groceryitem/{groceryItemId}"; // groceryItemId
    public const string BulkDeleteGroceryItems = "/api/groceryitem/{groceryItemId}"; // groceryItemId

    public const string CheckNetworkConnection = "/api/developer/checknetworkconnection";
}
