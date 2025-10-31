using Elastic.Clients.Elasticsearch;
using Microsoft.EntityFrameworkCore;
using SO.Data;
using SO.Web.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddHttpClient();

// Add DB context
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));

// Add Elasticsearch client
builder.Services.AddSingleton(sp =>
{
    var settings = new ElasticsearchClientSettings(new Uri("http://localhost:9200"))
        .DefaultIndex("users");
    return new ElasticsearchClient(settings);
});

// Add services
builder.Services.AddScoped<UserSearchService>(); 
builder.Services.AddScoped<ElasticUserSearchService>(); 

var app = builder.Build();

// Ensure Elasticsearch index exists and optionally index all users
using (var scope = app.Services.CreateScope())
{
    var esService = scope.ServiceProvider.GetRequiredService<ElasticUserSearchService>();
    
    // 1️⃣ Create index if missing
    await esService.CreateIndexAsync();
    
    // 2️⃣ Optional: bulk index users from DB
    await esService.IndexUsersAsync();
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();