using System.ComponentModel.DataAnnotations;
using VehicleQuotes.Validation;

namespace VehicleQuotes.ResourceModels
{
    public class ModelSpecification
    {
        public int ID { get; set; }
        [Required]
        public string Name { get; set; }
        [Required]

        public ModelSpecificationStyle[] Styles { get; set; }
    }
}
