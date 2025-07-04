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
                // Debug: Check authentication state
                Console.WriteLine($"🔍 DEBUG: User.Identity.IsAuthenticated = {User.Identity?.IsAuthenticated}");
                Console.WriteLine($"🔍 DEBUG: User.Identity.Name = {User.Identity?.Name}");
                
                // Debug: Check claims
                var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                var emailClaim = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;
                Console.WriteLine($"🔍 DEBUG: UserID from claims = {userIdClaim}");
                Console.WriteLine($"🔍 DEBUG: Email from claims = {emailClaim}");

                var user = await _userCacheService.GetCurrentUserAsync(User);
                Console.WriteLine($"🔍 DEBUG: UserCacheService returned user = {(user != null ? $"ID: {user.Id}, Name: {user.Name}" : "NULL")}");
                
                if (user == null) 
                {
                    // Try fallback approach like HomeController
                    if (!string.IsNullOrEmpty(userIdClaim))
                    {
                        Console.WriteLine($"🔍 DEBUG: Trying fallback - looking up user by ID: {userIdClaim}");
                        // You'll need to inject UserManager<User> for this fallback
                        // For now, return debug info
                        return Ok(new { 
                            success = false, 
                            message = "User not authenticated via UserCacheService", 
                            debug = new {
                                isAuthenticated = User.Identity?.IsAuthenticated,
                                userIdFromClaims = userIdClaim,
                                emailFromClaims = emailClaim
                            }
                        });
                    }
                    return Ok(new { success = false, message = "User not authenticated" });
                }

                string userId = user.Id;
                Console.WriteLine($"🔍 DEBUG: Using UserID = {userId}");

                var summary = await _context.NutritionSummaries
                    .FirstOrDefaultAsync(n => n.UserID == userId);

                Console.WriteLine($"🔍 DEBUG: Found nutrition summary = {(summary != null ? "YES" : "NO")}");

                if (summary == null)
                {
                    Console.WriteLine($"🔍 DEBUG: No summary found, returning default values");
                    return Ok(new
                    {
                        success = true,
                        data = new
                        {
                            calories = 0,
                            proteins = 0,
                            carbs = 0,
                            fats = 0
                        },
                        debug = new {
                            userId = userId,
                            foundSummary = false
                        }
                    });
                }

                Console.WriteLine($"🔍 DEBUG: Returning summary data - Calories: {summary.Calories}, Proteins: {summary.Proteins}");
                return Ok(new
                {
                    success = true,
                    data = new
                    {
                        calories = summary.Calories ?? 0,
                        proteins = summary.Proteins ?? 0,
                        carbs = summary.Carbs ?? 0,
                        fats = summary.Fats ?? 0
                    },
                    debug = new {
                        userId = userId,
                        foundSummary = true
                    }
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"🔍 DEBUG: Exception occurred: {ex.Message}");
                Console.WriteLine($"🔍 DEBUG: Stack trace: {ex.StackTrace}");
                return Ok(new { success = false, message = $"Error retrieving nutrition summary: {ex.Message}" });
            }
        }

            }
}