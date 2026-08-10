using Azure.Identity;
using Azure.Monitor.OpenTelemetry.Exporter;
using Azure.Storage.Blobs;
using JustTaskTracker.Archival.Functions.Abstractions.Archiving;
using JustTaskTracker.Archival.Functions.Abstractions.ExternalProviders;
using JustTaskTracker.Archival.Functions.Abstractions.Processing;
using JustTaskTracker.Archival.Functions.Archiving;
using JustTaskTracker.Archival.Functions.Archiving.Summary;
using JustTaskTracker.Archival.Functions.Constants;
using JustTaskTracker.Archival.Functions.ExternalProviders.Api;
using JustTaskTracker.Archival.Functions.ExternalProviders.Blob;
using JustTaskTracker.Archival.Functions.ExternalProviders.CosmosDB;
using JustTaskTracker.Archival.Functions.Options;
using JustTaskTracker.Archival.Functions.Processing;
using JustTaskTracker.Archival.Functions.Processing.Export;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Azure.Functions.Worker.OpenTelemetry;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

var builder = FunctionsApplication.CreateBuilder(args);

builder.ConfigureFunctionsWebApplication();

var keyVaultOptions = builder.Configuration
    .GetSection(KeyVaultOptions.SectionName)
    .Get<KeyVaultOptions>();

// Optional locally: omit KeyVault:Uri so local.settings / env supply secrets.
if (keyVaultOptions is not null && !string.IsNullOrWhiteSpace(keyVaultOptions.Uri))
{
    keyVaultOptions.Validate();
    builder.Configuration.AddAzureKeyVault(new Uri(keyVaultOptions.Uri), new DefaultAzureCredential());
}

if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("APPLICATIONINSIGHTS_CONNECTION_STRING")))
{
    builder.Services.AddOpenTelemetry()
        .UseFunctionsWorkerDefaults()
        .UseAzureMonitorExporter();
}

builder.Services
    .Configure<BoardExportApiClientOptions>(
        builder.Configuration.GetSection(BoardExportApiClientOptions.SectionName))

    .AddHttpClient<IBoardExportDataApiClient, BoardExportDataApiClient>((sp, client) =>
    {
        var options = sp.GetRequiredService<IOptions<BoardExportApiClientOptions>>().Value;
        options.Validate();

        client.BaseAddress = new Uri(options.BaseAddress.TrimEnd('/') + '/', UriKind.Absolute);
        client.Timeout = TimeSpan.FromMinutes(options.RequestTimeoutMinutes);
    })

    .Services
    .AddHttpClient<IBoardExportStatusNotifyApiClient, BoardExportStatusNotifyApiClient>((sp, client) =>
    {
        var options = sp.GetRequiredService<IOptions<BoardExportApiClientOptions>>().Value;
        options.Validate();

        client.BaseAddress = new Uri(options.BaseAddress.TrimEnd('/') + '/', UriKind.Absolute);
        client.Timeout = TimeSpan.FromMinutes(options.RequestTimeoutMinutes);
    })

    .Services
    .Configure<BlobStorageOptions>(
        builder.Configuration.GetSection(BlobStorageOptions.SectionName))
    .AddSingleton(_ =>
    {
        var connectionString = builder.Configuration.GetConnectionString(ConnectionStringNames.BlobStorage)
            ?? throw new InvalidOperationException("ConnectionStrings:BlobStorage is not configured.");

        return new BlobServiceClient(connectionString);
    })
    .AddSingleton<IBoardExportBlobService, BoardExportBlobService>()
    .Configure<CosmosDbOptions>(
        builder.Configuration.GetSection(CosmosDbOptions.SectionName))
    .AddSingleton(_ =>
    {
        var connectionString = builder.Configuration.GetConnectionString(ConnectionStringNames.CosmosDB)
            ?? throw new InvalidOperationException("ConnectionStrings:CosmosDB is not configured.");

        return new CosmosClient(connectionString);
    })
    .AddSingleton(sp =>
    {
        var options = sp.GetRequiredService<IOptions<CosmosDbOptions>>().Value;
        options.Validate();

        return sp.GetRequiredService<CosmosClient>()
            .GetContainer(options.DatabaseName, options.Containers.BoardExport);
    })
    .AddSingleton<IBoardExportDocumentClient, CosmosBoardExportDocumentClient>()
    .AddSingleton<IBoardExportSummaryWriter, JsonBoardExportSummaryWriter>()
    .AddSingleton<BoardExportSummaryWriterRegistry>()
    .AddSingleton<IBoardArchiveBuilder, BoardArchiveBuilder>()
    .AddSingleton<IBoardExportProcessor, BoardExportProcessor>()
    .AddSingleton<ExportContextResolver>()
    .AddSingleton<IBoardExportCompletionHandler, InitialExportCompletionHandler>()
    .AddSingleton<IBoardExportCompletionHandler, ReExportCompletionHandler>()
    .AddSingleton<BoardExportCompletionHandlerRegistry>();

builder.Build().Run();
