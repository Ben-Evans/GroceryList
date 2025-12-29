namespace GroceryList.Shared;

public static class ApiEndpointPaths
{
    public const string GetGroceryList = "/api/GroceryList";

    public const string AddGroceryItem = "/api/GroceryItem";
    public const string UpdateGroceryItem = "/api/GroceryItem";
    public const string UpdateGroceryItemIsChecked = "/api/GroceryItem2/{0}"; // groceryItemId
    public const string UpdateGroceryItemIsChecked2 = "/api/GroceryItem2/{groceryItemId}"; // groceryItemId
    public const string DeleteGroceryItem = "/api/GroceryItem/{0}"; // groceryItemId
    public const string DeleteGroceryItem2 = "/api/GroceryItem/{groceryItemId}"; // groceryItemId

    public const string CheckNetworkConnection = "/api/Developer/CheckNetworkConnection";
}
