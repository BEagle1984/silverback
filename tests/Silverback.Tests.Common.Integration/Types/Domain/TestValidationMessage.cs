// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.ComponentModel.DataAnnotations;

namespace Silverback.Tests.Types.Domain
{
    public class TestValidationMessage : IIntegrationEvent
    {
        public static TestValidationMessage ValidMessage =>
            new() { Id = "1", String10 = "123456789", IntRange = 5, NumbersOnly = "123" };

        public static TestValidationMessage MessageHavingSinglePropertyInvalid =>
            new() { Id = "1", String10 = "123456789abc", IntRange = 5, NumbersOnly = "123" };

        public static TestValidationMessage MessageHavingAllPropertiesInvalid =>
            new() { String10 = "123456789abc", IntRange = 56, NumbersOnly = "123ssss" };

        [StringLength(10)]
        public string String10 { get; set; } = null!;

        [Required]
        public string Id { get; set; } = null!;

        [Range(typeof(int), "5", "10")]
        public int IntRange { get; set; }

        [RegularExpression("^[0-9]*$")]
        public string NumbersOnly { get; set; } = null!;

        public ValidationMessageNestedModel? FirstNested { get; set; }

        public ValidationMessageNestedModel? SecondNested { get; set; }
    }
}
