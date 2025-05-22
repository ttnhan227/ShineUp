using Microsoft.EntityFrameworkCore;
using Server;
using Server.Data;

using Server.Helpers;
using Server.Interfaces;
using Server.Repositories;
using Server.Service;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.DataProtection;


var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddControllers();

builder.Services.AddDbContext<DatabaseContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddControllers(); // Add this line
builder.Services.AddSwaggerGen();
builder.Services.AddScoped<IVideoRepository, VideoRepository>();

builder.Services.AddScoped<ICloudinaryService, CloudinaryService>();
builder.Services.Configure<CloundinarySetting>(builder.Configuration.GetSection("CloudinarySettings"));

// Add Repositories
builder.Services.AddScoped<Server.Interfaces.IAuthRepository, Server.Repositories.AuthRepository>();

// Add Distributed Memory Cache for session state
builder.Services.AddDistributedMemoryCache();

// Add Session state for handling the OAuth state
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30); // Set a reasonable timeout
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// Add Data Protection for handling state (still needed for other potential uses)
builder.Services.AddDataProtection();

// Add Authentication
builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = GoogleDefaults.AuthenticationScheme; // Set default challenge scheme to Google
        options.DefaultSignInScheme = "GoogleAuthTemp"; // Set the default sign-in scheme
    })
    .AddCookie("GoogleAuthTemp", options => // Add cookie authentication for temporary storage and sign-in
    {
        options.Cookie.Name = "GoogleAuthTemp"; // Name for the temporary cookie
        options.ExpireTimeSpan = TimeSpan.FromMinutes(5); // Set an expiration time
    })
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(builder.Configuration["Jwt:Key"])),
            ValidateIssuer = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidateAudience = true,
            ValidAudience = builder.Configuration["Jwt:Audience"]
        };
    })
    .AddGoogle(options =>
    {
        options.ClientId = builder.Configuration["Authentication:Google:ClientId"];
        options.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"];
        options.SaveTokens = true; // Save tokens in authentication properties for state management
    });

builder.Services.AddAuthorization(); // Add Authorization service

// AddScoped for Repositories
builder.Services.AddScoped<IContestRepositories, ContestRepositories>();
builder.Services.AddScoped<IContestEntryRepositories, ContestEntryRepositories>();
builder.Services.AddScoped<IVoteRepositories, VoteRepositories>();
builder.Services.AddScoped<ICategoryRepository, CategoryRepository>();
builder.Services.AddScoped<ICommentRepository, CommentRepository>();
builder.Services.AddScoped<ILikeRepository, LikeRepository>();
builder.Services.AddScoped<IMessageRepository, MessageRepository>();
builder.Services.AddScoped<IPrivacyRepository, PrivacyRepository>();
builder.Services.AddScoped<IRoleRepository, RoleRepository>();

var app = builder.Build();

// Enable CORS
app.UseCors(builder => builder
    .AllowAnyOrigin()
    .AllowAnyMethod()
    .AllowAnyHeader());

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseStaticFiles(); // Add Static Files middleware

app.MapControllers();
app.UseHttpsRedirection();

app.UseSession(); // Add Session middleware

app.UseAuthentication(); // Add Authentication middleware
app.UseAuthorization(); // Add Authorization middleware


var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/weatherforecast", () =>
    {
        var forecast = Enumerable.Range(1, 5).Select(index =>
                new WeatherForecast
                (
                    DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
                    Random.Shared.Next(-20, 55),
                    summaries[Random.Shared.Next(summaries.Length)]
                ))
            .ToArray();
        return forecast;
    })
    .WithName("GetWeatherForecast")
    .WithOpenApi();



app.Run();
