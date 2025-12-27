namespace WebApp.Client.Services;

public interface IOfflineManagerService
{
    /*event EventHandler<bool>? NetworkConnectionStatusChanged;
    Task<bool> CheckNetworkConnectionAsync();*/
    public event EventHandler<bool> NetworkConnectionStatusChanged;
    public bool IsOffline { get; }
    public void ToggleIsOffline();
}

public class OfflineManagerService : IOfflineManagerService
{
    public event EventHandler<bool> NetworkConnectionStatusChanged;

    public bool IsOffline { get; private set; }

    public void ToggleIsOffline()
    {
        IsOffline = !IsOffline;

        NetworkConnectionStatusChanged.Invoke(this, IsOffline);
    }

    /*private readonly HttpClient _httpClient;
    private bool _isOnline;

    public OfflineManagerService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public event EventHandler<bool> NetworkConnectionStatusChanged;

    public async Task<bool> CheckNetworkConnectionAsync()
    {
        bool isOnline;
        try
        {
            isOnline = (await _httpClient.GetAsync(ApiEndpointPaths.CheckNetworkConnection)).IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            string exMessage = string.Format($"{nameof(ApiEndpointPaths.CheckNetworkConnection)} failed: {{0}}", ex);
            Console.WriteLine(exMessage);

            isOnline = false;
        }

        if (isOnline != _isOnline)
        {
            _isOnline = isOnline;
            NetworkConnectionStatusChanged.Invoke(this, isOnline);
        }
        return _isOnline;
    }*/
}
