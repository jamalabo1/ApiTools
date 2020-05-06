using System;

namespace ApiTools.Models
{
    public class EntitiesListHelperRequirements<T, T2>
    {
        public Func<T, T2, bool> MatchFunc { get; set; }
        public Func<T, T2, bool> ModifiedMatchFunc { get; set; }
        public Func<T2, T, T> MapModel { get; set; }
    }
}