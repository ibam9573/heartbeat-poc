using HeartbeatPOC.Services;
using HeartbeatPOC.Services.Interfaces;
using HeartbeatPOC.Models;
using HeartbeatPOC.Configuration;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.Configure<HeartbeatSettings>(builder.Configuration.GetSection(HeartbeatSettings.SectionName));
builder.Services.AddSingleton<IHeartbeatRegistry, HeartbeatRegistry>(); // Register HeartbeatRegistry as a singleton
builder.Services.AddHostedService<ProcessMonitorWorker>(); // Register ProcessMonitorWorker as a hosted service

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// POST /process/short-lived - Creates a short-lived process
app.MapPost("/process/short-lived", (IHeartbeatRegistry registry) =>
{
    string processId = registry.CreateProcess(false);
    return Results.Ok(new { ProcessId = processId, Message = $"Short-lived process {processId} created. It will expire if not kept alive." });
})
.WithName("CreateShortLivedProcess")
.WithOpenApi();

// POST /process/long-lived - Creates a long-lived process
app.MapPost("/process/long-lived", (IHeartbeatRegistry registry) =>
{
    string processId = registry.CreateProcess(true);
    return Results.Ok(new { ProcessId = processId, Message = $"Long-lived process {processId} created. It will be kept alive by the monitor." });
})
.WithName("CreateLongLivedProcess")
.WithOpenApi();

// GET /heartbeat/status - Lists all processes
app.MapGet("/heartbeat/status", (IHeartbeatRegistry registry) =>
{
    return Results.Ok(registry.GetStatuses());
})
.WithName("GetHeartbeatStatus")
.WithOpenApi();

// GET /heartbeat/active-status - Lists only active processes
app.MapGet("/heartbeat/active-status", (IHeartbeatRegistry registry) =>
{
    return Results.Ok(registry.GetActiveStatuses());
})
.WithName("GetActiveHeartbeatStatus")
.WithOpenApi();

// DELETE /heartbeat/{processId} - Simulates stopping a process
app.MapDelete("/heartbeat/{processId}", (string processId, IHeartbeatRegistry registry) =>
{
    registry.RemoveHeartbeat(processId);
    return Results.Ok($"Process {processId} heartbeat removed.");
})
.WithName("RemoveHeartbeat")
.WithOpenApi();

app.Run();
