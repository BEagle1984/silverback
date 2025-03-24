// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.ComponentModel.DataAnnotations;

namespace Silverback.Tests.Types.Domain;

public class ValidationMessageNestedModel
{
    [StringLength(5)]
    public string String5 { get; set; } = string.Empty;
}
