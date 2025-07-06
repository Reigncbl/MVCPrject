// Function to get meal count for a specific date
async function getMealCountForDate(date) {
    try {
        const dateString = date.toISOString().split('T')[0]; // Format as YYYY-MM-DD
        console.log('🍽️ MealPlanner.js: Getting meal count for date:', dateString);

        const response = await fetch(`/MealPlanner/GetMealLogsByDate?date=${dateString}`);

        if (!response.ok) {
            console.error('🍽️ MealPlanner.js: Failed to fetch meal logs for date:', dateString);
            return 0;
        }

        const result = await response.json();

        if (result.success && result.mealLogs) {
            const mealCount = result.mealLogs.length;
            console.log('🍽️ MealPlanner.js: Found', mealCount, 'meals for date:', dateString);
            return mealCount;
        } else {
            console.log('🍽️ MealPlanner.js: No meal logs found for date:', dateString);
            return 0;
        }
    } catch (error) {
        console.error('🍽️ MealPlanner.js: Error getting meal count for date:', error);
        return 0;
    }
}

document.addEventListener("DOMContentLoaded", async function () {
    console.log('🍽️ MealPlanner.js: DOM Content Loaded - Initializing meal planner');
    feather.replace();

    const getWeekday = (date) => {
        return date.toLocaleDateString('en-US', { weekday: 'short' });
    };


    const renderDateSection = async () => {
        const today = new Date();
        const currentDateEl = document.getElementById("current-date");
        const dateCardsContainer = document.getElementById("date-cards");

        currentDateEl.textContent = today.toLocaleDateString('en-US', {
            month: 'long'
        });

        // Clear existing cards
        dateCardsContainer.innerHTML = '';

        // Determine how many cards to show based on available width
        const getCardRange = () => {
            const containerWidth = dateCardsContainer.offsetWidth || window.innerWidth;

            // Adjust these breakpoints based on your card width + margins
            if (containerWidth < 400) return { start: 0, end: 0 };     // Show only today
            if (containerWidth < 600) return { start: -1, end: 1 };   // Show 3 cards (yesterday, today, tomorrow)
            if (containerWidth < 800) return { start: -2, end: 2 };   // Show 5 cards
            return { start: -3, end: 3 };                             // Show all 7 cards
        };

        const { start, end } = getCardRange();

        for (let i = start; i <= end; i++) {
            const date = new Date();
            date.setDate(today.getDate() + i);

            const isSelected = i === 0;
            const weekday = getWeekday(date);
            const day = date.getDate();
            //const mealCount = Math.floor(Math.random() * 5);
            const mealCount = await getMealCountForDate(date);

            const card = document.createElement("div");
            card.className = `day-card bg-white rounded-3 text-center px-3 py-2 shadow-sm ${isSelected ? "today-card border border-warning" : ""}`;
            card.dataset.date = date.toISOString().split('T')[0]; // Add data-date attribute
            card.style.cursor = 'pointer';

            card.innerHTML = `
                <small class="text-black fw-normal">${weekday}</small>
                <h5 class="m-0 fw-bold">${day}</h5>
                <small class="text-black fw-normal">${mealCount} meal${mealCount !== 1 ? 's' : ''}</small>
            `;

            // Add click event to load meals for selected date
            card.addEventListener('click', function () {
                // Remove previous selection
                document.querySelectorAll('.day-card').forEach(c => {
                    c.classList.remove('today-card', 'border', 'border-warning');
                });

                // Add selection to clicked card
                this.classList.add('today-card', 'border', 'border-warning');

                // Load meals for selected date
                console.log('🍽️ MealPlanner.js: Date card clicked, loading meals for:', date.toISOString().split('T')[0]);
                loadMealLogsForDate(date);

                // Update current date display
                document.getElementById("current-date").textContent = date.toLocaleDateString('en-US', {
                    month: 'long',
                    day: 'numeric'
                });
            });

            dateCardsContainer.appendChild(card);
        }
    };

    feather.replace();


    // MEAL LOGS

    const meals = [
        { id: 'almusal', label: 'Almusal (Breakfast)' },
        { id: 'tanghalian', label: 'Tanghalian (Lunch)' },
        { id: 'meryenda', label: 'Meryenda (Snack)' },
        { id: 'hapunan', label: 'Hapunan (Dinner)' }
    ];

    const row = document.getElementById('meal-row');

    meals.forEach(meal => {
        // Split label into main text and parenthesis part
        const match = meal.label.match(/^(.*?)(\s*\(.*\))?$/);
        const mainLabel = match ? match[1].trim() : meal.label;
        const parenthesisPart = match ? match[2] : '';

        const html = `
        <div class="col-md-6 mb-4">
            <div class="card meal-card" id="${meal.id}-card">
              <div class="meal-header">
                  <div>
                      <span class="fw-bold meal-label">${mainLabel}</span>
                      <span class="fw-normal ms-1 meal-label fs-6">${parenthesisPart}</span>
                  </div>
                <span class="add-btn" style="cursor:pointer;" onclick="openMealModal('${meal.id}')">
                  <i data-feather="plus" class="add-meal-btn"></i>
                </span>
              </div>
              <div class="p-3" id="${meal.id}-list">
                  <p class="text-muted">No logged meals yet.</p>
              </div>
              <div class="m-3">
                  <span class="md-3 text-muted">0 Cal</span>
                  <span class="md-3 text-muted"> | </span>
                  <span class="md-3 text-muted">0g Protein</span>
              </div>
            </div>
        </div>
        `;
        row.insertAdjacentHTML('beforeend', html);
    });

    console.log('🍽️ MealPlanner.js: Rendering date section and loading today\'s meals');
    await renderDateSection();

    // Load meal logs for today
    const today = new Date();
    console.log('🍽️ MealPlanner.js: Loading meal logs for today:', today.toISOString().split('T')[0]);
    loadMealLogsForDate(today);

    // Define globally so inline onclick works
    window.openMealModal = function (mealId) {
        console.log('🍽️ MealPlanner.js: Opening meal modal for meal type:', mealId);

        const modal = new bootstrap.Modal(document.getElementById('mealModal'));

        // Set the mealType based on the onclick parameter, default to meryenda if not provided
        const selectedMealType = mealId || 'meryenda';
        document.getElementById('mealType').value = selectedMealType;
        console.log('🍽️ MealPlanner.js: Set mealType to:', selectedMealType);

        const meal = meals.find(m => m.id === selectedMealType);

        if (meal) {
            // Split label into main text and parenthesis part
            const match = meal.label.match(/^(.*?)(\s*\(.*\))?$/);
            const mainLabel = match ? match[1].trim() : meal.label;
            const parenthesisPart = match ? match[2] : '';

            // Set modal title with styled HTML
            document.getElementById('mealModalLabel').innerHTML = `Add ${mainLabel}<span class="fw-normal small">${parenthesisPart}</span>`;
            console.log('🍽️ MealPlanner.js: Modal opened for:', mainLabel);
        } else {
            // Fallback if meal type not found
            document.getElementById('mealModalLabel').innerHTML = `Add Meal`;
            console.log('🍽️ MealPlanner.js: Modal opened with default title');
        }

        modal.show();

        // Reset and clear form when modal is hidden or exited
        const mealModalEl = document.getElementById('mealModal');

        // Only add event listener if not already added
        if (!mealModalEl.hasAttribute('data-modal-listener-added')) {
            mealModalEl.addEventListener('hidden.bs.modal', function () {
                // Clear form fields
                document.getElementById('mealForm').reset();

                // Clear hidden fields explicitly
                document.getElementById('mealType').value = 'meryenda';
                document.getElementById('recipeID').value = '';

                // Hide image preview and reset styling
                const preview = document.getElementById('mealPhotoPreview');
                if (preview) {
                    preview.src = '';
                    preview.classList.add('d-none');
                    preview.style.border = '';
                    preview.title = '';
                }

                // Show upload label again
                const uploadLabel = document.getElementById('uploadLabel');
                if (uploadLabel) {
                    uploadLabel.style.display = '';
                }

                console.log('🍽️ MealPlanner.js: Modal closed, form reset, recipe ID cleared');
            });

            // Mark that listener has been added
            mealModalEl.setAttribute('data-modal-listener-added', 'true');
        }
    };

    // MODAL FUNCTIONALITY
    const submitText = document.getElementById("submitText");
    const modeButtons = document.querySelectorAll('.mode-btn');

    let currentMode = "planned";

    // Load the single form (same content for both modes)
    function loadForm() {
        const formContent = document.getElementById("formContent");
        formContent.innerHTML = `
            <!-- Hidden input for meal type -->
            <input type="hidden" name="mealType" id="mealType" value="meryenda">
            <!-- Hidden input for recipe ID -->
            <input type="hidden" name="recipeID" id="recipeID" value="">
            
            <div class="mb-4">
              <div class="row g-3 align-items-stretch">
                <!-- Left: Photo Upload -->
                <div class="col-md-6 d-flex flex-column">
                  <label for="mealPhoto" class="form-label">Photo</label>
                  <div class="position-relative meal-photo-box text-center">
                    <input type="file" name="mealPhoto" id="mealPhoto" class="form-control d-none" accept="image/*">
                    <label for="mealPhoto" class="stretched-link d-flex flex-column justify-content-center align-items-center h-100 w-100" id="uploadLabel">
                      <i data-feather="upload" class="text-muted mb-2"></i>
                      <div class="text-muted small">Click to upload</div>
                    </label>
                    <img id="mealPhotoPreview" src="" alt="Preview"
                        class="img-fluid rounded d-none position-absolute top-0 start-0 w-100 h-100 object-fit-cover" />
                  </div>
                </div>

                <!-- Right: Form Inputs -->
                <div class="col-md-6 d-flex flex-column justify-content-between">
                  <div class="d-flex flex-column flex-grow-1">
                    <!-- Date, Time, Meal -->
                    <div class="mt-1 mb-2">
                      <label for="mealDate" class="form-label mb-1">Date</label>
                      <input type="date" name="mealDate" id="mealDate" class="form-control" required>
                    </div>
                    <div class="mb-2">
                      <label for="mealTime" class="form-label mb-1">Time</label>
                      <input type="time" name="mealTime" id="mealTime" class="form-control" required>
                    </div>
                    <div class="mb-3">
                      <label for="mealName" class="form-label mb-1">Meal</label>
                      <input type="text" name="mealName" id="mealName" class="form-control" placeholder="e.g. Chicken Adobo" required>
                    </div>                   
                  </div>
                </div>

              </div>
            </div>

            <!-- Macronutrients: Calories, Protein, Carbs, Fat -->
            <div class="row mb-4">
              <div class="col">
                <label for="calories" class="form-label">Calories</label>
                <input type="number" name="calories" id="calories" class="form-control" required>
              </div>
              <div class="col">
                <label for="protein" class="form-label">Protein (g)</label>
                <input type="number" name="protein" id="protein" class="form-control form-control-sm">
              </div>
              <div class="col">
                <label for="carbs" class="form-label">Carbs (g)</label>
                <input type="number" name="carbs" id="carbs" class="form-control form-control-sm">
              </div>
              <div class="col">
                <label for="fat" class="form-label">Fat (g)</label>
                <input type="number" name="fat" id="fat" class="form-control form-control-sm">
              </div>
            </div>

            <div class="text-center my-2">
                <label class="form-label fw-normal">OR</label>
            </div>

            <div class="mb-3 position-relative">
                <label for="recipeSearch" class="form-label">Find a Recipe</label>
                <input type="text" name="recipeSearch" id="recipeSearch" class="form-control" placeholder="Search for a recipe..." autocomplete="off">
                <div id="recipeDropdown" class="dropdown-menu w-100" style="max-height: 200px; overflow-y: auto; display: none;"></div>
            </div>
        `;

        // Set up preview AFTER form content is injected
        const fileInput = document.getElementById('mealPhoto');
        const preview = document.getElementById('mealPhotoPreview');
        const uploadLabel = document.getElementById('uploadLabel');

        fileInput.addEventListener('change', function (event) {
            const file = event.target.files[0];

            if (file) {
                const reader = new FileReader();
                reader.onload = function (e) {
                    preview.src = e.target.result;
                    preview.classList.remove('d-none');

                    // Hide the upload label
                    uploadLabel.style.display = 'none';
                };
                reader.readAsDataURL(file);
            } else {
                preview.src = '';
                preview.classList.add('d-none');

                // Show the label again if user clears file
                uploadLabel.style.display = '';
            }
        });

        // Set up recipe search functionality
        setupRecipeSearch();
    }

    // Update submit button text based on mode
    function updateSubmitButton() {
        submitText.textContent = currentMode === "planned" ? "Plan Meal" : "Log Meal";
    }

    // Load the form initially
    loadForm();
    updateSubmitButton();

    // Mode toggle functionality (just changes the button states and submit text)
    modeButtons.forEach(btn => {
        btn.addEventListener('click', () => {
            const mode = btn.dataset.mode;
            if (mode !== currentMode) {
                currentMode = mode;

                // Update button states
                modeButtons.forEach(b => b.classList.remove('active'));
                btn.classList.add('active');

                // Only update submit button text, form content stays the same
                updateSubmitButton();
            }
        });
    });

    // Handle form submission
    document.getElementById('mealForm').addEventListener('submit', async function (e) {
        e.preventDefault();
        console.log('🍽️ MealPlanner.js: Form submission started');

        const form = e.target;
        const formData = new FormData(form);

        const mealId = formData.get('mealType');
        const mealName = formData.get('mealName');
        const calories = parseInt(formData.get('calories'), 10) || 0;
        const protein = parseInt(formData.get('protein'), 10) || 0;
        const carbs = parseInt(formData.get('carbs'), 10) || 0;
        const fat = parseInt(formData.get('fat'), 10) || 0;
        const mealTime = formData.get('mealTime') || '';
        const formattedTime = formatTimeTo12Hour(mealTime);
        const mealDate = formData.get('mealDate') || '';
        const recipeID = formData.get('recipeID') || null;
        const photoInput = document.getElementById('mealPhoto');
        const photoFile = photoInput.files[0];
        const photoURL = photoFile ? URL.createObjectURL(photoFile) : '';

        const mode = currentMode; // "planned" or "logged"

        console.log('🍽️ MealPlanner.js: Form data collected:', {
            mealId, mealName, calories, protein, carbs, fat, mealTime, mealDate, recipeID, mode
        });

        // Additional validation and logging for mealType
        if (!mealId || mealId.trim() === '') {
            console.error('🍽️ MealPlanner.js: No meal type specified! mealId:', mealId);

            // Try to get from the input directly as fallback
            const mealTypeInput = document.getElementById('mealType');
            const fallbackMealId = mealTypeInput?.value || 'meryenda';
            console.log('🍽️ MealPlanner.js: Using fallback mealType:', fallbackMealId);

            if (!fallbackMealId || fallbackMealId.trim() === '') {
                showNotification('Please select a meal type', 'error');
                return;
            }

            // Use the fallback value
            const correctedMealId = fallbackMealId;
            console.log('🍽️ MealPlanner.js: Corrected mealId to:', correctedMealId);
        }

        const finalMealId = mealId || 'meryenda';
        console.log('🍽️ MealPlanner.js: Final mealId for API:', finalMealId);

        // Create meal log object for API
        const mealLogData = {
            mealType: finalMealId,
            mealName: mealName,
            mealDate: mealDate,
            mealTime: mealTime,
            calories: calories.toString(),
            protein: protein.toString(),
            carbohydrates: carbs.toString(),
            fat: fat.toString(),
            isPlanned: mode === "planned",
            recipeID: recipeID ? parseInt(recipeID) : null
        };

        console.log('🍽️ MealPlanner.js: Sending meal log data to API:', mealLogData);

        try {
            let response;
            let result;

            // Check if there's a photo file to upload
            if (photoFile) {
                console.log('🍽️ MealPlanner.js: Photo detected, using file upload endpoint');
                console.log('🍽️ MealPlanner.js: Photo file details:', {
                    name: photoFile.name,
                    size: photoFile.size,
                    type: photoFile.type
                });

                // Create FormData for file upload
                const formData = new FormData();
                formData.append('mealType', finalMealId);
                formData.append('mealName', mealName);
                formData.append('mealDate', mealDate);
                formData.append('mealTime', mealTime);
                formData.append('calories', calories.toString());
                formData.append('protein', protein.toString());
                formData.append('carbohydrates', carbs.toString());
                formData.append('fat', fat.toString());
                formData.append('isPlanned', mode === "planned");
                if (recipeID) formData.append('recipeID', recipeID);
                formData.append('mealPhoto', photoFile);

                // Send to file upload endpoint
                response = await fetch('/MealPlanner/CreateMealLogWithPhoto', {
                    method: 'POST',
                    body: formData // No Content-Type header for FormData
                });
            } else {
                console.log('🍽️ MealPlanner.js: No photo, using JSON endpoint');

                // Send to regular JSON endpoint
                response = await fetch('/MealPlanner/CreateMealLog', {
                    method: 'POST',
                    headers: {
                        'Content-Type': 'application/json',
                    },
                    body: JSON.stringify(mealLogData)
                });
            }

            console.log('🍽️ MealPlanner.js: API response status:', response.status);

            result = await response.json();
            console.log('🍽️ MealPlanner.js: API response data:', result);

            if (result.success) {
                console.log('🍽️ MealPlanner.js: Meal logged successfully, updating UI');

                // Use the photo URL from the server response if available
                const finalPhotoURL = result.photoUrl || photoURL;

                // The meal was saved on the server, now refresh the UI from the server
                // to ensure consistency.
                
                // Refresh the meal logs for the date the meal was added to
                await loadMealLogsForDate(new Date(mealDate));

                // Show success message
                showNotification('Meal logged successfully!', 'success');
            } else {
                console.error('🍽️ MealPlanner.js: API returned error:', result.message);
                showNotification(result.message || 'Failed to log meal', 'error');
            }
        } catch (error) {
            console.error('🍽️ MealPlanner.js: Error logging meal:', error);
            showNotification('Error logging meal. Please try again.', 'error');
        }

        form.reset();

        // Clear hidden fields explicitly
        document.getElementById('mealType').value = 'meryenda';
        document.getElementById('recipeID').value = '';

        const preview = document.getElementById('mealPhotoPreview');
        if (preview) {
            preview.src = '';
            preview.classList.add('d-none');
            preview.style.border = '';
            preview.title = '';
        }

        const uploadLabel = document.getElementById('uploadLabel');
        if (uploadLabel) {
            uploadLabel.style.display = '';
        }

        console.log('🍽️ MealPlanner.js: Form submitted and reset, recipe ID cleared');

        const modal = bootstrap.Modal.getInstance(document.getElementById('mealModal'));
        if (modal) modal.hide();
    });

    // FIXED SET GOAL BUTTON EVENT LISTENER
    const setGoalBtn = document.getElementById('setGoalBtn');
    if (setGoalBtn) {
        setGoalBtn.addEventListener('click', function () {
            console.log('🍽️ MealPlanner.js: Set Goal button clicked');

            // Clean up any existing backdrops FIRST
            cleanupModalBackdrops();

            const modalElement = document.getElementById('nutritionGoalModal');
            if (!modalElement) {
                console.error('🍽️ MealPlanner.js: Nutrition modal element not found');
                return;
            }

            // Get or create modal instance (don't dispose existing ones)
            let modal = bootstrap.Modal.getInstance(modalElement);
            if (!modal) {
                console.log('🍽️ MealPlanner.js: Creating new modal instance');
                modal = new bootstrap.Modal(modalElement);
            } else {
                console.log('🍽️ MealPlanner.js: Using existing modal instance');
            }

            modal.show();
        });
    }

    // Save Goal Button Event Listener
    const saveGoalBtn = document.getElementById('saveGoalBtn');
    if (saveGoalBtn) {
        saveGoalBtn.addEventListener('click', function () {
            console.log('🍽️ MealPlanner.js: Save Goal button clicked');
            updateNutritionCardsFromGoal();
            closeNutritionModal();
        });
    }

    // SINGLE nutrition modal cleanup listener
    const nutritionModal = document.getElementById('nutritionGoalModal');
    if (nutritionModal && !nutritionModal.hasAttribute('data-nutrition-listener-added')) {
        nutritionModal.addEventListener('hidden.bs.modal', function () {
            console.log('🍽️ MealPlanner.js: Nutrition modal hidden, cleaning up');
            const form = document.getElementById('nutritionGoalForm');
            if (form) form.reset();

            // Cleanup after modal is hidden
            setTimeout(() => cleanupModalBackdrops(), 100);
        });

        nutritionModal.setAttribute('data-nutrition-listener-added', 'true');
    }
});

