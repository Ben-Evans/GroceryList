using Blazored.LocalStorage;

namespace GroceryList.BlazorClient.Services;

public class UiStateService(ILocalStorageService _localStorage)
{
    public event Action? OnChange;

    private bool _isShoppingMode;
    public bool IsShoppingMode
    {
        get => _isShoppingMode;
        set { _isShoppingMode = value; SaveAndNotify(); }
    }

    public UserFilterPreferences Filters { get; private set; } = new();

    public async Task InitializeAsync()
    {
        _isShoppingMode = await _localStorage.GetItemAsync<bool>("isShoppingMode");
        var savedFilters = await _localStorage.GetItemAsync<UserFilterPreferences>("userFilters");
        if (savedFilters is not null)
            Filters = savedFilters;

        NotifyStateChanged();
    }

    public void UpdateFilters(string sortBy, IEnumerable<string> stores, IEnumerable<string> depts)
    {
        Console.WriteLine("UPDATE FILTERS: " + sortBy + " | stores: " + string.Join(", ", stores)  + " | depts: " + string.Join(", ", depts));

        Filters.SortBy = sortBy;
        Filters.FilterStores = stores.ToList();
        Filters.FilterDepartments = depts.ToList();
        SaveAndNotify();
    }

    private void SaveAndNotify()
    {
        _ = _localStorage.SetItemAsync("isShoppingMode", _isShoppingMode);
        _ = _localStorage.SetItemAsync("userFilters", Filters);
        NotifyStateChanged();
    }

    private void NotifyStateChanged() => OnChange?.Invoke();
}

public class UserFilterPreferences
{
    public string SortBy { get; set; } = "Most Recent";
    public List<string> FilterStores { get; set; } = new();
    public List<string> FilterDepartments { get; set; } = new();
}
