using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace ApiTools.Models
{
    public class ServiceOptions<TModel>
    {
        public static readonly ServiceOptions<TModel> DisableFilter = new ServiceOptions<TModel>
            {Filter = false};

        public bool EnableDashedProperty = true;

        public bool EnablePropertyNesting = false;
        public int MaxPropertyNestingLevel = 2;

        public bool Filter { get; set; } = true;
        public bool Sort { get; set; } = true;
        public string SelectFieldEntityId = null;

        public bool SelectFieldMany { get; set; } = false;
        public ContextOptions ContextOptions { get; set; }

        public IEnumerable<Expression<Func<TModel, dynamic>>> Includes { get; set; }
    }
}