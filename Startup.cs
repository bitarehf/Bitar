using System;
using System.Text;
using Bitar.Hubs;
using Bitar.Models;
using Bitar.Models.Settings;
using Bitar.Repositories;
using Bitar.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using NSwag;
using NSwag.Generation.Processors.Security;

namespace Bitar
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddDbContext<ApplicationDbContext>(options =>
                {
                    options.EnableSensitiveDataLogging();
                    options.UseNpgsql(
                        Configuration.GetConnectionString("DefaultConnection"));
                }
            );

            services.AddDefaultIdentity<ApplicationUser>()
                .AddEntityFrameworkStores<ApplicationDbContext>()
                .AddDefaultTokenProviders();

            services.Configure<IdentityOptions>(options =>
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

            services
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
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Configuration.GetSection("JwtSettings:JwtKey").Value)),
                    };
                });

            services.Configure<ForwardedHeadersOptions>(options => options.ForwardedHeaders = ForwardedHeaders.All);
            services.Configure<JwtSettings>(Configuration.GetSection("JwtSettings"));
            services.Configure<BitcoinSettings>(Configuration.GetSection("BitcoinSettings"));
            services.Configure<LandsbankinnSettings>(Configuration.GetSection("LandsbankinnSettings"));
            services.Configure<KrakenSettings>(Configuration.GetSection("KrakenSettings"));
            services.Configure<JaSettings>(Configuration.GetSection("JaSettings"));
            services.Configure<DilisenseSettings>(Configuration.GetSection("DilisenseSettings"));

            services.AddHttpClient<ArionService>();
            services.AddHttpClient<BlockchainService>();

            services.AddScoped<MarketRepository>();
            services.AddSingleton<BitcoinService>();
            services.AddSingleton<LandsbankinnService>();
            services.AddSingleton<AssetService>();
            services.AddSingleton<KrakenService>();
            services.AddSingleton<TickerService>();
            services.AddSingleton<OhlcService>();
            services.AddHostedService<MarketService>();

            services.AddControllers();

            services.AddSignalR();

            services.AddOpenApiDocument(c =>
            {
                c.Title = "Bitar API";
                c.OperationProcessors.Add(new AspNetCoreOperationSecurityScopeProcessor("JWT"));
                c.AddSecurity("JWT", new OpenApiSecurityScheme
                {
                    Type = OpenApiSecuritySchemeType.ApiKey,
                    Name = "Authorization",
                    In = OpenApiSecurityApiKeyLocation.Header,
                    Description = "Copy this into the value field: Bearer {token}",
                });
                c.PostProcess = document =>
                {
                    document.Info.Version = "v1";
                    document.Info.Title = "Bitar API";
                    document.Info.Description = "Endilega láttu okkur vita ef það vantar eitthvað eða þér finnst eitthvað mega bæta.";
                };
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseRouting();

            app.UseForwardedHeaders();

            app.UseAuthentication();
            app.UseAuthorization();

            // Enable middleware to serve generated Swagger as a JSON endpoint.
            app.UseOpenApi(c =>
            {
                c.Path = "/{documentName}/swagger.json";
            });

            // Enable middleware to serve ReDoc (HTML, JS, CSS, etc.).
            app.UseReDoc(c =>
            {
                c.DocumentTitle = "Bitar API Documentation";
                c.EnableUntrustedSpec();
                c.ExpandResponses("200,201");
                c.HideLoading();
                c.DisableSearch();
                c.RoutePrefix = string.Empty;
            });

            app.UseCors(builder => builder
                .WithOrigins("https://bitar.is", "https://www.bitar.is", "http://localhost:4200", "https://innskraning.island.is")
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials());

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapHub<TickerHub>("/tickers");
                endpoints.MapControllers();
            });
        }
    }
}