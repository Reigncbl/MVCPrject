using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MVCPrject.Models
{
    [Table("stored_recipes")]
    public class Recipe
    {
        [Key]
        public int RecipeID { get; set; }

        [Required]
        public string RecipeName { get; set; }

        [Required]
        public string RecipeImage { get; set; }

        [Required]
        public string RecipeDescription { get; set; }

        [Required]
        public string RecipeIngredients { get; set; }

        [Required]
        public string RecipeInstructions{ get; set; }

        [Required]
        public string RecipeURL{ get; set; }
     
        public string? RecipeType { get; set; }
    }
}
