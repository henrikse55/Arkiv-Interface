using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Builder;
using System.Linq;
using Arkiv.Data;
using System.IO;
using System;

namespace Arkiv
{
    public class Startup
    {
        public Startup()
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("Config.json");

            Configuration = builder.Build();
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            Func<IServiceProvider, ISqlData> sqlFactory = 
                new Func<IServiceProvider, ISqlData>(x => new SqlDataHandler(Configuration["Connection"]));
            Config config = new Config()
            {
                Connection = Configuration["Connection"],
                AdminGroups = Configuration.GetSection("AdminPanel:Groups").AsEnumerable().Where(x => !string.IsNullOrEmpty(x.Value)).Select(x => x.Value).ToArray(),
                AdminUsers = Configuration.GetSection("AdminPanel:Users").AsEnumerable().Where(x => !string.IsNullOrEmpty(x.Value)).Select(x => x.Value).ToArray(),
                ActivityLogging = Configuration["Logging:ActivityLogging"] == "true",
                PdfPath = Configuration["PdfPath"],
            };

            services.AddTransient<ISqlData>(sqlFactory)
                    .AddSingleton(config)
                    .AddMvc();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseBrowserLink();
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
            }

            app.UseStaticFiles();

            app.UseMvc(routes =>
            {
                routes.MapRoute(name: "default", template: "{controller=Archive}/{action=Index}/{id?}");
            });
        }
    }
}