using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace ApiTools.Context
{
    public class AppDbContext : DbContext, IDbContext
    {
        public AppDbContext(DbContextOptions options) : base(options)
        {
        }

        public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess,
            CancellationToken cancellationToken = default)
        {
            var addedEntities = ChangeTracker.Entries().Where(e => e.State == EntityState.Added).ToList();

            addedEntities.ForEach(e =>
            {
                if (e.Properties.Any(x => x.Metadata.Name == "CreationTime"))
                    e.Property("CreationTime").CurrentValue = DateTimeOffset.Now;
                if (e.Properties.Any(x => x.Metadata.Name == "ModificationTime"))
                    e.Property("ModificationTime").CurrentValue = DateTimeOffset.Now;
            });

            var editedEntities = ChangeTracker.Entries().Where(e => e.State == EntityState.Modified).ToList();

            editedEntities.ForEach(e =>
            {
                if (e.Properties.Any(x => x.Metadata.Name == "ModificationTime"))
                    e.Property("ModificationTime").CurrentValue = DateTimeOffset.Now;
            });

            return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
        }
    }
}