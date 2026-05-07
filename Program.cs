using MegaFintradeRiskMonitor.Options;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<Project1ApiOptions>(
    builder.Configuration.GetSection(Project1ApiOptions.SectionName));

builder.Services.Configure<AiIntegrationOptions>(
    builder.Configuration.GetSection(AiIntegrationOptions.SectionName));

builder.Services.Configure<AlertRuleOptions>(
    builder.Configuration.GetSection(AlertRuleOptions.SectionName));

builder.Services.Configure<MonitoringOptions>(
    builder.Configuration.GetSection(MonitoringOptions.SectionName));

// Razor Pages are used for the monitoring dashboard.
builder.Services.AddRazorPages();

// Controllers are used for REST APIs such as health, monitor, alerts, and future Project 1 status endpoints.
builder.Services.AddControllers();

// Swagger is useful for API testing during development.
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

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