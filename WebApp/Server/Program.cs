using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.Identity.Web;
using Microsoft.AspNetCore.OpenApi;
using WebApp.Shared;
using Microsoft.EntityFrameworkCore;
using WebApp.Server.DataAccess.DomainModels;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.DependencyInjection.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add services to the container.
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApi(builder.Configuration.GetSection("AzureAd"));

builder.Services.AddSqlServer<ApplicationDbContext>(builder.Configuration.GetConnectionString("DefaultConnection"));
//builder.Services.AddDbContext<IApplicationDbContext, ApplicationDbContext>();
builder.Services.AddScoped<IApplicationDbContext, ApplicationDbContext>();

builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();

var app = builder.Build();

// Configure the HTTP request pipeline.
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


app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();

app.UseBlazorFrameworkFiles();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapRazorPages();
app.MapControllers();
app.MapFallbackToFile("index.html");

app.MapGet(ApiEndpointPaths.GetGroceryList, async (IApplicationDbContext DbContext) =>
{
    List<GroceryItem> groceryItems = await DbContext.GroceryItems.ToListAsync();

    /*return new GroceryListDto()
    {
        Id = Guid.NewGuid(),
        Name = "Groceries",
        Description = "2023-06-12",
        GroceryItems = new()
        {
            new GroceryItemDto()
            {
                Id = Guid.NewGuid(),
                Name = "Ketchup",
                IsChecked = false,
                Brand = "Heinz",
                Department = "Dry Goods",
                Store = "Costco"
            },
            new GroceryItemDto()
            {
                Id = Guid.NewGuid(),
                Name = "Potatoes",
                IsChecked = false,
                Department = "Produce",
                Store = "Super Store",
                Quantity = 12
            },
            new GroceryItemDto()
            {
                Id = Guid.NewGuid(),
                Name = "Bread",
                IsChecked = false,
                Department = "Bakery",
                Store = "Super Store",
                HighPriority = true
            },
            new GroceryItemDto()
            {
                Id = Guid.NewGuid(),
                Name = "Milk",
                IsChecked = false,
                Department = "Dairy",
                Quantity = 8,
                QuantityType = "L"
            },
            new GroceryItemDto()
            {
                Id = Guid.NewGuid(),
                Name = "Pizza",
                IsChecked = false,
                Department = "Frozen",
                HighPriority = false
            },
            new GroceryItemDto()
            {
                Id = Guid.NewGuid(),
                Name = "Chicken",
                IsChecked = false,
                Department = "Meat",
                Quantity = 2400,
                QuantityType = "g"
            },
            new GroceryItemDto()
            {
                Id = Guid.NewGuid(),
                Name = "Chips",
                IsChecked = false,
                Department = "Dry Goods",
                HighPriority = true
            },
            new GroceryItemDto()
            {
                Id = Guid.NewGuid(),
                Name = "Toothpaste",
                IsChecked = false,
                Department = "Health & Beauty",
                Brand = "Colgate"
                // Add Type (Fresh Wave, etc)
            },
        }
        //.OrderBy(x => x.IsChecked)
        //    .ThenBy(x => x.DateAdded)
    };*/
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
//.WithName("GetWeatherForecast")
.WithOpenApi()
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
.WithOpenApi()
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
.WithOpenApi()
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
.WithOpenApi()
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
.WithOpenApi()
.RequireAuthorization();

app.Run();
