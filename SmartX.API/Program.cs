using SmartX.API.DataStructures;
using SmartX.API.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddCors(options =>
{
    options.AddPolicy("SmartXClient", policy =>
    {
        policy
            .WithOrigins(
                "http://localhost:5173",
                "https://localhost:5173")
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

builder.Services.AddSingleton<SensorService>();
builder.Services.AddSingleton<TelemetryService>();
builder.Services.AddSingleton<FileService>();
builder.Services.AddSingleton<CommandService>();

builder.Services.AddSingleton<ActiveAlertService>();
builder.Services.AddSingleton<TelemetryInputValidator>();
builder.Services.AddSingleton<TelemetryProcessingQueue>(
    _ => new TelemetryProcessingQueue(10000));

// Register one shared worker for hosting and status reporting.
builder.Services.AddSingleton<TelemetryQueueProcessor>();
builder.Services.AddHostedService<TelemetryQueueProcessor>(
    provider => provider.GetRequiredService<TelemetryQueueProcessor>());

builder.Services.AddHttpClient("SmartXSimulator", client =>
{
    var baseUrl = builder.Configuration["SmartX:SimulatorApiUrl"]
        ?? "http://localhost:5206/";

    client.BaseAddress = new Uri(baseUrl.TrimEnd('/') + "/");
    client.Timeout = TimeSpan.FromSeconds(5);
});

builder.Services.AddSingleton<TelemetrySimulationService>();
builder.Services.AddHostedService<TelemetrySimulationService>(
    provider => provider.GetRequiredService<TelemetrySimulationService>());

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("SmartXClient");
app.UseAuthorization();
app.MapControllers();

app.Run();



