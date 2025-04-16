using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;

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
        [Required]
        public string RecipeInstructions { get; set; }
        [Required]
        public string RecipeImage { get; set; }
        [Required]
        public string RecipeCategory { get; set; }
        [Required]
        public string RecipeTime { get; set; }
        [Required]
        public string RecipeServings { get; set; }
       
    }
}
