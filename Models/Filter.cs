using System.Linq;

namespace ApiTools.Models
{
    public interface IFilter
    {
        public IQueryable<T> ApplyFilter<T>(IQueryable<T> query);
    }
}