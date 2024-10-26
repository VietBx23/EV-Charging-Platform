
using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace FocusEV.OCPP.Management
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder
                        .ConfigureLogging((ctx, builder) =>
                        {
                            builder.AddConfiguration(ctx.Configuration.GetSection("Logging"));
                            //builder.AddEventLog(o => o.LogName = "FocusEV.OCPP");
                            builder.AddFile(o => o.RootPath = ctx.HostingEnvironment.ContentRootPath);
                            builder.Services.AddRazorPages().AddRazorRuntimeCompilation();
                        })
                        .UseStartup<Startup>();
                });

    }
}