// Helper function to clean up modal backdrops
function cleanupModalBackdrops() {
    // Remove all modal backdrops
    const backdrops = document.querySelectorAll('.modal-backdrop');
    console.log('🍽️ MealPlanner.js: Cleaning up', backdrops.length, 'backdrop(s)');
    backdrops.forEach(backdrop => backdrop.remove());

    // Remove modal-open class and reset body styles
    document.body.classList.remove('modal-open');
    document.body.style.overflow = '';
    document.body.style.paddingRight = '';
    document.body.style.marginRight = '';
}

// Recipe search variables
let searchTimeout;
const SEARCH_DELAY = 300; // milliseconds

// Recipe search functionality
function setupRecipeSearch() {
    const searchInput = document.getElementById('recipeSearch');
    const dropdown = document.getElementById('recipeDropdown');

    if (!searchInput || !dropdown) return;

    searchInput.addEventListener('input', function () {
        const query = this.value.trim();

        // Clear previous timeout
        if (searchTimeout) {
            clearTimeout(searchTimeout);
        }

        if (query.length === 0) {
            hideDropdown();
            return;
        }

        // Add loading indicator
        dropdown.innerHTML = '<div class="dropdown-item-text text-muted"><i class="spinner-border spinner-border-sm me-2"></i>Searching...</div>';
        dropdown.style.display = 'block';

        // Debounce the search to avoid too many API calls
        searchTimeout = setTimeout(() => {
            searchRecipesFromAPI(query);
        }, SEARCH_DELAY);
    });

    // Hide dropdown when clicking outside
    document.addEventListener('click', function (e) {
        if (!searchInput.contains(e.target) && !dropdown.contains(e.target)) {
            hideDropdown();
        }
    });

    // Handle keyboard navigation
    searchInput.addEventListener('keydown', function (e) {
        const items = dropdown.querySelectorAll('.dropdown-item:not(.dropdown-item-text)');
        const activeItem = dropdown.querySelector('.dropdown-item.active');
        let currentIndex = Array.from(items).indexOf(activeItem);

        switch (e.key) {
            case 'ArrowDown':
                e.preventDefault();
                if (items.length > 0) {
                    currentIndex = currentIndex < items.length - 1 ? currentIndex + 1 : 0;
                    setActiveItem(items, currentIndex);
                }
                break;
            case 'ArrowUp':
                e.preventDefault();
                if (items.length > 0) {
                    currentIndex = currentIndex > 0 ? currentIndex - 1 : items.length - 1;
                    setActiveItem(items, currentIndex);
                }
                break;
            case 'Enter':
                e.preventDefault();
                if (activeItem) {
                    activeItem.click();
                }
                break;
            case 'Escape':
                hideDropdown();
                break;
        }
    });
}

