
// Global state management for dashboard
const dashboardState = {
    nutritionData: null,
    lastUpdateTime: null,
    isLoading: false,
    mealLogs: {},
    nutritionGoals: {
        calories: 0,
        protein: 0,
        carbs: 0,
        fat: 0
    }
};

// Enhanced logging system similar to MealPlanner
function logDashboard(message, data = null) {
    const timestamp = new Date().toISOString().split('T')[1].split('.')[0];
    if (data) {
        console.log(`🏠 Dashboard [${timestamp}]: ${message}`, data);
    } else {
        console.log(`🏠 Dashboard [${timestamp}]: ${message}`);
    }
}

// Calculate nutrition totals with detailed logging
function calculateNutritionTotals() {
    logDashboard('Starting nutrition totals calculation');
    
    const totals = {
        calories: 0,
        protein: 0,
        carbs: 0,
        fat: 0
    };
    
    let totalMealsProcessed = 0;
    let loggedMealsCount = 0;
    let plannedMealsCount = 0;
    
    // Sum up nutrition values from all meal types, but only for logged meals
    ['almusal', 'tanghalian', 'meryenda', 'hapunan'].forEach(mealType => {
        if (dashboardState.mealLogs[mealType]) {
            logDashboard(`Processing ${dashboardState.mealLogs[mealType].length} meals for ${mealType}`);
            
            dashboardState.mealLogs[mealType].forEach(meal => {
                totalMealsProcessed++;
                
                // Only count logged meals, not planned ones
                if (meal.mode === 'logged') {
                    loggedMealsCount++;
                    totals.calories += meal.calories || 0;
                    totals.protein += meal.protein || 0;
                    totals.carbs += meal.carbs || 0;
                    totals.fat += meal.fat || 0;
                    
                    logDashboard(`Added logged meal: ${meal.name}`, {
                        calories: meal.calories,
                        protein: meal.protein,
                        carbs: meal.carbs,
                        fat: meal.fat
                    });
                } else {
                    plannedMealsCount++;
                    logDashboard(`Skipped planned meal: ${meal.name}`);
                }
            });
        }
    });
    
    logDashboard('Nutrition calculation completed', {
        totals,
        totalMealsProcessed,
        loggedMealsCount,
        plannedMealsCount
    });
    
    return totals;
}

// Enhanced data fetching with retry logic
async function fetchNutritionDataWithRetry(maxRetries = 3) {
    for (let attempt = 1; attempt <= maxRetries; attempt++) {
        try {
            logDashboard(`Fetching nutrition data (attempt ${attempt}/${maxRetries})`);
            
            const response = await fetch('/Nutrition/GetNutritionSummary');
            logDashboard(`API response status: ${response.status}`);
            
            if (!response.ok) {
                throw new Error(`HTTP ${response.status}: ${response.statusText}`);
            }
            
            const result = await response.json();
            logDashboard('API response received', result);
            
            return result;
        } catch (error) {
            logDashboard(`Attempt ${attempt} failed`, error.message);
            
            if (attempt === maxRetries) {
                throw error;
            }
            
            // Wait before retry (exponential backoff)
            const delay = Math.pow(2, attempt) * 1000;
            logDashboard(`Retrying in ${delay}ms...`);
            await new Promise(resolve => setTimeout(resolve, delay));
        }
    }
}

// Real-time meal data fetching
async function fetchTodaysMealData() {
    try {
        const today = new Date().toISOString().split('T')[0];
        logDashboard(`Fetching meal data for today: ${today}`);
        
        const response = await fetch(`/MealPlanner/GetMealLogsByDate?date=${today}`);
        logDashboard(`Meal data API response status: ${response.status}`);
        
        if (!response.ok) {
            throw new Error(`Failed to fetch meal data: ${response.status}`);
        }
        
        const result = await response.json();
        logDashboard('Meal data API response', result);
        
        if (result.success && result.mealLogs) {
            // Process meal logs similar to MealPlanner
            dashboardState.mealLogs = {};
            
            result.mealLogs.forEach(mealLog => {
                const mealType = mealLog.mealType || 'almusal';
                logDashboard(`Processing meal log: ${mealLog.mealName} for type: ${mealType}`);
                
                if (!dashboardState.mealLogs[mealType]) {
                    dashboardState.mealLogs[mealType] = [];
                }
                
                dashboardState.mealLogs[mealType].push({
                    id: mealLog.mealLogID,
                    name: mealLog.mealName,
                    calories: parseInt(mealLog.calories) || 0,
                    protein: parseInt(mealLog.protein) || 0,
                    carbs: parseInt(mealLog.carbohydrates) || 0,
                    fat: parseInt(mealLog.fat) || 0,
                    mode: mealLog.isPlanned ? 'planned' : 'logged',
                    date: mealLog.mealDate
                });
            });
            
            logDashboard('Meal logs processed successfully', dashboardState.mealLogs);
            return true;
        }
        
        logDashboard('No meal logs found for today');
        return false;
    } catch (error) {
        logDashboard('Error fetching meal data', error.message);
        return false;
    }
}

