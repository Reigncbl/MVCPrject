using Microsoft.EntityFrameworkCore;
using MVCPrject.Models;

namespace MVCPrject.Data
{
    public class MealLogService
    {
        private readonly DBContext _dbContext;

        public MealLogService(DBContext dbContext)
        {
            _dbContext = dbContext;
        }

        // Add a new meal log
        public async Task<int> AddMealLogAsync(MealLog mealLog)
        {
            _dbContext.MealLogs.Add(mealLog);
            await _dbContext.SaveChangesAsync();
            return mealLog.MealLogID;
        }

        // Get meal logs for a specific user and date
        public async Task<List<MealLogResponse>> GetMealLogsByDateAsync(string userId, DateTime date)
        {
            var startDate = date.Date;
            var endDate = startDate.AddDays(1);

            var mealLogs = await _dbContext.MealLogs
                .Include(ml => ml.Recipe)
                .Where(ml => ml.UserID == userId && ml.MealDate >= startDate && ml.MealDate < endDate)
                .OrderBy(ml => ml.MealTime)
                .ThenBy(ml => ml.CreatedAt)
                .Select(ml => new MealLogResponse
                {
                    MealLogID = ml.MealLogID,
                    MealType = ml.MealType,
                    MealName = ml.MealName,
                    MealDate = ml.MealDate,
                    MealTime = ml.MealTime.HasValue ? ml.MealTime.Value.ToString(@"hh\:mm") : null,
                    Calories = ml.Calories,
                    Protein = ml.Protein,
                    Carbohydrates = ml.Carbohydrates,
                    Fat = ml.Fat,
                    MealPhoto = ml.MealPhoto,
                    Mode = ml.Mode,
                    RecipeName = ml.Recipe != null ? ml.Recipe.RecipeName : null,
                    Notes = ml.Notes,
                    CreatedAt = ml.CreatedAt
                })
                .ToListAsync();

            return mealLogs;
        }

        // Get meal logs by meal type for a specific user and date
        public async Task<List<MealLogResponse>> GetMealLogsByTypeAsync(string userId, DateTime date, string mealType)
        {
            var startDate = date.Date;
            var endDate = startDate.AddDays(1);

            var mealLogs = await _dbContext.MealLogs
                .Include(ml => ml.Recipe)
                .Where(ml => ml.UserID == userId && 
                           ml.MealDate >= startDate && 
                           ml.MealDate < endDate && 
                           ml.MealType == mealType)
                .OrderBy(ml => ml.MealTime)
                .ThenBy(ml => ml.CreatedAt)
                .Select(ml => new MealLogResponse
                {
                    MealLogID = ml.MealLogID,
                    MealType = ml.MealType,
                    MealName = ml.MealName,
                    MealDate = ml.MealDate,
                    MealTime = ml.MealTime.HasValue ? ml.MealTime.Value.ToString(@"hh\:mm") : null,
                    Calories = ml.Calories,
                    Protein = ml.Protein,
                    Carbohydrates = ml.Carbohydrates,
                    Fat = ml.Fat,
                    MealPhoto = ml.MealPhoto,
                    Mode = ml.Mode,
                    RecipeName = ml.Recipe != null ? ml.Recipe.RecipeName : null,
                    Notes = ml.Notes,
                    CreatedAt = ml.CreatedAt
                })
                .ToListAsync();

            return mealLogs;
        }

        // Get daily summary for a user
        public async Task<DailySummaryResponse> GetDailySummaryAsync(string userId, DateTime date)
        {
            var startDate = date.Date;
            var endDate = startDate.AddDays(1);

            var mealLogs = await GetMealLogsByDateAsync(userId, date);

            var summary = new DailySummaryResponse
            {
                Date = date,
                TotalCalories = mealLogs.Sum(ml => int.TryParse(ml.Calories, out int cal) ? cal : 0),
                TotalProtein = mealLogs.Sum(ml => decimal.TryParse(ml.Protein, out decimal prot) ? prot : 0),
                TotalCarbs = mealLogs.Sum(ml => decimal.TryParse(ml.Carbohydrates, out decimal carb) ? carb : 0),
                TotalFat = mealLogs.Sum(ml => decimal.TryParse(ml.Fat, out decimal fat) ? fat : 0),
                MealCount = mealLogs.Count,
                Meals = mealLogs
            };

            return summary;
        }

