using System.Linq;
using ApiTools.Models;
using ApiTools.Provider;
using JetBrains.Annotations;

namespace ApiTools.Context
{
    public sealed class
        InternalContext<TModel, TModelKeyId, TDbContext, TResourceProvider> : Context<TModel, TModelKeyId>
        where TModel : ContextEntity<TModelKeyId>
        where TModelKeyId : new()
        where TResourceProvider : IResourceQueryProvider
        where TDbContext : IDbContext
    {
        [NotNull] private readonly TDbContext _context;
        private readonly TResourceProvider _resourceQueryProvider;

        public InternalContext([NotNull] TDbContext context,
            TResourceProvider resourceQueryProvider) : base(context)
        {
            _context = context;
            _resourceQueryProvider = resourceQueryProvider;
        }

        protected override IQueryable<TModel> GetQueryProvider(IQueryable<TModel> set)
        {
            return _resourceQueryProvider.GetQuery((dynamic) set);
        }

        protected override IQueryable<TModel> GetSetQuery()
        {
            return _context.Set<TModel>();
        }
    }
}