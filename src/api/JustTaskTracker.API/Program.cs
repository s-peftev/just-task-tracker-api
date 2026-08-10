using JustTaskTracker.API.Configurators;
using JustTaskTracker.API.Filters;
using JustTaskTracker.API.Handlers;
using JustTaskTracker.API.Middleware;
using JustTaskTracker.Infrastructure.Boards.Hubs;
using JustTaskTracker.Application.DI;
using JustTaskTracker.Infrastructure.Common.Constants;
using JustTaskTracker.Infrastructure.Common.Constants.Hubs;
using JustTaskTracker.Infrastructure.DI;
using JustTaskTracker.Infrastructure.DI.Modules;
using JustTaskTracker.Persistence.DI;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddControllers(options =>
{
    options.Filters.Add<PaginationValidationFilter>();
    options.Filters.Add<ApiResponseEnvelopeFilter>();

    options.Conventions.Add(new PrefixConventionConfigurator("api"));
});

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddApplication(builder.Configuration);
builder.Services.AddPersistence(builder.Configuration);

builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

SerilogConfigurator.Configure(builder.Configuration);
builder.Host.UseSerilog();

var app = builder.Build();

app.MapDefaultEndpoints();

app.UseMiddleware<CorrelationIdMiddleware>();
app.UseMiddleware<InternalApiKeyMiddleware>();
app.UseExceptionHandler();
app.UseHttpsRedirection();
app.UseCors(CorsPolicies.DefaultCorsPolicy);

app.UseAuthentication();
app.UseAuthorization();

app.UseHangfireDashboardModule();

app.MapControllers();

app.MapHub<BoardExportStatusHub>(HubPaths.BoardExportStatus);
app.MapHub<BoardActionsHub>(HubPaths.BoardActions);

app.Run();
