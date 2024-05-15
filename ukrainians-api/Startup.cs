using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using Ukrainians.WebAPI.Configs;
using Ukrainians.WebAPI.Helpers.IoC;
using Ukrainians.WebAPI.Hubs;
using Ukrainians.WebAPI.Middlewares;
using Ukrainians.WebAPI.Settings;
using System.Reflection;
using Ukrainians.Infrastrusture.Data.Context;
using Ukrainians.UtilityServices.Models.Common;
using Ukrainians.UtilityServices.Settings;
using WebPush;
using AspNetCoreRateLimit;

namespace Ukrainians.WebAPI
{
    public class Startup
    {
        public IConfiguration Configuration { get; }

        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public void ConfigureServices(IServiceCollection services)
        {
            services.RegisterEncryption(Configuration);

            services.RegisterAllinjections();

            var vapidDetails = new VapidDetails(
                Configuration.GetValue<string>("VapidDetails:Subject"),
                Configuration.GetValue<string>("VapidDetails:PublicKey"),
                Configuration.GetValue<string>("VapidDetails:PrivateKey"));

            services.AddTransient(c => vapidDetails);

            var emailConfig = Configuration
                .GetSection("EmailConfiguration")
                .Get<EmailConfigurationModel>();

            services.AddSingleton(emailConfig);

            services.AddSignalR(options =>
            {
                options.MaximumReceiveMessageSize = 52428800;
            });

            services.AddCors();

            services.AddControllers();

            var connectionString = Configuration.GetConnectionString("DefaultConnection");
            services.AddDbContext<ApplicationContext>(options => options.UseSqlServer(connectionString));

            services.SetupIdentity();

            services.RegisterJwt(Configuration);

            services.AddSingleton(provider => new MapperConfiguration(cfg =>
            {
                cfg.AddProfile(new MapperProfile(provider.GetService<EncryptionSettings>()));
            }).CreateMapper());

            services.AddControllersWithViews().AddNewtonsoftJson(opt =>
                opt.SerializerSettings.ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore);

            services.AddSwaggerGen(setup =>
            {
                setup.SwaggerDoc("v1", new OpenApiInfo { Title = "Ukrainians.WebAPI", Version = "v1" });
                setup.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Description = "JWT Authorization header using the bearer scheme",
                    Name = "Authorization",
                    In = ParameterLocation.Header,
                    Type = SecuritySchemeType.ApiKey
                });

                setup.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                            {
                                Id = "Bearer",
                                Type = ReferenceType.SecurityScheme
                            }
                        },
                        new List<string>()
                    }
                });
            });

            services.AddMemoryCache();
            services.Configure<IpRateLimitOptions>(options =>
            {
                options.GeneralRules = new List<RateLimitRule>
                {
                    new RateLimitRule
                    {
                        Endpoint = "*",
                        Period = "1s",
                        Limit = 10
                    }
                };
            });

            services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();
            services.AddInMemoryRateLimiting();
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseSwagger();
            app.UseSwaggerUI(s => s.SwaggerEndpoint("/swagger/v1/swagger.json", "Ukrainians.WebAPI v1"));

            app.UseRouting();

            app.UseCors(builder => builder
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials()
            .WithOrigins("http://localhost:4200"));

            app.UseMiddleware<IpFilterMiddleware>();
            app.UseMiddleware<AuthTokenMiddleware>();

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
                endpoints.MapHub<ChatHub>("/hubs/chat");
            });

            app.UseIpRateLimiting();
        }
    }
}
