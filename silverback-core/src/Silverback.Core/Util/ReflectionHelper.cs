using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Silverback.Util
{
    // TODO: Test
    internal static class ReflectionHelper
    {
        public static MethodInfo[] GetAnnotatedMethods<TAttribute>(this Type type)
            where TAttribute : Attribute
            => type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .Where(m => m.GetCustomAttribute<TAttribute>() != null)
                .ToArray();

        public static bool IsAsync(this MethodInfo methodInfo)
            => typeof(Task).IsAssignableFrom(methodInfo.ReturnType);

        public static object[] MapParameterValues(this MethodInfo methodInfo, Dictionary<Type, Func<Type, object>> valuesFactory)
            => MapParameterValues(methodInfo.GetParameters(), valuesFactory);

        public static object[] MapParameterValues(this ParameterInfo[] parameters,
            Dictionary<Type, Func<Type, object>> valuesFactory)
            => parameters
                .Select(p =>
                {
                    var factory = valuesFactory.FirstOrDefault(v => p.ParameterType.IsAssignableFrom(v.Key));
                    return factory.Value.Invoke(factory.Key);
                })
                .ToArray();
    }
}