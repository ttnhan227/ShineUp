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

// Add JWT Authentication
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
            ValidAudience = builder.Configuration["Jwt:Audience"]
        };
    });

builder.Services.AddAuthorization(); // Add Authorization service

// AddScoped for Repositories
builder.Services.AddScoped<IContestRepositories, ContestRepositories>();
builder.Services.AddScoped<IContestEntryRepositories, ContestEntryRepositories>();
builder.Services.AddScoped<IVoteRepositories, VoteRepositories>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
app.MapControllers();
app.UseHttpsRedirection();

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

