using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
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
    }
}