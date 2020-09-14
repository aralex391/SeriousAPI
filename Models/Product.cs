using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace SeriousAPI.Models 
{
    public class Product : IValidatableObject
    {
        public int Id { get; set; }
        [Required]
        public int Price { get; set; } //try feeding negative value
        [Required]
        public string Name { get; set; }
        [Required]
        public int Stock { get; set; }
        public string Description { get; set; }
        /*[Required]
         * public imagedatatype Image { get; set; }
         */



        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            var errorResponseList = new List<ValidationResult>();
            if (Name.Length < 1)
            {
                errorResponseList.Add(new ValidationResult("Name has to contain at least one character"));
            } else if (Name.Length > 120)
            {
                errorResponseList.Add(new ValidationResult("Name can not contain more than 120 characters"));
            }
            return errorResponseList;
        }
    }
}