// Search recipes from API
async function searchRecipesFromAPI(query) {
    try {
        console.log('🍽️ MealPlanner.js: Searching recipes for query:', query);
        const response = await fetch(`/Recipe/Search?query=${encodeURIComponent(query)}`);
        console.log('🍽️ MealPlanner.js: Recipe search API response status:', response.status);

        const data = await response.json();
        console.log('🍽️ MealPlanner.js: Recipe search API response data:', data);

        if (data.success) {
            console.log('🍽️ MealPlanner.js: Found', data.recipes?.length || 0, 'recipes');
            displayRecipeResults(data.recipes);
        } else {
            console.error('🍽️ MealPlanner.js: Recipe search failed:', data.message);
            showSearchError('Failed to search recipes');
        }
    } catch (error) {
        console.error('🍽️ MealPlanner.js: Recipe search error:', error);
        showSearchError('Error searching recipes');
    }
}

function showSearchError(message) {
    const dropdown = document.getElementById('recipeDropdown');
    dropdown.innerHTML = `<div class="dropdown-item-text text-danger">${message}</div>`;
    dropdown.style.display = 'block';
}

function displayRecipeResults(recipes) {
    const dropdown = document.getElementById('recipeDropdown');

    if (recipes.length === 0) {
        dropdown.innerHTML = '<div class="dropdown-item-text text-muted">No recipes found</div>';
        dropdown.style.display = 'block';
        return;
    }

    dropdown.innerHTML = recipes.map(recipe => {
        const calories = recipe.calories || 'N/A';
        const protein = recipe.protein || 'N/A';
        const description = recipe.description || recipe.type || 'No description available';
        const defaultImage = 'https://via.placeholder.com/40x40/e9ecef/6c757d?text=🍽️';
        const recipeImage = recipe.image || defaultImage;

        return `
            <div class="dropdown-item recipe-item" data-recipe='${JSON.stringify(recipe)}' style="cursor: pointer;">
                <div class="d-flex align-items-start">
                    <!-- Recipe Image -->
                    <div class="me-3">
                        <img src="${recipeImage}" 
                             alt="${recipe.name}" 
                             class="rounded" 
                             style="width: 40px; height: 40px; object-fit: cover;"
                             onerror="this.src='${defaultImage}'">
                    </div>
                    
                    <!-- Recipe Details -->
                    <div class="flex-grow-1">
                        <div class="fw-bold">${recipe.name}</div>
                        <small class="text-muted">${description}</small>
                        ${recipe.author ? `<br><small class="text-muted">by ${recipe.author}</small>` : ''}
                    </div>
                    
                    <!-- Nutrition Info -->
                    <div class="text-end ms-2">
                        <small class="text-primary">${calories} cal</small>
                        <br>
                        <small class="text-muted">${protein}g protein</small>
                        ${recipe.cookTime ? `<br><small class="text-muted">${recipe.cookTime} min</small>` : ''}
                    </div>
                </div>
            </div>
        `;
    }).join('');

    // Add click handlers to recipe items
    dropdown.querySelectorAll('.recipe-item').forEach(item => {
        item.addEventListener('click', function () {
            const recipe = JSON.parse(this.dataset.recipe);
            selectRecipe(recipe);
        });

        // Add hover effect
        item.addEventListener('mouseenter', function () {
            dropdown.querySelectorAll('.dropdown-item').forEach(i => i.classList.remove('active'));
            this.classList.add('active');
        });
    });

    dropdown.style.display = 'block';
}

