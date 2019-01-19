// Copyright (c) 2018-2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Reactive.Linq;
using System.Reflection;

namespace Silverback.Util
{
    // TOOD: Test
    internal static class ObservableOfTypeExtensions
    {
        public static IObservable<object> OfType(this IObservable<object> source, Type type) =>
            (IObservable<object>)
            typeof(Observable).GetMethod("OfType", BindingFlags.Static | BindingFlags.Public)
                .MakeGenericMethod(type)
                .Invoke(null, new object[] { source });
    }
}