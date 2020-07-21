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
        IPasswordService PasswordService { get; }
        ITokenService TokenService { get; }
        IMapperHelper MapperHelper { get; set; }
    }

    public class ServiceHelper : IServiceHelper
    {
        public ServiceHelper(IAuthorizationService authorization, IHttpContextAccessor accessor,
            IPagingService pagingService, ISort sort, IMapper mapper, IPasswordService passwordService,
            ITokenService tokenService, IMapperHelper mapperHelper)
        {
            Authorization = authorization;
            Accessor = accessor;
            PagingService = pagingService;
            Sort = sort;
            Mapper = mapper;
            PasswordService = passwordService;
            TokenService = tokenService;
            MapperHelper = mapperHelper;
        }

        protected ServiceHelper(IServiceHelper serviceHelper) : this(
            serviceHelper.Authorization,
            serviceHelper.Accessor,
            serviceHelper.PagingService,
            serviceHelper.Sort,
            serviceHelper.Mapper,
            serviceHelper.PasswordService,
            serviceHelper.TokenService,
            serviceHelper.MapperHelper
        )
        {
        }

        public IAuthorizationService Authorization { get; }
        public IHttpContextAccessor Accessor { get; }
        public IPagingService PagingService { get; }
        public ISort Sort { get; }
        public IMapper Mapper { get; }
        public IPasswordService PasswordService { get; }
        public ITokenService TokenService { get; }
        public IMapperHelper MapperHelper { get; set; }
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

        protected ServiceHelper(IServiceHelper<TModel, TModelKeyId> serviceHelper) : this(serviceHelper,
            serviceHelper.Context)
        {
        }

        public IContext<TModel, TModelKeyId> Context { get; }
    }


    public interface IServiceHelper<TModel, TModelKeyId, TModelDto> : IServiceHelper<TModel, TModelKeyId>
        where TModel : IContextEntity<TModelKeyId> where TModelKeyId : new() where TModelDto : IDtoModel<TModelKeyId>
    {
        IService<TModel, TModelKeyId, TModelDto> Service { get; }
    }

    public class ServiceHelper<TModel, TModelKeyId, TModelDto> : ServiceHelper<TModel, TModelKeyId>,
        IServiceHelper<TModel, TModelKeyId, TModelDto>
        where TModel : IContextEntity<TModelKeyId> where TModelKeyId : new() where TModelDto : IDtoModel<TModelKeyId>
    {
        public ServiceHelper(IServiceHelper<TModel, TModelKeyId> serviceHelper,
            IService<TModel, TModelKeyId, TModelDto> service) : base(serviceHelper)
        {
            Service = service;
        }

        protected ServiceHelper(IServiceHelper<TModel, TModelKeyId, TModelDto> serviceHelper) : this(serviceHelper,
            serviceHelper.Service)
        {
        }

        public IService<TModel, TModelKeyId, TModelDto> Service { get; }
    }
}