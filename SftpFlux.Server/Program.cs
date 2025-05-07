using Renci.SshNet;
using SftpFlux.Server;
using SftpFlux.Server.Authorization;
using SftpFlux.Server.Caching;
using SftpFlux.Server.Connection;
using SftpFlux.Server.Polling;
using System.IO.Pipes;
using System.Text;

using ConnectionInfo = Renci.SshNet.ConnectionInfo;

var config = new ConfigurationBuilder()
    .AddCommandLine(args)
    .Build();

async Task SendCommandToPipe(string command)
{
    try
    {

        using var pipeClient = new NamedPipeClientStream(".", "sftpflux-admin", PipeDirection.InOut);
        await pipeClient.ConnectAsync(2000); // 2 second timeout

        using var writer = new StreamWriter(pipeClient, Encoding.UTF8) { AutoFlush = true };
        using var reader = new StreamReader(pipeClient, Encoding.UTF8);

        await writer.WriteLineAsync("hello from client");

        var response = await reader.ReadLineAsync();

        Console.WriteLine($"[Response] {response}");
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine($"[Error] Failed to send command: {ex.Message}");
    }
}


var hostString = config["host"];
var userString = config["user"];
var adminString = config["admin"];

if (hostString == null || userString == null)
{
    var command = string.Join(' ', args.Skip(1));

    await SendCommandToPipe(command);

    return;

    //Console.WriteLine("Usage: --host sftp.example.com:22 --user username@password");
    //return;
}

// Parse inputs
var hostParts = hostString.Split(':');
var hostname = hostParts[0];
var port = int.Parse(hostParts[1]);

var userParts = userString.Split('@', 2);
var username = userParts[0];
var password = userParts[1];

if (!string.IsNullOrEmpty(adminString))
{
    var adminParts = adminString.Split("@", 2);
    var adminUsername = adminParts[0];
    var adminPassword = adminParts[1];
}

var defaultConnectionInfo = new SftpConnectionInfo {
    Host = hostname,
    Port = port,
    Username = username,
    Password = password,
    Id = Guid.NewGuid().ToString("N")
};

GlobalConfig.IsTestBypassAuth = true;

// Start Web API in the background
var builder = WebApplication.CreateBuilder();
//builder.Logging.ClearProviders();

builder.Services.AddSingleton(defaultConnectionInfo);

var runInMemory = true;

var inTestMode = true;

builder.Services.AddSingleton<ISftpMetadataCacheService>(new InMemorySftpMetadataCacheService(TimeSpan.FromMinutes(10)));
builder.Services.AddSingleton<ISftpConnectionRegistry, InMemorySftpConnectionRegistry>();

if (runInMemory) {

    builder.Services.AddSingleton<IApiKeyService, InMemoryApiKeyService>();
    builder.Services.AddSingleton<IWebhookService, InMemoryWebhookService>();

} else {

    builder.Services.AddSingleton<IApiKeyService, PersistentApiKeyService>();
}

var app = builder.Build();

// Connect and disconnect on demand (stateless)
SftpClient ConnectSftp(SftpConnectionInfo connection)
{
    var sftpClient = new SftpClient(connection.Host, connection.Port, connection.Username, connection.Password);

    if (!sftpClient.IsConnected)
        sftpClient.Connect();

    return sftpClient;
}

// List files in a directory
app.MapGet("/files/{*path}", async (
    string? path, 
    ISftpConnectionRegistry sftpConnectionRegistry, 
    ISftpMetadataCacheService cacheService) =>
{
    path ??= ".";

    var connections = sftpConnectionRegistry.GetAll();

    var cached = cacheService.GetDirectoryEntries(path, connections.First().Id);

    if (cached != null)
        return Results.Ok(cached);

    var client = ConnectSftp(connections.First());

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

    cacheService.SetDirectoryEntries(path, entries, connections.First().Id);

    return Results.Ok(entries);
})
.RequireScopedDirectory("read", ctx => ctx.Arguments[0]?.ToString() ?? "/");

// List files in a directory
app.MapGet("/sftp/{sftpId}/files/{*path}", 
    async (
        string sftpId, 
        string? path, 
        ISftpMetadataCacheService cacheService,
        ISftpConnectionRegistry sftpConnectionRegistry) =>
{
    var connection = sftpConnectionRegistry.Get(sftpId);

    if (connection == null)
        return Results.NotFound($"SFTP config '{sftpId}' not found.");

    path ??= ".";

    var cached = cacheService.GetDirectoryEntries(path, sftpId);
    if (cached != null)
        return Results.Ok(cached);

    var client = ConnectSftp(connection);

    if (!client.Exists(path))
        return Results.NotFound($"Path not found: {path}");

    var entries = client.ListDirectory(path)
        .Where(f => f.Name != "." && f.Name != "..")
        .Select(f => new SftpMetadataEntry
        {
            Path = path,
            Name = f.Name,
            IsDirectory = f.IsDirectory,
            Size = f.Attributes.Size,
            LastModifiedUtc = f.Attributes.LastWriteTimeUtc
        })
        .ToList();

    cacheService.SetDirectoryEntries(path, entries, sftpId);

    return Results.Ok(entries);
})
.RequireScopedDirectory("read", ctx => ctx.Arguments[0]?.ToString() ?? "/");

