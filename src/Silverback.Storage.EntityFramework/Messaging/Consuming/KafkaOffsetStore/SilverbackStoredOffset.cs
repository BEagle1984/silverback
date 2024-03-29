// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace Silverback.Messaging.Consuming.KafkaOffsetStore;

/// <summary>
///     Represents a Kafka offset stored in the database.
/// </summary>
[PrimaryKey("GroupId", "Topic", "Partition")]
public class SilverbackStoredOffset
{
    /// <summary>
    ///     Gets the group identifier.
    /// </summary>
    [Required]
    [MaxLength(300)]
    public string GroupId { get; init; } = string.Empty;

    /// <summary>
    ///     Gets the name of the topic.
    /// </summary>
    [Required]
    [MaxLength(300)]
    public string Topic { get; init; } = string.Empty;

    /// <summary>
    ///     Gets or sets the partition.
    /// </summary>
    [Required]
    public int Partition { get; set; }

    /// <summary>
    ///     Gets or sets the offset.
    /// </summary>
    [Required]
    public long Offset { get; set; }
}