        // Update a meal log
        public async Task<bool> UpdateMealLogAsync(int mealLogId, string userId, MealLog updatedMealLog)
        {
            var existingMealLog = await _dbContext.MealLogs
                .FirstOrDefaultAsync(ml => ml.MealLogID == mealLogId && ml.UserID == userId);

            if (existingMealLog == null)
                return false;

            // Update properties
            existingMealLog.MealType = updatedMealLog.MealType;
            existingMealLog.MealName = updatedMealLog.MealName;
            existingMealLog.MealDate = updatedMealLog.MealDate;
            existingMealLog.MealTime = updatedMealLog.MealTime;
            existingMealLog.Calories = updatedMealLog.Calories;
            existingMealLog.Protein = updatedMealLog.Protein;
            existingMealLog.Carbohydrates = updatedMealLog.Carbohydrates;
            existingMealLog.Fat = updatedMealLog.Fat;
            existingMealLog.MealPhoto = updatedMealLog.MealPhoto;
            existingMealLog.Mode = updatedMealLog.Mode;
            existingMealLog.RecipeID = updatedMealLog.RecipeID;
            existingMealLog.Notes = updatedMealLog.Notes;
            existingMealLog.UpdatedAt = DateTime.UtcNow;

            await _dbContext.SaveChangesAsync();
            return true;
        }

        // Delete a meal log
        public async Task<bool> DeleteMealLogAsync(int mealLogId, string userId)
        {
            var mealLog = await _dbContext.MealLogs
                .FirstOrDefaultAsync(ml => ml.MealLogID == mealLogId && ml.UserID == userId);

            if (mealLog == null)
                return false;

            _dbContext.MealLogs.Remove(mealLog);
            await _dbContext.SaveChangesAsync();
            return true;
        }

        // Get meal logs for a date range
        public async Task<List<DailySummaryResponse>> GetMealLogsForDateRangeAsync(string userId, DateTime startDate, DateTime endDate)
        {
            var summaries = new List<DailySummaryResponse>();
            
            for (var date = startDate.Date; date <= endDate.Date; date = date.AddDays(1))
            {
                var summary = await GetDailySummaryAsync(userId, date);
                summaries.Add(summary);
            }

            return summaries;
        }

        // Get nutrition totals by meal type for a specific date
        public async Task<Dictionary<string, object>> GetNutritionByMealTypeAsync(string userId, DateTime date)
        {
            var startDate = date.Date;
            var endDate = startDate.AddDays(1);

            var mealLogs = await _dbContext.MealLogs
                .Where(ml => ml.UserID == userId && ml.MealDate >= startDate && ml.MealDate < endDate)
                .ToListAsync();

            var mealTypeNutrition = mealLogs
                .GroupBy(ml => ml.MealType)
                .ToDictionary(g => g.Key ?? "unknown", g => (object)new
                {
                    TotalCalories = g.Sum(ml => int.TryParse(ml.Calories, out int cal) ? cal : 0),
                    TotalProtein = g.Sum(ml => decimal.TryParse(ml.Protein, out decimal prot) ? prot : 0),
                    TotalCarbs = g.Sum(ml => decimal.TryParse(ml.Carbohydrates, out decimal carb) ? carb : 0),
                    TotalFat = g.Sum(ml => decimal.TryParse(ml.Fat, out decimal fat) ? fat : 0),
                    MealCount = g.Count()
                });

            return mealTypeNutrition;
        }
    }
}