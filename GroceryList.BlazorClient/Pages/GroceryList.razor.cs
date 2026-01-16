using GroceryList.BlazorClient.Services;
using Microsoft.AspNetCore.Components;
using MudBlazor.Services;
using System.Net.Http.Json;

namespace GroceryList.BlazorClient.Pages;

public partial class GroceryList : IBrowserViewportObserver, IAsyncDisposable
{
    protected NewGroceryItemModel NewGroceryItem = new();
    protected string AddItemValue { get; set; } = string.Empty;
    protected string AddItemsValue { get; set; } = string.Empty;
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

    private Dictionary<string, bool> _categoryExpansionState = new();
    private bool IsGroupedByDepartment => SortByValue == "Department";
    private bool AutoCollapseCompletedDepartments = true;

    //protected bool EnableEditMode { get; set; } = false;
    //protected bool EnableGroupBy { get; set; } = false;
    protected bool IsOffline => OfflineManager.IsOffline;
    protected bool IsMobile { get; set; }
    protected bool Initialized { get; set; }

    [Inject] private IOfflineManagerService OfflineManager { get; set; } = null!;
    [Inject] private IBrowserViewportService BrowserViewportService { get; set; } = null!;
    [Inject] private UiStateService UiStateService { get; set; } = null!;
    [Inject] private IDialogService DialogService { get; set; } = null!;
    [Inject] private ISnackbar Snackbar { get; set; } = null!;
    [Inject] private HttpClient HttpClient { get; set; } = null!;

    private bool IsStoreFilterExclude { get; set; }

    private void ToggleStoreFilterMode()
    {
        if (FilterByStoreValues?.Any() == false)
            return;

        IsStoreFilterExclude = !IsStoreFilterExclude;
        ApplyFilterAndSort();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
            await BrowserViewportService.SubscribeAsync(this, fireImmediately: true);

        await base.OnAfterRenderAsync(firstRender);
    }

    public async ValueTask DisposeAsync()
    {
        UiStateService.OnChange -= StateHasChanged;

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
        await UiStateService.InitializeAsync();

        SortByValue = UiStateService.Filters.SortBy;
        FilterByStoreValues = UiStateService.Filters.FilterStores;
        FilterByDepartmentValues = UiStateService.Filters.FilterDepartments;

        UiStateService.OnChange += StateHasChanged;

        OfflineManager.NetworkConnectionStatusChanged += NetworkConnectionStatusChanged;

        Initialized = await FetchData();
    }

    private void NetworkConnectionStatusChanged(object? _, bool isOffline) => StateHasChanged();

    protected async Task<IEnumerable<string>> SearchGroceryItems(string value, CancellationToken cancellation)
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

        /*ApplyFilterAndSort();
        StateHasChanged();*/

        groceryItem.IsChecked = isChecked;

        CollapseCompletedDepartmentSections(groceryItem);

        StateHasChanged();

        try
        {
            var url = string.Format(ApiEndpointPaths.GroceryItemUpdateChecked, groceryItemId);
            var response = await HttpClient.PutAsJsonAsync(url, isChecked);
            response.EnsureSuccessStatusCode();

            await FetchData();
        }
        catch (Exception)
        {
            /*// TODO: Display "failed network popup" once offline service implemented + log failure

            // TODO: Move inside try once offline service implemented?
            groceryItem.IsChecked = !groceryItem.IsChecked;

            // offlineService.Update(entityId, propertyName, newValue)

            ApplyFilterAndSort();*/
        }

