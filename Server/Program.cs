using Google.Apis.Auth;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Server;
using Server.Data;
using Server.Helpers;
using Server.Interfaces;
using Server.Repositories;
using Server.Services;
using System.Text;
using System.Security.Claims;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();

// Remove duplicate AddControllers and configure JSON
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = null;
        options.JsonSerializerOptions.WriteIndented = true;
    });

builder.Services.AddDbContext<DatabaseContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "ShineUp API",
        Version = "v1",
        Description = "API for ShineUp social media platform"
    });

    // Add JWT Authentication
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Enter 'Bearer' [space] and then your token",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                },
                Scheme = "oauth2",
                Name = "Bearer",
                In = ParameterLocation.Header
            },
            new List<string>()
        }
    });
});
builder.Services.AddScoped<IVideoRepository, VideoRepository>();

builder.Services.AddScoped<ICloudinaryService, CloudinaryService>();
builder.Services.Configure<CloundinarySetting>(builder.Configuration.GetSection("CloudinarySettings"));

// Add Repositories
builder.Services.AddScoped<IAuthRepository, AuthRepository>();

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
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(builder.Configuration["Jwt:Key"])),
            ValidateIssuer = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidateAudience = true,
            ValidAudience = builder.Configuration["Jwt:Audience"],
            ValidateLifetime = true,
            NameClaimType = ClaimTypes.NameIdentifier, // Changed to NameIdentifier
            RoleClaimType = ClaimTypes.Role
        };
        
        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = context =>
            {
                var userId = context.Principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    context.Fail("UserID claim is missing from token");
                }
                return Task.CompletedTask;
            },
            OnMessageReceived = context => 
            {
                return Task.CompletedTask;
            }
        };
    });

// Configure Google token validation
builder.Services.Configure<GoogleJsonWebSignature.ValidationSettings>(options =>
{
    options.Audience = new[] { builder.Configuration["Authentication:Google:ClientId"] };
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
builder.Services.AddScoped<IUserProfileRepository, UserProfileRepository>();

// Update CORS configuration
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(builder =>
    {
        builder.SetIsOriginAllowed(_ => true)
               .AllowAnyMethod()
               .AllowAnyHeader()
               .AllowCredentials();
    });
});

builder.Services.AddLogging(logging =>
{
    logging.ClearProviders();
    logging.AddConsole();
    logging.AddDebug();
    logging.SetMinimumLevel(LogLevel.Information);
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "ShineUp API v1");
    });
}

// Correct middleware order
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseCors();  // Must be after UseRouting and before UseAuthentication

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
