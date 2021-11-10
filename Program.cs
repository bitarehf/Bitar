using System.Text;
using Bitar.Hubs;
using Bitar.Models;
using Bitar.Models.Settings;
using Bitar.Repositories;
using Bitar.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddUserSecrets<Program>(optional: false); // Forces user secrets to load.

// Add services to the container.
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    {
        options.EnableSensitiveDataLogging();
        options.UseNpgsql(
            builder.Configuration.GetConnectionString("DefaultConnection"));
    }
);

builder.Services.AddDefaultIdentity<ApplicationUser>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

builder.Services.Configure<IdentityOptions>(options =>
{
    // Password settings.
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;

    // Lockout settings.
    options.Lockout.MaxFailedAccessAttempts = 10;

    // User settings.
    options.User.AllowedUserNameCharacters = "aábcdðeéfghiíjklmnoópqrstuvwxyýzþæöAÁBCDÐEÉFGHIÍJKLMNOÓPQRSTUVWXYÝZÞÆÖ0123456789 -._@+";
    options.User.RequireUniqueEmail = true;
});

builder.Services
    .AddAuthentication(options =>
    {
        // Identity made Cookie authentication the default.
        // However, we want JWT Bearer Auth to be the default.
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;

    })
    .AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = false;
        options.SaveToken = true;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            ValidIssuer = "https://bitar.is",
            ValidAudience = "https://bitar.is",
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration.GetSection("JwtSettings:JwtKey").Value)),
        };
    });

builder.Services.Configure<ForwardedHeadersOptions>(options => options.ForwardedHeaders = ForwardedHeaders.All);
builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("JwtSettings"));
builder.Services.Configure<BitcoinSettings>(builder.Configuration.GetSection("BitcoinSettings"));
builder.Services.Configure<LandsbankinnSettings>(builder.Configuration.GetSection("LandsbankinnSettings"));
builder.Services.Configure<KrakenSettings>(builder.Configuration.GetSection("KrakenSettings"));
builder.Services.Configure<JaSettings>(builder.Configuration.GetSection("JaSettings"));
builder.Services.Configure<DilisenseSettings>(builder.Configuration.GetSection("DilisenseSettings"));

builder.Services.AddHttpClient<ArionService>();
builder.Services.AddHttpClient<BlockchainService>();

builder.Services.AddScoped<MarketRepository>();
builder.Services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
builder.Services.AddSingleton<BitcoinService>();
builder.Services.AddSingleton<LandsbankinnService>();
builder.Services.AddSingleton<AssetService>();
builder.Services.AddSingleton<KrakenService>();
builder.Services.AddSingleton<TickerService>();
builder.Services.AddSingleton<OhlcService>();
builder.Services.AddHostedService<MarketService>();

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(
        builder =>
        {
            builder.WithOrigins("https://bitar.is",
                "https://www.bitar.is",
                "http://localhost:4200",
                "https://innskraning.island.is")
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials();
        });
});

builder.Services.AddControllers();

builder.Services.AddSignalR();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{

}

app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "v1");
    options.RoutePrefix = string.Empty;
});

app.UseRouting();

app.UseForwardedHeaders();

app.UseAuthentication();
app.UseAuthorization();

app.UseCors();

app.MapControllers();
app.MapHub<TickerHub>("/tickers");

app.Run("http://localhost:5000");