        /*groceryItem.IsChecked = isChecked;

        CollapseCompletedDepartmentSections(groceryItem);*/
    }

    private void InitializeExpansionStates()
    {
        if (!IsGroupedByDepartment || !AutoCollapseCompletedDepartments)
            return;

        foreach (var group in FilteredGroceryItems.GroupBy(x => x.Department))
        {
            if (!_categoryExpansionState.ContainsKey(group.Key))
            {
                _categoryExpansionState[group.Key] = !group.All(x => x.IsChecked);
            }
            else
            {
                _categoryExpansionState[group.Key] = !group.All(x => x.IsChecked);
            }
        }
    }

    private void CollapseCompletedDepartmentSections(GroceryItemDto groceryItem)
    {
        if (IsGroupedByDepartment && AutoCollapseCompletedDepartments) // groceryItem.IsChecked)
        {
            var deptItems = FilteredGroceryItems
                .Where(x => x.Department == groceryItem.Department)
                .ToList();

            if (deptItems.Any() && deptItems.All(x => x.IsChecked))
            {
                if (_categoryExpansionState.ContainsKey(groceryItem.Department))
                {
                    _categoryExpansionState[groceryItem.Department] = false;
                }
            }
            else if (!groceryItem.IsChecked)
            {
                _categoryExpansionState[groceryItem.Department] = true;
            }
        }

        //StateHasChanged();
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

    List<GroceryItemDto> GrocerySort(List<GroceryItemDto> filteredGroceryItems)
    {
        static int GetPrioritySequentialOrder(bool? highPriority)
        {
            if (highPriority.HasValue && highPriority.Value)
                return 1;
            else if (!highPriority.HasValue)
                return 2;

            return 3;
        }

        return SortByValue switch
        {
            // "Most Recent", "Priority", "Department", "Alphabetical" + "Default" + ...
            "Most Recent" => filteredGroceryItems
                .OrderBy(x => x.IsChecked)
                .ThenByDescending(x => x.DateModified).ToList(),
            "Priority" => filteredGroceryItems
                .OrderBy(x => x.IsChecked)
                .ThenBy(x => GetPrioritySequentialOrder(x.HighPriority))
                .ThenByDescending(x => x.DateModified).ToList(),
            "Department & Priority" => filteredGroceryItems
                .OrderBy(x => x.IsChecked)
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
    private void HandleSortChanged(string value)
    {
        Console.WriteLine("HandleSortChanged CALLED");

        SortByValue = value;
        
        ApplyFilterAndSort();
    }

    private void HandleStoreFilterChanged(IEnumerable<string> values)
    {
        Console.WriteLine("HandleStoreFilterChanged CALLED");

        FilterByStoreValues = values;
        
        ApplyFilterAndSort();
    }

    void ApplyFilterAndSort()
    {
        Console.WriteLine("ApplyFilterAndSort CALLED");

        static int GetPrioritySequentialOrder(bool? highPriority)
        {
            if (highPriority.HasValue && highPriority.Value)
                return 1;
            else if (!highPriority.HasValue)
                return 2;
            
            return 3;
        }

        if (SortByValue != UiStateService.Filters.SortBy
            || (FilterByStoreValues.Except(UiStateService.Filters.FilterStores).Any() || UiStateService.Filters.FilterStores.Except(FilterByStoreValues).Any())
            || (FilterByDepartmentValues.Except(UiStateService.Filters.FilterDepartments).Any() || UiStateService.Filters.FilterDepartments.Except(FilterByDepartmentValues).Any()))
        {
            UiStateService.UpdateFilters(SortByValue, FilterByStoreValues, FilterByDepartmentValues);
        }

        List<GroceryItemDto> filteredGroceryItems = GroceryListDto.GroceryItems;
        if (FilterByStoreValues.Any())
            //filteredGroceryItems = filteredGroceryItems.Where(x => FilterByStoreValues.Any(y => string.IsNullOrEmpty(y) || (y == x.Store) == IsStoreFilterExclude)).ToList();
            filteredGroceryItems = filteredGroceryItems = filteredGroceryItems.Where(x => IsStoreFilterExclude ? !FilterByStoreValues.Contains(x.Store) : FilterByStoreValues.Contains(x.Store)).ToList();

        /*if (FilterByDepartmentValues.Any())
            filteredGroceryItems = filteredGroceryItems.Where(x => FilterByDepartmentValues.Any(y => string.IsNullOrEmpty(y) || y == x.Department)).ToList();*/

        FilteredGroceryItems = SortByValue switch
        {
            // "Most Recent", "Priority", "Department", "Alphabetical" + "Default" + ...
            "Most Recent" => filteredGroceryItems
                .OrderBy(x => x.IsChecked)
                .ThenByDescending(x => x.DateModified).ToList(),
            "Priority" => filteredGroceryItems
                .OrderBy(x => x.IsChecked)
                .ThenBy(x => GetPrioritySequentialOrder(x.HighPriority))
                .ThenByDescending(x => x.DateModified).ToList(),
            "Department & Priority" => filteredGroceryItems
                .OrderBy(x => x.IsChecked)
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

        InitializeExpansionStates();
    }

    async Task<GroceryItemDto> OnAdd(string item = "", bool multipleItems = false)
    {
        if (multipleItems)
        {
            if (string.IsNullOrWhiteSpace(item))
                return default;

            string[] items = item.Split(",")
                .SelectMany(x => x.Trim().Split("&")
                    .SelectMany(y => y.Trim().Split("and")
                        .Select(z => z.Trim()))).ToArray();
            if (!items.Any())
                return default;

            // TODO: Bulk add items
        }

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

        DialogResult? result = await dialog.Result;

        Console.WriteLine("OpenItemDetails: result is not null = " + result is not null);
        Console.WriteLine("OpenItemDetails: result is not null && !result.Canceled = " + (result is not null && !result.Canceled));
        Console.WriteLine("OpenItemDetails: result is not null && result.Data is GroceryItemDto = " + (result is not null && result.Data is GroceryItemDto));

        if (result is not null && !result.Canceled && result.Data is GroceryItemDto updatedGroceryItem)
        {
            Console.WriteLine("OpenItemDetails: CALLING OnUpdate");
            await OnUpdate(updatedGroceryItem);
        }
    }

    protected void UpdateExpansionStates()
    {
        var departments = FilteredGroceryItems.Select(x => x.Department).Distinct();
        foreach (var dept in departments)
        {
            if (!_categoryExpansionState.ContainsKey(dept))
            {
                // Default to expanded
                _categoryExpansionState[dept] = true;
            }
        }
    }

    async Task DeleteAllCheckedItems()
    {
        bool? confirm = await DialogService.ShowMessageBox(
            "Delete Checked Items",
            "Are you sure you want to permanently remove all completed items from your list?",
            yesText: "Delete", cancelText: "Cancel");

        if (confirm == true)
        {
            var checkedIds = FilteredGroceryItems.Where(x => x.IsChecked).Select(x => x.Id).ToList();

            try
            {
                // API call - Adjust based on your backend (e.g., a BulkDelete endpoint)
                var response = await HttpClient.PostAsJsonAsync(ApiEndpointPaths.BulkDeleteGroceryItems, checkedIds);
                if (response.IsSuccessStatusCode)
                {
                    // Locally remove to update UI instantly
                    GroceryListDto.GroceryItems.RemoveAll(x => checkedIds.Contains(x.Id));
                    ApplyFilterAndSort();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("DeleteAllCheckedItems Error: " + ex);
            }
        }
    }

    private async Task<bool> FetchData()
    {
        try
        {
            var groceryListDto = await HttpClient.GetFromJsonAsync<GroceryListDto>(ApiEndpointPaths.GetGroceryList);
            ArgumentNullException.ThrowIfNull(groceryListDto);

            GroceryListDto = groceryListDto;

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

    /// <summary>
    /// Fake class to try to appease to EditForm.
    /// </summary>
    protected class NewGroceryItemModel
    {
        public string FakeValue { get; set; } = string.Empty;
    }
}
