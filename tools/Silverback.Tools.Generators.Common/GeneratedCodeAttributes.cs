// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

namespace Silverback.Tools.Generators.Common;

public static class GeneratedCodeAttributes
{
    // Applied to the generated members rather than to the types, because the types are partial and the attributes would also exclude
    // the hand-written parts
    public const string ExcludeFromCodeCoverage = "    [ExcludeFromCodeCoverage(Justification = \"Generated code\")]";
}
