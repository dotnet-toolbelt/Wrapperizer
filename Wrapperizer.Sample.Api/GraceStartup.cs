using System;
using Grace.DependencyInjection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.StackExchangeRedis;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using Wrapperizer.Extensions.DependencyInjection.Abstractions;
using Wrapperizer.Sample.Application.Handlers.Commands;
using Wrapperizer.Sample.Configurations;
using Wrapperizer.Sample.Domain.Repositories;
using Wrapperizer.Sample.Infra.Persistence;
using Wrapperizer.Sample.Infra.Persistence.AspNetCore.Migrator;
using Wrapperizer.Sample.Infra.Persistence.Repositories;
using static HealthChecks.UI.Client.UIResponseWriter;
using SqlServerConnection = Wrapperizer.Sample.Configurations.SqlServerConnection;

namespace Wrapperizer.Sample.Api
{
    public abstract class GraceStartup
    {
        protected IConfiguration Configuration { get; }
        protected GraceStartup(IConfiguration configuration)
        {
            Configuration = configuration;
        }


        // This method gets called by the runtime. Use this method to add services to the container.
        public virtual void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers();
            
            services.AddOpenApiDocument(setting => setting.Title = "Sample Api");

            services.AddDbContext<UniversityDbContext>((provider,builder) =>
            {
                var sqlConnection = provider.GetRequiredService<SqlServerConnection>();

                builder.UseSqlServer(sqlConnection.ConnectionString, options =>
                {
                    options.EnableRetryOnFailure(3);
                });
            });

            var requestHandlersAssembly = typeof(RegisterStudentHandler).Assembly;
            var notificationHandlerAssembly = typeof(StudentRegisteredHandler).Assembly;

            services.AddWrapperizer()
                .AddHandlers(context => context
                        // .AddDistributedCaching()
                        // .AddGlobalValidation()
                        .AddTransactionalCommands()
                , assemblies:new []{requestHandlersAssembly,notificationHandlerAssembly}
                )
                .AddUnitOfWork<UniversityDbContext>()
                .AddTransactionalUnitOfWork<UniversityDbContext>()
                .AddRepositories()
                // .AddCrudRepositories<WeatherForecastDbContext>((provider, options) =>
                // {
                //     options.UseInMemoryDatabase("WeatherForecast");
                //     options.UseLoggerFactory(provider.GetRequiredService<ILoggerFactory>());
                // })
                ;

            services.AddUniversityMigrator();

            // services.AddScoped<IStudentRepository, StudentRepository>();

        }
        
        public virtual void ConfigureContainer(IInjectionScope injectionScope)
        {
        }
        
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseWrapperizerApiExceptionHandler();

            app.UseHttpsRedirection();

            app.UseOpenApi();
            app.UseSwaggerUi3();

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapHealthChecks("/healthz", new HealthCheckOptions
                {
                    Predicate = _ => true,
                    ResponseWriter = WriteHealthCheckUIResponse 
                });
                
                endpoints.MapControllers();
            });
            
        }
    }
}
