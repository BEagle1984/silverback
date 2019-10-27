// Copyright (c) 2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Reflection;
using System.Threading.Tasks;

namespace Silverback.Util
{
    internal static class ReflectionHelper
    {
        public static bool ReturnsTask(this MethodInfo methodInfo) =>
            typeof(Task).IsAssignableFrom(methodInfo.ReturnType);
    }
}