// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.ComponentModel.DataAnnotations;

namespace Silverback.Tests.Performance.TestTypes;

public class TestValidationLargeMessage
{
    public static TestValidationLargeMessage ValidMessage =>
        new()
        {
            Id = "1234",
            Index = "120",
            Identifier = Guid.Parse("2e070c93-7d9c-4e4e-9ff8-4cbafc5c2a77"),
            IsActive = "true",
            Balance = "$3,749.42",
            Picture = "http://placehold.it/32x32",
            Age = "30",
            EyeColor = "blue",
            Name = "Wong",
            Gender = "Male",
            Company = "QUILK",
            Email = "faulknerwong@quilk.com",
            Phone = "+1 (924) 484-2151",
            Address = "adr",
            About = "123",
            Registered = "2021-01-08T09:22:17 -01:00"
        };

    public static TestValidationLargeMessage MessageHavingSinglePropertyInvalid =>
        new()
        {
            Id = "123456789abcd",
            Index = "120",
            Identifier = Guid.Parse("2e070c93-7d9c-4e4e-9ff8-4cbafc5c2a77"),
            IsActive = "true",
            Balance = "$3,749.42",
            Picture = "http://placehold.it/32x32",
            Age = "30",
            EyeColor = "blue",
            Name = "Wong",
            Gender = "Male",
            Company = "QUILK",
            Email = "faulknerwong@quilk.com",
            Phone = "+1 (924) 484-2151",
            Address = "adr",
            About = "123",
            Registered = "2021-01-08T09:22:17 -01:00"
        };

    public static TestValidationLargeMessage MessageHavingSeveralPropertiesInvalid =>
        new()
        {
            Id = "60f586ec9dbad942f192ad02",
            Identifier = Guid.Parse("2e070c93-7d9c-4e4e-9ff8-4cbafc5c2a77"),
            IsActive = "true",
            Balance = "$3,749.42",
            Picture = "http://placehold.it/32x32",
            Age = "30000",
            EyeColor = "blue",
            Name = "Faulkner Wong 1233455436",
            Gender = "Male",
            Company = "QUILK 1233245345",
            Email = "faulknerwong",
            Phone = "+1 (924) 484-2151 9999999999999999999999999",
            Address = "907 Tech Place, Rockhill, Arkansas, 9829",
            About =
                "Irure adipisicing consequat magna ipsum dolor eiusmod labore magna deserunt pariatur duis. Esse quis magna esse aliqua Lorem esse irure veniam ex magna cillum exercitation. Minim aliqua tempor anim laboris officia eiusmod dolor eu duis fugiat. Reprehenderit ipsum excepteur est culpa officia sunt non. Cillum exercitation aliquip ex minim tempor occaecat magna elit cupidatat aliquip ea. In id cillum aliqua est occaecat ea cupidatat.",
            Registered = "2021-01-08T09:22:17 -01:00"
        };

    [Required]
    public string Id { get; set; } = null!;

    [Required]
    public string Index { get; set; } = null!;

    [Required]
    public Guid Identifier { get; set; }

    [Required]
    [StringLength(10)]
    public string IsActive { get; set; } = null!;

    [Required]
    [StringLength(10)]
    public string Balance { get; set; } = null!;

    public string Picture { get; set; } = null!;

    [Required]
    [StringLength(2)]
    public string Age { get; set; } = null!;

    public string EyeColor { get; set; } = null!;

    [Required]
    [StringLength(10)]
    public string Name { get; set; } = null!;

    [Required]
    [StringLength(10)]
    public string Gender { get; set; } = null!;

    [StringLength(10)]
    public string Company { get; set; } = null!;

    public string Email { get; set; } = null!;

    [StringLength(20)]
    public string Phone { get; set; } = null!;

    [StringLength(10)]
    public string Address { get; set; } = null!;

    [StringLength(10)]
    public string About { get; set; } = null!;

    [StringLength(50)]
    public string Registered { get; set; } = null!;
}
