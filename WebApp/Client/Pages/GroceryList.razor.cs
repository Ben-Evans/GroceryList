using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using MudBlazor.Services;
using System.Net.Http.Json;
using WebApp.Client.Services;

namespace WebApp.Client.Pages;

public partial class GroceryList : IBrowserViewportObserver, IAsyncDisposable
{
    protected string AddItemValue { get; set; } = string.Empty;
    protected GroceryListDto GroceryListDto { get; set; } = null!;
    protected List<GroceryItemDto> FilteredGroceryItems { get; set; }
    protected IReadOnlyList<string> AllKnownGroceryItems { get; set; }
    protected IReadOnlyList<string> QuickAddItems { get; set; } = new[] { "Apples", "Bananas", "Grapes", "Kleenex", "Toilet Paper", "Milk", "Cream", "Butter", "Beef", "Chicken", "Peanut Butter", "Jam", "Tomato Sauce" };
    protected string SortByValue { get; set; } = "Most Recent";
    protected IEnumerable<string> FilterByStoreValues { get; set; } = new List<string>();
    protected IEnumerable<string> FilterByDepartmentValues { get; set; } = new List<string>();

    // TODO: if sorting by Department have refrigerated last
    // TODO: Add another department for laundry/dishwasher or rename household/add a new department for appliance, electronics, clothes etc
    protected string[] ListSortValues { get; set; } = new[] { "Most Recent", "Priority", "Department", "Alphabetical" };
    protected string[] ListStoreValues { get; set; } = new[] { "Costco", "Super Store", "Shoppers Drug Mart", "Amazon", "Best Buy", "Canadian Tire", "Home Depot", "Marks", "Sport Chek", "PetSmart", "Other" };
    protected string[] ListDepartmentValues { get; set; } = new[] { "Produce", "Dry Goods", "Beverages", "Baking", "Frozen", "Dairy", "Bakery", "Meat", "Deli", "Seafood", "Household", "Health & Beauty", "Pet", "Alcohol", "Other" };

    //protected bool EnableEditMode { get; set; } = false;
    //protected bool EnableGroupBy { get; set; } = false;
    protected bool IsOffline => OfflineManager.IsOffline;
    protected bool IsMobile { get; set; }
    protected bool Initialized { get; set; }

    [Inject] private IOfflineManagerService OfflineManager { get; set; } = null!;
    [Inject] private IBrowserViewportService BrowserViewportService { get; set; } = null!;
    [Inject] private IDialogService DialogService { get; set; } = null!;
    [Inject] private HttpClient HttpClient { get; set; } = null!;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
            await BrowserViewportService.SubscribeAsync(this, fireImmediately: true);

