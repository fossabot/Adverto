using System;
using System.IO;
using System.Reflection;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Application;
using Application.Persistence.Interfaces;
using Application.Validation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using Persistence.Logging;
using Persistence.Primary;
using Presentation.Common.Extensions;
using Presentation.Common.Middlewares;
using Presentation.Common.Swagger.SchemaFilters;

namespace Presentation.API
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
            RegisterSwaggerGen(services);

            services.AddCors();

            RegisterControllers(services);

            services.AddHttpContextAccessor();

            services.AddApplication();
            services.AddApplicationValidation();

            services.AddPersistence(Configuration);
            services.AddPersistenceLogging(Configuration);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseSwagger();
                app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Adverto"));
            }

            app.UseCors(Configuration);

            app.UseHttpsRedirection();

            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseMiddleware<ExceptionHandlerMiddleware>();

            app.UseEndpoints(endpoints => { endpoints.MapControllers(); });

            var _ = RunPrimaryDbSeederAsync(app);
        }

        /// <summary>
        /// Registers the swagger-gen in the specified <paramref name="serviceCollection"/>.
        /// </summary>
        private void RegisterSwaggerGen(IServiceCollection serviceCollection)
        {
            serviceCollection.AddSwaggerGen
            (c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo
                {
                    Title = "Adverto",
                    Version = "v1",
                    Description = "The advertisement API, made on .NET Core 3.1"
                });
                
                c.SchemaFilter<PropertiesWithoutSetterSchemaFilter>();
                
                var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
                var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
                c.IncludeXmlComments(xmlPath);
            });
        }

        /// <summary>
        /// Registers the controllers in the specified <paramref name="serviceCollection"/>.
        /// </summary>
        private void RegisterControllers(IServiceCollection serviceCollection)
        {
            serviceCollection
                .AddControllers()
                .AddJsonOptions(opts =>
                {
                    var enumConverter = new JsonStringEnumConverter();
                    opts.JsonSerializerOptions.Converters.Add(enumConverter);
                });
        }

        private async Task RunPrimaryDbSeederAsync(IApplicationBuilder app)
        {
            using IServiceScope scope = app.ApplicationServices.CreateScope();

            IDbSeeder<IAdvertoDbContext> seeder = scope.ServiceProvider.GetRequiredService<IDbSeeder<IAdvertoDbContext>>();
            await seeder.SeedAsync();
        }
    }
}