function selectRecipe(recipe) {
    console.log('🍽️ MealPlanner.js: Recipe selected:', recipe);

    // Fill in the form fields with recipe data
    document.getElementById('mealName').value = recipe.name;

    // Store the recipe ID in the hidden field
    const recipeIdField = document.getElementById('recipeID');
    if (recipeIdField && recipe.recipeID) {
        recipeIdField.value = recipe.recipeID;
        console.log('🍽️ MealPlanner.js: Set recipe ID:', recipe.recipeID);
    } else if (recipeIdField && recipe.id) {
        recipeIdField.value = recipe.id;
        console.log('🍽️ MealPlanner.js: Set recipe ID (from id field):', recipe.id);
    } else {
        console.log('🍽️ MealPlanner.js: No recipe ID found in recipe object:', recipe);
    }

    // Handle nutrition values - convert strings to numbers if needed
    const calories = recipe.calories ? (typeof recipe.calories === 'string' ? parseInt(recipe.calories) : recipe.calories) : '';
    const protein = recipe.protein ? (typeof recipe.protein === 'string' ? parseInt(recipe.protein) : recipe.protein) : '';
    const carbs = recipe.carbs ? (typeof recipe.carbs === 'string' ? parseInt(recipe.carbs) : recipe.carbs) : '';
    const fat = recipe.fat ? (typeof recipe.fat === 'string' ? parseInt(recipe.fat) : recipe.fat) : '';

    document.getElementById('calories').value = calories;
    document.getElementById('protein').value = protein;
    document.getElementById('carbs').value = carbs;
    document.getElementById('fat').value = fat;

    // Show recipe image preview if available
    if (recipe.image) {
        const preview = document.getElementById('mealPhotoPreview');
        const uploadLabel = document.getElementById('uploadLabel');

        if (preview && uploadLabel) {
            preview.src = recipe.image;
            preview.classList.remove('d-none');
            uploadLabel.style.display = 'none';

            console.log('🍽️ MealPlanner.js: Showing recipe image preview:', recipe.image);

            // Add a visual indicator that this is from a recipe
            preview.style.border = '2px solid #007bff';
            preview.title = 'Recipe Image - will be used automatically';
        }
    }

    // Update search input to show selected recipe
    document.getElementById('recipeSearch').value = recipe.name;

    // Hide dropdown
    hideDropdown();

    // Optional: Show a success message or highlight the filled fields
    showRecipeSelectedFeedback();
}

function hideDropdown() {
    const dropdown = document.getElementById('recipeDropdown');
    if (dropdown) {
        dropdown.style.display = 'none';
    }
}

