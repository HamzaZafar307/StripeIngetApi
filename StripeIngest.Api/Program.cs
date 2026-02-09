using Microsoft.EntityFrameworkCore;
using StripeIngest.Api.Infrastructure.Data;
using StripeIngest.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo { Title = "Stripe Ingestion API", Version = "v1" });
    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    c.IncludeXmlComments(xmlPath);
});

// DB Context
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Services
builder.Services.AddScoped<IEventProcessor, EventProcessor>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthorization();

app.MapControllers();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    // Wait for DB to be ready in case of docker race condition
    try {
        // RESET DB for testing (Remove in production)
        db.Database.EnsureDeleted();
        db.Database.EnsureCreated();
        
        // Re-create View
        db.Database.ExecuteSqlRaw(@"
            CREATE OR ALTER VIEW MonthlyMrrReport AS
            SELECT 
                FORMAT(Timestamp, 'yyyy-MM') as Month,
                SUM(CASE WHEN ChangeType = 'new' THEN MRRDelta ELSE 0 END) as NewMRR,
                SUM(CASE WHEN ChangeType = 'upgrade' THEN MRRDelta ELSE 0 END) as ExpansionMRR,
                SUM(CASE WHEN ChangeType = 'downgrade' THEN MRRDelta ELSE 0 END) as ContractionMRR,
                SUM(CASE WHEN ChangeType = 'churn' THEN MRRDelta ELSE 0 END) as ChurnedMRR,
                SUM(MRRDelta) as NetMRRChange
            FROM SubscriptionHistory
            GROUP BY FORMAT(Timestamp, 'yyyy-MM')
        ");
    } catch (Exception ex) {
        Console.WriteLine($"DB Init Error: {ex.Message}");
    }
}

app.Run();
