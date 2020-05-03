using System.Collections.Generic;

namespace ApiTools.Models
{
    public class RouteRules
    {
        public IEnumerable<string> Roles { get; set; }
        public bool AllowAnonymous { get; set; } = false;
    }
}