function setActiveItem(items, index) {
    items.forEach(item => item.classList.remove('active'));
    if (items[index]) {
        items[index].classList.add('active');
    }
}

function showRecipeSelectedFeedback() {
    const searchInput = document.getElementById('recipeSearch');
    if (searchInput) {
        searchInput.classList.add('is-valid');
        setTimeout(() => {
            searchInput.classList.remove('is-valid');
        }, 2000);
    }
}

// OUTSIDE the DOMContentLoaded event listener

// Function to format time from 24-hour to 12-hour format
// Example: "14:30" -> "2:30 PM"
function formatTimeTo12Hour(timeStr) {
    if (!timeStr) return '';

    const [hour, minute] = timeStr.split(':').map(Number);
    const ampm = hour >= 12 ? 'PM' : 'AM';
    const hour12 = hour % 12 || 12;

    return `${hour12}:${minute.toString().padStart(2, '0')} ${ampm}`;
}

// Function to format TimeSpan from API to 12-hour format
// Example: "14:30:00" -> "2:30 PM"
function formatTimeSpanTo12Hour(timeSpan) {
    if (!timeSpan) return '';

    // TimeSpan from API might be in format "HH:MM:SS" or just "HH:MM"
    const timeParts = timeSpan.split(':');
    if (timeParts.length < 2) return '';

    const hour = parseInt(timeParts[0]);
    const minute = parseInt(timeParts[1]);
    const ampm = hour >= 12 ? 'PM' : 'AM';
    const hour12 = hour % 12 || 12;

    return `${hour12}:${minute.toString().padStart(2, '0')} ${ampm}`;
}

// For generate grocery list btn
function generateGroceryList() {
    const modalEl = document.getElementById('groceryListModal');
    const selectedDateCard = document.querySelector('.day-card.today-card'); // Find the selected card

    if (!modalEl || !selectedDateCard) {
        console.error('Modal or selected date card not found.');
        return;
    }

    const anchorDateString = selectedDateCard.dataset.date; // Get the selected date
    modalEl.dataset.anchorDate = anchorDateString; // Store it on the modal

    // Use event delegation for dropdown items
    if (!modalEl.hasAttribute('data-delegation-attached')) {
        // Log when the dropdown button is clicked
        const dropdownBtn = document.getElementById('dateRangeDropdown');
        if (dropdownBtn) {
            dropdownBtn.addEventListener('click', function(e) {
                console.log('🍽️ MealPlanner.js: Dropdown button clicked');
                const dropdownMenu = modalEl.querySelector('.dropdown-menu');
                if (dropdownMenu) {
                    // Toggle visibility
                    if (dropdownMenu.style.display === 'block') {
                        dropdownMenu.style.display = 'none';
                        console.log('🍽️ MealPlanner.js: Dropdown menu hidden');
                    } else {
                        dropdownMenu.style.display = 'block';
                        console.log('🍽️ MealPlanner.js: Dropdown menu shown');
                    }
                }
            });
        }
        modalEl.addEventListener('click', function(e) {
            const target = e.target;
            if (target && target.classList.contains('dropdown-item')) {
                e.preventDefault();
                console.log('🍽️ MealPlanner.js: Dropdown item clicked:', target.textContent, 'data-range:', target.dataset.range);
                const range = target.dataset.range;
                const anchorDate = new Date(modalEl.dataset.anchorDate + 'T00:00:00');
                let startDate, endDate;

                switch (range) {
                    case 'today':
                        startDate = anchorDate;
                        endDate = anchorDate;
                        break;
                    case 'next7':
                        startDate = anchorDate;
                        endDate = new Date(anchorDate);
                        endDate.setDate(anchorDate.getDate() + 6);
                        break;
                    case 'thisMonth':
                        startDate = new Date(anchorDate.getFullYear(), anchorDate.getMonth(), 1);
                        endDate = new Date(anchorDate.getFullYear(), anchorDate.getMonth() + 1, 0);
                        break;
                }

                const startDateString = startDate.toISOString().split('T')[0];
                const endDateString = endDate.toISOString().split('T')[0];

                document.getElementById('dateRangeDropdown').textContent = target.textContent;
                console.log('🍽️ MealPlanner.js: Fetching grocery list for range:', startDateString, 'to', endDateString);
                fetchGroceryList(startDateString, endDateString);
            }
        });
        modalEl.setAttribute('data-delegation-attached', 'true');
    }

    // Show the modal
    const modal = new bootstrap.Modal(modalEl);
    modal.show();
    feather.replace();
    console.log('🍽️ MealPlanner.js: Grocery list modal shown');

    // Default action: fetch for the selected date ("Today")
    document.getElementById('dateRangeDropdown').textContent = 'Today';
    console.log('🍽️ MealPlanner.js: Fetching grocery list for Today:', anchorDateString);
    fetchGroceryList(anchorDateString, anchorDateString);
}

function fetchGroceryList(startDateString, endDateString) {
    const modalEl = document.getElementById('groceryListModal');
    const spinner = modalEl.querySelector('#groceryListSpinner');
    const groceryItemsContainer = modalEl.querySelector('#groceryItems');

    spinner.style.display = 'block';
    groceryItemsContainer.style.display = 'none';
    groceryItemsContainer.innerHTML = '';

    fetch(`/api/GroceryList?startDate=${startDateString}&endDate=${endDateString}`)
        .then(response => {
            if (!response.ok) {
                throw new Error('Network response was not ok');
            }
            return response.json();
        })
        .then(data => {
            spinner.style.display = 'none';
            groceryItemsContainer.style.display = 'block';

            if (data.length === 0) {
                groceryItemsContainer.innerHTML = '<div class="list-group-item">No ingredients found for the selected dates.</div>';
            } else {
                data.forEach(recipe => {
                    const recipeName = recipe.recipeName || recipe.RecipeName;
                    const ingredients = recipe.ingredients || recipe.Ingredients;

                    if (recipeName && ingredients && ingredients.length > 0) {
                        const header = document.createElement('h6');
                        header.className = 'mt-3 mb-2 fw-bold';
                        header.textContent = recipeName;
                        groceryItemsContainer.appendChild(header);

                        ingredients.forEach(item => {
                            const unit = item.unit || item.Unit || '';
                            const name = item.ingredientName || item.IngredientName || '';
                            const quantity = item.quantity || item.Quantity || '';

                            const div = document.createElement('div');
                            div.className = 'list-group-item';
                            div.innerHTML = `<span>${quantity} ${unit} ${name}</span>`;
                            groceryItemsContainer.appendChild(div);
                        });
                    }
                });
            }
        })
        .catch(error => {
            spinner.style.display = 'none';
            groceryItemsContainer.style.display = 'block';
            groceryItemsContainer.innerHTML = '<div class="list-group-item text-danger">Failed to load grocery list.</div>';
            console.error('JS: Grocery list fetch error:', error);
        });
}

function closeGroceryModal() {
    const modalEl = document.getElementById('groceryListModal');
    const modal = bootstrap.Modal.getInstance(modalEl);
    if (modal) {
        modal.hide();
    }
}


// To update dropdown btn text
function updateDateButton(selectedOption) {
    document.querySelector('#dateDropdown').textContent = selectedOption;
}

// For set goal btn
let nutritionGoalModalInstance = null;
const nutritionGoalModalEl = document.getElementById('nutritionGoalModal');
if (nutritionGoalModalEl) {
    nutritionGoalModalInstance = new bootstrap.Modal(nutritionGoalModalEl);
    document.getElementById('setGoalBtn').addEventListener('click', function () {
        nutritionGoalModalInstance.show();
    });
}

