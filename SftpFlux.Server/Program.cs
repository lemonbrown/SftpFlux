﻿using Org.BouncyCastle.Asn1.Ocsp;
using Renci.SshNet;
using Renci.SshNet.Messages;
using SftpFlux.Server;
using SftpFlux.Server.Authorization;
using SftpFlux.Server.Caching;
using SftpFlux.Server.Connection;
using SftpFlux.Server.Jobs;
using SftpFlux.Server.Pipes;
using SftpFlux.Server.Polling;
using SftpFlux.Server.Polling.Webhooks;
using SftpFlux.Server.Queries;
using SftpFlux.Server.Queues;
using SftpFlux.Server.User;
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

        using var pipeClient = new NamedPipeClientStream(
            ".", "sftpflux-admin", PipeDirection.InOut, PipeOptions.Asynchronous);

        Console.WriteLine("[Client] Connecting...");
        await pipeClient.ConnectAsync(5000);
        Console.WriteLine("[Client] Connected.");

        var messageBytes = Encoding.UTF8.GetBytes(command);
        await pipeClient.WriteAsync(messageBytes, 0, messageBytes.Length);
        await pipeClient.FlushAsync();

        byte[] responseBuffer = new byte[1024];
        int bytesRead = await pipeClient.ReadAsync(responseBuffer, 0, responseBuffer.Length);
        var response = Encoding.UTF8.GetString(responseBuffer, 0, bytesRead);

        Console.WriteLine($"[Client] Received: {response}");
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine($"[Error] Failed to send command: {ex.Message}");
    }
}


var hostString = config["host"];
var userString = config["user"];
var adminString = config["admin"];

var isClient = false;

