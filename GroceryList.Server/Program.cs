using GroceryList.Server;
using GroceryList.Server.DataAccess.DomainModels;
using GroceryList.Shared;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Identity.Web;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

Environment.SetEnvironmentVariable("APP_BASE_DIR", AppContext.BaseDirectory, EnvironmentVariableTarget.Process);

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .CreateLogger();

builder.Host.UseSerilog(Log.Logger);

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApi(builder.Configuration.GetSection("AzureAd"));

builder.Services.SetupDatabase(builder.Configuration);

builder.Services.AddAntiforgery();

//builder.Services.AddServices();

string[] corsWhitelist =
[
    builder.Configuration.GetValue("BlazorClientBaseAddress", string.Empty),
    builder.Configuration.GetValue("BlazorClientLocalBaseAddress", string.Empty)
];
corsWhitelist = [.. corsWhitelist.Where(x => !string.IsNullOrWhiteSpace(x)).Distinct()];

bool corsEnabled = corsWhitelist.Length > 0;
if (corsEnabled)
{
    builder.Services.AddCors(options =>
    {
        options.AddPolicy(name: "AllowBlazorClient", policy =>
        {
            policy.WithOrigins(corsWhitelist)
                  .AllowAnyHeader()
                  .AllowAnyMethod()
                  .AllowCredentials();
        });
    });
}

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
}
else
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();
if (corsEnabled)
    app.UseCors("AllowBlazorClient");

app.UseAuthentication();
app.UseAuthorization();

app.UseAntiforgery();
app.MapStaticAssets();

//app.UseSerilogIngestion();
app.UseSerilogRequestLogging();

app.MapGet(ApiEndpointPaths.GetGroceryList, async (IApplicationDbContext DbContext) =>
{
    List<GroceryItem> groceryItems = await DbContext.GroceryItems.ToListAsync();

    return new GroceryListDto()
    {
        Id = Guid.NewGuid(),
        Name = "Groceries",
        Description = DateTime.Now.ToString("yyyy-MM-dd"),
        GroceryItems = groceryItems.Select(x => new GroceryItemDto()
        {
            Id = x.Id,
            Name = x.Name,
            DateCreated = x.DateCreated,
            DateModified = x.DateModified,
            IsChecked = x.IsChecked,
            Store = x.Store,
            Brand = x.Brand,
            Department = x.Department,
            Quantity = x.Quantity,
            QuantityType = x.QuantityType,
            HighPriority = x.HighPriority
        }).ToList()
    };
})
.RequireAuthorization();

app.MapPost(ApiEndpointPaths.AddGroceryItem, async (GroceryItemDto GroceryItemDto, IApplicationDbContext DbContext) =>
{
    DateTimeOffset dateCreated = DateTimeOffset.Now;

    // TODO: Check/validate for unique value
    GroceryItem groceryItem = new()
    {
        //Id = groceryItemDto.Id,
        Name = GroceryItemDto.Name.Trim(),
        DateCreated = dateCreated,
        DateModified = dateCreated,
        IsChecked = GroceryItemDto.IsChecked,
        Store = GroceryItemDto.Store.Trim(),
        Brand = GroceryItemDto.Brand.Trim(),
        Department = GroceryItemDto.Department.Trim(),
        Quantity = GroceryItemDto.Quantity, // int.Max(GroceryItemDto.Quantity, 1),
        QuantityType = GroceryItemDto.QuantityType.Trim(), // !string.IsNullOrWhiteSpace(GroceryItemDto.QuantityType) ? GroceryItemDto.QuantityType.Trim() : "Item",
        HighPriority = GroceryItemDto.HighPriority
    };

    DbContext.GroceryItems.Add(groceryItem);
    await DbContext.SaveChangesAsync();

    // TODO: Replace "value" with newly created one
    return Results.Created(ApiEndpointPaths.UpdateGroceryItem, GroceryItemDto);
})
.RequireAuthorization();

app.MapPut(ApiEndpointPaths.UpdateGroceryItem, async (GroceryItemDto GroceryItemDto, IApplicationDbContext DbContext) =>
{
    // TODO: Check/validate for unique value
    GroceryItem? groceryItem = await DbContext.GroceryItems.AsTracking().FirstOrDefaultAsync(x => x.Id == GroceryItemDto.Id);
    if (groceryItem is null)
        return Results.NotFound();

    //groceryItem.Id = groceryItemDto.Id;
    groceryItem.Name = GroceryItemDto.Name.Trim();
    //groceryItem.DateCreated = DateTimeOffset.Now;
    groceryItem.DateModified = DateTimeOffset.Now;
    groceryItem.IsChecked = GroceryItemDto.IsChecked;
    groceryItem.Store = GroceryItemDto.Store.Trim();
    groceryItem.Brand = GroceryItemDto.Brand.Trim();
    groceryItem.Department = GroceryItemDto.Department.Trim();
    groceryItem.Quantity = GroceryItemDto.Quantity; //int.Max(GroceryItemDto.Quantity, 1);
    groceryItem.QuantityType = GroceryItemDto.QuantityType.Trim(); //!string.IsNullOrWhiteSpace(GroceryItemDto.QuantityType) ? GroceryItemDto.QuantityType.Trim() : "Item";
    groceryItem.HighPriority = GroceryItemDto.HighPriority;

    await DbContext.SaveChangesAsync();

    return Results.NoContent();
})
.RequireAuthorization();

app.MapPut(ApiEndpointPaths.UpdateGroceryItemIsChecked2, async (Guid groceryItemId, bool isChecked, IApplicationDbContext dbContext) =>
{
    GroceryItem? groceryItem = await dbContext.GroceryItems.AsTracking().FirstOrDefaultAsync(x => x.Id == groceryItemId);
    if (groceryItem is null)
        return Results.NotFound();

    groceryItem.IsChecked = isChecked;
    groceryItem.DateModified = DateTimeOffset.Now;

    await dbContext.SaveChangesAsync();

    return Results.NoContent();
})
.RequireAuthorization();

app.MapDelete(ApiEndpointPaths.DeleteGroceryItem2, async (Guid groceryItemId, IApplicationDbContext dbContext) =>
{
    if (await dbContext.GroceryItems.FindAsync(groceryItemId) is GroceryItem groceryItem)
    {
        dbContext.GroceryItems.Remove(groceryItem);
        await dbContext.SaveChangesAsync();

        return Results.NoContent();
    }

    return Results.NotFound();
})
.RequireAuthorization();

await app.Services.ApplyMigrationsAndSeedDatabase();

try
{
    Log.Information("Starting up");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application start-up failed");
}
finally
{
    Log.CloseAndFlush();
}
