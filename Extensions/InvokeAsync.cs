using System.Reflection;
using System.Threading.Tasks;

namespace ApiTools.Extensions
{
    public static class ExtensionMethods
    {
        public static async Task<object> InvokeAsync(this MethodInfo method, object obj, params object[] parameters)
        {
            dynamic awaitable = method.Invoke(obj, parameters);
            if (awaitable == null) return null;
            await awaitable;
            return awaitable.GetAwaiter().GetResult();
        }
    }
}