using Microsoft.AspNetCore.Components.WebAssembly.Authentication;

namespace GroceryList.BlazorClient;

public class CustomAuthorizationMessageHandler : DelegatingHandler
{
    private readonly IAccessTokenProvider _tokenProvider;
    private readonly string[] _scopes;

    public CustomAuthorizationMessageHandler(
        IAccessTokenProvider tokenProvider,
        IConfiguration configuration)
    {
        _tokenProvider = tokenProvider;
        _scopes = new[] { configuration.GetValue<string>("ApiAuthScopes") };
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var tokenResult = await _tokenProvider.RequestAccessToken(
            new AccessTokenRequestOptions { Scopes = _scopes });

        if (tokenResult.TryGetToken(out var token))
        {
            request.Headers.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token.Value);
        }

        return await base.SendAsync(request, cancellationToken);
    }
}
