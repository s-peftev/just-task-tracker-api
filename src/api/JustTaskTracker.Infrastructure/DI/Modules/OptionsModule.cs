using JustTaskTracker.Infrastructure.Archiving;
using JustTaskTracker.Infrastructure.Common.Constants;
using JustTaskTracker.Infrastructure.Common.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace JustTaskTracker.Infrastructure.DI.Modules;

internal static class OptionsModule
{
    internal static IServiceCollection AddOptionsModule(this IServiceCollection services, IConfiguration configuration)
    {
        var frontendOptions = configuration
            .GetSection(ConfigSections.Frontend)
            .Get<FrontendOptions>() ?? new FrontendOptions();

        services.AddSingleton(frontendOptions);

        var azureAdOptions = configuration
            .GetSection(ConfigSections.AzureAd)
            .Get<AzureAdOptions>()
            ?? throw new InvalidOperationException($"{ConfigSections.AzureAd} section is not configured.");

        azureAdOptions.Validate();
        services.AddSingleton(azureAdOptions);

        var paginationDefaultsOptions = configuration
            .GetSection(ConfigSections.PaginationDefaults)
            .Get<PaginationDefaultsOptions>()
            ?? throw new InvalidOperationException($"{ConfigSections.PaginationDefaults} section is not configured.");

        paginationDefaultsOptions.Validate();
        services.AddSingleton(paginationDefaultsOptions);

        var serviceBusOptions = configuration
            .GetSection(ConfigSections.ServiceBus)
            .Get<ServiceBusOptions>()
            ?? throw new InvalidOperationException($"{ConfigSections.ServiceBus} section is not configured.");

        serviceBusOptions.Validate();
        services.AddSingleton(serviceBusOptions);

        var cosmosDbOptions = configuration
            .GetSection(ConfigSections.CosmosDB)
            .Get<CosmosDbOptions>()
            ?? throw new InvalidOperationException($"{ConfigSections.CosmosDB} section is not configured.");

        cosmosDbOptions.Validate();
        services.AddSingleton(cosmosDbOptions);

        var internalApiOptions = configuration
            .GetSection(InternalApiOptions.SectionName)
            .Get<InternalApiOptions>()
            ?? throw new InvalidOperationException($"{InternalApiOptions.SectionName} section is not configured.");

        internalApiOptions.Validate();
        services.AddSingleton(internalApiOptions);

        var stripeOptions = configuration
            .GetSection(ConfigSections.Stripe)
            .Get<StripeOptions>()
            ?? throw new InvalidOperationException($"{ConfigSections.Stripe} section is not configured.");

        stripeOptions.Validate();
        services.AddSingleton(stripeOptions);

        var acsOptions = configuration
            .GetSection(ConfigSections.Acs)
            .Get<AcsOptions>()
            ?? throw new InvalidOperationException($"{ConfigSections.Acs} section is not configured.");

        acsOptions.Validate();
        services.AddSingleton(acsOptions);

        var aiSearchOptions = configuration
            .GetSection(ConfigSections.AiSearch)
            .Get<AiSearchOptions>()
            ?? throw new InvalidOperationException($"{ConfigSections.AiSearch} section is not configured.");

        aiSearchOptions.Validate();
        services.AddSingleton(aiSearchOptions);

        var azureOpenAiOptions = configuration
            .GetSection(ConfigSections.AzureOpenAi)
            .Get<AzureOpenAiOptions>()
            ?? throw new InvalidOperationException($"{ConfigSections.AzureOpenAi} section is not configured.");

        azureOpenAiOptions.Validate();
        services.AddSingleton(azureOpenAiOptions);

        var keyVaultOptions = configuration
            .GetSection(ConfigSections.KeyVault)
            .Get<KeyVaultOptions>();

        if (keyVaultOptions is not null && !string.IsNullOrWhiteSpace(keyVaultOptions.Uri))
        {
            keyVaultOptions.Validate();
            services.AddSingleton(keyVaultOptions);
        }

        return services;
    }
}
