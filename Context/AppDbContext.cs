using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ApiTools.Context
{
    public class AppDbContext : DbContext, IDbContext
    {
        private static readonly ILoggerFactory DbContextLoggerFactory
            = LoggerFactory.Create(builder => { builder.AddConsole(); });



        public AppDbContext(DbContextOptions options) : base(options)
        {
        }



        public override async Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess,
            CancellationToken cancellationToken = default)
        {
            _SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
            return await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
        }

        protected virtual void _SaveChangesAsync(bool acceptAllChangesOnSuccess,
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
        }

        protected virtual void AddTillNext(int i, byte[] bytes, byte[] nBytes)
        {
            if (nBytes.Length - 1 <= i) return;

            if (bytes[i] + nBytes[i] < 225)
                bytes[i] += nBytes[i];
            else
                AddTillNext(i + 1, bytes, nBytes);
        }


        protected virtual Guid NGuid(int n)
        {
            var start = Guid.Empty;
            var bytes = start.ToByteArray();
            var nByte = BitConverter.GetBytes(n);

            for (var i = 0; i < nByte.Length; i++)
                AddTillNext(i, bytes, nByte);

            return new Guid(bytes);
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder
                .UseLoggerFactory(DbContextLoggerFactory)
                .UseLazyLoadingProxies();

        }
    }
}