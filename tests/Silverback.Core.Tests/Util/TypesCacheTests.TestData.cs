// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Silverback.Tests.Core.Util
{
    public partial class TypesCacheTests
    {
        public static IEnumerable<object[]> AssemblyQualifiedNameType_ReturnType()
        {
            // Shortened qualified name with no types
            yield return new object[]
            {
                "Silverback.Tests.Core.Util.TypesCacheTests+GenericTypeTest`2, Silverback.Core.Tests",
                new AssemblyQualifiedGenericTypeResult(
                        typeof(GenericTypeTest<,>)),
            };

            // Shortened qualified name with types [shortened qualified name],[shortened qualified name, assembly]
            yield return new object[]
            {
                "Silverback.Tests.Core.Util.TypesCacheTests+GenericTypeTest`2[[System.Int64],[Silverback.Tests.Core.Util.TypesCacheTests, Silverback.Core.Tests]], Silverback.Core.Tests",
                new AssemblyQualifiedGenericTypeResult(
                        typeof(GenericTypeTest<long, TypesCacheTests>)),
            };

            // Shortened qualified name with types [full qualified name],[shortened qualified name]
            yield return new object[]
            {
                 "Silverback.Tests.Core.Util.TypesCacheTests+GenericTypeTest`2[[System.Int64, System.Private.CoreLib, Version=1.2.3.4, Culture=neutral, PublicKeyToken=7cec85d7bea7798e],[Silverback.Tests.Core.Util.TypesCacheTests, Silverback.Core.Tests]], Silverback.Core.Tests",
                 new AssemblyQualifiedGenericTypeResult(
                        typeof(GenericTypeTest<long, TypesCacheTests>)),
            };

            // Shortened qualified name with types [full qualified name],[full qualified name]
            yield return new object[]
            {
                 "Silverback.Tests.Core.Util.TypesCacheTests+GenericTypeTest`2[[System.Int64, System.Private.CoreLib, Version=1.2.3.4, Culture=neutral, PublicKeyToken=7cec85d7bea7798e],[Silverback.Tests.Core.Util.TypesCacheTests, Silverback.Core.Tests, Version=1.2.3.4, Culture=neutral, PublicKeyToken=null]], Silverback.Core.Tests",
                 new AssemblyQualifiedGenericTypeResult(
                        typeof(GenericTypeTest<long, TypesCacheTests>)),
            };

            // Shortened qualified name with types [shortened qualified name],[full qualified name]
            yield return new object[]
            {
                 "Silverback.Tests.Core.Util.TypesCacheTests+GenericTypeTest`2[[System.Int64, System.Private.CoreLib],[Silverback.Tests.Core.Util.TypesCacheTests, Silverback.Core.Tests, Version=1.2.3.4, Culture=neutral, PublicKeyToken=null]], Silverback.Core.Tests",
                 new AssemblyQualifiedGenericTypeResult(
                        typeof(GenericTypeTest<long, TypesCacheTests>)),
            };

            // Full qualified name with full qualified types.
            yield return new object[]
            {
                 "Silverback.Tests.Core.Util.TypesCacheTests+GenericTypeTest`2[[System.Int64, System.Private.CoreLib, Version=1.2.3.4, Culture=neutral, PublicKeyToken=7cec85d7bea7798e],[Silverback.Tests.Core.Util.TypesCacheTests, Silverback.Core.Tests, Version=1.2.3.4, Culture=neutral, PublicKeyToken=null]], Silverback.Core.Tests, Version=1.2.3.4, Culture=neutral, PublicKeyToken=null",
                 new AssemblyQualifiedGenericTypeResult(
                        typeof(GenericTypeTest<long, TypesCacheTests>)),
            };

            // Full qualified name - no culture, with type [full qualified name - no version]
            yield return new object[]
            {
                 "System.Collections.Generic.List`1[[System.Char, System.Private.CoreLib, Culture=neutral]], System.Private.CoreLib, Version=5.0.0.0, PublicKeyToken=7cec85d7bea7798e",
                 new AssemblyQualifiedGenericTypeResult(
                        typeof(List<char>)),
            };
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1034:Nested types should not be visible", Justification = "Record belongs to unit test data.")]
        public record AssemblyQualifiedGenericTypeResult(Type MatchingType)
        {
        }

        [SuppressMessage("StyleCop.CSharp.DocumentationRules", "CA1812: Avoid uninstantiated internal classes", Justification = "Class loaded via tests.")]
        private class GenericTypeTest<T1, T2>
                where T2 : class
        {
            [SuppressMessage("Major Code Smell", "S1144:Unused private types or members should be removed", Justification = "Generic type assignment.")]
            public T1? P1 { get; }

            [SuppressMessage("Major Code Smell", "S1144:Unused private types or members should be removed", Justification = "Generic type assignment.")]
            public T2? P2 { get; set; }
        }
    }
}
