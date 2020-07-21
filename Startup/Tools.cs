using System;
using System.Linq;
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
            services.AddScoped<IMapperHelper, MapperHelper>();

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

        private (Type, Type)[] ServiceHelperTypes { get; set; }

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

        public void SetServiceHelperType((Type, Type)[] types)
        {
            ServiceHelperTypes = types;
        }


        public void AddDefaultRequirement<TModel, TModelData>(Type cIServiceType = null, Type cServiceType = null)
            where TModelData : class
        {
            AddDefaultContext<TModel>();
            var modelType = typeof(TModel);
            var dataType = typeof(TModelData);
            var modelIdType = PropertyHelper.GetModelIdType(modelType);

            var serviceType = cServiceType;
            if (serviceType == null)
            {
                if (DataServiceType.GetGenericArguments().Length == 2)
                    serviceType = DataServiceType.MakeGenericType(modelType, dataType);
                else if (DataServiceType.GetGenericArguments().Length == 3)
                    serviceType = DataServiceType.MakeGenericType(modelType, modelIdType, dataType);
                else
                    return;
            }


            Services.AddScoped(typeof(IServiceHelper<,>).MakeGenericType(modelType, modelIdType),
                typeof(ServiceHelper<,>).MakeGenericType(modelType, modelIdType));


            Services.AddScoped(typeof(IServiceHelper<,,>).MakeGenericType(modelType, modelIdType, dataType),
                typeof(ServiceHelper<,,>).MakeGenericType(modelType, modelIdType, dataType));

            foreach (var helperType in ServiceHelperTypes)
            {
                Type serviceHelperType;
                if (helperType.Item1.GetGenericArguments().Length == 1)
                    serviceHelperType = helperType.Item1.MakeGenericType(modelType);
                else
                    serviceHelperType = helperType.Item1.MakeGenericType(modelType, dataType);

                Type iServiceHelperType;
                if (helperType.Item1.GetGenericArguments().Length == 1)
                    iServiceHelperType = helperType.Item2.MakeGenericType(modelType);
                else
                    iServiceHelperType = helperType.Item2.MakeGenericType(modelType, dataType);

                Services.AddScoped(iServiceHelperType, serviceHelperType);
            }

            var iServiceType = typeof(IService<,,>).MakeGenericType(modelType, modelIdType, dataType);
            Services.AddScoped(iServiceType, serviceType);
            if (cIServiceType != null) Services.AddScoped(cIServiceType, serviceType);

            // var serviceMethod = GetType().GetMethods()
            //     .Single(x => x.Name == nameof(AddDefaultRequirement) && x.GetGenericArguments().Length == 6)
            //     .MakeGenericMethod(modelType, modelIdType, dataType, serviceType, iServiceHelperType,
            //         serviceHelperType);
            // serviceMethod.Invoke(this, new object[0]);
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

        public void AddDefaultRequirement<TModel, TModelIdKey, TModelData, TInternalService, TIServiceHelper,
            TServiceHelper>()
            where TModel : ContextEntity<TModelIdKey>
            where TModelIdKey : new()
            where TModelData : class, IDtoModel<TModelIdKey>
            where TInternalService : class, IService<TModel, TModelIdKey, TModelData>
            where TIServiceHelper : class, IServiceHelper
            where TServiceHelper : class, TIServiceHelper
        {
            Services.AddScoped<IServiceHelper<TModel, TModelIdKey>, ServiceHelper<TModel, TModelIdKey>>();
            Services.AddScoped<TIServiceHelper, TServiceHelper>();
            Services.AddScoped<IService<TModel, TModelIdKey, TModelData>, TInternalService>();
        }

        // public void AddDefaultRequirement<TModel, TModelIdKey, TInternalService>()
        //     where TModel : ContextEntity<TModelIdKey>
        //     where TModelIdKey : new()
        //     where TInternalService : class, IService<TModel, TModelIdKey>
        // {
        //     Services.AddScoped<IServiceHelper<TModel, TModelIdKey>, ServiceHelper<TModel, TModelIdKey>>();
        //     Services.AddScoped<IService<TModel, TModelIdKey>, TInternalService>();
        // }

        public void AddDefaultContext
            <TModel>()
        {
            var modelType = typeof(TModel);
            var modelIdType = PropertyHelper.GetModelIdType(modelType);

            var contextType = typeof(InternalContext<,,,>).MakeGenericType(modelType, modelIdType,
                typeof(TDbContextType), typeof(TResourceProvider));
            Services.AddScoped(typeof(IContext<,>).MakeGenericType(modelType, modelIdType), contextType);
            // var contextMethod = GetType().GetMethods()
            //     .Single(x => x.Name == nameof(AddDefaultContext) && x.GetGenericArguments().Length == 2)
            //     .MakeGenericMethod(modelType, modelIdType);
            // contextMethod.Invoke(this, new object[0]);
        }

        public void AddDefaultContext
            <TModel, TModelIdKey>()
            where TModelIdKey : new()
            where TModel : ContextEntity<TModelIdKey>
        {
            Services
                .AddScoped<IContext<TModel, TModelIdKey>,
                    InternalContext<TModel, TModelIdKey, TDbContextType, TResourceProvider>>();
        }
    }
}