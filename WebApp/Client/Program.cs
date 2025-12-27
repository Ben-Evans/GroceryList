using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MudBlazor.Services;
using Syncfusion.Blazor;
using WebApp.Client;
using WebApp.Client.Services;

//Syncfusion.Licensing.SyncfusionLicenseProvider.RegisterLicense("MjM5Nzc0NkAzMjMxMmUzMDJlMzBZUzd3dWlBWG9Pc2ZqMXBOcWlUdEV1S09rNDRSNjlIZmNWdUhJM3Q4T0p3PQ==");

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddHttpClient("WebApp.ServerAPI", client => client.BaseAddress = new Uri(builder.HostEnvironment.BaseAddress))
    .AddHttpMessageHandler<BaseAddressAuthorizationMessageHandler>();

// Supply HttpClient instances that include access tokens when making requests to the server project
builder.Services.AddScoped(sp => sp.GetRequiredService<IHttpClientFactory>().CreateClient("WebApp.ServerAPI"));

builder.Services.AddSingleton<IOfflineManagerService, OfflineManagerService>();

builder.Services.AddMsalAuthentication(options =>
{
    builder.Configuration.Bind("AzureAd", options.ProviderOptions.Authentication);
    options.ProviderOptions.DefaultAccessTokenScopes.Add(builder.Configuration.GetSection("ServerApi")["Scopes"]);
});

//builder.Services.AddSyncfusionBlazor();
builder.Services.AddMudServices(); // Updated 6.4.1 => 6.11.0

await builder.Build().RunAsync();
