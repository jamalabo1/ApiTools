using System.Collections.Generic;
using System.Linq;

namespace ApiTools.Models
{
    public class PagingServiceResponse<T>
    {
        public IEnumerable<T> Data { get; set; }
        public int Size { get; set; }
        public int CurrentSize { get; set; }
        public int Page { get; set; }
        public int PerPage { get; set; }

        public static PagingServiceResponse<T> Empty()
        {
            return new PagingServiceResponse<T>
            {
                Page = 0,
                Size = 0,
                PerPage = 0,
                Data = Enumerable.Empty<T>()
            };
        }
    }
}