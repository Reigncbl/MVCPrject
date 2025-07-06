using Microsoft.AspNetCore.Mvc;
using MVCPrject.Services;
using MVCPrject.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using Azure.Storage.Blobs;
using Microsoft.EntityFrameworkCore;
using MVCPrject.Data;

namespace MVCPrject.Controllers
{
    [Authorize] // Require authentication for all actions
    [Route("MealPlanner")]
    public class MealPlannerController : Controller
    {
        private readonly MealLogService _mealLogService;
        private readonly ILogger<MealPlannerController> _logger;
        private readonly IUserCacheService _userCacheService;
        private readonly BlobServiceClient _blobServiceClient;
        private readonly UserManager<User> _userManager;

        public MealPlannerController(MealLogService mealLogService, ILogger<MealPlannerController> logger, IUserCacheService userCacheService, BlobServiceClient blobServiceClient, UserManager<User> userManager)
        {
            _mealLogService = mealLogService;
            _logger = logger;
            _userCacheService = userCacheService;
            _blobServiceClient = blobServiceClient;
            _userManager = userManager;
        }

        // GET: Main meal planner page
        [HttpGet]
        [Route("")]
        public async Task<IActionResult> MealPlanner()
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                var userName = User.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value;
                var email = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;

                var user = await _userManager.GetUserAsync(User);

                // If user is null, try to find by ID from claims
                if (user == null && !string.IsNullOrEmpty(userId))
                {
                    user = await _userManager.FindByIdAsync(userId);
                }

                // If still null, try to find by email
                if (user == null && !string.IsNullOrEmpty(email))
                {
                    user = await _userManager.FindByEmailAsync(email);
                }

                ViewBag.UserName = user?.Name ?? userName ?? email ?? "User";
            }

