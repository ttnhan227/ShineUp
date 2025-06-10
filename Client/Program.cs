using Client.Models;
using Client.Service;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http.Features;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

// Add secrets configuration
builder.Configuration.SetBasePath(builder.Environment.ContentRootPath)
    .AddJsonFile("appsettings.secrets.json", false, true);

// Add services to the container.
builder.Services.AddControllersWithViews()
    .AddJsonOptions(options => { options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.Preserve; });

// Configure request size limits
builder.Services.Configure<IISServerOptions>(options =>
{
    options.MaxRequestBodySize = 100 * 1024 * 1024; // 100MB
});

builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 100 * 1024 * 1024; // 100MB
});

// Add Session
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// Add HttpClient factory
builder.Services.AddHttpClient("API", client =>
{
    var apiBaseUrl = builder.Configuration["ApiBaseUrl"];
    if (string.IsNullOrEmpty(apiBaseUrl))
    {
        throw new InvalidOperationException("ApiBaseUrl is not configured in appsettings.json");
    }

    client.BaseAddress = new Uri(apiBaseUrl);
});


// Register IHttpContextAccessor
builder.Services.AddHttpContextAccessor();

builder.Services.AddAuthentication(options =>
    {
        options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    })
    .AddCookie(options =>
    {
        options.LoginPath = "/Auth/Login"; // Changed to Auth controller
        options.LogoutPath = "/Auth/Logout"; // Changed to Auth controller
        options.AccessDeniedPath = "/Auth/AccessDenied"; // Changed to Auth controller
        options.Cookie.Name = "ShineUpAuth";
        options.ClaimsIssuer = "ShineUpClient"; // Explicitly set claims issuer

        // Ensure NameIdentifier is correctly mapped
        options.Events.OnValidatePrincipal = async context =>
        {
            var userIdClaim = context.Principal?.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
            {
                // If NameIdentifier is missing, try to re-authenticate or sign out
                context.RejectPrincipal();
                await context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            }
        };
    });

builder.Services.AddSingleton(provider =>
{
    var configuration = provider.GetRequiredService<IConfiguration>();
    var apiBaseUrl = configuration["ApiBaseUrl"];

    var handler = new HttpClientHandler
    {
        UseCookies = true,
        AllowAutoRedirect = true
    };

    var client = new HttpClient(handler);
    client.BaseAddress = new Uri(apiBaseUrl!);
    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

    return client;
});

builder.Services.Configure<OpenRouterOptions>(builder.Configuration.GetSection("OpenRouter"));
builder.Services.AddHttpClient<OpenRouterService>();


var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles(new StaticFileOptions
{
    OnPrepareResponse = ctx => { ctx.Context.Response.Headers.Append("Cache-Control", "public, max-age=600"); }
});

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

// Add Session middleware
app.UseSession();

// Area route
app.MapControllerRoute(
    "areas",
    "{area:exists}/{controller=Home}/{action=Index}/{id?}");

// Opportunities route with category filter
app.MapControllerRoute(
    "opportunities",
    "Opportunities/{action=Index}/{id?}",
    new { controller = "Opportunities" });

// Default route
app.MapControllerRoute(
    "default",
    "{controller=Home}/{action=Index}/{id?}");

// Explicit route for google-auth
app.MapControllerRoute(
    "googleAuth",
    "Auth/google-auth",
    new { controller = "Auth", action = "GoogleAuth" });

app.MapControllers();

app.Run();