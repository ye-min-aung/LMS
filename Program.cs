using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Localization;
using Microsoft.EntityFrameworkCore;
using Amazon.S3;
using Amazon.Extensions.NETCore.Setup;
using System.Globalization;
using LMSPlatform.Data;
using LMSPlatform.Models;
using LMSPlatform.Services.Middleware;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddDbContext<LMSDbContext>(options =>
    options.UseMySql(builder.Configuration.GetConnectionString("DefaultConnection"), 
        new MySqlServerVersion(new Version(8, 0, 21))));

builder.Services.AddDefaultIdentity<User>(options => 
{
    options.SignIn.RequireConfirmedAccount = false;
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequiredLength = 8;
    options.Password.RequireNonAlphanumeric = false;
    options.User.RequireUniqueEmail = true;
})
.AddRoles<IdentityRole<int>>()
.AddEntityFrameworkStores<LMSDbContext>();

// Add authorization policies
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
    options.AddPolicy("StudentOnly", policy => policy.RequireRole("Student", "Admin"));
    options.AddPolicy("ApprovedStudent", policy => 
        policy.RequireAssertion(context =>
            context.User.IsInRole("Admin") ||
            (context.User.IsInRole("Student") && context.User.HasClaim(c => c.Type == "IsApproved" && c.Value == "true"))));
    
    // Course access policy
    options.AddPolicy("CourseAccess", policy =>
        policy.RequireAuthenticatedUser());
    
    // Lesson access policy with prerequisite checking
    options.AddPolicy("LessonAccess", policy =>
        policy.RequireAuthenticatedUser());
    
    // Content management policy (admin only)
    options.AddPolicy("ContentManagement", policy =>
        policy.RequireRole("Admin"));
    
    // Payment management policy
    options.AddPolicy("PaymentManagement", policy =>
        policy.RequireRole("Admin"));
});

// Register authorization handlers
builder.Services.AddScoped<Microsoft.AspNetCore.Authorization.IAuthorizationHandler, 
    LMSPlatform.Services.Authorization.CourseAccessHandler>();
builder.Services.AddScoped<Microsoft.AspNetCore.Authorization.IAuthorizationHandler, 
    LMSPlatform.Services.Authorization.LessonAccessHandler>();
builder.Services.AddScoped<Microsoft.AspNetCore.Authorization.IAuthorizationHandler, 
    LMSPlatform.Services.Authorization.AdminOperationHandler>();

// Add AWS services
builder.Services.AddDefaultAWSOptions(builder.Configuration.GetAWSOptions());
builder.Services.AddAWSService<IAmazonS3>();

// Add HttpClient for KBZ Pay
builder.Services.AddHttpClient("KBZPay", client =>
{
    client.Timeout = TimeSpan.FromSeconds(30);
    client.DefaultRequestHeaders.Add("Accept", "application/json");
});

// Add application services
builder.Services.AddScoped<LMSPlatform.Services.IEmailService, LMSPlatform.Services.EmailService>();
builder.Services.AddScoped<LMSPlatform.Services.IUserService, LMSPlatform.Services.UserService>();
builder.Services.AddScoped<LMSPlatform.Services.ICourseService, LMSPlatform.Services.CourseService>();
builder.Services.AddScoped<LMSPlatform.Services.ILessonProgressService, LMSPlatform.Services.LessonProgressService>();
builder.Services.AddScoped<LMSPlatform.Services.IPrerequisiteService, LMSPlatform.Services.PrerequisiteService>();
builder.Services.AddScoped<LMSPlatform.Services.IPaymentService, LMSPlatform.Services.PaymentService>();
builder.Services.AddScoped<LMSPlatform.Services.IVideoService, LMSPlatform.Services.VideoService>();
builder.Services.AddScoped<LMSPlatform.Services.IQuizService, LMSPlatform.Services.QuizService>();
builder.Services.AddScoped<LMSPlatform.Services.IAssignmentService, LMSPlatform.Services.AssignmentService>();
builder.Services.AddScoped<LMSPlatform.Services.ICertificateService, LMSPlatform.Services.CertificateService>();
builder.Services.AddScoped<LMSPlatform.Services.IContentAccessService, LMSPlatform.Services.ContentAccessService>();

// Register Lazy<ICertificateService> to break circular dependency
builder.Services.AddScoped(sp => new Lazy<LMSPlatform.Services.ICertificateService>(
    () => sp.GetRequiredService<LMSPlatform.Services.ICertificateService>()));

// Add localization services
builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");
builder.Services.Configure<RequestLocalizationOptions>(options =>
{
    var supportedCultures = new[]
    {
        new CultureInfo("en"),
        new CultureInfo("my")
    };
    options.DefaultRequestCulture = new RequestCulture("en");
    options.SupportedCultures = supportedCultures;
    options.SupportedUICultures = supportedCultures;
    
    // Add cookie-based culture provider for persistence
    options.RequestCultureProviders.Insert(0, new CookieRequestCultureProvider
    {
        CookieName = ".AspNetCore.Culture"
    });
});

builder.Services.AddRazorPages()
    .AddViewLocalization()
    .AddDataAnnotationsLocalization();
builder.Services.AddControllers();

var app = builder.Build();

// Use localization middleware
app.UseRequestLocalization();

// Configure the HTTP request pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// Add security logging middleware
app.UseSecurityLogging();

app.UseAuthentication();
app.UseAuthorization();

app.MapRazorPages();
app.MapControllers();

// Initialize database
using (var scope = app.Services.CreateScope())
{
    try
    {
        var context = scope.ServiceProvider.GetRequiredService<LMSDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<int>>>();
        
        // Create database if it doesn't exist
        var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
        var connectionStringBuilder = new MySqlConnector.MySqlConnectionStringBuilder(connectionString!);
        var databaseName = connectionStringBuilder.Database;
        connectionStringBuilder.Database = null; // Connect without specifying database
        
        using (var connection = new MySqlConnector.MySqlConnection(connectionStringBuilder.ConnectionString))
        {
            await connection.OpenAsync();
            using (var command = connection.CreateCommand())
            {
                command.CommandText = $"CREATE DATABASE IF NOT EXISTS `{databaseName}`";
                await command.ExecuteNonQueryAsync();
            }
        }
        
        // Now create tables
        await context.Database.EnsureCreatedAsync();
        
        // Create roles
        string[] roles = { "Admin", "Student", "Guest" };
        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole<int>(role));
            }
        }
        
        // Create admin user
        const string adminEmail = "admin@lms.com";
        var adminUser = await userManager.FindByEmailAsync(adminEmail);
        if (adminUser == null)
        {
            adminUser = new User
            {
                UserName = adminEmail,
                Email = adminEmail,
                FullName = "System Administrator",
                Role = "Admin",
                IsApprovedStudent = true,
                EmailConfirmed = true
            };
            
            var result = await userManager.CreateAsync(adminUser, "Admin123!");
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(adminUser, "Admin");
            }
        }
        
        Console.WriteLine("Database initialized successfully!");
        Console.WriteLine($"Admin login: {adminEmail} / Admin123!");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Database initialization error: {ex.Message}");
    }
}

app.Run();

// Make Program class accessible for integration tests
public partial class Program { }
