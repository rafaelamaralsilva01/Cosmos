using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using Earth.Contracts;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using Satellite.Aggregates.Food;
using Shuttle.Bus;

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

            services.AddSingleton<DiscoveryShuttle>(sp =>
            {
                var connection = sp.GetRequiredService<RabbitMQConnection>();
                var iLifetimeScope = sp.GetRequiredService<ILifetimeScope>();
                var bus = new DiscoveryShuttle(connection, "teste", iLifetimeScope);

                // Config
                bus.Subscribe<FoodMessage, SendFoodHandler>();
                bus.Subscribe<FoodMessage, TesteHandler>();

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

            services.AddTransient<SendFoodHandler>();
            services.AddTransient<TesteHandler>();

            var builder = new ContainerBuilder();
            builder.Populate(services);
            var container = builder.Build();
            container.Resolve<DiscoveryShuttle>();
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
