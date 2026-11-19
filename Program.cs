using Microsoft.EntityFrameworkCore;
using EventEaseApp.Data;
using EventEaseApp.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// Register the EF Core DbContext, pointed at the connection string in appsettings.json
builder.Services.AddDbContext<EventEaseDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("EventEaseDbConnection")));

// Register the Azure Blob Storage service used for Venue/Event image uploads
builder.Services.AddScoped<IBlobStorageService, BlobStorageService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

// Re-executes the pipeline against a friendly page for status codes like 404,
// instead of showing the browser's or server's default error page.
app.UseStatusCodePagesWithReExecute("/Home/PageNotFound", "?statusCode={0}");

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Venue}/{action=Index}/{id?}");

app.Run();
