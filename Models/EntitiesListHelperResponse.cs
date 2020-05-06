using System.Collections.Generic;

namespace ApiTools.Models
{
    public class EntitiesListHelperResponse<T>
    {
        public IEnumerable<T> Create { get; set; }
        public IEnumerable<T> Delete { get; set; }
        public IEnumerable<T> Update { get; set; }
    }
}