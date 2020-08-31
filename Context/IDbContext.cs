using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace ApiTools.Context
{
    public interface IDbContext
    {
        int SaveChanges(bool acceptAllChangesOnSuccess);
        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

        Task<int> SaveChangesAsync(
            bool acceptAllChangesOnSuccess,
            CancellationToken cancellationToken = default);

        ValueTask<EntityEntry> AddAsync(
            [NotNull] object entity,
            CancellationToken cancellationToken = default);

        void UpdateRange([NotNull] params object[] entities);
        void UpdateRange([NotNull] IEnumerable<object> entities);
        EntityEntry Update([NotNull] object entity);
        EntityEntry Remove([NotNull] object entity);
        void RemoveRange([NotNull] params object[] entities);
        void RemoveRange([NotNull] IEnumerable<object> entities);
        EntityEntry Entry([NotNull] object entity);

        Task AddRangeAsync(
            [NotNull] IEnumerable<object> entities,
            CancellationToken cancellationToken = default);

        Task AddRangeAsync([NotNull] params object[] entities);
        EntityEntry Add([NotNull] object entity);
        DbSet<TEntity> Set<TEntity>() where TEntity : class;

        public ValueTask<TEntity> FindAsync<TEntity>([CanBeNull] params object[] keyValues) where TEntity : class;

        public ValueTask<TEntity> FindAsync<TEntity>([CanBeNull] object[] keyValues,
            CancellationToken cancellationToken)
            where TEntity : class;

        public EntityEntry<TEntity> Attach<TEntity>([NotNull] TEntity entity)
            where TEntity : class;

        public EntityEntry Attach([NotNull] object entity);
    }
}