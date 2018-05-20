using System;
using System.ComponentModel.DataAnnotations;

namespace Common.Domain
{
    /// <summary>
    /// The base class for all entities
    /// </summary>
    public abstract class ShopEntity : Silverback.Domain.Entity
    {
        [Key]
        public Guid Id { get; set; }
    }
}
