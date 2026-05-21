using HP_Detailing;
using HP_Detailing.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Instantiate Startup with builder.Configuration
var startup = new Startup(builder.Configuration);

// Register services
startup.ConfigureServices(builder.Services);

var app = builder.Build();

// Configure request pipeline
startup.Configure(app, app.Environment);

// Automatically migrate database and seed data
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<HP_DetailingDbContext>();
        context.Database.Migrate();
        DbInitializer.Initialize(context, services).GetAwaiter().GetResult();
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred creating/migrating the DB.");
    }
}

app.Run();
