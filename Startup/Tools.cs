using System;
using System.Linq;
using System.Reflection;
using ApiTools.Context;
using ApiTools.Helpers;
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
            services.AddScoped<IServiceHelper, ServiceHelper>();

            return services;
        }
    }

    public class StartupTools<TDbContextType, TResourceProvider, TFilter>
        where TDbContextType : IDbContext
        where TResourceProvider : IResourceQueryProvider
        where TFilter : IFilter
    {
        private IServiceCollection Services { get; set; }
        private Type DataServiceType { get; set; }
        private Type ServiceType { get; set; }

        public void SetDataServiceType(Type serviceType)
        {
            DataServiceType = serviceType;
        }

        public void SetServiceType(Type serviceType)
        {
            ServiceType = serviceType;
        }

        public void SetServices(IServiceCollection services)
        {
            Services = services;
        }



        public void AddDefaultRequirement<TModel, TModelData>()
            where TModelData : class
        {
            AddDefaultContext<TModel>();
            var modelType = typeof(TModel);
            var dataType = typeof(TModelData);
            var modelIdType = PropertyHelper.GetModelIdType(modelType);

            var serviceType = DataServiceType.MakeGenericType(modelType, modelIdType, dataType);

            var serviceMethod = GetType().GetMethods()
                .Single(x => x.Name == nameof(AddDefaultRequirement) && x.GetGenericArguments().Length == 4)
                .MakeGenericMethod(modelType, modelIdType, dataType, serviceType);
            serviceMethod.Invoke(this, new object[0]);
        }


        public void AddDefaultRequirement<TModel>()
        {
            AddDefaultContext<TModel>();
            var modelType = typeof(TModel);
            var modelIdType = PropertyHelper.GetModelIdType(modelType);


            var serviceType = ServiceType.MakeGenericType(modelType, modelIdType);

            var serviceMethod = GetType().GetMethods()
                .Single(x => x.Name == nameof(AddDefaultRequirement) && x.GetGenericArguments().Length == 3)
                .MakeGenericMethod(modelType, modelIdType, serviceType);
            serviceMethod.Invoke(this, new object[0]);
        }

        public void AddDefaultRequirement<TModel, TModelIdKey, TModelData, TInternalService>()
            where TModel : ContextEntity<TModelIdKey>
            where TModelIdKey : new()
            where TModelData : class
            where TInternalService : class, IService<TModel, TModelIdKey, TModelData>
        {
            Services.AddScoped<IServiceHelper<TModel, TModelIdKey, TModelData>, ServiceHelper<TModel, TModelIdKey, TModelData>>();
            Services.AddScoped<IService<TModel, TModelIdKey, TModelData>, TInternalService>();
        }

        public void AddDefaultRequirement<TModel, TModelIdKey, TInternalService>()
            where TModel : ContextEntity<TModelIdKey>
            where TModelIdKey : new()
            where TInternalService : class, IService<TModel, TModelIdKey>
        {
            Services.AddScoped<IServiceHelper<TModel, TModelIdKey>, ServiceHelper<TModel, TModelIdKey>>();
            Services.AddScoped<IService<TModel, TModelIdKey>, TInternalService>();
        }

        public void AddDefaultContext
            <TModel>()
        {
            var modelType = typeof(TModel);
            var modelIdType = PropertyHelper.GetModelIdType(modelType);
            var contextMethod = GetType().GetMethods()
                .Single(x => x.Name == nameof(AddDefaultContext) && x.GetGenericArguments().Length == 2)
                .MakeGenericMethod(modelType, modelIdType);
            contextMethod.Invoke(this, new object[0]);
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