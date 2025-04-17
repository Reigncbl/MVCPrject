using System.ComponentModel.DataAnnotations;

namespace MVCPrject.Models
{
    public class Recipe
    {
        [Key]
        public int Id { get; set; }
        [Required]
        public string RecipeName { get; set; }
        [Required]
        public string RecipeDescription { get; set; }
        [Required]
        public string RecipeIngredients { get; set; }
     

       
    }
}
