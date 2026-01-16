using Blazored.LocalStorage;
using GroceryList.BlazorClient;
using GroceryList.BlazorClient.Services;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MudBlazor.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddTransient<CustomAuthorizationMessageHandler>();

builder.Services.AddHttpClient("Default", client => client.BaseAddress = new Uri(builder.Configuration.GetValue("ApiBaseAddress", string.Empty)))
    .AddHttpMessageHandler<CustomAuthorizationMessageHandler>();

builder.Services.AddScoped(sp => sp.GetRequiredService<IHttpClientFactory>().CreateClient("Default"));

builder.Services.AddBlazoredLocalStorage();
builder.Services.AddSingleton<IOfflineManagerService, OfflineManagerService>();
builder.Services.AddScoped<UiStateService>();

builder.Services.AddMsalAuthentication(options =>
{
    builder.Configuration.Bind("AzureAd", options.ProviderOptions.Authentication);

    options.ProviderOptions.DefaultAccessTokenScopes.Add(builder.Configuration.GetValue("ApiAuthScopes", string.Empty));
});

builder.Services.AddMudServices();

await builder.Build().RunAsync();
