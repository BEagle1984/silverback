using System;
using System.ComponentModel.DataAnnotations;
using Silverback.Domain;

namespace Common.Domain
{
    /// <summary>
    /// The base class for all entities
    /// </summary>
    public abstract class ShopEntity : Entity
    {
        [Key]
        public Guid Id { get; set; }
    }
}
