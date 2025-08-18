using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using TaskManagementMvc.Data;
using TaskManagementMvc.Models;
using TaskManagementMvc.Services;
using TaskManagementMvc.Services.Authorization;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
builder.Services.AddDbContext<TaskManagementContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

// Configure Identity
builder.Services.AddIdentity<ApplicationUser, ApplicationRole>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequiredLength = 6;

    options.User.RequireUniqueEmail = true;
    options.SignIn.RequireConfirmedEmail = false;
})
.AddEntityFrameworkStores<TaskManagementContext>()
.AddDefaultTokenProviders();

// Configure cookie authentication
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/Account/Logout";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.ExpireTimeSpan = TimeSpan.FromHours(8);
    options.SlidingExpiration = true;
});

// Register permission-based authorization
builder.Services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();
builder.Services.AddScoped<IAuthorizationHandler, PermissionAuthorizationHandler>();

// App services
builder.Services.AddSingleton<IEmailSender, SmtpEmailSender>();
builder.Services.AddSingleton<ITelegramSender, TelegramSender>();
builder.Services.AddScoped<IRazorViewRenderer, RazorViewRenderer>();
builder.Services.AddScoped<ProjectAccessService>();
builder.Services.AddScoped<IInvoiceAutomationService, InvoiceAutomationService>();
builder.Services.AddHostedService<InvoiceAutomationHostedService>();

// Configure notification settings
builder.Services.Configure<NotificationSettings>(
    builder.Configuration.GetSection("Notifications"));

// Configure Redis connection (conditionally)
var notificationSettings = builder.Configuration.GetSection("Notifications").Get<NotificationSettings>() ?? new NotificationSettings();
if (notificationSettings.UseRedis)
{
    var redisConnectionString = builder.Configuration.GetConnectionString("Redis") ?? "localhost:6379";
    try
    {
        builder.Services.AddSingleton<IConnectionMultiplexer>(provider =>
        {
            return ConnectionMultiplexer.Connect(redisConnectionString);
        });
    }
    catch (Exception ex)
    {
        // اگر Redis در دسترس نباشد، null اضافه کنیم
        Console.WriteLine($"Redis connection failed: {ex.Message}. Notifications will work without Redis persistence.");
        builder.Services.AddSingleton<IConnectionMultiplexer>(provider => null);
    }
}
else
{
    // اگر Redis غیرفعال باشد، null service اضافه کنیم
    builder.Services.AddSingleton<IConnectionMultiplexer>(provider => null);
}

// Configure SignalR for real-time notifications
builder.Services.AddSignalR();

// Add scalable notification service
builder.Services.AddScoped<IScalableNotificationService, ScalableNotificationService>();

// Add both notification services
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<INotificationService, NotificationService>();

var app = builder.Build();

// auto-migrate DB on startup
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<TaskManagementContext>();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();

    db.Database.Migrate();

    // Initialize database with sample data
    DbInitializer.Initialize(db, userManager, roleManager);
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

// Add authentication and authorization middleware
app.UseAuthentication();
app.UseAuthorization();

// Map SignalR hub
app.MapHub<NotificationHub>("/notificationHub");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();