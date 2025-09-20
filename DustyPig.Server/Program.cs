using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NLog;
using NLog.Web;
using System;
using System.Net.Http;

namespace DustyPig.Server
{
    public class Program
    {
        //Shared HttpClient to use across the entire app
        public static readonly HttpClient SharedHttpClient = new();


        public static void Main(string[] args)
        {
#if DEBUG
            //var logger = NLogBuilder.ConfigureNLog("nlog.config").GetCurrentClassLogger();
            var logger = LogManager.Setup().LoadConfigurationFromFile(args).GetCurrentClassLogger();
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
                    config.AddJsonFile("/config/secrets.json",
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
                    logging.SetMinimumLevel(Microsoft.Extensions.Logging.LogLevel.Debug);
#endif
                })
                .UseNLog();


        public static Version ServerVersion => typeof(Program).Assembly.GetName().Version;
    }
}
