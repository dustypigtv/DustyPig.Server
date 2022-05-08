using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NLog.Web;
using System;

namespace DustyPig.Server
{
    public class Program
    {
        public static void Main(string[] args)
        {
#if DEBUG
            var logger = NLogBuilder.ConfigureNLog("nlog.config").GetCurrentClassLogger();
#endif
            try
            {
                CreateHostBuilder(args).Build().Run();
            }
#if DEBUG
            catch (Exception ex)
            {
                logger.Error(ex, "Stopped program because of exception");
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(ex.Message);
                Console.ResetColor();
                Console.ReadLine();
            }
#endif
            finally
            {
                NLog.LogManager.Shutdown();
            }
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureAppConfiguration((hostingContext, config) =>
                {
                    //Prod secrets file
                    config.AddJsonFile("/etc/DustyPig.Server/secrets.json",
                                       optional: true,
                                       reloadOnChange: true);
                })
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                })
                .ConfigureLogging(logging =>
                {
                    logging.ClearProviders();
#if DEBUG
                    logging.AddConsole();
                    logging.SetMinimumLevel(LogLevel.Debug);
#endif
                })
                .UseNLog();


        public static Version ServerVersion => typeof(Program).Assembly.GetName().Version;
    }
}
