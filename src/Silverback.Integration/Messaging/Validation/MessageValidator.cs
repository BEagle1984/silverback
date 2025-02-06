// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;

namespace Silverback.Messaging.Validation
{
    internal static class MessageValidator
    {
        private static readonly ConcurrentDictionary<Type, IReadOnlyCollection<PropertyInfo>>
            NestedObjectPropertiesCache = new();

        public static (bool IsValid, string? ValidationErrors) CheckMessageIsValid(
            object message,
            MessageValidationMode validationMode)
        {
            var validMessage = TryValidateObject(message, out IReadOnlyList<ValidationResult> results);

            if (validMessage)
                return (true, null);

            var validationResults = string.Join(
                string.Empty,
                results.Select(
                    validationResult => $"{Environment.NewLine}- {validationResult.ErrorMessage}"));

            if (validationMode == MessageValidationMode.ThrowException)
                throw new MessageValidationException($"The message is not valid:{validationResults}");

            return (false, validationResults);
        }

        private static bool TryValidateObject(
            object obj,
            out IReadOnlyList<ValidationResult> validationResults)
        {
            ValidationContext validationContext = new(obj);

            List<ValidationResult> results = new();
            validationResults = results;
            bool result = Validator.TryValidateObject(obj, validationContext, results, true);

            return result & ValidateNestedObjects(obj, results);
        }

        private static bool ValidateNestedObjects(object obj, List<ValidationResult> results)
        {
            bool result = true;

            foreach (PropertyInfo property in GetNestedObjectProperties(obj))
            {
                object? value = property.GetValue(obj);

                if (value == null)
                    continue;

                if (value is IEnumerable<object> valueEnumerable)
                {
                    foreach (object? valueItem in valueEnumerable)
                    {
                        bool nextResult = ValidateNestedObject(valueItem, results);
                        if (result)
                            result = nextResult;
                    }
                }
                else
                {
                    bool nextResult = ValidateNestedObject(value, results);
                    if (result)
                        result = nextResult;
                }
            }

            return result;
        }

        private static bool ValidateNestedObject(object obj, List<ValidationResult> results)
        {
            if (TryValidateObject(obj, out IReadOnlyList<ValidationResult> nestedResults))
                return true;

            results.AddRange(nestedResults);
            return false;
        }

        private static IReadOnlyCollection<PropertyInfo> GetNestedObjectProperties(object obj)
        {
            Type type = obj.GetType();

            return NestedObjectPropertiesCache.GetOrAdd(
                type,
                static addedType => GetNestedObjectProperties(addedType));
        }

        private static IReadOnlyCollection<PropertyInfo> GetNestedObjectProperties(Type type)
        {
            if (type.IsPrimitive || type.IsArray && type.HasElementType && type.GetElementType()!.IsPrimitive)
                return Array.Empty<PropertyInfo>();

            return type
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(propertyInfo => propertyInfo.CanRead && propertyInfo.GetIndexParameters().Length == 0)
                .Where(propertyInfo => NestedTypeMustBeValidated(propertyInfo.PropertyType))
                .ToList();
        }

        private static bool NestedTypeMustBeValidated(Type type)
        {
            if (type == typeof(string) || type.IsValueType)
                return false;

            if (type.IsArray && type.HasElementType)
            {
                Type elementType = type.GetElementType()!;
                return NestedTypeMustBeValidated(elementType);
            }

            return true;
        }
    }
}
