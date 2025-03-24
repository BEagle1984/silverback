// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Xunit;

namespace Silverback.Tests.Core.Util;

public partial class TypesCacheFixture
{
    [SuppressMessage("ReSharper", "InconsistentNaming", Justification = "TestData")]
    public static TheoryData<string, Type> GetType_ShouldReturnCorrectGenericType_TestData =>
        new()
        {
            // Shortened qualified name with no types
            {
                "Silverback.Tests.Core.Util.TypesCacheFixture+GenericTypeTest`2, Silverback.Core.Tests",
                typeof(GenericTypeTest<,>)
            },

            // Shortened qualified name with types [shortened qualified name],[shortened qualified name, assembly]
            {
                "Silverback.Tests.Core.Util.TypesCacheFixture+GenericTypeTest`2[[System.Int64],[Silverback.Tests.Core.Util.TypesCacheFixture, Silverback.Core.Tests]], Silverback.Core.Tests",
                typeof(GenericTypeTest<long, TypesCacheFixture>)
            },

            // Shortened qualified name with types [full qualified name],[shortened qualified name]
            {
                "Silverback.Tests.Core.Util.TypesCacheFixture+GenericTypeTest`2[[System.Int64, System.Private.CoreLib, Version=1.2.3.4, Culture=neutral, PublicKeyToken=7cec85d7bea7798e],[Silverback.Tests.Core.Util.TypesCacheFixture, Silverback.Core.Tests]], Silverback.Core.Tests",
                typeof(GenericTypeTest<long, TypesCacheFixture>)
            },

            // Shortened qualified name with types [full qualified name],[full qualified name]
            {
                "Silverback.Tests.Core.Util.TypesCacheFixture+GenericTypeTest`2[[System.Int64, System.Private.CoreLib, Version=1.2.3.4, Culture=neutral, PublicKeyToken=7cec85d7bea7798e],[Silverback.Tests.Core.Util.TypesCacheFixture, Silverback.Core.Tests, Version=1.2.3.4, Culture=neutral, PublicKeyToken=null]], Silverback.Core.Tests",
                typeof(GenericTypeTest<long, TypesCacheFixture>)
            },

            // Shortened qualified name with types [shortened qualified name],[full qualified name]
            {
                "Silverback.Tests.Core.Util.TypesCacheFixture+GenericTypeTest`2[[System.Int64, System.Private.CoreLib],[Silverback.Tests.Core.Util.TypesCacheFixture, Silverback.Core.Tests, Version=1.2.3.4, Culture=neutral, PublicKeyToken=null]], Silverback.Core.Tests",
                typeof(GenericTypeTest<long, TypesCacheFixture>)
            },

            // Full qualified name with full qualified types.
            {
                "Silverback.Tests.Core.Util.TypesCacheFixture+GenericTypeTest`2[[System.Int64, System.Private.CoreLib, Version=1.2.3.4, Culture=neutral, PublicKeyToken=7cec85d7bea7798e],[Silverback.Tests.Core.Util.TypesCacheFixture, Silverback.Core.Tests, Version=1.2.3.4, Culture=neutral, PublicKeyToken=null]], Silverback.Core.Tests, Version=1.2.3.4, Culture=neutral, PublicKeyToken=null",
                typeof(GenericTypeTest<long, TypesCacheFixture>)
            },

            // Full qualified name - no culture, with type [full qualified name - no version]
            {
                "System.Collections.Generic.List`1[[System.Char, System.Private.CoreLib, Culture=neutral]], System.Private.CoreLib, Version=5.0.0.0, PublicKeyToken=7cec85d7bea7798e",
                typeof(List<char>)
            },
        };
}
