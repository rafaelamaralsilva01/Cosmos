using Autofac;
using Autofac.Extensions.DependencyInjection;
using Earth.Contracts;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RabbitMQ.Client;
using Satellite.Aggregates.Technologies;
using Shuttle.Bus;
using System;

namespace Satellite
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        /// <summary>
        /// This method gets called by the runtime. Use this method to add services to the container.
        /// </summary>
        /// <param name="services"></param>
        public IServiceProvider ConfigureServices(IServiceCollection services)
        {
            services.AddMvc();

            services.AddSingleton<IShuttle>(sp =>
            {
                var connection = sp.GetRequiredService<RabbitMQConnection>();
                var iLifetimeScope = sp.GetRequiredService<ILifetimeScope>();
                var bus = new DiscoveryShuttle(connection, iLifetimeScope);

                // Config
                bus.Subscribe<AskForTechMessage, AskForTechHandler>();

                return bus;
            });

            services.AddTransient<RabbitMQConnection>(sp =>
            {
                var factory = new ConnectionFactory()
                {
                    HostName = "localhost",
                    UserName = "guest",
                    Password = "guest"
                };

                var connection = new RabbitMQConnection(factory);
                return connection;
            });

            services.AddTransient<AskForTechHandler>();

            var builder = new ContainerBuilder();
            builder.Populate(services);
            var container = builder.Build();
            container.Resolve<IShuttle>();
            return new AutofacServiceProvider(container);
        }

        /// <summary>
        /// This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        /// </summary>
        /// <param name="app"></param>
        /// <param name="env"></param>
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseMvc();
        }
    }
}
