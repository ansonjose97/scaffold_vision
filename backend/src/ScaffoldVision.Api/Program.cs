using ScaffoldVision.Api.AI;
using ScaffoldVision.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

// Domain services. The catalog is a singleton because it's read-only seeded data;
// configurations are also held in-process for the MVP. A Postgres-backed implementation
// is described in docs/ARCHITECTURE.md as the production path.
builder.Services.AddSingleton<IComponentCatalog, InMemoryComponentCatalog>();
builder.Services.AddSingleton<IConfigurationStore, InMemoryConfigurationStore>();
builder.Services.AddScoped<IConfigurationService, ConfigurationService>();
builder.Services.AddScoped<IScaffoldRecommender, RuleBasedRecommender>();

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins("http://localhost:5173")
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

var app = builder.Build();

app.UseCors();
app.UseAuthorization();
app.MapControllers();

app.Run();

public partial class Program { }
