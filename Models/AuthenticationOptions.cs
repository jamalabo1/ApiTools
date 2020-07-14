using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;
using ApiTools.Context;

namespace ApiTools.Models
{
    public class AuthenticationOptions<TModel, TModelId>
    {
        public Func<IContext<TModel, TModelId>, Task<TModel>> ContextFind { get; set; }
        public Expression<Func<TModel, bool>> Expression { get; set; }
        public string Password { get; set; }
        public string Role { get; set; }
        public IEnumerable<Func<TModel, ServiceResponse<ILoginResponse>>> ValidationOptions { get; set; }
        
        
    }
}