// Calculate progress percentages
function calculateProgressPercentages(totals, goals) {
    logDashboard('Calculating progress percentages', { totals, goals });
    
    const percentages = {};
    
    Object.keys(totals).forEach(nutrient => {
        const actual = totals[nutrient] || 0;
        const goal = goals[nutrient] || 0;
        
        if (goal > 0) {
            percentages[nutrient] = Math.min(100, Math.round((actual / goal) * 100));
        } else {
            percentages[nutrient] = 0;
        }
        
        logDashboard(`${nutrient}: ${actual}/${goal} = ${percentages[nutrient]}%`);
    });
    
    return percentages;
}
// Helper function to create dashboard cards
function createDashboardCards(nutritionData) {
    console.log('🍽️ Dashboard: createDashboardCards called with data:', nutritionData);
    console.log('🍽️ Dashboard: nutritionData type:', typeof nutritionData);
    console.log('🍽️ Dashboard: nutritionData length:', nutritionData?.length);
    
    // Check if nutritionData is valid
    if (!nutritionData || !Array.isArray(nutritionData)) {
        console.error('🍽️ Dashboard: Invalid nutritionData - not an array:', nutritionData);
        return;
    }
    
    if (nutritionData.length === 0) {
        console.warn('🍽️ Dashboard: nutritionData array is empty');
        return;
    }
    
    const container = document.getElementById("dashboard-cards");
    if (!container) {
        console.error('🍽️ Dashboard: Container "dashboard-cards" not found! Check your HTML.');
        return;
    }

    // Clear existing cards
    container.innerHTML = '';
    console.log('🍽️ Dashboard: Container cleared, creating cards...');

    // Create cards with the data
    nutritionData.forEach((item, index) => {
        console.log(`🍽️ Dashboard: Creating card ${index + 1}:`, {
            label: item.label,
            value: item.value,
            percent: item.percent
        });
        
        const col = document.createElement("div");
        col.className = "col-6 col-md-3";

        col.innerHTML = `
            <div class="rounded shadow-sm py-3 px-3 text-center" style="background-color: #C2CCFFAB;">
                <div class="fw-bold fs-4">${item.value}</div>
                <div class="text-muted">${item.label}</div>
                <div class="progress" style="height: 10px; border-radius: 1rem; background-color: white;">
                    <div class="progress-bar" role="progressbar"
                        style="width: ${item.percent}%; background-color: #2C358A; border-radius: 1rem;">
                    </div>
                </div>
            </div>
        `;

        container.appendChild(col);
        console.log(`🍽️ Dashboard: Card ${index + 1} added to container`);
    });
    
    console.log('🍽️ Dashboard: All cards created. Container now has', container.children.length, 'children');
}

