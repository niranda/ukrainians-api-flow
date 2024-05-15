using System.ComponentModel.DataAnnotations;

namespace Ukrainians.Infrastrusture.Data.Entities.Base
{
    public class BaseEntity
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        public bool IsDeleted { get; set; }
    }
}
