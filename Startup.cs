using System.Text;
using Bitar.Hubs;
using Bitar.Models;
using Bitar.Models.Settings;
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
                //options.User.AllowedUserNameCharacters ="0123456789";
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

            services.AddSingleton<BitcoinService>();
            services.AddSingleton<LandsbankinnService>();
            services.AddSingleton<KrakenService>();
            services.AddSingleton<StockService>();
            services.AddHostedService<PaymentService>();

            services.AddControllers();

            services.AddSignalR();

        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseRouting();

            app.UseAuthentication();

            app.UseCors(builder => builder
                .WithOrigins("https://bitar.is", "https://www.bitar.is", "http://localhost:4200")
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials());

            app.UseForwardedHeaders();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapHub<StockHub>("/stocks");
                endpoints.MapControllers();
            });
        }
    }
}