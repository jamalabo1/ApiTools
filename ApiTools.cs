using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using ApiTools.Context;
using ApiTools.Models;
using ApiTools.Provider;
using ApiTools.Services;
using Microsoft.Extensions.DependencyInjection;

namespace ApiTools
{
    public static class ApiTools
    {
        public static IServiceCollection AddApiToolsServices(this IServiceCollection services)
        {
            services.AddHttpContextAccessor();

            services.AddAuthorization();

            services.AddScoped<IResourceQueryProvider, ResourceQueryProvider>();
            services.AddSingleton<IQueueService, QueueService>();

            services.AddScoped<IPasswordService, PasswordService>();
            services.AddScoped<IAuthenticationService, AuthenticationService>();
            services.AddScoped<IPagingService, PagingService>();
            services.AddScoped<ISort, Sort>();

            return services;
        }

        public static void AddAuthorizationHandlers(this IServiceCollection services)
        {
            var stackTrace = new StackTrace();
            var method = stackTrace.GetFrame(1)?.GetMethod();
            if (method != null && method.ReflectedType != null)
            {
                var className = method.ReflectedType.Name;
                var authorizationClassNames = className + ".Authorization";

                var asm = Assembly.GetCallingAssembly();

                var namespaces = (from type in asm.GetTypes()
                    where type.Namespace == authorizationClassNames
                    select type.Name).ToList();

                foreach (var ns in namespaces) Console.WriteLine(ns);
            }
        }

        public static void AddDefaultRequirement<TModel, TModelIdKey, TModelData>(this IServiceCollection services,
            Func<IDbContext, IQueryable<TModel>> predicate)
            where TModel : ContextEntity<TModelIdKey>
            where TModelIdKey : new()
            where TModelData : class
        {
            services.AddScoped<IContext<TModel, TModelIdKey>>(x =>
            {
                var dbContext = x.GetService<IDbContext>();
                return new Context<TModel, TModelIdKey>(dbContext, predicate(dbContext));
            });

            services
                .AddScoped<IService<TModel, TModelIdKey, TModelData>, InternalService<TModel, TModelIdKey, TModelData>>();
        }
    }
}