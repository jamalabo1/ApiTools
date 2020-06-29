using System;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace ApiTools.DbInterceptors
{
    public class UpsertDbInterceptor : DbCommandInterceptor
    {
        public override Task<InterceptionResult<DbDataReader>> ReaderExecutingAsync(DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result,
            CancellationToken cancellationToken = new CancellationToken())
        {
            Console.WriteLine(command);
            return base.ReaderExecutingAsync(command, eventData, result, cancellationToken);
        }
    }
}