async function initializeDashboardNutritionCards() {
    try {
        logDashboard('Starting dashboard initialization');
        dashboardState.isLoading = true;
        dashboardState.lastUpdateTime = new Date();
        
        // Check if container exists first
        const container = document.getElementById("dashboard-cards");
        if (!container) {
            logDashboard('ERROR: Container "dashboard-cards" not found! Check your HTML.');
            return;
        }
        
        logDashboard('Container found, proceeding with data fetching');
        
        // Step 1: Fetch nutrition goals from database
        logDashboard('Step 1: Fetching nutrition goals from database');
        const nutritionResult = await fetchNutritionDataWithRetry();
        
        if (nutritionResult.success && nutritionResult.data) {
            dashboardState.nutritionGoals = {
                calories: nutritionResult.data.calories || 0,
                protein: nutritionResult.data.proteins || 0,
                carbs: nutritionResult.data.carbs || 0,
                fat: nutritionResult.data.fats || 0
            };
            logDashboard('Found nutrition goals in database', dashboardState.nutritionGoals);
        } else {
            logDashboard('No nutrition goals found in database, using defaults');
        }
        
        // Step 2: Fetch today's meal data
        logDashboard('Step 2: Fetching today\'s meal data');
        const mealDataFetched = await fetchTodaysMealData();
        
        // Step 3: Calculate actual nutrition totals from logged meals
        logDashboard('Step 3: Calculating nutrition totals from logged meals');
        const actualTotals = calculateNutritionTotals();
        
        // Step 4: Calculate progress percentages
        logDashboard('Step 4: Calculating progress percentages');
        const percentages = calculateProgressPercentages(actualTotals, dashboardState.nutritionGoals);
        
        // Step 5: Prepare nutrition data for display
        logDashboard('Step 5: Preparing nutrition data for display');
        const nutritionData = [
            { 
                label: 'Calories', 
                value: dashboardState.nutritionGoals.calories || 0, 
                percent: percentages.calories || 0,
                actual: actualTotals.calories || 0
            },
            { 
                label: 'Protein', 
                value: dashboardState.nutritionGoals.protein || 0, 
                percent: percentages.protein || 0,
                actual: actualTotals.protein || 0
            },
            { 
                label: 'Carbohydrates', 
                value: dashboardState.nutritionGoals.carbs || 0, 
                percent: percentages.carbs || 0,
                actual: actualTotals.carbs || 0
            },
            { 
                label: 'Fats', 
                value: dashboardState.nutritionGoals.fat || 0, 
                percent: percentages.fat || 0,
                actual: actualTotals.fat || 0
                
            }
        ];
        
        logDashboard('Final nutrition data prepared', nutritionData);
        
        // Step 6: Update dashboard state and create cards
        logDashboard('Step 6: Creating dashboard cards');
        dashboardState.nutritionData = nutritionData;
        createDashboardCards(nutritionData);
        
        logDashboard('Dashboard initialization completed successfully', {
            mealDataFetched,
            totalMeals: Object.values(dashboardState.mealLogs).reduce((sum, meals) => sum + meals.length, 0),
            actualTotals,
            percentages
        });
        
    } catch (error) {
        logDashboard('ERROR during dashboard initialization', error.message);
        
        // Fallback to default values on error
        const nutritionData = [
            { label: 'Calories', value: 0, percent: 0, actual: 0 },
            { label: 'Protein', value: 0, percent: 0, actual: 0 },
            { label: 'Fats', value: 0, percent: 0, actual: 0 },
            { label: 'Carbohydrates', value: 0, percent: 0, actual: 0 }
        ];
        
        createDashboardCards(nutritionData);
        logDashboard('Dashboard initialized with fallback values');
    } finally {
        dashboardState.isLoading = false;
        logDashboard('Dashboard loading state cleared');
    }
}

// Function to refresh dashboard cards after updates
async function refreshDashboardNutritionCards() {
    logDashboard('Refreshing nutrition cards...');
    
    if (dashboardState.isLoading) {
        logDashboard('Dashboard is already loading, skipping refresh');
        return;
    }
    
    await initializeDashboardNutritionCards();
}

// Real-time update function for cross-page communication
window.updateDashboardFromMealPlanner = async function() {
    logDashboard('Received update request from MealPlanner');
    await refreshDashboardNutritionCards();
};

// Auto-refresh functionality
let autoRefreshInterval = null;

function startAutoRefresh(intervalMinutes = 5) {
    if (autoRefreshInterval) {
        clearInterval(autoRefreshInterval);
    }
    
    const intervalMs = intervalMinutes * 60 * 1000;
    logDashboard(`Starting auto-refresh every ${intervalMinutes} minutes`);
    
    autoRefreshInterval = setInterval(async () => {
        logDashboard('Auto-refresh triggered');
        await refreshDashboardNutritionCards();
    }, intervalMs);
}

function stopAutoRefresh() {
    if (autoRefreshInterval) {
        clearInterval(autoRefreshInterval);
        autoRefreshInterval = null;
        logDashboard('Auto-refresh stopped');
    }
}

