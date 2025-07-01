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

        // Foreign key to User table
        [ForeignKey("Author")]
        public string? AuthorId { get; set; }

        // Navigation property
        public virtual User? Author { get; set; }

        public string? RecipeServings { get; set; }

        [Range(0, 1440, ErrorMessage = "Prep time must be between 0 and 1440 minutes")]
        public int? PrepTimeMin { get; set; }

        [Range(0, 1440, ErrorMessage = "Cook time must be between 0 and 1440 minutes")]
        public int? CookTimeMin { get; set; }
        public string? RecipeMode { get; set; }

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
    public class AddRecipeRequest
    {
        public string? RecipeName { get; set; }
        public string? Description { get; set; }
        public string? AuthorId { get; set; }
        public int? Servings { get; set; }
        public int? CookingTime { get; set; }
        public int? Calories { get; set; }
        public int? Protein { get; set; }
        public int? Carbs { get; set; }
        public int? Fat { get; set; }
        public List<string>? Ingredients { get; set; }
        public List<string>? Instructions { get; set; }
        public string? ImageUrl { get; set; } // Base64 or file path
        public string? RecipeType { get; set; }
    }

    [Table("meal_log")]
    public class MealLog
    {
        [Key]
        [Column("LogID")]
        public int MealLogID { get; set; }

        [Required]
        [ForeignKey("User")]
        public string? UserID { get; set; }

        [Required]
        [StringLength(50)]
        public string? MealType { get; set; } // "almusal", "tanghalian", "meryenda", "hapunan"

        [Required]
        [StringLength(200)]
        public string? MealName { get; set; }

        [Required]
        [Column("date")]
        public DateTime MealDate { get; set; }

        public TimeSpan? MealTime { get; set; }

        public string? Calories { get; set; } // Keep as string to match your RecipeNutritionFacts pattern

        public string? Protein { get; set; } // Keep as string to match your RecipeNutritionFacts pattern

        public string? Carbohydrates { get; set; } // Keep as string to match your RecipeNutritionFacts pattern

        public string? Fat { get; set; } // Keep as string to match your RecipeNutritionFacts pattern

        public string? MealPhoto { get; set; } // URL or file path

        public int? RecipeID { get; set; } // Optional reference to recipe if selected from search

        public bool IsPlanned { get; set; } = false; // Track if this is a planned meal or logged meal

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public virtual User? User { get; set; }

        [ForeignKey("RecipeID")]
        public virtual Recipe? Recipe { get; set; }
    }

    // DTO for API requests
    public class AddMealLogRequest
    {
        public string? MealType { get; set; }
        public string? MealName { get; set; }
        public DateTime MealDate { get; set; }
        public TimeSpan? MealTime { get; set; }
        public string? Calories { get; set; }
        public string? Protein { get; set; }
        public string? Carbohydrates { get; set; }
        public string? Fat { get; set; }
        public string? MealPhoto { get; set; }

        public int? RecipeID { get; set; }
    }

    // DTO for API requests from frontend
    public class CreateMealLogRequest
    {
        public string? MealType { get; set; }
        public string? MealName { get; set; }
        public string? MealDate { get; set; } // String from frontend
        public string? MealTime { get; set; } // String from frontend (e.g., "23:35")
        public string? Calories { get; set; }
        public string? Protein { get; set; }
        public string? Carbohydrates { get; set; }
        public string? Fat { get; set; }
        public string? MealPhoto { get; set; }
        public bool IsPlanned { get; set; }
        public int? RecipeID { get; set; }
    }

    // DTO for API requests with file upload from frontend
    public class CreateMealLogWithPhotoRequest
    {
        public string? MealType { get; set; }
        public string? MealName { get; set; }
        public string? MealDate { get; set; } // String from frontend
        public string? MealTime { get; set; } // String from frontend (e.g., "23:35")
        public string? Calories { get; set; }
        public string? Protein { get; set; }
        public string? Carbohydrates { get; set; }
        public string? Fat { get; set; }
        public IFormFile? MealPhoto { get; set; } // File upload
        public bool IsPlanned { get; set; }
        public int? RecipeID { get; set; }
    }

    // DTO for API responses
    public class MealLogResponse
    {
        public int MealLogID { get; set; }
        public string? MealType { get; set; }
        public string? MealName { get; set; }
        public DateTime MealDate { get; set; }
        public string? MealTime { get; set; }
        public string? Calories { get; set; }
        public string? Protein { get; set; }
        public string? Carbohydrates { get; set; }
        public string? Fat { get; set; }
        public string? MealPhoto { get; set; }

        public string? RecipeName { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    // DTO for daily summary
    public class DailySummaryResponse
    {
        public DateTime Date { get; set; }
        public int TotalCalories { get; set; }
        public decimal TotalProtein { get; set; }
        public decimal TotalCarbs { get; set; }
        public decimal TotalFat { get; set; }
        public int MealCount { get; set; }
        public List<MealLogResponse> Meals { get; set; } = new List<MealLogResponse>();
    }

}
