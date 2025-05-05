using Microsoft.AspNetCore.Authentication;
using Renci.SshNet;
using SftpFlux.Server;
using System.Reflection.Metadata.Ecma335;
using System.Text;

using ConnectionInfo = Renci.SshNet.ConnectionInfo;

var config = new ConfigurationBuilder()
    .AddCommandLine(args)
    .Build();

var hostString = config["host"];
var userString = config["user"];

if (hostString == null || userString == null)
{
    Console.WriteLine("Usage: --host sftp.example.com:22 --user username@password");
    return;
}

// Parse inputs
var hostParts = hostString.Split(':');
var hostname = hostParts[0];
var port = int.Parse(hostParts[1]);

var userParts = userString.Split('@', 2);
var username = userParts[0];
var password = userParts[1];

var connectionInfo = new ConnectionInfo(
    hostname,
    port,
    username,
    new PasswordAuthenticationMethod(username, password)
);

// Start Web API in the background
var builder = WebApplication.CreateBuilder();
builder.Logging.ClearProviders();
builder.Services.AddSingleton(connectionInfo);

// Register SftpClient in the DI container (use your connection details here)
builder.Services.AddSingleton<SftpClient>(sp =>
{
    var host = hostname;

    return new SftpClient(host, port, username, password);
});

builder.Services.AddSingleton<ISftpMetadataCacheService>(new InMemorySftpMetadataCacheService(TimeSpan.FromMinutes(10)));
builder.Services.AddSingleton<IApiKeyService, InMemoryApiKeyService>();

var app = builder.Build();

// Connect and disconnect on demand (stateless)
SftpClient ConnectSftp(SftpClient client)
{
    if (!client.IsConnected)
        client.Connect();
    return client;
}

// List files in a directory
app.MapGet("/files/{*path}", async (string? path, SftpClient sftpClient, ISftpMetadataCacheService cacheService) =>
{
    path ??= ".";

    var cached = cacheService.GetDirectoryEntries(path);
    if (cached != null)
        return Results.Ok(cached);

    var client = ConnectSftp(sftpClient);
    if (!client.Exists(path))
        return Results.NotFound($"Path not found: {path}");

    var entries = client.ListDirectory(path)
        .Where(f => f.Name != "." && f.Name != "..")
        .Select(f => new SftpMetadataEntry {
            Path = path,
            Name = f.Name,
            IsDirectory = f.IsDirectory,
            Size = f.Attributes.Size,
            LastModifiedUtc = f.Attributes.LastWriteTimeUtc
        })
        .ToList();

    cacheService.SetDirectoryEntries(path, entries);

    return Results.Ok(entries);
})
.RequireScopedDirectory("read", ctx => ctx.Arguments[0]?.ToString() ?? "/");


app.MapGet("/file/{*path}", async (string path, SftpClient sftpClient) =>
{
    // If the path is not provided, default to the root directory
    path ??= ".";

    // Ensure the SFTP client is connected
    if (!sftpClient.IsConnected)
        sftpClient.Connect();

    // Check if the file exists in the specified path
    if (!sftpClient.Exists(path))
        return Results.NotFound($"File not found: {path}");

    // Open the file from the SFTP server and stream it to the client

    var fileStream = sftpClient.OpenRead(path);

    var fileName = Path.GetFileName(path); // Get the file name from the path
    var fileType = MimeTypes.GetMimeType(fileName); // Get MIME type for the file (can use a library like MimeTypesMap)

    // Return the file as a stream with the appropriate content-type and headers
    return Results.File(fileStream, fileType, fileName);

});

app.MapPost("/webhooks", (WebhookSubscription sub) =>
{
    WebhookStore.Subscriptions.Add(sub);
    return Results.Created($"/webhooks/{sub.Id}", sub);
});

