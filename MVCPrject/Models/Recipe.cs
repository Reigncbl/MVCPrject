using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MVCPrject.Models
{

    [Table("recipe_ingredients")]
    public class RecipeIngredients
    {
        [Key]
        public int IngredientID { get; set; }
        public int RecipeID { get; set; }
        public string? IngredientName { get; set; }
        public decimal? Quantity { get; set; }
        public string? Unit { get; set; }
        public Recipe? Recipe { get; set; }
    }
    [Table("recipe_instructions")]
    public class RecipeInstructions
    {
        [Key]
        public int InstructionID { get; set; }
        public int? RecipeID { get; set; }
        public int? StepNumber { get; set; }
        public string? Instruction { get; set; }

        public Recipe? Recipe { get; set; }
    }


    [Table("recipes")]
    public class Recipe
    {
        [Key]
        public int RecipeID { get; set; }
        
        [Required(ErrorMessage = "Recipe name is required")]
        [StringLength(200, ErrorMessage = "Recipe name cannot exceed 200 characters")]
        public string? RecipeName { get; set; }
        
        [Url(ErrorMessage = "Please enter a valid URL for the image")]
        public string? RecipeImage { get; set; }
        
        [StringLength(1000, ErrorMessage = "Description cannot exceed 1000 characters")]
        public string? RecipeDescription { get; set; }
        
        [Url(ErrorMessage = "Please enter a valid URL")]
        public string? RecipeURL { get; set; }
        
        public string? RecipeType { get; set; }
        
        [StringLength(100, ErrorMessage = "Author name cannot exceed 100 characters")]
        public string? RecipeAuthor { get; set; }
        
        public string? RecipeServings { get; set; }
        
        [Range(0, 1440, ErrorMessage = "Prep time must be between 0 and 1440 minutes")]
        public int? PrepTimeMin { get; set; }
        
        [Range(0, 1440, ErrorMessage = "Cook time must be between 0 and 1440 minutes")]
        public int? CookTimeMin { get; set; }

        // Computed property for total time
        public int? TotalTimeMin
        {
            get
            {
                return (PrepTimeMin ?? 0) + (CookTimeMin ?? 0);
            }
        }

        public virtual ICollection<RecipeIngredients> Ingredients { get; set; } = new List<RecipeIngredients>();
        public virtual ICollection<RecipeInstructions> Instructions { get; set; } = new List<RecipeInstructions>();
    }



    [Table("RecipeNutritionFacts")]
    public class RecipeNutritionFacts
    {
        [Key]
        public int NutritionFactsID { get; set; }
        public int? RecipeID { get; set; }
        public string? Calories { get; set; }
        public string? Carbohydrates { get; set; }
        public string? Protein { get; set; }
        public string? Fat { get; set; }
        public string? Monounsaturated_Fat { get; set; }
        public string? Trans_Fat { get; set; }
        public string? Cholesterol { get; set; }
        public string? Sodium { get; set; }
        public string? Potassium { get; set; }
        public string? Fiber { get; set; }
        public string? Sugar { get; set; }
        public string? Vitamin_A { get; set; }
        public string? Vitamin_C { get; set; }
        public string? Calcium { get; set; }
        public string? Iron { get; set; }

    }

    [Table("recipe_likes")]
    public class RecipeLikes
    {
        [Key]
        public int LikeID { get; set; }

        [Required]
        [ForeignKey("Recipe")]
        public int RecipeID { get; set; }

        [Required]
        [ForeignKey("User")]
        public string? UserID { get; set; }

        [Required]
        public DateTime LikedAt { get; set; } = DateTime.UtcNow;

        public virtual Recipe? Recipe { get; set; }

        // Assuming a User model exists
        public virtual User? User { get; set; }
    }

    public class RecipeDetailsViewModel
    {
        public Recipe? Recipe { get; set; }
        public RecipeNutritionFacts? NutritionFacts { get; set; }
    }


    public class LikeRequest
    {
        public int RecipeId { get; set; }
    }

}