document.getElementById('saveGoalBtn')?.addEventListener('click', function () {
    updateNutritionCardsFromGoal();
    closeNutritionModal();
});

// For save goal btn
function closeNutritionModal() {
    const modalElement = document.getElementById('nutritionGoalModal');
    if (!modalElement) return;
    if (nutritionGoalModalInstance) {
        nutritionGoalModalInstance.hide();
    } else {
        let modalInstance = bootstrap.Modal.getInstance(modalElement);
        if (!modalInstance) {
            modalInstance = new bootstrap.Modal(modalElement);
        }
        modalInstance.hide();
    }
}

// Function to update nutrition cards and send goals to controller
// Function specifically for page load - doesn't rely on form inputs
async function initializeNutritionCardsOnLoad() {
    try {
        console.log('🍽️ MealPlanner.js: Initializing nutrition cards on page load');
        
        // Try to get current values from database first
        const response = await fetch('/Nutrition/GetNutritionSummary');
        const result = await response.json();
        
        if (result.success && result.data) {
            // Found data in database - use it
            const currentValues = {
                calories: result.data.calories,
                protein: result.data.proteins,
                carbs: result.data.carbs,
                fat: result.data.fats
            };
            
            console.log('🍽️ MealPlanner.js: Found current values in database:', currentValues);
            
            // Update the cards with database values
            document.querySelectorAll('.nutrition-card').forEach(card => {
                const label = card.querySelector('.nutrition-label')?.textContent.trim().toLowerCase();
                if (currentValues[label] !== undefined) {
                    let value = currentValues[label];
                    if (["protein", "carbs", "fat"].includes(label)) {
                        value += "g";
                    }
                    card.querySelector('.nutrition-value').textContent = value;
                }
            });
            
            console.log('🍽️ MealPlanner.js: Nutrition cards initialized with database values');

            // Also update the global nutritionGoals object
            nutritionGoals.calories = result.data.calories || 0;
            nutritionGoals.protein = result.data.proteins || 0;
            nutritionGoals.carbs = result.data.carbs || 0;
            nutritionGoals.fat = result.data.fats || 0;

        } else {
            // No data in database - show default/placeholder values
            console.log('🍽️ MealPlanner.js: No data in database, showing default values');
            
            const defaultValues = {
                calories: 0,
                protein: 0,
                carbs: 0,
                fat: 0
            };
                        
            document.querySelectorAll('.nutrition-card').forEach(card => {
                const label = card.querySelector('.nutrition-label')?.textContent.trim().toLowerCase();
                if (defaultValues[label] !== undefined) {
                    let value = defaultValues[label];
                    if (["protein", "carbs", "fat"].includes(label)) {
                        value += "g";
                    }
                    card.querySelector('.nutrition-value').textContent = value;
                }
            });
            
            console.log('🍽️ MealPlanner.js: Nutrition cards initialized with default values');
        }
        
    } catch (error) {
        console.error('🍽️ MealPlanner.js: Error initializing nutrition cards:', error);
        
        // Fallback to default values on error
        const defaultValues = {
            calories: 0,
            protein: 0,
            carbs: 0,
            fat: 0
        };
        
        document.querySelectorAll('.nutrition-card').forEach(card => {
            const label = card.querySelector('.nutrition-label')?.textContent.trim().toLowerCase();
            if (defaultValues[label] !== undefined) {
                let value = defaultValues[label];
                if (["protein", "carbs", "fat"].includes(label)) {
                    value += "g";
                }
                card.querySelector('.nutrition-value').textContent = value;
            }
        });
        
        console.log('🍽️ MealPlanner.js: Nutrition cards initialized with default values (error fallback)');
    }
}

// Keep your existing updateNutritionCardsFromGoal function for when goals are updated
async function updateNutritionCardsFromGoal() {
    // Extract and sanitize values from modal inputs
    const caloriesGoal = parseInt(document.getElementById('caloriesGoal').value, 10) || 0;
    const proteinGoal  = parseInt(document.getElementById('proteinGoal').value, 10) || 0;
    const carbsGoal    = parseInt(document.getElementById('carbsGoal').value, 10) || 0;
    const fatGoal      = parseInt(document.getElementById('fatGoal').value, 10) || 0;
    
    const formData = new FormData();
    formData.append('calories', caloriesGoal);
    formData.append('proteins', proteinGoal);
    formData.append('carbs', carbsGoal);
    formData.append('fats', fatGoal);
    
    try {
        console.log('🍽️ MealPlanner.js: About to send request to /Nutrition/UpdateGoals');
        const payload = Object.fromEntries(formData);
        console.log('🍽️ MealPlanner.js: Request payload:', payload);
        
        const response = await fetch('/Nutrition/UpdateGoals', {
            method: 'POST',
            body: formData
        });

        console.log('🍽️ MealPlanner.js: Response status:', response.status);
        console.log('🍽️ MealPlanner.js: Response ok:', response.ok);

        if (!response.ok) {
            const errorText = await response.text();
            console.error('🍽️ MealPlanner.js: HTTP error!', response.status, errorText);
            showNotification(`HTTP Error: ${response.status}`, 'error');
            return;
        }

        const result = await response.json();
        console.log('🍽️ MealPlanner.js: Nutrition goals API response:', result);

        if (result.success) {
        console.log('🍽️ MealPlanner.js: Nutrition goals saved successfully');
        showNotification('Nutrition goals saved successfully!', 'success');
        
        // After saving, refresh the cards to show the latest data
        await initializeNutritionCardsOnLoad();
        
        // Also update dashboard cards if available (for when both pages are open)
        if (typeof window.updateDashboardFromMealPlanner === 'function') {
        console.log('🍽️ MealPlanner.js: Updating dashboard cards...');
        await window.updateDashboardFromMealPlanner();
        }
        } else {
            console.error('🍽️ MealPlanner.js: Failed to save nutrition goals:', result.message);
            showNotification(`Failed to save nutrition goals: ${result.message}`, 'error');
        }
    } catch (error) {
        console.error('🍽️ MealPlanner.js: Error saving nutrition goals:', error);
        showNotification('Error saving nutrition goals', 'error');
    }
}

// Usage in your onload event:
// Call initializeNutritionCardsOnLoad() instead of updateNutritionCardsFromGoal()
window.addEventListener('load', function() {
    initializeNutritionCardsOnLoad();
    // ... other onload code
});

// Log Meal BTN on Calendar
function openLogMealForm() {
    const modal = new bootstrap.Modal(document.getElementById('mealModal'));

    // Set mealType to default (meryenda/snack)
    document.getElementById('mealType').value = 'meryenda';
    console.log('🍽️ MealPlanner.js: openLogMealForm - Set mealType to default: meryenda');

    // Update the modal title
    document.getElementById('mealModalLabel').innerHTML = `Log Meal`;

    // Ensure form content is loaded (in case it hasn't been)
    const formContent = document.getElementById("formContent");
    if (!formContent.innerHTML.trim()) {
        loadForm(); // call your existing function
    }

    // Reset form
    document.getElementById('mealForm').reset();

    // Hide preview
    const preview = document.getElementById('mealPhotoPreview');
    if (preview) {
        preview.src = '';
        preview.classList.add('d-none');
    }

    const uploadLabel = document.getElementById('uploadLabel');
    if (uploadLabel) {
        uploadLabel.style.display = '';
    }

    // Set submit button text to "Log Meal"
    const submitText = document.getElementById("submitText");
    if (submitText) submitText.textContent = "Log Meal";

    modal.show();
}

