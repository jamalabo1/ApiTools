using System.Collections.Generic;

namespace ApiTools.Models
{
    public partial class RouteRules
    {
        public IEnumerable<string> Rules { get; set; }
        public bool AllowAnonymous { get; set; } = false;
    }
}