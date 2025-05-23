using Microsoft.Extensions.Configuration;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

var builder = WebApplication.CreateBuilder(args);

// Add secrets configuration
builder.Configuration.SetBasePath(builder.Environment.ContentRootPath)
    .AddJsonFile("appsettings.secrets.json", optional: false, reloadOnChange: true);

// Add services to the container.
builder.Services.AddControllersWithViews();

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
            await context.HttpContext.SignOutAsync(Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationDefaults.AuthenticationScheme);
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
    client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
    
    return client;
});

builder.Services.AddHttpContextAccessor();

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
    OnPrepareResponse = ctx =>
    {
        ctx.Context.Response.Headers.Append("Cache-Control", "public, max-age=600");
    }
});

app.UseRouting();

app.UseAuthentication(); // Add this line before UseAuthorization
app.UseAuthorization();

app.UseEndpoints(endpoints =>
{
    endpoints.MapControllerRoute(
        name: "default",
        pattern: "{controller=Home}/{action=Index}/{id?}");
        
    // Add explicit route for google-auth
    endpoints.MapControllerRoute(
        name: "googleAuth",
        pattern: "Auth/google-auth",
        defaults: new { controller = "Auth", action = "GoogleAuth" });
});

app.Run();
