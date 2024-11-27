using Service.Implementations;
using Service.interfaces;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// Register ReboxService
builder.Services.AddHttpClient<IWebhookService, WebhookService>();

// Add session services
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// Register AIServiceFactory
builder.Services.AddSingleton<AIServiceFactory>(sp =>
{
    var factoryInfoPath = Path.Combine(builder.Environment.ContentRootPath, "App_Data", "factory_info.json");
    var inventoryPath = Path.Combine(builder.Environment.ContentRootPath, "App_Data", "inventory.json");
    var ordersPath = Path.Combine(builder.Environment.ContentRootPath, "App_Data", "orders.json");

    if (!File.Exists(factoryInfoPath) || !File.Exists(inventoryPath) || !File.Exists(ordersPath))
    {
        throw new FileNotFoundException("Required configuration files are missing.");
    }

    var reboxService = sp.GetRequiredService<IWebhookService>();
    var configuration = sp.GetRequiredService<IConfiguration>(); // add IConfiguration

    return new AIServiceFactory(factoryInfoPath, inventoryPath, ordersPath, reboxService, configuration);
});

// Add logging
builder.Services.AddLogging(logging =>
{
    logging.AddConfiguration(builder.Configuration.GetSection("Logging"));
    logging.AddConsole();
    logging.AddDebug();
});

// Add CORS services and define a default policy
builder.Services.AddCors(options =>
{
    options.AddPolicy("DefaultCorsPolicy", policy =>
    {
        policy.WithOrigins("http://localhost:3000") // Add your client application's origin.
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// Enable CORS with the default policy
app.UseCors("DefaultCorsPolicy");

app.UseSession();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapControllers();

app.Run();
