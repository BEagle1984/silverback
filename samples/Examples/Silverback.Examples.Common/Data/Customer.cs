using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Silverback.Examples.Common.Data
{
    public class Customer
    {
        [Key]
        public int Id { get; set; }

        [StringLength(100)]
        public string Name { get; set; }
    }
}
