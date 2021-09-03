using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using CustomerApi.Data.Database;
using CustomerApi.Data.Repository.v1;
using CustomerApi.Domain.Entities;
using CustomerApi.Messaging.Send.Options.v1;
using CustomerApi.Messaging.Send.Sender.v1;
using CustomerApi.Models.v1;
using CustomerApi.Service.v1.Command;
using CustomerApi.Service.v1.Query;
using CustomerApi.Validators.v1;
using FluentValidation;
using FluentValidation.AspNetCore;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;

namespace CustomerApi
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
            services.AddOptions();
            services.Configure<RabbitMqConfiguration>(Configuration.GetSection("RabbitMq"));
            services.AddDbContext<CustomerContext>(opt => opt.UseInMemoryDatabase(Guid.NewGuid().ToString()));

            services.AddAutoMapper(typeof(Startup));
            services.AddMvc().AddFluentValidation();

            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo
                {
                    Version = "v1",
                    Title = "Customer Api",
                    Description = "A simple API to create or update customers",
                    Contact = new OpenApiContact
                    {
                        Name = "Wolfgang Ofner",
                        Email = "Wolfgang@programmingwithwolfgang.com",
                        Url = new Uri("https://www.programmingwithwolfgang.com/")
                    }
                });

                var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
                var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
                c.IncludeXmlComments(xmlPath);
            });
            services.AddMediatR(Assembly.GetExecutingAssembly());
            services.AddTransient(typeof(IRepository<>), typeof(Repository<>));
            services.AddTransient<ICustomerRepository, CustomerRepository>();

            services.AddTransient<IValidator<CreateCustomerModel>, CreateCustomerModelValidator>();
            services.AddTransient<IValidator<UpdateCustomerModel>, UpdateCustomerModelValidator>();

            bool.TryParse(Configuration["BaseServiceSettings:UserabbitMq"], out var useRabbitMq);

            if (useRabbitMq)
            {
                services.AddSingleton<ICustomerUpdateSender, CustomerUpdateSender>();
            }
            //else
            //{
            //    services.AddSingleton<ICustomerUpdateSender, CustomerUpdateSenderServiceBus>();
            //}

            services.AddTransient<IRequestHandler<CreateCustomerCommand, Customer>, CreateCustomerCommandHandler>();
            services.AddTransient<IRequestHandler<UpdateCustomerCommand, Customer>, UpdateCustomerCommandHandler>();
            services.AddTransient<IRequestHandler<GetCustomerByIdQuery, Customer>, GetCustomerByIdQueryHandler>();
            services.AddTransient<IRequestHandler<GetCustomersQuery, List<Customer>>, GetCustomersQueryHandler>();


            services.AddControllers();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "Customer API V1");
                c.RoutePrefix = string.Empty;
            });
            app.UseRouting();

            //app.UseMetricServer();
            //app.UseMiddleware<ResponseMetricMiddleware>();
            //app.UseHttpMetrics();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
                endpoints.MapHealthChecks("/health");
            });
        }
    }
}
