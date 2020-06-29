using System;
using System.Linq;
using ApiTools.Models;
using ApiTools.Provider;

namespace ApiTools.Context
{
    public sealed class InternalContext<TModel, TModelKeyId, TResourceProvider> : Context<TModel, TModelKeyId>
        where TModel : ContextEntity<TModelKeyId>
        where TModelKeyId : new()
        where TResourceProvider : IResourceQueryProvider
    {
        private readonly Func<IQueryable<TModel>, IQueryable<TModel>> _queryMapper;
        private readonly TResourceProvider _resourceQueryProvider;

        public InternalContext(IDbContext context,
            TResourceProvider resourceQueryProvider) : base(context)
        {
            _resourceQueryProvider = resourceQueryProvider;
        }

        public InternalContext(IDbContext context,
            Func<IQueryable<TModel>, IQueryable<TModel>> queryMapper) : base(context)
        {
            _queryMapper = queryMapper;
        }

        protected override IQueryable<TModel> GetQueryProvider(IQueryable<TModel> set)
        {
            return _resourceQueryProvider == null
                ? _queryMapper(set)
                : (IQueryable<TModel>) _resourceQueryProvider.GetQuery((dynamic) set);
        }
    }
}