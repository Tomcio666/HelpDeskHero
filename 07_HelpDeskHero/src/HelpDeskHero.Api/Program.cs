using System.Text;
using Hangfire;
using Hangfire.SqlServer;
using HelpDeskHero.Api.Application.Interfaces;
using HelpDeskHero.Api.Application.Services;
using HelpDeskHero.Api.BackgroundJobs;
using HelpDeskHero.Api.BackgroundJobs.Contracts;
using HelpDeskHero.Api.Domain;
using HelpDeskHero.Api.Hubs;
using HelpDeskHero.Api.Infrastructure.Background;
using HelpDeskHero.Api.Infrastructure.Notifications;
using HelpDeskHero.Api.Infrastructure.Persistence;
using HelpDeskHero.Api.Infrastructure.Services;
using HelpDeskHero.Api.Infrastructure.Storage;
using HelpDeskHero.Api.Middleware;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

const string CorsPolicyName = "BlazorUi";

builder.Services.AddCors(options =>
{
    options.AddPolicy(CorsPolicyName, policy =>
    {
        policy.WithOrigins("https://localhost:7045", "http://localhost:5045")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddIdentityCore<ApplicationUser>(options =>
{
    options.Password.RequiredLength = 8;
    options.Password.RequireDigit = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireNonAlphanumeric = false;

    options.User.RequireUniqueEmail = false;
})
.AddRoles<IdentityRole>()
.AddEntityFrameworkStores<AppDbContext>()
.AddSignInManager()
.AddDefaultTokenProviders();

var jwtSection = builder.Configuration.GetSection("Jwt");
var issuer = jwtSection["Issuer"] ?? throw new InvalidOperationException("Missing Jwt:Issuer.");
var audience = jwtSection["Audience"] ?? throw new InvalidOperationException("Missing Jwt:Audience.");
var key = jwtSection["Key"] ?? throw new InvalidOperationException("Missing Jwt:Key.");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateIssuerSigningKey = true,
            ValidateLifetime = true,
            ValidIssuer = issuer,
            ValidAudience = audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
            ClockSkew = TimeSpan.FromSeconds(30)
        };

        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                var path = context.HttpContext.Request.Path;

                if (!string.IsNullOrWhiteSpace(accessToken) &&
                    path.StartsWithSegments("/hubs/tickets"))
                {
                    context.Token = accessToken;
                }

                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("CanManageTickets", p => p.RequireRole("Admin", "Agent"));
    options.AddPolicy("CanViewAudit", p => p.RequireRole("Admin"));
});

builder.Services.AddHttpContextAccessor();

builder.Services.AddScoped<TokenService>();
builder.Services.AddScoped<RefreshTokenService>();
builder.Services.AddScoped<AuditService>();

builder.Services.AddScoped<IFileStorage, LocalFileStorage>();

builder.Services.AddHttpClient<WebhookNotificationSender>();
builder.Services.AddScoped<INotificationSender, EmailNotificationSender>();
builder.Services.AddScoped<INotificationSender, WebhookNotificationSender>();
builder.Services.AddScoped<INotificationSender, InAppNotificationSender>();
builder.Services.AddScoped<INotificationDispatcher, NotificationDispatcher>();

builder.Services.AddScoped<INotificationJob, NotificationJob>();
builder.Services.AddScoped<ITicketLiveNotifier, SignalRTicketLiveNotifier>();
builder.Services.AddScoped<ISlaCalculator, SlaCalculator>();
builder.Services.AddScoped<ISlaMonitorService, SlaMonitorService>();
builder.Services.AddScoped<ITicketAssignmentService, TicketAssignmentService>();
builder.Services.AddScoped<IOutboxWriter, OutboxWriter>();

builder.Services.AddHostedService<OutboxProcessorService>();

builder.Services.AddSignalR();

builder.Services.AddHangfire(config =>
{
    config.UseSimpleAssemblyNameTypeSerializer()
          .UseRecommendedSerializerSettings()
          .UseSqlServerStorage(
              builder.Configuration.GetConnectionString("DefaultConnection"),
              new SqlServerStorageOptions
              {
                  PrepareSchemaIfNecessary = true
              });
});

builder.Services.AddHangfireServer();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseMiddleware<GlobalExceptionMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors(CorsPolicyName);

app.UseAuthentication();
app.UseAuthorization();

app.UseHangfireDashboard("/hangfire");

app.MapControllers();
app.MapHub<TicketsHub>("/hubs/tickets");

RecurringJob.AddOrUpdate<INotificationJob>(
    "daily-summary",
    job => job.SendDailySummaryAsync(default),
    "0 7 * * *");

RecurringJob.AddOrUpdate<ISlaMonitorService>(
    "sla-monitor",
    job => job.CheckBreachesAsync(default),
    "*/5 * * * *");

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();
}

await SeedData.InitializeAsync(app.Services);

app.Run();

public partial class Program
{
}