// Enhanced card creation with more detailed information
function createEnhancedDashboardCards(nutritionData) {
    logDashboard('Creating enhanced dashboard cards', nutritionData);
    
    const container = document.getElementById("dashboard-cards");
    if (!container) {
        logDashboard('ERROR: Container not found for enhanced cards');
        return;
    }

    // Clear existing cards
    container.innerHTML = '';
    logDashboard('Container cleared, creating enhanced cards...');

    // Create cards with enhanced data
    nutritionData.forEach((item, index) => {
        logDashboard(`Creating enhanced card ${index + 1}:`, {
            label: item.label,
            value: item.value,
            actual: item.actual,
            percent: item.percent
        });
        
        const col = document.createElement("div");
        col.className = "col-6 col-md-3";



        col.innerHTML = `
            <div class="rounded shadow-sm py-3 px-3 text-center position-relative" style="background-color: #C2CCFFAB;">
                <div class="fw-bold fs-4">${['Protein', 'Carbohydrates', 'Fats'].includes(item.label) ? `${item.value}g` : item.value}</div>
                <div class="text-muted">${item.label}</div>
                <div class="progress" style="height: 10px; border-radius: 1rem; background-color: white;">
                    <div class="progress-bar" role="progressbar"
                        style="width: ${item.percent}%; background: linear-gradient(90deg, #4299e1 0%, #2b6cb0 100%);"
                    </div>
                </div>
            </div>
        `;

        container.appendChild(col);
        logDashboard(`Enhanced card ${index + 1} added to container`);
    });
    
    logDashboard('All enhanced cards created. Container now has', container.children.length, 'children');
}

// Update the main createDashboardCards function to use enhanced version
function createDashboardCards(nutritionData) {
    // Use enhanced version if actual data is available
    if (nutritionData.some(item => item.actual !== undefined)) {
        createEnhancedDashboardCards(nutritionData);
        return;
    }
    
    // Fallback to original version
    logDashboard('createDashboardCards called with data:', nutritionData);
    logDashboard('nutritionData type:', typeof nutritionData);
    logDashboard('nutritionData length:', nutritionData?.length);
    
    // Check if nutritionData is valid
    if (!nutritionData || !Array.isArray(nutritionData)) {
        logDashboard('ERROR: Invalid nutritionData - not an array:', nutritionData);
        return;
    }
    
    if (nutritionData.length === 0) {
        logDashboard('WARNING: nutritionData array is empty');
        return;
    }
    
    const container = document.getElementById("dashboard-cards");
    if (!container) {
        logDashboard('ERROR: Container "dashboard-cards" not found! Check your HTML.');
        return;
    }

    // Clear existing cards
    container.innerHTML = '';
    logDashboard('Container cleared, creating cards...');

    // Create cards with the data
    nutritionData.forEach((item, index) => {
        logDashboard(`Creating card ${index + 1}:`, {
            label: item.label,
            value: item.value,
            percent: item.percent
        });
        
        const col = document.createElement("div");
        col.className = "col-6 col-md-3";

        col.innerHTML = `
        <div class="rounded shadow-sm py-3 px-3 text-center" style="background-color: #C2CCFFAB;">
            <div class="fw-bold fs-4">${['Protein', 'Carbohydrates', 'Fats'].includes(item.label) ? `${item.value}g` : item.value}</div>
            <div class="text-muted">${item.label}</div>
            <div class="progress" style="height: 10px; border-radius: 1rem; background-color: white;">
                <div class="progress-bar" role="progressbar"
                    style="width: ${item.percent}%; background-color: #2C358A; border-radius: 1rem;">
                </div>
            </div>
        </div>
    `;

        container.appendChild(col);
        logDashboard(`Card ${index + 1} added to container`);
    });
    
    logDashboard('All cards created. Container now has', container.children.length, 'children');
}

// Dashboard state monitoring
function getDashboardState() {
    return {
        ...dashboardState,
        timestamp: new Date().toISOString()
    };
}

function logDashboardState() {
    logDashboard('Current dashboard state', getDashboardState());
}