if (hostString == null || userString == null)
{
    isClient = true;
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

// Start Web API in the background
var builder = WebApplication.CreateBuilder();
//builder.Logging.ClearProviders();

builder.Services.AddSingleton(defaultConnectionInfo);

var runInMemory = true;

builder.Services.AddSingleton<ISftpMetadataCacheService>(new InMemorySftpMetadataCacheService(TimeSpan.FromSeconds(10)));
builder.Services.AddSingleton<ISftpConnectionRegistry, InMemorySftpConnectionRegistry>();
builder.Services.AddSingleton<IUserService, InMemoryUserService>();
builder.Services.AddSingleton<IFileQueryService, FileQueryService>();
builder.Services.AddSingleton<IScheduledJobRegistry, InMemoryScheduledJobRegistry>();
builder.Services.AddSingleton<ScheduledJobProcessor, ScheduledJobProcessor>();
builder.Services.AddSingleton<WebhookNotifier, WebhookNotifier>();
builder.Services.AddSingleton<IFileQueueService, InMemoryFileQueueService>();
builder.Services.AddHttpClient();

if (runInMemory) {

    builder.Services.AddSingleton<IApiKeyService, InMemoryApiKeyService>();
    builder.Services.AddSingleton<IWebhookService, InMemoryWebhookService>();

} else {

    builder.Services.AddSingleton<IApiKeyService, PersistentApiKeyService>();
}

var app = builder.Build();


app.UseDefaultFiles();    // Serves index.html from wwwroot
app.UseStaticFiles();     // For Tailwind/HTMX

//app.UseMiddleware<BasicAuthMiddleware>();

// Connect and disconnect on demand (stateless)
SftpClient ConnectSftp(SftpConnectionInfo connection)
{
    var sftpClient = new SftpClient(connection.Host, connection.Port, connection.Username, connection.Password);

    if (!sftpClient.IsConnected)
        sftpClient.Connect();

    return sftpClient;
}


app.MapGet("/files/{*path}", async (
    string? path,
    HttpRequest request,
    IFileQueryService fileQueryService) =>
{
 
    var query = new FileQuery(
        NameContains: request.Query["name"],
        FileType: request.Query["type"],
        ModifiedFrom: DateTime.TryParse(request.Query["modifiedFrom"], out var mf) ? mf : null,
        ModifiedTo: DateTime.TryParse(request.Query["modifiedTo"], out var mt) ? mt : null,
        CreatedFrom: DateTime.TryParse(request.Query["createdFrom"], out var cf) ? cf : null,
        CreatedTo: DateTime.TryParse(request.Query["createdTo"], out var ct) ? ct : null,
        SortBy: request.Query["sortBy"],
        SortOrder: request.Query["sortOrder"],
        Path: path ?? ".",
        Page: int.TryParse(request.Query["page"], out var p) ? p : 1,
        PageSize: int.TryParse(request.Query["pageSize"], out var ps) ? ps : 50);

    var result = await fileQueryService.QueryFilesAsync(query);


    return Results.Ok(result);
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


app.MapPost("/webhooks", async (
    HttpContext context, 
    WebhookSubscription sub, 
    IWebhookService webhookService, 
    IApiKeyService apiKeyService,
    IUserService userService,
    ISftpConnectionRegistry sftpConnectionRegistry) =>
{
    var apikey = await apiKeyService.GetKeyAsync(context.Request.Headers.Authorization.ToString());

    if(apikey == null && !await userService.CheckForAdminInHeaders(context))
        return Results.Unauthorized();

    if (string.IsNullOrEmpty(sub.SftpId))
        sub.SftpId = sftpConnectionRegistry.GetAll().FirstOrDefault()?.Id ?? "";

    await webhookService.AddAsync(sub, apikey);

    WebhookStore.Subscriptions.Add(sub);
    return Results.Created($"/webhooks/{sub.Id}", sub);
});

app.MapGet("/webhooks/{*sftpId}", async (
    HttpContext context,
    string? sftpId,
    IWebhookService webhookService,
    IApiKeyService apiKeyService,
    IUserService userService,
    ISftpConnectionRegistry sftpConnectionRegistry) => {

        var apikey = await apiKeyService.GetKeyAsync(context.Request.Headers.Authorization.ToString());

        if (apikey == null && !await userService.CheckForAdminInHeaders(context))
            return Results.Unauthorized();

        if(string.IsNullOrEmpty(sftpId))
            sftpId = sftpConnectionRegistry.GetAll().FirstOrDefault()?.Id;

        var webhooks = await webhookService.GetWebhooksForSftpAsync(sftpId);

        return Results.Ok(webhooks);
    });

app.MapPost("/jobs", (
    ScheduledFileJob job,
    IScheduledJobRegistry registry) => {
        registry.AddJob(job);
        return Results.Ok(new { message = "Job scheduled", jobId = job.Id });
    });

app.MapPost("/admin/apikeys", async (ApiKeyRequest request, IApiKeyService apiKeyService) => {

    var apiKey = await apiKeyService.CreateKeyAsync(request.Scopes, request.SftpIds);
    
    return Results.Ok(apiKey);
});

app.MapGet("/admin/apikeys", async (IApiKeyService apiKeyService) => {
    return Results.Ok(await apiKeyService.GetAllKeysAsync());
});

app.MapGet("/admin/apikeys/{id}", async (string id, IApiKeyService apiKeyService) => {
    var key = await apiKeyService.GetKeyAsync(id);
    return key != null
        ? Results.Ok(key)
        : Results.NotFound();
});

app.MapPut("/admin/apikeys/{id}/scopes", async (string id, ScopeUpdateRequest update, IApiKeyService apiKeyService) => {

    var key = await apiKeyService.GetKeyAsync(id);
    
    await apiKeyService.UpdateScopesAsync(id, update.Scopes.Distinct().ToList());

    return Results.Ok(key);
});

app.MapPost("/admin/apikeys/{id}/revoke", async (string id, IApiKeyService apiKeyService) => {

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

app.MapDelete("/admin/apikeys/{id}", async (string id, IApiKeyService apiKeyService) => {
    return await apiKeyService.DeleteKeyAsync(id) ? Results.Ok() : Results.NotFound();
});

app.MapPost("/admin/sftp", (SftpConnectionInfoRequest request, ISftpConnectionRegistry registry) =>
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

app.MapGet("/admin/sftp", (ISftpConnectionRegistry registry) =>
{
    return Results.Ok(registry.GetAll());
});

app.MapPost("/queues/{queueName}/dequeue", async (
    string queueName,
    IFileQueueService fileQueueService) => {
        var item = await fileQueueService.DequeueAsync(queueName);

        return item is not null
            ? Results.Ok(item)
            : Results.NoContent();
    });

app.MapPost("/test", (FileQueryResult queryResult) => {
    return Results.Ok(queryResult); 
});

var serverTask = app.RunAsync("http://localhost:5000");

var pollingService = new SftpPollingService(app.Services);

await pollingService.StartAsync(new CancellationToken());

if (!isClient) {

    //var pipeListener = new PipeCommandListener(CancellationToken.None);

    //pipeListener.Start();

    //var jobService = new JobSchedulerService(app.Services);

    //await jobService.StartAsync(new CancellationToken());
}

await Task.Delay(-1);

record ApiKeyRequest(string Name, List<string>? Scopes, List<string>? SftpIds);

record ScopeUpdateRequest(List<string> Scopes);