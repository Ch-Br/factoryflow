using FactoryFlow.Modules.Audit.Application;
using FactoryFlow.Modules.Audit.Infrastructure.Services;
using FactoryFlow.Modules.Notifications.Application;
using FactoryFlow.Modules.Notifications.Application.Commands.MarkNotificationRead;
using FactoryFlow.Modules.Notifications.Application.Queries.GetMyNotifications;
using FactoryFlow.Modules.Notifications.Infrastructure.Services;
using FactoryFlow.Modules.Identity;
using FactoryFlow.Modules.Identity.Domain.Entities;
using FactoryFlow.Modules.Identity.Infrastructure.Seeds;
using FactoryFlow.Modules.Identity.Services;
using FactoryFlow.Modules.Tickets.Application.Commands.AddTicketAttachment;
using FactoryFlow.Modules.Tickets.Application.Commands.AddTicketComment;
using FactoryFlow.Modules.Tickets.Application.Commands.ChangeTicketStatus;
using FactoryFlow.Modules.Tickets.Application.Commands.CreateTicket;
using FactoryFlow.Modules.Tickets.Application.Commands.EscalateOverdueTickets;
using FactoryFlow.Modules.Tickets.Application.Commands.UpdateTicket;
using FactoryFlow.Modules.Tickets.Application.Queries.GetOverdueTickets;
using FactoryFlow.Modules.Tickets.Application.Queries.GetTicketCreationLookups;
using FactoryFlow.Modules.Tickets.Application.Queries.GetTicketDetail;
using FactoryFlow.Modules.Tickets.Application.Queries.GetTicketStatusLookups;
using FactoryFlow.Modules.Tickets.Application.Queries.GetTicketsList;
using FactoryFlow.Modules.Tickets.Domain.Services;
using FactoryFlow.Modules.Tickets.Infrastructure.Services;
using FactoryFlow.SharedKernel.Domain;
using FactoryFlow.SharedKernel.Infrastructure;
using FactoryFlow.Web.Infrastructure;
using FactoryFlow.Web.Components;
using FactoryFlow.Web.Data;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog((ctx, cfg) => cfg
        .ReadFrom.Configuration(ctx.Configuration)
        .WriteTo.Console());

    // --- EF Core + PostgreSQL ---
    builder.Services.AddDbContext<FactoryFlowDbContext>(options =>
        options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

    // Expose DbContext as base type for modules that depend on DbContext directly
    builder.Services.AddScoped<DbContext>(sp => sp.GetRequiredService<FactoryFlowDbContext>());

    // --- ASP.NET Core Identity ---
    builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
        {
            options.Password.RequireDigit = true;
            options.Password.RequiredLength = 6;
            options.Password.RequireNonAlphanumeric = false;
            options.SignIn.RequireConfirmedAccount = false;
        })
        .AddEntityFrameworkStores<FactoryFlowDbContext>()
        .AddDefaultTokenProviders();

    builder.Services.AddAuthorization(options =>
    {
        options.AddPolicy(AuthPolicies.TicketsUse, policy =>
            policy.RequireRole(AppRoles.User, AppRoles.Supervisor, AppRoles.Admin));
        options.AddPolicy(AuthPolicies.TicketsManage, policy =>
            policy.RequireRole(AppRoles.Supervisor, AppRoles.Admin));
    });

    builder.Services.ConfigureApplicationCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.AccessDeniedPath = "/Account/AccessDenied";
    });

    // --- SharedKernel services ---
    builder.Services.AddHttpContextAccessor();
    builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();

    // --- Tickets module ---
    builder.Services.AddScoped<ITicketNumberGenerator, TicketNumberGenerator>();
    builder.Services.AddScoped<CreateTicketCommandHandler>();
    builder.Services.AddScoped<ChangeTicketStatusCommandHandler>();
    builder.Services.AddScoped<AddTicketCommentCommandHandler>();
    builder.Services.AddScoped<AddTicketAttachmentCommandHandler>();
    builder.Services.AddScoped<UpdateTicketCommandHandler>();
    builder.Services.AddScoped<EscalateOverdueTicketsCommandHandler>();
    builder.Services.AddScoped<GetTicketCreationLookupsQueryHandler>();
    builder.Services.AddScoped<GetTicketsListQueryHandler>();
    builder.Services.AddScoped<GetTicketStatusLookupsQueryHandler>();
    builder.Services.AddScoped<GetTicketDetailQueryHandler>();
    builder.Services.AddScoped<GetOverdueTicketsQueryHandler>();

    // --- File storage ---
    builder.Services.AddSingleton<IFileStorage, LocalDiskFileStorage>();

    // --- Notifications module ---
    builder.Services.AddScoped<IEscalationNotificationPublisher, EscalationNotificationPublisher>();
    builder.Services.AddScoped<GetMyNotificationsQueryHandler>();
    builder.Services.AddScoped<MarkNotificationReadCommandHandler>();

    // --- Audit module ---
    builder.Services.AddScoped<IAuditWriter, AuditWriter>();

    // --- Blazor ---
    builder.Services.AddCascadingAuthenticationState();
    builder.Services.AddRazorComponents()
        .AddInteractiveServerComponents();

    // --- API (Minimal API for Swagger/tests) ---
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(c =>
    {
        c.SwaggerDoc("v1", new() { Title = "FactoryFlow API", Version = "v1" });
    });

    var app = builder.Build();

    // --- Middleware pipeline ---
    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }
    else
    {
        app.UseExceptionHandler("/Error", createScopeForErrors: true);
    }

    app.UseWhen(
        context => !context.Request.Path.StartsWithSegments("/api"),
        branch => branch.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true));
    app.UseAntiforgery();
    app.UseAuthentication();
    app.UseAuthorization();

    app.MapStaticAssets();

    // Map API endpoints
    FactoryFlow.Modules.Tickets.Presentation.TicketsEndpoints.Map(app);
    FactoryFlow.Modules.Notifications.Presentation.NotificationsEndpoints.Map(app);

    app.MapRazorComponents<App>()
        .AddInteractiveServerRenderMode();

    // --- Database migration & seed ---
    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<FactoryFlowDbContext>();
        await db.Database.MigrateAsync();
        await IdentitySeedData.SeedUsersAsync(scope.ServiceProvider);
    }

    app.Run();
}
catch (Exception ex) when (ex is not HostAbortedException)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}

// Needed for WebApplicationFactory in integration tests
public partial class Program;
