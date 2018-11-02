using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Silverback.Util
{
    internal static class ReflectionHelper
    {
        public static MethodInfo[] GetAnnotatedMethods<TAttribute>(this Type type)
            where TAttribute : Attribute
            => type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .Where(m => m.GetCustomAttribute<TAttribute>() != null)
                .ToArray();

        public static bool IsAsync(this MethodInfo methodInfo)
            => typeof(Task).IsAssignableFrom(methodInfo.ReturnType);
    }
}