            return View();
        }

        // GET: Create meal log page
        [HttpGet]
        [Route("Create")]
        public IActionResult Create()
        {
            return View();
        }

        // POST: Create meal log
        [HttpPost]
        [Route("Create")]
        public async Task<IActionResult> Create(MealLog mealLog)
        {
            if (ModelState.IsValid)
            {
                await _mealLogService.AddMealLogAsync(mealLog);
                return RedirectToAction("MealPlanner");
            }
            return View(mealLog);
        }

        // GET: Edit meal log
        [HttpGet]
        [Route("Edit/{id}")]
        public async Task<IActionResult> Edit(int id)
        {
            var mealLog = await _mealLogService.GetMealLogByIdAsync(id);
            if (mealLog == null) return NotFound();
            return View(mealLog);
        }

        // POST: Edit meal log
        [HttpPost]
        [Route("Edit")]
        public async Task<IActionResult> Edit(MealLog mealLog)
        {
            if (ModelState.IsValid)
            {
                await _mealLogService.UpdateMealLogAsync(mealLog);
                return RedirectToAction("MealPlanner");
            }
            return View(mealLog);
        }

        // POST: Delete meal log
        [HttpPost]
        [Route("Delete/{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            await _mealLogService.DeleteMealLogAsync(id);
            return RedirectToAction("MealPlanner");
        }

        // GET: View all meal logs
        [HttpGet]
        [Route("List")]
        public async Task<IActionResult> List()
        {
            var mealLogs = await _mealLogService.ReadMealLogsAsync();
            return View(mealLogs);
        }

        // API endpoints for JavaScript integration

        // GET: Get meal logs for a specific date
        [HttpGet]
        [Route("GetMealLogsByDate")]
        public async Task<IActionResult> GetMealLogsByDate(DateTime date)
        {
            try
            {
                _logger.LogInformation("[API] [GetMealLogsByDate] Called for date: {Date}", date.ToString("yyyy-MM-dd"));

                // Get current user using UserCacheService
                var currentUser = await _userCacheService.GetCurrentUserAsync(User);
                if (currentUser == null)
                {
                    _logger.LogWarning("[API] [GetMealLogsByDate] User not authenticated");
                    return Json(new { success = false, message = "User not authenticated" });
                }

                var currentUserId = currentUser.Id;
                _logger.LogInformation("[API] [GetMealLogsByDate] Getting meal logs for user: {UserID}", currentUserId);

                var mealLogs = await _mealLogService.GetMealLogsByDateAndUserAsync(date, currentUserId);

                _logger.LogInformation("[API] [GetMealLogsByDate] Retrieved {Count} meal logs for date: {Date} and user: {UserID}",
                    mealLogs.Count, date.ToString("yyyy-MM-dd"), currentUserId);

                // Log each meal log for debugging
                foreach (var mealLog in mealLogs)
                {
                    _logger.LogInformation("[API] [GetMealLogsByDate] MealLogID: {MealLogID}, Name: {MealName}, Date: {MealDate}, Type: {MealType}, Photo: {PhotoUrl}",
                        mealLog.MealLogID, mealLog.MealName, mealLog.MealDate, mealLog.MealType, mealLog.MealPhoto);
                }

                return Json(new { success = true, mealLogs = mealLogs });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[API] [GetMealLogsByDate] Error retrieving meal logs for date: {Date}", date.ToString("yyyy-MM-dd"));
                return Json(new { success = false, message = "Error retrieving meal logs" });
            }
        }

        // POST: Create meal log via API
        [HttpPost]
        [Route("CreateMealLog")]
        public async Task<IActionResult> CreateMealLog([FromBody] CreateMealLogRequest request)
        {
            try
            {
                _logger.LogInformation("[API] [CreateMealLog] Called with request: {@Request}", request);

                // Validate request
                if (request == null)
                {
                    _logger.LogWarning("[API] [CreateMealLog] Received null request");
                    return Json(new { success = false, message = "Invalid request data" });
                }

                // Set the UserID from the current authenticated user using UserCacheService
                _logger.LogInformation("[API] [CreateMealLog] Attempting to get current user for meal log creation");
                var currentUser = await _userCacheService.GetCurrentUserAsync(User);

                if (currentUser == null)
                {
                    _logger.LogWarning("[API] [CreateMealLog] User not authenticated - UserCacheService returned null");
                    _logger.LogInformation("[API] [CreateMealLog] User.Identity.IsAuthenticated: {IsAuthenticated}", User.Identity?.IsAuthenticated);
                    _logger.LogInformation("[API] [CreateMealLog] User.Identity.Name: {Name}", User.Identity?.Name);
                    return Json(new { success = false, message = "User not authenticated" });
                }

                // Get recipe image if RecipeID is provided
                string? mealPhotoUrl = request.MealPhoto;
                if (request.RecipeID.HasValue && request.RecipeID > 0)
                {
                    var recipeImage = await GetRecipeImageAsync(request.RecipeID.Value);
                    if (!string.IsNullOrEmpty(recipeImage))
                    {
                        mealPhotoUrl = recipeImage;
                        _logger.LogInformation("[API] [CreateMealLog] Using recipe image for meal log: {RecipeImage}", recipeImage);
                    }
                }

                // Convert DTO to MealLog model
                var mealLog = new MealLog
                {
                    UserID = currentUser.Id,
                    MealType = request.MealType,
                    MealName = request.MealName,
                    Calories = request.Calories,
                    Protein = request.Protein,
                    Carbohydrates = request.Carbohydrates,
                    Fat = request.Fat,
                    MealPhoto = mealPhotoUrl,
                    IsPlanned = request.IsPlanned,
                    RecipeID = request.RecipeID,
                    CreatedAt = DateTime.UtcNow
                };

                // Parse and set MealDate
                if (DateTime.TryParse(request.MealDate, out DateTime parsedDate))
                {
                    mealLog.MealDate = parsedDate;
                }
                else
                {
                    _logger.LogWarning("[API] [CreateMealLog] Invalid MealDate format: {MealDate}", request.MealDate);
                    return Json(new { success = false, message = "Invalid date format" });
                }

                // Parse and set MealTime
                if (!string.IsNullOrEmpty(request.MealTime) && TimeSpan.TryParse(request.MealTime, out TimeSpan parsedTime))
                {
                    mealLog.MealTime = parsedTime;
                }
                else if (!string.IsNullOrEmpty(request.MealTime))
                {
                    _logger.LogWarning("[API] [CreateMealLog] Invalid MealTime format: {MealTime}", request.MealTime);
                    return Json(new { success = false, message = "Invalid time format" });
                }

                _logger.LogInformation("[API] [CreateMealLog] Created MealLog object: {@MealLog}", mealLog);

                // Clear ModelState to avoid validation issues from the DTO binding
                ModelState.Clear();

                // Validate the MealLog model
                if (TryValidateModel(mealLog))
                {
                    var result = await _mealLogService.AddMealLogAsync(mealLog);

                    if (result)
                    {
                        _logger.LogInformation("[API] [CreateMealLog] Successfully created meal log for: {MealName}", mealLog.MealName);
                        return Json(new { success = true, message = "Meal logged successfully" });
                    }
                    else
                    {
                        _logger.LogWarning("[API] [CreateMealLog] Failed to create meal log for: {MealName}", mealLog.MealName);
                        return Json(new { success = false, message = "Failed to save meal log" });
                    }
                }

                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                _logger.LogWarning("[API] [CreateMealLog] Invalid model state: {Errors}", string.Join(", ", errors));

                // Log detailed model state information
                foreach (var modelState in ModelState)
                {
                    if (modelState.Value.Errors.Count > 0)
                    {
                        _logger.LogWarning("[API] [CreateMealLog] Model validation error for {Key}: {Errors}",
                            modelState.Key,
                            string.Join(", ", modelState.Value.Errors.Select(e => e.ErrorMessage)));
                    }
                }

                return Json(new { success = false, message = "Invalid data", errors = errors });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[API] [CreateMealLog] Error creating meal log for: {MealName}", request?.MealName ?? "Unknown");
                return Json(new { success = false, message = ex.Message });
            }
        }

        // POST: Create meal log with file upload via API
        [HttpPost]
        [Route("CreateMealLogWithPhoto")]
        public async Task<IActionResult> CreateMealLogWithPhoto([FromForm] CreateMealLogWithPhotoRequest request)
        {
            try
            {
                _logger.LogInformation("CreateMealLogWithPhoto called for meal: {MealName}, Type: {MealType}, Date: {Date}",
                    request.MealName, request.MealType, request.MealDate);

                // Validate request
                if (request == null)
                {
                    _logger.LogWarning("CreateMealLogWithPhoto received null request");
                    return Json(new { success = false, message = "Invalid request data" });
                }

                // Set the UserID from the current authenticated user using UserCacheService
                _logger.LogInformation("Attempting to get current user for meal log creation");
                var currentUser = await _userCacheService.GetCurrentUserAsync(User);

                if (currentUser == null)
                {
                    _logger.LogWarning("User not authenticated for meal log creation - UserCacheService returned null");
                    _logger.LogInformation("User.Identity.IsAuthenticated: {IsAuthenticated}", User.Identity?.IsAuthenticated);
                    _logger.LogInformation("User.Identity.Name: {Name}", User.Identity?.Name);
                    return Json(new { success = false, message = "User not authenticated" });
                }

                // Handle photo - either uploaded file or recipe image
                string? photoUrl = null;

                // First, check if there's a recipe ID and get recipe image
                if (request.RecipeID.HasValue && request.RecipeID > 0)
                {
                    var recipeImage = await GetRecipeImageAsync(request.RecipeID.Value);
                    if (!string.IsNullOrEmpty(recipeImage))
                    {
                        photoUrl = recipeImage;
                        _logger.LogInformation("Using recipe image for meal log: {RecipeImage}", recipeImage);
                    }
                }

                // If no recipe image and user uploaded a photo, use uploaded photo
                if (string.IsNullOrEmpty(photoUrl) && request.MealPhoto != null && request.MealPhoto.Length > 0)
                {
                    try
                    {
                        photoUrl = await UploadMealPhotoAsync(request.MealPhoto, currentUser.Id);
                        _logger.LogInformation("Successfully uploaded meal photo: {PhotoUrl}", photoUrl);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error uploading meal photo");
                        return Json(new { success = false, message = "Error uploading photo" });
                    }
                }

                // Convert DTO to MealLog model
                var mealLog = new MealLog
                {
                    UserID = currentUser.Id,
                    MealType = request.MealType,
                    MealName = request.MealName,
                    Calories = request.Calories,
                    Protein = request.Protein,
                    Carbohydrates = request.Carbohydrates,
                    Fat = request.Fat,
                    MealPhoto = photoUrl, // Use the uploaded photo URL
                    IsPlanned = request.IsPlanned,
                    RecipeID = request.RecipeID,
                    CreatedAt = DateTime.UtcNow
                };

                // Parse and set MealDate
                if (DateTime.TryParse(request.MealDate, out DateTime parsedDate))
                {
                    mealLog.MealDate = parsedDate;
                }
                else
                {
                    _logger.LogWarning("Invalid MealDate format: {MealDate}", request.MealDate);
                    return Json(new { success = false, message = "Invalid date format" });
                }

                // Parse and set MealTime
                if (!string.IsNullOrEmpty(request.MealTime) && TimeSpan.TryParse(request.MealTime, out TimeSpan parsedTime))
                {
                    mealLog.MealTime = parsedTime;
                }
                else if (!string.IsNullOrEmpty(request.MealTime))
                {
                    _logger.LogWarning("Invalid MealTime format: {MealTime}", request.MealTime);
                    return Json(new { success = false, message = "Invalid time format" });
                }

                _logger.LogInformation("Successfully created MealLog object - UserID: {UserID}, MealType: {MealType}, Date: {Date}, Photo: {PhotoUrl}",
                    mealLog.UserID, mealLog.MealType, mealLog.MealDate.ToString("yyyy-MM-dd"), photoUrl ?? "None");

                // Clear ModelState to avoid validation issues from the DTO binding
                ModelState.Clear();

                // Validate the MealLog model
                if (TryValidateModel(mealLog))
                {
                    var result = await _mealLogService.AddMealLogAsync(mealLog);

                    if (result)
                    {
                        _logger.LogInformation("Successfully created meal log for: {MealName}", mealLog.MealName);
                        return Json(new { success = true, message = "Meal logged successfully", photoUrl = photoUrl });
                    }
                    else
                    {
                        _logger.LogWarning("Failed to create meal log for: {MealName}", mealLog.MealName);
                        return Json(new { success = false, message = "Failed to save meal log" });
                    }
                }

                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                _logger.LogWarning("Invalid model state for CreateMealLogWithPhoto: {Errors}", string.Join(", ", errors));

                // Log detailed model state information
                foreach (var modelState in ModelState)
                {
                    if (modelState.Value.Errors.Count > 0)
                    {
                        _logger.LogWarning("Model validation error for {Key}: {Errors}",
                            modelState.Key,
                            string.Join(", ", modelState.Value.Errors.Select(e => e.ErrorMessage)));
                    }
                }

                return Json(new { success = false, message = "Invalid data", errors = errors });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating meal log for: {MealName}", request?.MealName ?? "Unknown");
                return Json(new { success = false, message = ex.Message });
            }
        }

        // Helper method to upload meal photo to Azure Blob Storage
        private async Task<string> UploadMealPhotoAsync(IFormFile photo, string userId)
        {
            var containerClient = _blobServiceClient.GetBlobContainerClient("meal-photos");
            await containerClient.CreateIfNotExistsAsync();

            // Generate unique blob name
            var fileExtension = Path.GetExtension(photo.FileName);
            var blobName = $"{userId}/{Guid.NewGuid()}{fileExtension}";
            var blobClient = containerClient.GetBlobClient(blobName);

            // Upload the file
            using (var stream = photo.OpenReadStream())
            {
                await blobClient.UploadAsync(stream, overwrite: true);
            }

            return blobClient.Uri.ToString();
        }

        // Helper method to get recipe image URL from database
        private async Task<string?> GetRecipeImageAsync(int recipeId)
        {
            try
            {
                // You'll need to inject a recipe service or use DbContext directly
                // For now, I'll show the pattern - you may need to adjust based on your architecture
                using var scope = HttpContext.RequestServices.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<DBContext>();

                var recipe = await dbContext.Recipes
                    .Where(r => r.RecipeID == recipeId)
                    .Select(r => new { r.RecipeImage })
                    .FirstOrDefaultAsync();

                return recipe?.RecipeImage;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving recipe image for RecipeID: {RecipeID}", recipeId);
                return null;
            }
        }

        // PUT: Update meal log via API
        [HttpPut]
        [Route("UpdateMealLog")]
        public async Task<IActionResult> UpdateMealLog([FromBody] MealLog mealLog)
        {
            try
            {
                _logger.LogInformation("UpdateMealLog called for meal ID: {MealLogID}, Name: {MealName}",
                    mealLog.MealLogID, mealLog.MealName);

                if (ModelState.IsValid)
                {
                    var result = await _mealLogService.UpdateMealLogAsync(mealLog);

                    if (result)
                    {
                        _logger.LogInformation("Successfully updated meal log ID: {MealLogID}", mealLog.MealLogID);
                        return Json(new { success = true, message = "Meal updated successfully" });
                    }
                    else
                    {
                        _logger.LogWarning("Failed to update meal log ID: {MealLogID} - not found", mealLog.MealLogID);
                        return Json(new { success = false, message = "Meal log not found" });
                    }
                }

                _logger.LogWarning("Invalid model state for UpdateMealLog: {Errors}",
                    string.Join(", ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)));
                return Json(new { success = false, message = "Invalid data" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating meal log ID: {MealLogID}", mealLog?.MealLogID ?? 0);
                return Json(new { success = false, message = ex.Message });
            }
        }

        // DELETE: Delete meal log via API
        [HttpDelete]
        [Route("DeleteMealLog/{id}")]
        public async Task<IActionResult> DeleteMealLog(int id)
        {
            try
            {
                _logger.LogInformation("DeleteMealLog called for meal ID: {MealLogID}", id);

                // Get current user using UserCacheService
                var currentUser = await _userCacheService.GetCurrentUserAsync(User);
                if (currentUser == null)
                {
                    _logger.LogWarning("User not authenticated for DeleteMealLog");
                    return Json(new { success = false, message = "User not authenticated" });
                }

                var currentUserId = currentUser.Id;

                // Check if the meal log belongs to the current user
                var mealLog = await _mealLogService.GetMealLogByIdAsync(id);
                if (mealLog == null)
                {
                    _logger.LogWarning("Meal log ID: {MealLogID} not found", id);
                    return Json(new { success = false, message = "Meal log not found" });
                }

                if (mealLog.UserID != currentUserId)
                {
                    _logger.LogWarning("User {UserID} attempted to delete meal log {MealLogID} belonging to user {OwnerUserID}",
                        currentUserId, id, mealLog.UserID);
                    return Json(new { success = false, message = "Unauthorized to delete this meal log" });
                }

                var result = await _mealLogService.DeleteMealLogAsync(id);

                if (result)
                {
                    _logger.LogInformation("Successfully deleted meal log ID: {MealLogID} for user: {UserID}", id, currentUserId);
                    return Json(new { success = true, message = "Meal deleted successfully" });
                }
                else
                {
                    _logger.LogWarning("Failed to delete meal log ID: {MealLogID}", id);
                    return Json(new { success = false, message = "Failed to delete meal log" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting meal log ID: {MealLogID}", id);
                return Json(new { success = false, message = ex.Message });
            }
        }
    }
}