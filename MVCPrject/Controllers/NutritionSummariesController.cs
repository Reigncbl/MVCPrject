using Microsoft.AspNetCore.Mvc;
using MVCPrject.Services;
using MVCPrject.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using Azure.Storage.Blobs;
using Microsoft.EntityFrameworkCore;
using MVCPrject.Data;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MVCPrject.Controllers
{
    public class NutritionController : Controller
    {
        private readonly DBContext _context;
        private readonly IUserCacheService _userCacheService;

        public NutritionController(DBContext context, IUserCacheService userCacheService)
        {
            _context = context;
            _userCacheService = userCacheService;
        }

        // GET: /Nutrition/Summary
        public async Task<IActionResult> Summary()
        {
            var user = await _userCacheService.GetCurrentUserAsync(User);
            if (user == null) return Unauthorized();

            string userId = user.Id;

            var summary = await _context.NutritionSummaries
                .FirstOrDefaultAsync(n => n.UserID == userId);

            if (summary == null)
            {
                summary = new NutritionSummary
                {
                    UserID = userId,
                    Calories = 0,
                    Proteins = 0,
                    Carbs = 0,
                    Fats = 0
                };
                _context.NutritionSummaries.Add(summary);
                await _context.SaveChangesAsync();
            }

            var viewModel = new NutritionSummaryViewModel
            {
                UserID = summary.UserID,
                Calories = summary.Calories,
                Proteins = summary.Proteins,
                Carbs = summary.Carbs,
                Fats = summary.Fats
            };

            return View(viewModel);
        }

        // POST: /Nutrition/UpdateGoals
        [HttpPost]
        public async Task<IActionResult> UpdateGoals([FromForm] NutritionSummary model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    var errors = string.Join(", ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
                    return Ok(new { success = false, message = $"Invalid model data: {errors}" });
                }

                var user = await _userCacheService.GetCurrentUserAsync(User);
                if (user == null) return Ok(new { success = false, message = "User not authenticated" });

                string userId = user.Id;

                var summary = await _context.NutritionSummaries
                    .FirstOrDefaultAsync(n => n.UserID == userId);

                if (summary != null)
                {
                    summary.Calories = model.Calories;
                    summary.Proteins = model.Proteins;
                    summary.Carbs = model.Carbs;
                    summary.Fats = model.Fats;

                    _context.Entry(summary).State = EntityState.Modified;
                }
                else
                {
                    summary = new NutritionSummary
                    {
                        UserID = userId,
                        Calories = model.Calories,
                        Proteins = model.Proteins,
                        Carbs = model.Carbs,
                        Fats = model.Fats
                    };
                    _context.NutritionSummaries.Add(summary);
                }

                await _context.SaveChangesAsync();

                return Ok(new
                {
                    success = true,
                    message = "Goals updated successfully",
                    data = new
                    {
                        calories = summary.Calories,
                        proteins = summary.Proteins,
                        carbs = summary.Carbs,
                        fats = summary.Fats
                    }
                });
            }
            catch (Exception ex)
            {
                return Ok(new { success = false, message = $"Error updating goals: {ex.Message}" });
            }
        }

        // GET: /Nutrition/GetNutritionSummary
        [HttpGet]
        public async Task<IActionResult> GetNutritionSummary()
        {
            try
            {
                var user = await _userCacheService.GetCurrentUserAsync(User);
                if (user == null) return Ok(new { success = false, message = "User not authenticated" });

                string userId = user.Id;

                var summary = await _context.NutritionSummaries
                    .FirstOrDefaultAsync(n => n.UserID == userId);

                if (summary == null)
                {
                    return Ok(new
                    {
                        success = true,
                        data = new
                        {
                            calories = 0,
                            proteins = 0,
                            carbs = 0,
                            fats = 0
                        }
                    });
                }

                return Ok(new
                {
                    success = true,
                    data = new
                    {
                        calories = summary.Calories ?? 0,
                        proteins = summary.Proteins ?? 0,
                        carbs = summary.Carbs ?? 0,
                        fats = summary.Fats ?? 0
                    }
                });
            }
            catch (Exception ex)
            {
                return Ok(new { success = false, message = $"Error retrieving nutrition summary: {ex.Message}" });
            }
        }

            }
}