using System.Threading.Tasks;
using MVCPrject.Data;
using MVCPrject.Models;
using Microsoft.EntityFrameworkCore;

namespace MVCPrject.Services
{
    public class MealLogService
    {
        private readonly DBContext _context;

        public MealLogService(DBContext context)
        {
            _context = context;
        }

        // Add a new meal log
        public async Task<bool> AddMealLogAsync(MealLog mealLog)
        {
            try
            {
                await _context.MealLogs.AddAsync(mealLog);
                await _context.SaveChangesAsync();
                return true;
            }
            catch
            {
                // Log exception (optional)
                return false;
            }
        }

        // Get all meal logs for a specific user
        public async Task<List<MealLog>> GetMealLogsByUserAsync(string userId)
        {
            return await _context.MealLogs
                .Where(m => m.UserID == userId)
                .OrderByDescending(m => m.MealDate)
                .ToListAsync();
        }

        // Get meal logs for a specific date
        public async Task<List<MealLog>> GetMealLogsByDateAsync(DateTime date)
        {
            return await _context.MealLogs
                .Include(m => m.Recipe)
                .Where(m => m.MealDate.Date == date.Date)
                .OrderBy(m => m.MealTime)
                .ToListAsync();
        }

        // Get meal logs for a specific date and user
        public async Task<List<MealLog>> GetMealLogsByDateAndUserAsync(DateTime date, string userId)
        {
            return await _context.MealLogs
                .Include(m => m.Recipe)
                .Where(m => m.MealDate.Date == date.Date && m.UserID == userId)
                .OrderBy(m => m.MealTime)
                .ToListAsync();
        }

        // Get all meal logs (Admin/Global Read)
        public async Task<List<MealLog>> ReadMealLogsAsync()
        {
            return await _context.MealLogs
                .Include(m => m.User) // Optional: Include navigation properties if needed
                .Include(m => m.Recipe)
                .OrderByDescending(m => m.MealDate)
                .ToListAsync();
        }

        // Get a single meal log by ID
        public async Task<MealLog?> GetMealLogByIdAsync(int logId)
        {
            return await _context.MealLogs
                .Include(m => m.Recipe)
                .FirstOrDefaultAsync(m => m.MealLogID == logId);
        }

        // Update an existing meal log
        public async Task<bool> UpdateMealLogAsync(MealLog updatedMealLog)
        {
            try
            {
                var existingMealLog = await _context.MealLogs.FindAsync(updatedMealLog.MealLogID);
                if (existingMealLog == null) return false;

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
                existingMealLog.RecipeID = updatedMealLog.RecipeID;
                existingMealLog.IsPlanned = updatedMealLog.IsPlanned;

                await _context.SaveChangesAsync();
                return true;
            }
            catch
            {
                // Log exception (optional)
                return false;
            }
        }

        // Delete a meal log
        public async Task<bool> DeleteMealLogAsync(int logId)
        {
            try
            {
                var mealLog = await _context.MealLogs.FindAsync(logId);
                if (mealLog == null) return false;

                _context.MealLogs.Remove(mealLog);
                await _context.SaveChangesAsync();
                return true;
            }
            catch
            {
                // Log exception (optional)
                return false;
            }
        }
    }
}
