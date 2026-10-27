using GreenTasty.Preorder.Api;
using Swashbuckle.AspNetCore.SwaggerGen;

// Render/Docker injects PORT (10000 by default); locally it's 5000 (see launchSettings.json).
// Both URL settings are aligned so the container's default ASPNETCORE_HTTP_PORTS=8080 never wins.
var port = Environment.GetEnvironmentVariable("PORT") ?? "5000";
Environment.SetEnvironmentVariable("ASPNETCORE_URLS", $"http://0.0.0.0:{port}");
Environment.SetEnvironmentVariable("ASPNETCORE_HTTP_PORTS", port);

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton(Store.Seed());
builder.Services.AddControllers();
builder.Services.AddSwaggerGen(c => c.OperationFilter<AuthHeaderOperationFilter>());

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));
app.MapControllers();
app.Run();
