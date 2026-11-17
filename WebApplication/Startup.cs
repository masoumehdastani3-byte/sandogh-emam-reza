using AutoMapper;
using Domain.Repositories;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using Persistence;
using Services;
using Services.Abstractions;
using System;
using System.IO;
using System.Net.Http;
using System.Net.Security;
using System.Security.Authentication;
using WebApplication.Middleware;

namespace WebApplication
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers()
                .AddApplicationPart(typeof(Presentation.AssemblyReference).Assembly)
                .AddNewtonsoftJson(options =>
                {
                    options.SerializerSettings.ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore;
                    options.SerializerSettings.ContractResolver = new Newtonsoft.Json.Serialization.CamelCasePropertyNamesContractResolver();
                });

            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "لبخند فردا API", Version = "v1" });
            });

            services.AddAutoMapper(System.AppDomain.CurrentDomain.GetAssemblies());
            services.AddHttpClient();
            services.AddHttpClient("NikooPayment")
                .ConfigurePrimaryHttpMessageHandler(sp =>
                {
                    var config = sp.GetRequiredService<IConfiguration>();
                    var env = sp.GetRequiredService<IWebHostEnvironment>();
                    var allowUntrusted = env.IsDevelopment() &&
                        string.Equals(config["NikooPayment:AllowUntrustedCertificate"], "true", StringComparison.OrdinalIgnoreCase);

                    var handler = new SocketsHttpHandler
                    {
                        UseProxy = false,
                        AllowAutoRedirect = false,
                        SslOptions = new System.Net.Security.SslClientAuthenticationOptions
                        {
                            EnabledSslProtocols = SslProtocols.Tls12 | SslProtocols.Tls13
                        }
                    };

                    if (allowUntrusted)
                    {
                        handler.SslOptions.RemoteCertificateValidationCallback =
                            static (_, _, _, _) => true;
                    }

                    return handler;
                });
            services.AddScoped<IServiceManager, ServiceManager>();
            services.AddScoped<IRepositoryManager, RepositoryManager>();
            services.AddSingleton(Configuration);

            services.AddDbContext<RepositoryDbContext>(builder =>
            {
                var connectionString = Configuration.GetConnectionString("Database");
                builder.UseSqlServer(connectionString);
            });

            services.AddTransient<ExceptionHandlingMiddleware>();
            services.AddHostedService<WebApplication.HostedServices.BaleUpdatePollingService>();
            services.AddHostedService<WebApplication.HostedServices.ReminderLogCleanupService>();
            services.AddCors(options =>
            {
                options.AddDefaultPolicy(policy =>
                    policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod());
            });
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
                app.UseDeveloperExceptionPage();

            app.UseCors();
            app.UseUserRequstMiddleware();
            app.UseSwagger();
            app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "لبخند فردا API v1"));

            var wwwroot = Path.Combine(env.ContentRootPath, "wwwroot");
            Directory.CreateDirectory(wwwroot);
            app.UseDefaultFiles();
            app.UseStaticFiles();

            app.UseRouting();
            app.UseAuthorization();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