app.MapGet("/file/{*path}", async (string path, ISftpConnectionRegistry sftpConnectionRegistry) =>
{
    // If the path is not provided, default to the root directory
    path ??= ".";

    var connections = sftpConnectionRegistry.GetAll();

    var sftpClient = ConnectSftp(connections.First());

    // Check if the file exists in the specified path
    if (!sftpClient.Exists(path))
        return Results.NotFound($"File not found: {path}");

    // Open the file from the SFTP server and stream it to the client

    var fileStream = sftpClient.OpenRead(path);

    var fileName = Path.GetFileName(path); // Get the file name from the path
    var fileType = MimeTypes.GetMimeType(fileName); // Get MIME type for the file (can use a library like MimeTypesMap)

    // Return the file as a stream with the appropriate content-type and headers
    return Results.File(fileStream, fileType, fileName);

})
.RequireScopedDirectory("read", ctx => ctx.Arguments[0]?.ToString() ?? "/");


app.MapPost("/webhooks", async (HttpContext context, WebhookSubscription sub, IWebhookService webhookService, IApiKeyService apiKeyService) =>
{
    var apikey = await apiKeyService.GetKeyAsync(context.Request.Headers.Authorization.ToString());

    if (inTestMode)
        apikey ??= new();
    else
        return Results.Unauthorized();

    await webhookService.AddAsync(sub, apikey);

    WebhookStore.Subscriptions.Add(sub);
    return Results.Created($"/webhooks/{sub.Id}", sub);
});

app.MapPost("/apikeys", async (ApiKeyRequest request, IApiKeyService apiKeyService) => {

    var apiKey = await apiKeyService.CreateKeyAsync(request.Scopes, request.SftpIds);
    
    return Results.Ok(apiKey);
});

app.MapGet("/apikeys", async (IApiKeyService apiKeyService) => {
    return Results.Ok(await apiKeyService.GetAllKeysAsync());
});

app.MapGet("/apikeys/{id}", async (string id, IApiKeyService apiKeyService) => {
    var key = await apiKeyService.GetKeyAsync(id);
    return key != null
        ? Results.Ok(key)
        : Results.NotFound();
});

app.MapPut("/apikeys/{id}/scopes", async (string id, ScopeUpdateRequest update, IApiKeyService apiKeyService) => {

    var key = await apiKeyService.GetKeyAsync(id);
    
    await apiKeyService.UpdateScopesAsync(id, update.Scopes.Distinct().ToList());

    return Results.Ok(key);
});

app.MapPost("/apikeys/{id}/revoke", async (string id, IApiKeyService apiKeyService) => {

    if (!await apiKeyService.ReinstateKeyAsync(id))
        return Results.NotFound();    

    return Results.Ok();
});

//app.MapPost("/apikeys/{id}/reinstate", (string id) => {
//    if (!apiKeyStore.TryGetValue(id, out var key))
//        return Results.NotFound();

//    key.Revoked = false;
//    return Results.Ok(key);
//});

app.MapDelete("/apikeys/{id}", async (string id, IApiKeyService apiKeyService) => {
    return await apiKeyService.DeleteKeyAsync(id) ? Results.Ok() : Results.NotFound();
});

app.MapPost("/sftp", (SftpConnectionInfoRequest request, ISftpConnectionRegistry registry) =>
{
    var info = new SftpConnectionInfo {
        Id = request.Id,
        Host = request.Host,
        Password = request.Password,
        Username = request.Username,
        Port = request.Port
    };

    registry.Add(info);
    return Results.Ok();
});

app.MapGet("/sftp", (ISftpConnectionRegistry registry) =>
{
    return Results.Ok(registry.GetAll());
});


var serverTask = app.RunAsync("http://localhost:5000");

var pollingService = new SftpPollingService(app.Services);

//await pollingService.StartAsync(new CancellationToken());

// Start REPL
var client = new HttpClient();
Console.WriteLine("SFTP API running at http://localhost:5000");
//Console.WriteLine("Enter commands (e.g., 'get /files') or 'exit' to quit.");

var pipeListener = new PipeCommandListener(CancellationToken.None);
pipeListener.Start();


//while (true) {
//    Console.Write("> ");
//    var input = Console.ReadLine();
//    if (string.IsNullOrWhiteSpace(input))
//        continue;

//    if (input.Trim().ToLower() == "exit")
//        break;

//    if (input.StartsWith("post ", StringComparison.OrdinalIgnoreCase)) {
//        var parts = input.Split(' ', 3);
//        var url = parts.Length > 1 ? parts[1] : "";
//        var jsonBody = parts.Length > 2 ? parts[2] : "{}";

//        var content = new StringContent(jsonBody, Encoding.UTF8, "application/json");

//        var response = await client.PostAsync("http://localhost:5000" + url, content);
//        var result = await response.Content.ReadAsStringAsync();

//        Console.WriteLine($"[{(int)response.StatusCode}] {result}");
//    }

//    if (input.StartsWith("get ", StringComparison.OrdinalIgnoreCase)) {
//        var parts = input.Split(' ', 2);
//        if (parts.Length != 2) {
//            Console.WriteLine("Invalid command format. Use: get /files");
//            continue;
//        }

//        var method = parts[0].ToLower();
//        var endpoint = parts[1];

//        try {
//            switch (method) {
//                case "get":
//                    var response = await client.GetAsync("http://localhost:5000" + endpoint);
//                    response.EnsureSuccessStatusCode();
//                    //var result = await response.Content.ReadFromJsonAsync<List<string>>();
//                    var result = await response.Content.ReadAsStringAsync();
//                    Console.WriteLine(string.Join('\n', result ?? ""));
//                    break;
//                default:
//                    Console.WriteLine("Unsupported method.");
//                    break;
//            }
//        } catch (Exception ex) {
//            Console.WriteLine($"Error: {ex.Message}");
//        }
//    }   
//}

await Task.Delay(-1);

record ApiKeyRequest(string Name, List<string>? Scopes, List<string>? SftpIds);

record ScopeUpdateRequest(List<string> Scopes);

static class GlobalConfig {

    public static bool IsTestBypassAuth { get; set; }

}