// Function to update meal count on date cards
async function updateDateCardMealCount(dateString) {
    try {
        console.log('🍽️ MealPlanner.js: Updating meal count for date:', dateString);

        // Find the date card for this date
        const targetDate = new Date(dateString);
        const today = new Date();

        // Find the card by checking the date difference
        const dateCards = document.querySelectorAll('.day-card');
        dateCards.forEach(async (card, index) => {
            const cardDate = new Date();
            cardDate.setDate(today.getDate() + (index - Math.floor(dateCards.length / 2)));

            if (cardDate.toISOString().split('T')[0] === dateString) {
                const mealCount = await getMealCountForDate(targetDate);
                const mealCountElement = card.querySelector('small:last-child');
                if (mealCountElement) {
                    mealCountElement.textContent = `${mealCount} meal${mealCount !== 1 ? 's' : ''}`;
                    console.log('🍽️ MealPlanner.js: Updated meal count to', mealCount, 'for date:', dateString);
                }
            }
        });
    } catch (error) {
        console.error('🍽️ MealPlanner.js: Error updating date card meal count:', error);
    }
}

// Function to log a meal
const loggedMeals = {};

function logMeal(mealId, mealData) {
    if (!loggedMeals[mealId]) {
        loggedMeals[mealId] = [];
    }

    loggedMeals[mealId].push(mealData);
    updateMealSummary(mealId);

    // Update the meal count for the date card
    updateDateCardMealCount(mealData.date);

    }



// Function to update the UI
function updateMealSummary(mealId) {
    const mealLogs = loggedMeals[mealId] || [];
    const totalCalories = mealLogs.reduce((sum, log) => sum + log.calories, 0);
    const totalProtein = mealLogs.reduce((sum, log) => sum + log.protein, 0);

    console.log('🍽️ MealPlanner.js: Updating meal summary for', mealId, '- Meals:', mealLogs.length, 'Calories:', totalCalories, 'Protein:', totalProtein);

    const card = document.getElementById(`${mealId}-card`);
    const summaryEl = card.querySelector('.m-3');
    summaryEl.innerHTML = `
        <span class="md-3 text-muted">${totalCalories} Cal</span>
        <span class="md-3 text-muted"> | </span>
        <span class="md-3 text-muted">${totalProtein}g Protein</span>
    `;

    const listEl = document.getElementById(`${mealId}-list`);
    listEl.innerHTML = mealLogs.map(log => {
        const defaultImage = 'https://via.placeholder.com/50?text=🍽️';
        const imageUrl = log.photo || defaultImage;

        return `
        <div class="card shadow-sm mb-2">
            <div class="card-body log-entry position-relative">
                <div class="row align-items-center">
                    <!-- Image -->
                    <div class="col-auto">
                        <img src="${imageUrl}" 
                             class="rounded meal-photo" 
                             style="width:50px; height:50px; object-fit:cover; cursor: pointer;" 
                             alt="Meal Image"
                             onerror="this.src='${defaultImage}'"
                             onclick="showMealPhotoModal('${imageUrl}', '${log.name}')">
                    </div>

                    <!-- Content -->
                    <div class="col">
                        <h6 class="mb-1 fw-bold">${log.name}</h6>
                        <div class="text-muted small">
                            ${log.calories} cal <span class="mx-2">|</span>
                            ${log.time || 'Time N/A'}
                        </div>
                        ${log.recipeId ? `<div class="text-muted small"><i class="fas fa-book"></i> From Recipe</div>` : ''}
                    </div>
                </div>

                <!-- Top-right: Badge -->
                <span class="badge position-absolute top-0 end-0 mt-2 me-2 p-2 custom-badge ${log.mode}">
                  ${log.mode.charAt(0).toUpperCase() + log.mode.slice(1)}
                </span>

                <!-- Bottom-right: Action Icons -->
                <div class="action-buttons position-absolute bottom-0 end-0 mb-2 me-2 d-flex gap-2">
                  <i data-feather="edit-2" style="cursor: pointer;" onclick="editMealLog(${log.id || 0})"></i>
                  <i data-feather="trash-2" style="cursor: pointer;" onclick="deleteMealLog(${log.id || 0}, '${mealId}')"></i>
                </div>

            </div>
        </div>
        `;
    }).join('');
    feather.replace(); // Re-apply Feather icons to new content
}

// API Integration Functions

// Load meal logs for a specific date
async function loadMealLogsForDate(date) {
    try {
        const dateString = date.toISOString().split('T')[0]; // Format as YYYY-MM-DD
        console.log('🍽️ MealPlanner.js: Loading meal logs for date:', dateString);

        if (!nutritionGoals || Object.values(nutritionGoals).every(val => val === 0)) {
            console.log('🍽️ Initializing nutrition cards and goals...');
            await initializeNutritionCardsOnLoad();
        }

        const response = await fetch(`/MealPlanner/GetMealLogsByDate?date=${dateString}`);
        console.log('🍽️ MealPlanner.js: API response status for date load:', response.status);

        const result = await response.json();
        console.log('🍽️ MealPlanner.js: API response data for date load:', result);

        if (result.success && result.mealLogs) {
            console.log('🍽️ MealPlanner.js: Processing', result.mealLogs.length, 'meal logs');

            // Clear existing logged meals
            Object.keys(loggedMeals).forEach(key => {
                loggedMeals[key] = [];
            });

            // Process and display the loaded meal logs
            result.mealLogs.forEach(mealLog => {
                const mealType = mealLog.mealType || 'almusal'; // Default to breakfast if not specified
                console.log('🍽️ MealPlanner.js: Processing meal log:', mealLog.mealName, 'for type:', mealType);

                if (!loggedMeals[mealType]) {
                    loggedMeals[mealType] = [];
                }

                loggedMeals[mealType].push({
                    id: mealLog.mealLogID,
                    name: mealLog.mealName,
                    calories: parseInt(mealLog.calories) || 0,
                    protein: parseInt(mealLog.protein) || 0,
                    carbs: parseInt(mealLog.carbohydrates) || 0,
                    fat: parseInt(mealLog.fat) || 0,
                    time: formatTimeSpanTo12Hour(mealLog.mealTime),
                    date: mealLog.mealDate,
                    photo: mealLog.mealPhoto || 'https://via.placeholder.com/50?text=🍽️',
                    mode: mealLog.isPlanned ? 'planned' : 'logged',
                    recipeId: mealLog.recipeID
                });
            });

            // Update UI for all meal types
            ['almusal', 'tanghalian', 'meryenda', 'hapunan'].forEach(mealType => {
                updateMealSummary(mealType);
                console.log('🍽️ MealPlanner.js: Updated UI for meal type:', mealType, 'with', loggedMeals[mealType]?.length || 0, 'meals');
            });

            // Update the meal count for the current date card
            updateDateCardMealCount(dateString);

            // Update the nutrition progress bar
            updateNutritionProgressBar(date);
        } else {
            console.log('🍽️ MealPlanner.js: No meal logs found for date:', dateString);
            // Still update the progress bar to show 0 for the day
            updateNutritionProgressBar(date);
        }
    } catch (error) {
        console.error('🍽️ MealPlanner.js: Error loading meal logs:', error);
        showNotification('Error loading meal logs', 'error');
    }
}

