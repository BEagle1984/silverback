// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Silverback.Tools.Generators.Common;

public static class ReflectionHelper
{
    private static readonly CodeDomProvider CodeDomProvider = CodeDomProvider.CreateProvider("C#");

    public static PropertyInfo[] GetProperties(Type type, bool includeInherited)
    {
        BindingFlags bindingFlags = BindingFlags.Instance | BindingFlags.Public;

        if (!includeInherited)
            bindingFlags |= BindingFlags.DeclaredOnly;

        return type.GetProperties(bindingFlags).ToArray();
    }

    public static string GetPropertyTypeString(Type propertyType)
    {
        Type? nullableType = Nullable.GetUnderlyingType(propertyType);
        if (nullableType != null)
            return GetTypeName(nullableType) + "?";

        return GetTypeName(propertyType);
    }

    private static string GetTypeName(Type type)
    {
        CodeTypeReferenceExpression typeReferenceExpression = new(new CodeTypeReference(type));

        using StringWriter writer = new();

        CodeDomProvider.GenerateCodeFromExpression(typeReferenceExpression, writer, new CodeGeneratorOptions());
        return writer.GetStringBuilder().ToString();
    }
}
