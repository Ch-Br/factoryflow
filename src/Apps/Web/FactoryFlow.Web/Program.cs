using FactoryFlow.Modules.Audit.Application;
using FactoryFlow.Modules.Audit.Infrastructure.Services;
using FactoryFlow.Modules.Identity.Domain.Entities;
using FactoryFlow.Modules.Identity.Infrastructure.Seeds;
using FactoryFlow.Modules.Identity.Services;
using FactoryFlow.Modules.Tickets.Application.Commands.AddTicketComment;
using FactoryFlow.Modules.Tickets.Application.Commands.ChangeTicketStatus;
using FactoryFlow.Modules.Tickets.Application.Commands.CreateTicket;
using FactoryFlow.Modules.Tickets.Application.Queries.GetTicketCreationLookups;
using FactoryFlow.Modules.Tickets.Application.Queries.GetTicketDetail;
using FactoryFlow.Modules.Tickets.Application.Queries.GetTicketStatusLookups;
using FactoryFlow.Modules.Tickets.Application.Queries.GetTicketsList;
using FactoryFlow.Modules.Tickets.Domain.Services;
using FactoryFlow.Modules.Tickets.Infrastructure.Services;
using FactoryFlow.SharedKernel.Domain;
using FactoryFlow.Web.Components;
using FactoryFlow.Web.Data;
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
    builder.Services.AddScoped<GetTicketCreationLookupsQueryHandler>();
    builder.Services.AddScoped<GetTicketsListQueryHandler>();
    builder.Services.AddScoped<GetTicketStatusLookupsQueryHandler>();
    builder.Services.AddScoped<GetTicketDetailQueryHandler>();

    // --- Audit module ---
    builder.Services.AddScoped<IAuditWriter, AuditWriter>();

    // --- Blazor ---
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

    app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
    app.UseAntiforgery();
    app.UseAuthentication();
    app.UseAuthorization();

    app.MapStaticAssets();

    // Map API endpoints
    FactoryFlow.Modules.Tickets.Presentation.TicketsEndpoints.Map(app);

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
