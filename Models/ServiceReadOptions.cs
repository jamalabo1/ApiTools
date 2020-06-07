using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace ApiTools.Models
{
    public class ServiceReadOptions<TModel>
    {
        public static readonly ServiceReadOptions<TModel> DisableFilter = new ServiceReadOptions<TModel>
            {Filter = false};

        public bool EnableDashedProperty = true;

        public bool EnablePropertyNesting = false;
        public int MaxPropertyNestingLevel = 2;

        public bool Filter { get; set; } = true;
        public bool Sort { get; set; } = true;
        public long? SelectFieldEntityId = null;

        public IEnumerable<Expression<Func<TModel, dynamic>>> Includes { get; set; }
    }
}