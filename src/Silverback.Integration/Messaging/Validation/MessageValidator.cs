// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace Silverback.Messaging.Validation
{
    internal static class MessageValidator
    {
        public static (bool IsValid, string? ValidationErrors) CheckMessageIsValid(
            object message,
            MessageValidationMode validationMode)
        {
            var validationContext = new ValidationContext(message);
            var results = new List<ValidationResult>();
            var validMessage = Validator.TryValidateObject(message, validationContext, results, true);

            if (validMessage)
            {
                return (true, null);
            }

            var validationResults = string.Join(
                string.Empty,
                results.Select(
                    validationResult => $"{Environment.NewLine}- {validationResult.ErrorMessage}"));

            if (validationMode == MessageValidationMode.ThrowException)
            {
                throw new
                    MessageValidationException($"The message is not valid:{validationResults}");
            }

            return (false, validationResults);
        }
    }
}
