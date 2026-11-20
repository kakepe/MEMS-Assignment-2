using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.Extensions.Configuration;

// Matching Blazor hosting integration is experimental and explicitly pinned.
#pragma warning disable ASPIREBLAZOR001
var builder = DistributedApplication.CreateBuilder(args);

// Development-only credentials persist with this workspace, never in source or WASM.
var localDirectory = Path.GetFullPath(Path.Combine(builder.AppHostDirectory, "../../.local"));
Directory.CreateDirectory(localDirectory);
var secretsPath = Path.Combine(localDirectory, "apphost-secrets.json");
if (!File.Exists(secretsPath))
{
    var password = Convert.ToBase64String(RandomNumberGenerator.GetBytes(36)) + "aA1!";
    using var file = new FileStream(secretsPath, FileMode.CreateNew, FileAccess.Write, FileShare.None);
    JsonSerializer.Serialize(file, new { Parameters = new Dictionary<string, string> { ["mems-sql-password"] = password } });
}
builder.Configuration.AddJsonFile(secretsPath, optional: false, reloadOnChange: false);
builder.Configuration.AddJsonFile(Path.Combine(localDirectory, "identity-seed.json"), optional: true, reloadOnChange: false);
var identitySeedPassword = builder.AddParameter("mems-identity-seed-password", secret: true);

var passwordParameter = builder.AddParameter("mems-sql-password", secret: true);
var sql = builder.AddSqlServer("mems-assignment2-sql", password: passwordParameter)
    .WithContainerName("mems-assignment2-sql")
    .WithImageTag("2022-CU24-ubuntu-22.04")
    .WithDataVolume("mems-assignment2-sql-data")
    .WithLifetime(ContainerLifetime.Persistent);
var database = sql.AddDatabase("mems", "MEMSAssignment2");
var httpTemp=Path.Combine(localDirectory,"http-temp");
Directory.CreateDirectory(httpTemp);

var api = builder.AddProject<Projects.MEMS_Api>("mems-api")
    .WithEnvironment("ASPNETCORE_TEMP",httpTemp)
    .WithEnvironment("Mems__LocalStorage", localDirectory)
    .WithEnvironment("Seed__DevelopmentPassword", identitySeedPassword)
    .WithEnvironment("Database__Initialize", "true")
    .WithReference(database).WaitFor(database).WithHttpHealthCheck("/health");

builder.AddProject<Projects.MEMS_Admin>("mems-admin")
    .WithEnvironment("Mems__LocalStorage", localDirectory)
    .WithReference(database).WithReference(api).WaitFor(api).WithHttpHealthCheck("/health");

var publicClient = builder.AddBlazorWasmProject<Projects.MEMS_Public>("mems-public")
    .WithReference(api);

// Aspire-managed runtime resource, not an eighth source project.
#pragma warning disable ASPIRECSHARPAPPS001
builder.AddCSharpApp("mems-public-gateway", "Gateway/PublicGateway.cs")
    .WithBlazorClientApp(publicClient, apiPrefix: "api", proxyTelemetry: false)
    .WaitFor(api)
    .WithHttpsEndpoint(port: 7290)
    .WithHttpEndpoint(port: 5290)
    .WithExternalHttpEndpoints();
#pragma warning restore ASPIRECSHARPAPPS001

builder.Build().Run();

