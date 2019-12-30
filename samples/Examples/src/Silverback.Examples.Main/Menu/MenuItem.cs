// TODO: Delete

// // Copyright (c) 2019 Sergio Aquilini
// // This code is licensed under MIT license (see LICENSE file for details)
//
// using System;
// using System.Collections.Generic;
// using System.Diagnostics.CodeAnalysis;
// using System.Linq;
//
// namespace Silverback.Examples.Main.Menu
// {
//     public abstract class MenuItem
//     {
//         private static readonly Dictionary<Type, MenuItem[]> Cache = new Dictionary<Type, MenuItem[]>();
//         
//         protected MenuItem(string name, int sortIndex = 100)
//         {
//             Name = name;
//             SortIndex = sortIndex;
//         }
//
//         public string Name { get; }
//         public int SortIndex { get; }
//
//         [SuppressMessage("ReSharper", "CoVariantArrayConversion")]
//         public static T[] GetAll<T>() where T : MenuItem
//         {
//             var baseType = typeof(T);
//
//             if (!Cache.ContainsKey(baseType))
//             {
//                 Cache[baseType] = baseType.Assembly.GetTypes()
//                     .Where(t => t.IsClass && !t.IsAbstract && t.IsSubclassOf(baseType))
//                     .Select(Activator.CreateInstance)
//                     .Cast<T>()
//                     .OrderBy(c => c.SortIndex).ThenBy(c => c.Name)
//                     .ToArray();
//             }
//             return Cache[baseType].Cast<T>().ToArray();
//         }
//
//         public T[] GetChildren<T>() where T : MenuItem =>
//             GetAll<T>()
//                 .Where(t => t.GetType().Namespace.StartsWith(GetType().Namespace))
//                 .ToArray();
//     }
// }
