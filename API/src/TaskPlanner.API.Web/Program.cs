using System.Text;
using System.Text.Json.Serialization;
using AutoMapper;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using MongoDB.Driver;
using TaskPlanner.API.Core.Configuration;
using TaskPlanner.API.Core.Interfaces;
using TaskPlanner.API.Core.Models;
using TaskPlanner.API.Core.Models.Identity;
using TaskPlanner.API.Core.Services;
using TaskPlanner.API.Data.Configuration;
using TaskPlanner.API.Data.Interfaces;
using TaskPlanner.API.Data.Repositories;
using TaskPlanner.API.Web.Configuration;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers().AddJsonOptions(o =>
{
    o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
});;

builder.Services.AddCors(options =>
{
    options.AddPolicy("ViteDev", policy =>
        policy
            .WithOrigins(
                "http://127.0.0.1:5173",
                "http://localhost:5173"
            )
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials()
    );
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var jwtSection = builder.Configuration.GetSection("Jwt");
var mongoSection = builder.Configuration.GetSection("MongoDb");
var jwtSecret = jwtSection["Secret"] ?? throw new InvalidOperationException("Jwt:Secret is missing from configuration.");

builder.Services.Configure<JwtOptions>(jwtSection);

builder.Services.AddSingleton<IMongoClient>(_ =>
{
    var connectionString = mongoSection["ConnectionString"] ?? "mongodb://localhost:27017/?replicaSet=rs0&directConnection=true";
    return new MongoClient(connectionString);
});

builder.Services.AddScoped<IMongoDatabase>(sp =>
{
    var client = sp.GetRequiredService<IMongoClient>();
    var databaseName = mongoSection["DatabaseName"] ?? "TaskPlanner";
    return client.GetDatabase(databaseName);
});

builder.Services.SetupServices();
builder.Services.SetupDataServices();
builder.Services.SetupValidation();

builder.Services.AddScoped<IMapper>(sp =>
{
    var loggerFactory = sp.GetRequiredService<ILoggerFactory>();

    var config = new MapperConfiguration(cfg =>
    {
        cfg.AddMaps(AppDomain.CurrentDomain.GetAssemblies());
    }, loggerFactory);

    config.AssertConfigurationIsValid();

    return config.CreateMapper();
});

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateIssuerSigningKey = true,
            ValidateLifetime = true,
            ValidIssuer = jwtSection["Issuer"],
            ValidAudience = jwtSection["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret))
        };
    });

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseCors("ViteDev");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

public partial class Program;
