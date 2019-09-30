using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace Bitar
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureAppConfiguration((hostingContext, config) =>
                {
                    config.AddUserSecrets<Startup>(optional: false);
                })
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseKestrel(serverOptions =>
                    {
                        // Set properties and call methods on options.
                    });
                    webBuilder.UseUrls("http://localhost:5000");
                    webBuilder.UseStartup<Startup>();
                });
                
    }
}
