using MegaFintradeRiskMonitor.Services;
using MegaFintradeRiskMonitor.Clients;
using MegaFintradeRiskMonitor.Data;
using MegaFintradeRiskMonitor.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;


var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<JavaBackendApiOptions>(
    builder.Configuration.GetSection(JavaBackendApiOptions.SectionName));

builder.Services.Configure<AiIntegrationOptions>(
    builder.Configuration.GetSection(AiIntegrationOptions.SectionName));

builder.Services.Configure<AlertRuleOptions>(
    builder.Configuration.GetSection(AlertRuleOptions.SectionName));

builder.Services.Configure<MonitoringOptions>(
    builder.Configuration.GetSection(MonitoringOptions.SectionName));

builder.Services.AddDbContext<RiskMonitorDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("RiskMonitorDatabase");

    if (string.IsNullOrWhiteSpace(connectionString))
    {
        throw new InvalidOperationException(
            "Connection string 'RiskMonitorDatabase' is missing.");
    }

    options.UseSqlite(connectionString);
});

builder.Services.AddHttpClient("JavaBackendApi", (serviceProvider, client) =>
{
    var options = serviceProvider
        .GetRequiredService<IOptions<JavaBackendApiOptions>>()
        .Value;

    client.BaseAddress = new Uri(options.BaseUrl);
    client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
});

builder.Services.AddScoped<IJavaBackendApiClient, JavaBackendApiClient>();

builder.Services.AddScoped<IAlertRuleEngine, AlertRuleEngine>();

builder.Services.AddScoped<IAlertService, AlertService>();

builder.Services.AddScoped<IRiskMonitoringService, RiskMonitoringService>();

builder.Services.AddHostedService<RiskMonitoringBackgroundService>();

builder.Services.AddRazorPages();
builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

var databaseDirectory = Path.Combine(app.Environment.ContentRootPath, "data");

if (!Directory.Exists(databaseDirectory))
{
    Directory.CreateDirectory(databaseDirectory);
}

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<RiskMonitorDbContext>();

    dbContext.Database.Migrate();
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseRouting();

app.MapRazorPages();
app.MapControllers();

app.Run();