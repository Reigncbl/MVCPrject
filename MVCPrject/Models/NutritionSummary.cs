using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MVCPrject.Models
{
    public class NutritionSummary
    {
        [Key]
        public int NutritionSummaryID { get; set; }

        [ForeignKey("User")]
        public string? UserID { get; set; }

        
        public int? Calories { get; set; }
        public int? Proteins { get; set; }
        public int? Carbs { get; set; }
        public int? Fats { get; set; }

    }

    public class NutritionSummaryViewModel
    {
        public string? UserID { get; set; }

        
        [Display(Name = "Calorie Goal")]
        public int? Calories { get; set; }

        [Display(Name = "Protein Goal")]
        public int? Proteins { get; set; }

        [Display(Name = "Carb Goal")]
        public int? Carbs { get; set; }

        [Display(Name = "Fat Goal")]
        public int? Fats { get; set; }
    }

    public class UpdateNutritionGoalsRequest
    {
        [Required(ErrorMessage = "Calorie goal is required")]
        [Range(0, 10000, ErrorMessage = "Calorie goal must be between 0 and 10,000")]
        [Display(Name = "Calorie Goal")]
        public int Calories { get; set; }

        [Required(ErrorMessage = "Protein goal is required")]
        [Range(0, 1000, ErrorMessage = "Protein goal must be between 0 and 1,000 grams")]
        [Display(Name = "Protein Goal (g)")]
        public int Proteins { get; set; }

        [Required(ErrorMessage = "Carb goal is required")]
        [Range(0, 2000, ErrorMessage = "Carb goal must be between 0 and 2,000 grams")]
        [Display(Name = "Carb Goal (g)")]
        public int Carbs { get; set; }

        [Required(ErrorMessage = "Fat goal is required")]
        [Range(0, 500, ErrorMessage = "Fat goal must be between 0 and 500 grams")]
        [Display(Name = "Fat Goal (g)")]
        public int Fats { get; set; }
    }

    }