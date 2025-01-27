using System.Linq;
using KachnaOnline.App.Extensions;
using KachnaOnline.Business.Extensions;
using KachnaOnline.Business.Configuration;
using KachnaOnline.Business.Constants;
using KachnaOnline.Business.Data.Extensions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Serilog;

namespace KachnaOnline.App
{
    /// <summary>
    /// ASP.NET Core Startup class that configures the app's services container and request pipeline.
    /// </summary>
    public class Startup
    {
        public const string LocalCorsPolicy = "LocalPolicy";
        public const string MainCorsPolicy = "MainPolicy";

        public IConfiguration Configuration { get; }

        public Startup(IConfiguration configuration)
        {
            this.Configuration = configuration;
        }

        /// <summary>
        /// Configures the app's dependency injection container.
        /// </summary>
        /// <param name="services">A service collection.</param>
        public void ConfigureServices(IServiceCollection services)
        {
            // Add CORS policy for local development and internal apps
            services.AddCors(o =>
            {
                o.AddPolicy(LocalCorsPolicy, builder =>
                {
                    builder.WithOrigins("http://localhost:4200").AllowAnyHeader().AllowAnyMethod();
                    builder.WithOrigins("https://localhost:4200").AllowAnyHeader().AllowAnyMethod();
                    builder.WithOrigins("https://su-int.fit.vutbr.cz").AllowAnyHeader().AllowAnyMethod();
                });

                o.AddPolicy(MainCorsPolicy, builder =>
                {
                    builder.WithOrigins("https://su.fit.vut.cz", "https://su.fit.vutbr.cz",
                               "https://www.su.fit.vut.cz", "https://www.su.fit.vutbr.cz",
                               "https://su-int.fit.vutbr.cz", "https://su-dev.fit.vutbr.cz")
                           .AllowAnyHeader().AllowAnyMethod();
                });
            });

            // Load configuration objects.
            services.Configure<KisOptions>(this.Configuration.GetSection("Kis"));
            services.Configure<JwtOptions>(this.Configuration.GetSection("Jwt"));
            services.Configure<ClubStateOptions>(this.Configuration.GetSection("States"));
            services.Configure<BoardGamesOptions>(this.Configuration.GetSection("BoardGames"));
            services.Configure<PushOptions>(this.Configuration.GetSection("Push"));
            services.Configure<MailOptions>(this.Configuration.GetSection("Mail"));
            services.Configure<EventsOptions>(this.Configuration.GetSection("Events"));

            // Configures custom rules for Serilog's request logging.
            services.ConfigureSerilogRequestLogging();

            // Add scoped database context.
            services.AddAppData(this.Configuration);

            // Add business layer services.
            services.AddBusinessLayer(this.Configuration);

            // Add MVC controllers.
            services.AddControllers()
                    .AddNewtonsoftJson(options =>
                    {
                        options.SerializerSettings.NullValueHandling = NullValueHandling.Include;
                        options.SerializerSettings.Converters.Add(new StringEnumConverter());
                    })
                    .AddMvcOptions(options =>
                    {
                        var inputFormatter = options.InputFormatters.OfType<NewtonsoftJsonInputFormatter>().First();
                        inputFormatter.SupportedMediaTypes.Clear();
                        inputFormatter.SupportedMediaTypes.Add("application/json");
                        inputFormatter.SupportedMediaTypes.Add("text/json");
                    });

            // Add JWT authentication.
            services.AddCustomJwtAuthentication(this.Configuration);

            // Add custom authorization policies.
            services.AddAuthorization(options =>
            {
                options.AddPolicy(AuthConstants.AnyManagerPolicy, policy =>
                    policy.RequireRole(AuthConstants.Admin, AuthConstants.EventsManager, AuthConstants.StatesManager,
                        AuthConstants.BoardGamesManager));
                options.AddPolicy(AuthConstants.AdminOrBoardGamesManagerPolicy, policy =>
                    policy.RequireRole(AuthConstants.Admin, AuthConstants.BoardGamesManager));
            });

            // Add OpenAPI document service.
            services.AddCustomSwaggerGen();
        }

        /// <summary>
        /// Configures the request processing pipeline.
        /// </summary>
        /// <param name="app">An application builder.</param>
        /// <param name="env">
        /// An <see cref="IWebHostEnvironment" /> instance that contains information about the current
        /// environment.
        /// </param>
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseStaticFiles(new StaticFileOptions()
                {
                    RequestPath = "/kachna",
                    FileProvider = new PhysicalFileProvider(env.WebRootPath)
                });
            }

            app.UsePathBase("/kachna/api");

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                // Use the ErrorController to handle unhandled exceptions.
                app.UseExceptionHandler("/error");

                // Add the Strict-Transport-Security header.
                app.UseHsts();
            }

            // Add Serilog's request logging middleware.
            app.UseSerilogRequestLogging();

            // Serve uploaded images.
            app.UseUploadedImagesStaticFiles(env);

            // Handle 400+ HTTP status codes.
            app.UseStatusCodePagesWithReExecute("/error/{0}");

            // Add OpenAPI document providing middleware.
            app.UseSwagger();

            // Add SwaggerUI.
            app.UseSwaggerUI(c => c.SwaggerEndpoint("v1/swagger.json", "Kachna Online API"));

            // Add routing middleware.
            app.UseRouting();

            app.UseCors(env.IsDevelopment() ? LocalCorsPolicy : MainCorsPolicy);

            // Add authorization middleware.
            app.UseAuthentication();
            app.UseAuthorization();

            // Map controller endpoints using the default mapping strategy.
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
                if (env.IsDevelopment())
                {
                    endpoints.MapFallbackToFile("index.html");
                }
            });
        }
    }
}
