using ApiTools.Context;
using ApiTools.Models;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;

namespace ApiTools.Services
{
    public interface IServiceHelper
    {
        IAuthorizationService Authorization { get; }
        IHttpContextAccessor Accessor { get; }
        IPagingService PagingService { get; }
        ISort Sort { get; }
        IMapper Mapper { get; }
    }

    public class ServiceHelper : IServiceHelper
    {
        public ServiceHelper(IAuthorizationService authorization, IHttpContextAccessor accessor,
            IPagingService pagingService, ISort sort, IMapper mapper)
        {
            Authorization = authorization;
            Accessor = accessor;
            PagingService = pagingService;
            Sort = sort;
            Mapper = mapper;
        }

        public ServiceHelper(IServiceHelper serviceHelper) : this(serviceHelper.Authorization, serviceHelper.Accessor,
            serviceHelper.PagingService, serviceHelper.Sort, serviceHelper.Mapper)
        {
        }

        public IAuthorizationService Authorization { get; }
        public IHttpContextAccessor Accessor { get; }
        public IPagingService PagingService { get; }
        public ISort Sort { get; }
        public IMapper Mapper { get; }
    }

    public interface IServiceHelper<TModel, in TModelKeyId> : IServiceHelper
        where TModel : IContextEntity<TModelKeyId> where TModelKeyId : new()
    {
        IContext<TModel, TModelKeyId> Context { get; }
    }

    public class ServiceHelper<TModel, TModelKeyId> : ServiceHelper, IServiceHelper<TModel, TModelKeyId>
        where TModel : IContextEntity<TModelKeyId> where TModelKeyId : new()
    {
        public ServiceHelper(IServiceHelper serviceHelper,
            IContext<TModel, TModelKeyId> context) : base(serviceHelper)
        {
            Context = context;
        }

        public ServiceHelper(IServiceHelper<TModel, TModelKeyId> serviceHelper) : this(serviceHelper,
            serviceHelper.Context)
        {
        }

        public IContext<TModel, TModelKeyId> Context { get; }
    }


    public interface IServiceHelper<TModel, TModelKeyId, TModelDto> : IServiceHelper<TModel, TModelKeyId>
        where TModel : IContextEntity<TModelKeyId> where TModelKeyId : new()
    {
        IService<TModel, TModelKeyId, TModelDto> Service { get; }
    }

    public class ServiceHelper<TModel, TModelKeyId, TModelDto> : ServiceHelper<TModel, TModelKeyId>, IServiceHelper<TModel, TModelKeyId, TModelDto>
        where TModel : IContextEntity<TModelKeyId> where TModelKeyId : new()
    {
        public ServiceHelper(IServiceHelper<TModel, TModelKeyId> serviceHelper,
            IService<TModel, TModelKeyId, TModelDto> service) : base(serviceHelper)
        {
            Service = service;
        }

        public IService<TModel, TModelKeyId, TModelDto> Service { get; }
    }
}