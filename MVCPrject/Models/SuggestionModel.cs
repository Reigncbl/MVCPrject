using MVCPrject.Services;
using System;

namespace MVCPrject.Models
{
    public class Suggestion
    {
        public RecipeLikes? recipeLikes { get; set; }
        public Recipe? recipe { get; set; }
    }
}