// Wait for DOM to be ready before initializing
document.addEventListener('DOMContentLoaded', function() {
    console.log('🍽️ Dashboard: DOM loaded, waiting for container...');
    
    // Double-check that container exists
    const container = document.getElementById("dashboard-cards");
    if (container) {
        console.log('🍽️ Dashboard: Container found, initializing cards');
        initializeDashboardNutritionCards();
    } else {
        console.log('🍽️ Dashboard: Container not found, waiting...');
        // Wait a bit more and try again
        setTimeout(() => {
            initializeDashboardNutritionCards();
        }, 500);
    }
});

// Alternative fallback - use window load event
window.addEventListener('load', function() {
    console.log('🍽️ Dashboard: Window loaded');
    
    // Only initialize if not already done
    const container = document.getElementById("dashboard-cards");
    if (container && container.children.length === 0) {
        console.log('🍽️ Dashboard: Cards not loaded yet, initializing...');
        initializeDashboardNutritionCards();
    }
});

document.addEventListener("DOMContentLoaded", function () {
    const suggestions = [
        {
            title: "Mediterranean Quinoa Bowl",
            description: "A protein-packed dish featuring nutty quinoa, roasted vegetables, and feta cheese, drizzled with tangy lemon-herb dressing. Perfect for meal prep!"
        },
        {
            title: "Classic Chicken Parmesan",
            description: "Crispy breaded chicken cutlets topped with marinara sauce and melted mozzarella. Serve with pasta for an Italian comfort food favorite."
        },
        {
            title: "Thai Coconut Curry Soup",
            description: "A fragrant soup with coconut milk, lemongrass, and your choice of protein. Customize with your favorite vegetables for a warming meal."
        },
        {
            title: "Honey Garlic Salmon",
            description: "Quick pan-seared salmon glazed with a sweet and savory honey garlic sauce. Pairs perfectly with steamed vegetables and rice."
        }
    ];

    const container = document.getElementById("suggested-cards");
    
    // Add defensive check to prevent appendChild error
    if (!container) {
        console.error('🍽️ Dashboard: Container "suggested-cards" not found! Check your HTML.');
        return;
    }

    suggestions.forEach(item => {
        const col = document.createElement("div");
        col.className = "col-12 col-md-3";

        col.innerHTML = `
            <div class="card h-100 shadow-sm">
                <div class="position-absolute bg-danger text-white px-2 py-1 rounded-pill small"
                    style="top:8px; left:8px;">AI-Curated</div>
                <div style="background:#e1e1e1;height:110px;" class="rounded-top"></div>
                <div class="card-body">
                    <div class="fw-bold">${item.title}</div>
                    <p class="card-text small text-muted">${item.description}</p>
                </div>
            </div>
        `;

        container.appendChild(col);
    });
});

document.addEventListener("DOMContentLoaded", function () {
    const tips = [
        {
            title: "Knife Care",
            description: "Never put good knives in the dishwasher. Hand wash and dry immediately to maintain the edge and prevent damage.",
            icon: "knife"
        },
        {
            title: "Revive Greens",
            description: "Revive wilted greens by soaking them in ice water for 10 minutes before using.",
            icon: "vegetable"
        },
        {
            title: "Seasoning Heights",
            description: "Season from a height when cooking for more even distribution of salt and spices.",
            icon: "salt"
        },
        {
            title: "Pan Temperature",
            description: "Test if a pan is hot enough by sprinkling a few drops of water—they should dance and sizzle.",
            icon: "pan"
        }
    ];

    const container = document.getElementById("cooking-tips");
    
    // Add defensive check to prevent appendChild error
    if (!container) {
        console.error('🍽️ Dashboard: Container "cooking-tips" not found! Check your HTML.');
        return;
    }

    tips.forEach(tip => {
        const col = document.createElement("div");
        col.className = "col-12 col-md-3";

        col.innerHTML = `
            <div class="card shadow-sm rounded-2 h-100">
                <div class="d-flex align-items-center">
                    <div class="me-3 d-flex align-items-center justify-content-center "
                        style="width:100px;height:100px;background-color:#D9D9D9;">
                        <img src="/img/${tip.icon}.png" alt="${tip.title}" style="width: 40px; height: 40px;"></img>
                    </div>
                    <div class="flex-grow-1">
                        <div class="fw-bold text-dark">${tip.title}</div>
                    </div>
                </div>
                <div class="mt-2 small text-muted p-3">
                    ${tip.description}
                </div>
            </div>
        `;

        container.appendChild(col);
    });
});

if (typeof feather !== 'undefined') {
    feather.replace();
}