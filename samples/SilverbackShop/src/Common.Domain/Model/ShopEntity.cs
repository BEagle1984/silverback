using System.ComponentModel.DataAnnotations;

namespace Common.Domain.Model
{
    /// <summary>
    /// The base class for all entities
    /// </summary>
    public abstract class ShopEntity : Silverback.Domain.Entity
    {
        [Key]
        public int Id { get; set; }

        public bool IsTransient() => Id == default;
    }
}
