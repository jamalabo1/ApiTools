using ApiTools.Context;
using ApiTools.Models;
using ApiTools.Provider;
using ApiTools.Services;
using Microsoft.Extensions.DependencyInjection;

namespace ApiTools.Startup
{
    public static class StartupToolsExtension
    {
        public static IServiceCollection AddApiToolsServices(this IServiceCollection services)
        {
            services.AddHttpContextAccessor();

            services.AddAuthorization();

            services.AddSingleton<IQueueService, QueueService>();

            services.AddScoped<IPasswordService, PasswordService>();
            services.AddScoped<IAuthenticationService, AuthenticationService>();
            services.AddScoped<IPagingService, PagingService>();
            services.AddScoped<ISort, Sort>();

            return services;
        }
    }

    public class StartupTools<TDbContextType, TResourceProvider>
        where TDbContextType : IDbContext
        where TResourceProvider : IResourceQueryProvider
    {
        private IServiceCollection Services { get; set; }

        public void SetServices(IServiceCollection services)
        {
            Services = services;
        }

        public void AddDefaultRequirement<TModel, TModelIdKey, TModelData>()
            where TModel : ContextEntity<TModelIdKey>
            where TModelIdKey : new()
            where TModelData : class
        {
            AddDefaultContext<TModel, TModelIdKey>();

            Services
                .AddScoped<IService<TModel, TModelIdKey, TModelData>,
                    InternalService<TModel, TModelIdKey, TModelData, TResourceProvider>
                >();
        }

        public void AddDefaultRequirement<TModel, TModelIdKey>()
            where TModel : ContextEntity<TModelIdKey>
            where TModelIdKey : new()
        {
            AddDefaultContext<TModel, TModelIdKey>();

            Services
                .AddScoped<IService<TModel, TModelIdKey>, InternalService<TModel, TModelIdKey, TResourceProvider>
                >();
        }


        public void AddDefaultContext
            <TModel, TModelIdKey>()
            where TModelIdKey : new()
            where TModel : ContextEntity<TModelIdKey>
        {
            Services.AddScoped<IContext<TModel, TModelIdKey>>(x =>
            {
                var dbContext = x.GetRequiredService<TDbContextType>();
                var queryProvider = x.GetRequiredService<TResourceProvider>();
                return new InternalContext<TModel, TModelIdKey, TResourceProvider>(dbContext,
                    queryProvider);
            });
        }
    }
}