// Edit meal log
async function editMealLog(mealLogId) {
    // For now, show a simple alert. You can implement a full edit modal later
    showNotification('Edit functionality coming soon!', 'info');
}

// Delete meal log
async function deleteMealLog(mealLogId, mealType) {
    console.log('🍽️ MealPlanner.js: Delete meal log requested for ID:', mealLogId, 'Type:', mealType);

    if (!confirm('Are you sure you want to delete this meal log?')) {
        console.log('🍽️ MealPlanner.js: Delete cancelled by user');
        return;
    }

    try {
        console.log('🍽️ MealPlanner.js: Sending delete request to API');
        const response = await fetch(`/MealPlanner/DeleteMealLog/${mealLogId}`, {
            method: 'DELETE'
        });

        console.log('🍽️ MealPlanner.js: Delete API response status:', response.status);
        const result = await response.json();
        console.log('🍽️ MealPlanner.js: Delete API response data:', result);

        if (result.success) {
            console.log('🍽️ MealPlanner.js: Meal deleted successfully, updating UI');

            // Remove from local storage and get the date for updating meal count
            let deletedMealDate = null;
            if (loggedMeals[mealType]) {
                const beforeCount = loggedMeals[mealType].length;
                const mealToDelete = loggedMeals[mealType].find(meal => meal.id === mealLogId);
                if (mealToDelete) {
                    deletedMealDate = mealToDelete.date;
                }
                loggedMeals[mealType] = loggedMeals[mealType].filter(meal => meal.id !== mealLogId);
                const afterCount = loggedMeals[mealType].length;
                console.log('🍽️ MealPlanner.js: Removed meal from local storage. Before:', beforeCount, 'After:', afterCount);
                updateMealSummary(mealType);

                // Update the meal count for the date card
                if (deletedMealDate) {
                    updateDateCardMealCount(deletedMealDate);
                }
            }

            showNotification('Meal deleted successfully!', 'success');
        } else {
            console.error('🍽️ MealPlanner.js: Delete API returned error:', result.message);
            showNotification(result.message || 'Failed to delete meal', 'error');
        }
    } catch (error) {
        console.error('🍽️ MealPlanner.js: Error deleting meal:', error);
        showNotification('Error deleting meal. Please try again.', 'error');
    }
}

// Show notification
function showNotification(message, type = 'info') {
    // Create notification element
    const notification = document.createElement('div');
    notification.className = `alert alert-${type === 'error' ? 'danger' : type === 'success' ? 'success' : 'info'} alert-dismissible fade show position-fixed`;
    notification.style.cssText = 'top: 20px; right: 20px; z-index: 9999; min-width: 300px;';
    notification.innerHTML = `
        ${message}
        <button type="button" class="btn-close" data-bs-dismiss="alert"></button>
    `;

    document.body.appendChild(notification);

    // Auto-remove after 5 seconds
    setTimeout(() => {
        if (notification.parentNode) {
            notification.parentNode.removeChild(notification);
        }
    }, 5000);
}

// Show meal photo in modal
function showMealPhotoModal(imageUrl, mealName) {
    console.log('🍽️ MealPlanner.js: Showing photo modal for:', mealName, 'URL:', imageUrl);

    // Check if it's a placeholder image
    if (imageUrl.includes('placeholder')) {
        showNotification('No photo available for this meal', 'info');
        return;
    }

    // Create modal HTML
    const modalHtml = `
        <div class="modal fade" id="mealPhotoModal" tabindex="-1" aria-labelledby="mealPhotoModalLabel" aria-hidden="true">
            <div class="modal-dialog modal-lg modal-dialog-centered">
                <div class="modal-content">
                    <div class="modal-header">
                        <h5 class="modal-title" id="mealPhotoModalLabel">${mealName}</h5>
                        <button type="button" class="btn-close" data-bs-dismiss="modal" aria-label="Close"></button>
                    </div>
                    <div class="modal-body text-center">
                        <img src="${imageUrl}" 
                             class="img-fluid rounded" 
                             alt="${mealName}"
                             style="max-height: 70vh; object-fit: contain;"
                             onerror="this.parentElement.innerHTML='<p class=\\"text-muted\\">Image could not be loaded</p>'">
                    </div>
                    <div class="modal-footer">
                        <button type="button" class="btn btn-secondary" data-bs-dismiss="modal">Close</button>
                        <a href="${imageUrl}" target="_blank" class="btn btn-primary">Open in New Tab</a>
                    </div>
                </div>
            </div>
        </div>
    `;

    // Remove existing modal if any
    const existingModal = document.getElementById('mealPhotoModal');
    if (existingModal) {
        existingModal.remove();
    }

    // Add modal to body
    document.body.insertAdjacentHTML('beforeend', modalHtml);

    // Show modal
    const modal = new bootstrap.Modal(document.getElementById('mealPhotoModal'));
    modal.show();

    // Clean up modal when hidden
    document.getElementById('mealPhotoModal').addEventListener('hidden.bs.modal', function () {
        this.remove();
    });
}
const nutritionGoals = {
    calories: 0,
    protein: 0,
    carbs: 0,
    fat: 0
};


// Function to calculate nutrition totals from logged meals only (not planned)
function calculateNutritionTotals() {
    const totals = {
        calories: 0,
        protein: 0,
        carbs: 0,
        fat: 0
    };
    
    // Sum up nutrition values from all meal types, but only for logged meals
    ['almusal', 'tanghalian', 'meryenda', 'hapunan'].forEach(mealType => {
        if (loggedMeals[mealType]) {
            loggedMeals[mealType].forEach(meal => {
                // Only count logged meals, not planned ones
                if (meal.mode === 'logged') {
                    totals.calories += meal.calories || 0;
                    totals.protein += meal.protein || 0;
                    totals.carbs += meal.carbs || 0;
                    totals.fat += meal.fat || 0;
                }
            });
        }
    });
    
    console.log('🍽️ Calculated nutrition totals (logged only):', totals);
    return totals;
}

async function updateNutritionProgressBar(date) {
    const dateString = date.toISOString().split('T')[0];
    console.log('🍽️ Updating nutrition progress for:', dateString);
    
    try {
        const goals = nutritionGoals;
        
        if (!goals || Object.values(goals).every(val => val === 0)) {
            console.warn('🍽️ Nutrition goals not initialized yet.');
            return null;
        }
        
        // Calculate totals from logged meals
        const totals = calculateNutritionTotals();
        
        // Store percentage values to return
        const percentages = {};
        
        // Update each card
        document.querySelectorAll('.nutrition-card').forEach(card => {
            const label = card.querySelector('.nutrition-label')?.textContent.trim().toLowerCase();
            const valueEl = card.querySelector('.nutrition-value');
            const barEl = card.querySelector('.progress-fill');
            
            if (label && goals[label] !== undefined && valueEl && barEl) {
                const actual = totals[label] || 0;
                const goal = goals[label];
                const percent = goal > 0 ? Math.min(100, Math.round((actual / goal) * 100)) : 0;
                
                // Store the percentage for return
                percentages[label] = percent;
                
                // Update progress bar
                barEl.style.width = `${percent}%`;
                barEl.style.backgroundColor = percent >= 100 ? '#28a745' : '#ffc107';
                barEl.title = `${actual} / ${goal} ${label === 'calories' ? 'Cal' : 'g'}`;
            }
        });
        
        
    } catch (error) {
        console.error('❌ Error updating nutrition progress:', error);
        return null;
    }
}