        await base.OnAfterRenderAsync(firstRender);
    }

    public async ValueTask DisposeAsync()
    {
        OfflineManager.NetworkConnectionStatusChanged -= NetworkConnectionStatusChanged;

        await BrowserViewportService.UnsubscribeAsync(this);
    }

    Guid IBrowserViewportObserver.Id { get; } = Guid.NewGuid();

    ResizeOptions IBrowserViewportObserver.ResizeOptions { get; } = new()
    {
        ReportRate = 250,
        NotifyOnBreakpointOnly = true
    };

    Task IBrowserViewportObserver.NotifyBrowserViewportChangeAsync(BrowserViewportEventArgs browserViewportEventArgs)
    {
        IsMobile = browserViewportEventArgs.Breakpoint < Breakpoint.Md;

        return InvokeAsync(StateHasChanged);
    }

    protected override async Task OnInitializedAsync()
    {
        OfflineManager.NetworkConnectionStatusChanged += NetworkConnectionStatusChanged;

        Initialized = await FetchData();
    }

    private void NetworkConnectionStatusChanged(object? _, bool isOffline) => StateHasChanged();

    async Task<IEnumerable<string>> SearchGroceryItems(string value)
    {
        // if text is null or empty, show complete list
        if (string.IsNullOrEmpty(value))
            return AllKnownGroceryItems;

        return AllKnownGroceryItems.Where(x => x.Contains(value, StringComparison.InvariantCultureIgnoreCase));
    }

    async Task OnDelete(GroceryItemDto groceryItem)
    {
        //await DialogService.ShowMessageBox("Are you sure you want to delete this item?");

        /*DialogParameters parameters = new()
        {
            { "ContentText", "Do you really want to delete these records? This process cannot be undone." },
            { "ButtonText", "Delete" },
            { "Color", Color.Error }
        };

        DialogOptions options = new() { CloseButton = true, MaxWidth = MaxWidth.ExtraSmall };

        DialogService.Show<T>("Delete", parameters, options);*/

        /*
        bool? result = await DialogService.ShowMessageBox(
            "Warning", 
            "Deleting can not be undone!", 
            yesText:"Delete!", cancelText:"Cancel");
        state = result == null ? "Canceled" : "Deleted!";
        StateHasChanged();*/

        Guid groceryItemId = groceryItem.Id;
        try
        {
            var response = await HttpClient.DeleteAsync(string.Format(ApiEndpointPaths.DeleteGroceryItem, groceryItemId));
            response.EnsureSuccessStatusCode();

            await FetchData();
        }
        catch (Exception)
        {
            throw;
        }
    }

    async Task OnUpdate(GroceryItemDto groceryItem)
    {
        try
        {
            var response = await HttpClient.PutAsJsonAsync(ApiEndpointPaths.UpdateGroceryItem, groceryItem);
            response.EnsureSuccessStatusCode();

            await FetchData();
        }
        catch (Exception)
        {
            throw;
        }
    }

    async Task OnCheck(GroceryItemDto groceryItem)
    {
        Guid groceryItemId = groceryItem.Id;
        bool isChecked = !groceryItem.IsChecked;
        try
        {
            var response = await HttpClient.PutAsJsonAsync(string.Format(ApiEndpointPaths.UpdateGroceryItemIsChecked, groceryItemId), isChecked);
            response.EnsureSuccessStatusCode();

            await FetchData();
        }
        catch (Exception)
        {
            // TODO: Display "failed network popup" once offline service implemented + log failure

            // TODO: Move inside try once offline service implemented?
            groceryItem.IsChecked = !groceryItem.IsChecked;

            // offlineService.Update(entityId, propertyName, newValue)

            ApplyFilterAndSort();
        }
    }

    void ApplyFilterAndSort()
    {
        static int GetPrioritySequentialOrder(bool? highPriority)
        {
            if (highPriority.HasValue && highPriority.Value)
                return 1;
            else if (!highPriority.HasValue)
                return 2;
            
            return 3;
        }

        static int GetDepartmentSequentialOrder(string department)
        {
            Dictionary<string, int> departments = new()
            {
                { "Produce", 1 },
                { "Dry Goods", 7 },
                { "Beverages", 4 },
                { "Baking", 8 },
                { "Frozen", 10 },
                { "Dairy", 12 },
                { "Bakery", 2 },
                { "Meat", 13 },
                { "Deli", 14 },
                { "Seafood", 11 },
                { "Household", 5 },
                { "Health & Beauty", 9 },
                { "Pet", 3 },
                { "Alcohol", 15 },
                { "Other", 6 }
            };

            return departments.TryGetValue(department, out int sequence) ? sequence : int.MaxValue;
        }

        List<GroceryItemDto> filteredGroceryItems = GroceryListDto.GroceryItems;
        if (FilterByStoreValues.Any())
            filteredGroceryItems = filteredGroceryItems.Where(x => FilterByStoreValues.Any(y => y == x.Store)).ToList();
        if (FilterByDepartmentValues.Any())
            filteredGroceryItems = filteredGroceryItems.Where(x => FilterByDepartmentValues.Any(y => y == x.Department)).ToList();

        FilteredGroceryItems = SortByValue switch
        {
            // "Most Recent", "Priority", "Department", "Alphabetical" + "Default" + ...
            "Most Recent" => filteredGroceryItems
                .OrderBy(x => !x.IsChecked)
                .ThenByDescending(x => x.DateModified).ToList(),
            "Priority" => filteredGroceryItems
                .OrderBy(x => !x.IsChecked)
                .ThenBy(x => GetPrioritySequentialOrder(x.HighPriority))
                .ThenByDescending(x => x.DateModified).ToList(),
            "Department & Priority" => filteredGroceryItems
                .OrderBy(x => !x.IsChecked)
                .ThenBy(x => GetDepartmentSequentialOrder(x.Department))
                .ThenBy(x => GetPrioritySequentialOrder(x.HighPriority))
                .ThenByDescending(x => x.DateModified).ToList(),
            "Department" => filteredGroceryItems
                .OrderBy(x => x.IsChecked)
                .ThenBy(x => GetDepartmentSequentialOrder(x.Department))
                .ThenByDescending(x => x.DateModified).ToList(),
            "Alphabetical" => filteredGroceryItems
                .OrderBy(x => x.IsChecked)
                .ThenBy(x => x.Name)
                .ThenByDescending(x => x.DateModified).ToList(),
            _ => throw new ArgumentOutOfRangeException(nameof(SortByValue), $"Unexpected sort by value: {SortByValue}"),
        };
    }

    async Task<GroceryItemDto> OnAdd(string item = "")
    {
        item = (!string.IsNullOrWhiteSpace(item) ? item : AddItemValue).Trim();

        GroceryItemDto groceryItemDto = new()
        {
            Id = Guid.Empty,
            Name = item,
            IsChecked = false
        };

        AddItemValue = string.Empty;

        try
        {
            var response = await HttpClient.PostAsJsonAsync(ApiEndpointPaths.AddGroceryItem, groceryItemDto);
            response.EnsureSuccessStatusCode();
            
            await FetchData();
        }
        catch (Exception)
        {
            throw;
        }

        return groceryItemDto;
    }

    async Task OpenItemDetails(GroceryItemDto groceryItem)
    {
        DialogParameters parameters = new()
        {
            { nameof(GroceryItemDetails.GroceryItem), groceryItem },
            { nameof(GroceryItemDetails.OnSearchGroceryItems), SearchGroceryItems }
        };

        IDialogReference dialog = await DialogService.ShowAsync<GroceryItemDetails>("Item Details", parameters);

        DialogResult result = await dialog.Result;
        if (!result.Canceled && result.Data is GroceryItemDto updatedGroceryItem)
        {
            await OnUpdate(updatedGroceryItem);

            await FetchData();
        }
    }

    private async Task<bool> FetchData()
    {
        try
        {
            GroceryListDto = await HttpClient.GetFromJsonAsync<GroceryListDto>(ApiEndpointPaths.GetGroceryList);
            ArgumentNullException.ThrowIfNull(GroceryListDto);

            // TODO: Replace with separate API call
            AllKnownGroceryItems = GroceryListDto.GroceryItems.Select(x => x.Name).Distinct().ToArray();

            ApplyFilterAndSort();

            return true;
        }
        catch (Exception)
        {
            if (!IsOffline)
                throw;

            // TODO: Get locally cached offline data
            ApplyFilterAndSort();

            return true;
        }
    }
}
