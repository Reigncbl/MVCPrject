using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MVCPrject.Models
{

    [Table("recipe_ingredients")]
    public class RecipeIngredients {
        [Key]
        public int IngredientID { get; set; }
        public int RecipeID { get; set; }
        public string IngredientName { get; set; }
        public decimal? Quantity { get; set; }
        public string Unit { get; set; }
       public Recipe Recipe { get; set; }
    }
    [Table("recipe_instructions")]
    public class RecipeInstructions
    {
        [Key]
        public int InstructionID { get; set; }
        public int RecipeID { get; set; }
        public int StepNumber { get; set; }
        public string Instruction { get; set; }

        public Recipe Recipe { get; set; }
    }


    [Table("recipes")]
    public class Recipe
    {
        [Key]
        public int RecipeID { get; set; }
        public string? RecipeName { get; set; }
        public string? RecipeImage { get; set; }
        public string? RecipeDescription { get; set; }
        public string? RecipeURL { get; set; }
        public string? RecipeType { get; set; }
        public string? RecipeAuthor { get; set; }
        public string? RecipeServings { get; set; }
        public int? PrepTimeMin { get; set; }
        public int? CookTimeMin { get; set; }

        public virtual ICollection<RecipeIngredients> Ingredients { get; set; } = new List<RecipeIngredients>();
        public virtual ICollection<RecipeInstructions> Instructions { get; set; } = new List<RecipeInstructions>();


    }
}