app.MapPost("/apikeys", async (ApiKeyRequest request, IApiKeyService apiKeyService) => {

    var apiKey = await apiKeyService.CreateKeyAsync(request.Scopes);
    
    return Results.Ok(apiKey);
});

app.MapGet("/apikeys", async (IApiKeyService apiKeyService) => Results.Ok(await apiKeyService.GetAllKeysAsync()));

//app.MapGet("/apikeys/{id}", (string id) => {
//    return apiKeyStore.TryGetValue(id, out var key)
//        ? Results.Ok(key)
//        : Results.NotFound();
//});

app.MapPut("/apikeys/{id}/scopes", async (string id, ScopeUpdateRequest update, IApiKeyService apiKeyService) => {

    var key = await apiKeyService.GetKeyAsync(id);
    
    await apiKeyService.UpdateScopesAsync(id, update.Scopes.Distinct().ToList());

    return Results.Ok(key);
});

//app.MapPost("/apikeys/{id}/revoke", (string id) => {
//    if (!apiKeyStore.TryGetValue(id, out var key))
//        return Results.NotFound();

//    key.Revoked = true;
//    return Results.Ok(key);
//});

//app.MapPost("/apikeys/{id}/reinstate", (string id) => {
//    if (!apiKeyStore.TryGetValue(id, out var key))
//        return Results.NotFound();

//    key.Revoked = false;
//    return Results.Ok(key);
//});

//app.MapDelete("/apikeys/{id}", (string id) => {
//    return apiKeyStore.TryRemove(id, out _) ? Results.Ok() : Results.NotFound();
//});


var serverTask = app.RunAsync("http://localhost:5000");

var pollingService = new SftpPollingService(app.Services);

//await pollingService.StartAsync(new CancellationToken());

// Start REPL
var client = new HttpClient();
Console.WriteLine("SFTP API running at http://localhost:5000");
Console.WriteLine("Enter commands (e.g., 'get /files') or 'exit' to quit.");

while (true) {
    Console.Write("> ");
    var input = Console.ReadLine();
    if (string.IsNullOrWhiteSpace(input))
        continue;

    if (input.Trim().ToLower() == "exit")
        break;

    if (input.StartsWith("post ", StringComparison.OrdinalIgnoreCase)) {
        var parts = input.Split(' ', 3);
        var url = parts.Length > 1 ? parts[1] : "";
        var jsonBody = parts.Length > 2 ? parts[2] : "{}";

        var content = new StringContent(jsonBody, Encoding.UTF8, "application/json");

        var response = await client.PostAsync("http://localhost:5000" + url, content);
        var result = await response.Content.ReadAsStringAsync();

        if (url.Contains("apikeys")) {
            ReplApiKey.ApiKey = result.Trim('"');
        }

        Console.WriteLine($"[{(int)response.StatusCode}] {result}");
    }

    if (input.StartsWith("get ", StringComparison.OrdinalIgnoreCase)) {
        var parts = input.Split(' ', 2);
        if (parts.Length != 2) {
            Console.WriteLine("Invalid command format. Use: get /files");
            continue;
        }

        var method = parts[0].ToLower();
        var endpoint = parts[1];

        try {
            switch (method) {
                case "get":
                    var response = await client.GetAsync("http://localhost:5000" + endpoint);
                    response.EnsureSuccessStatusCode();
                    //var result = await response.Content.ReadFromJsonAsync<List<string>>();
                    var result = await response.Content.ReadAsStringAsync();
                    Console.WriteLine(string.Join('\n', result ?? ""));
                    break;
                default:
                    Console.WriteLine("Unsupported method.");
                    break;
            }
        } catch (Exception ex) {
            Console.WriteLine($"Error: {ex.Message}");
        }
    }   
}
Console.WriteLine("Shutting down...");


record ApiKeyRequest(string Name, List<string> Scopes);
record ScopeUpdateRequest(List<string> Scopes);

static class ReplApiKey {
    public static string ApiKey { get; set; } = "";

}