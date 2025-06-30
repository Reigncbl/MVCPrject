document.addEventListener("DOMContentLoaded", function () {
    feather.replace();

    const getWeekday = (date) => {
        return date.toLocaleDateString('en-US', { weekday: 'short' });
    };

    const renderDateSection = () => {
    const today = new Date();
    const currentDateEl = document.getElementById("current-date");
    const dateCardsContainer = document.getElementById("date-cards");

    currentDateEl.textContent = today.toLocaleDateString('en-US', {
        month: 'long',
        day: 'numeric'
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
            const mealCount = Math.floor(Math.random() * 5);

            const card = document.createElement("div");
            card.className = `day-card bg-white rounded-3 text-center px-3 py-2 shadow-sm ${isSelected ? "today-card border border-warning" : ""}`;

            card.innerHTML = `
                <small class="text-black fw-normal">${weekday}</small>
                <h5 class="m-0 fw-bold">${day}</h5>
                <small class="text-black fw-normal">${mealCount} meal${mealCount !== 1 ? 's' : ''}</small>
            `;
            dateCardsContainer.appendChild(card);
        }
    };

    // Initialize Flatpickr
    flatpickr("#calendarWrapper", {
        wrap: true,
        allowInput: true,
        position: "left", // or just "auto"
        onChange: function(selectedDates, dateStr) {
            console.log("Picked:", dateStr);
        }
    });

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
                <span class="add-btn" style="cursor:pointer;" onclick="openMealModal('${meal.id}')">+</span>
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

    renderDateSection();

    // Define globally so inline onclick works
    window.openMealModal = function(mealId) {
        const modal = new bootstrap.Modal(document.getElementById('mealModal'));
        document.getElementById('mealType').value = mealId;

        const meal = meals.find(m => m.id === mealId);
        
        // Split label into main text and parenthesis part
        const match = meal.label.match(/^(.*?)(\s*\(.*\))?$/);
        const mainLabel = match ? match[1].trim() : meal.label;
        const parenthesisPart = match ? match[2] : '';
        
        // Set modal title with styled HTML
        document.getElementById('mealModalLabel').innerHTML = `Add ${mainLabel}<span class="fw-normal small">${parenthesisPart}</span>`;

        modal.show();

        // Reset and clear form when modal is hidden or exited
        const mealModalEl = document.getElementById('mealModal');

        mealModalEl.addEventListener('hidden.bs.modal', function () {
            // Clear form fields
            document.getElementById('mealForm').reset();

            // Hide image preview
            const preview = document.getElementById('mealPhotoPreview');
            if (preview) {
                preview.src = '';
                preview.classList.add('d-none');
            }

            // Show upload label again
            const uploadLabel = document.getElementById('uploadLabel');
            if (uploadLabel) {
                uploadLabel.style.display = '';
            }
        });

    };

    // MODAL FUNCTIONALITY
    const submitText = document.getElementById("submitText");
    const modeButtons = document.querySelectorAll('.mode-btn');
    
    let currentMode = "planned";
    
    // Load the single form (same content for both modes)
    function loadForm() {
        const formContent = document.getElementById("formContent");
        formContent.innerHTML = `
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
    document.getElementById('mealForm').addEventListener('submit', function (e) {
      e.preventDefault();

      const form = e.target;
      const formData = new FormData(form);

      const mealId = formData.get('mealType');
      const mealName = formData.get('mealName');
      const calories = parseInt(formData.get('calories'), 10) || 0;
      const protein = parseInt(formData.get('protein'), 10) || 0;
      const mealTime = formData.get('mealTime') || '';
      const formattedTime = formatTimeTo12Hour(mealTime);
      const mealDate = formData.get('mealDate') || '';
      const photoInput = document.getElementById('mealPhoto');
      const photoFile = photoInput.files[0];
      const photoURL = photoFile ? URL.createObjectURL(photoFile) : '';

      const mode = currentMode; // "planned" or "logged"

      logMeal(mealId, {
          name: mealName,
          calories,
          protein,
          time: formattedTime,  // not mealTime
          date: mealDate,
          photo: photoURL,
          mode
      });

      form.reset();

      const preview = document.getElementById('mealPhotoPreview');
      if (preview) {
          preview.src = '';
          preview.classList.add('d-none');
      }

      const uploadLabel = document.getElementById('uploadLabel');
      if (uploadLabel) {
          uploadLabel.style.display = '';
      }

      const modal = bootstrap.Modal.getInstance(document.getElementById('mealModal'));
      if (modal) modal.hide();
  });


});

// Recipe search variables
let searchTimeout;
const SEARCH_DELAY = 300; // milliseconds

// Recipe search functionality
function setupRecipeSearch() {
    const searchInput = document.getElementById('recipeSearch');
    const dropdown = document.getElementById('recipeDropdown');
    
    if (!searchInput || !dropdown) return;

    searchInput.addEventListener('input', function() {
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
    document.addEventListener('click', function(e) {
        if (!searchInput.contains(e.target) && !dropdown.contains(e.target)) {
            hideDropdown();
        }
    });

    // Handle keyboard navigation
    searchInput.addEventListener('keydown', function(e) {
        const items = dropdown.querySelectorAll('.dropdown-item:not(.dropdown-item-text)');
        const activeItem = dropdown.querySelector('.dropdown-item.active');
        let currentIndex = Array.from(items).indexOf(activeItem);

        switch(e.key) {
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
        const response = await fetch(`/Recipe/Search?query=${encodeURIComponent(query)}`);
        const data = await response.json();
        
        if (data.success) {
            displayRecipeResults(data.recipes);
        } else {
            showSearchError('Failed to search recipes');
        }
    } catch (error) {
        console.error('Recipe search error:', error);
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
        item.addEventListener('click', function() {
            const recipe = JSON.parse(this.dataset.recipe);
            selectRecipe(recipe);
        });

        // Add hover effect
        item.addEventListener('mouseenter', function() {
            dropdown.querySelectorAll('.dropdown-item').forEach(i => i.classList.remove('active'));
            this.classList.add('active');
        });
    });

    dropdown.style.display = 'block';
}

function selectRecipe(recipe) {
    // Fill in the form fields with recipe data
    document.getElementById('mealName').value = recipe.name;
    
    // Handle nutrition values - convert strings to numbers if needed
    const calories = recipe.calories ? (typeof recipe.calories === 'string' ? parseInt(recipe.calories) : recipe.calories) : '';
    const protein = recipe.protein ? (typeof recipe.protein === 'string' ? parseInt(recipe.protein) : recipe.protein) : '';
    const carbs = recipe.carbs ? (typeof recipe.carbs === 'string' ? parseInt(recipe.carbs) : recipe.carbs) : '';
    const fat = recipe.fat ? (typeof recipe.fat === 'string' ? parseInt(recipe.fat) : recipe.fat) : '';
    
    document.getElementById('calories').value = calories;
    document.getElementById('protein').value = protein;
    document.getElementById('carbs').value = carbs;
    document.getElementById('fat').value = fat;
    
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

// For generate grocery list btn
function generateGroceryList() {
    const modalEl = document.getElementById('groceryListModal');
    if (modalEl) {
        const modal = new bootstrap.Modal(modalEl);
        modal.show();
        feather.replace();
    } else {
        console.error('Modal not found.');
    }
}

function closeGroceryModal() {
    const modalEl = document.getElementById('groceryListModal');
    const modal = bootstrap.Modal.getInstance(modalEl);
    if (modal) {
        modal.hide();
    }
}

function printGroceryList() {
  // Clone the modal content
  const printContent = document.getElementById('groceryListModal').cloneNode(true);
  
  // Create a hidden iframe for printing
  const printFrame = document.createElement('iframe');
  printFrame.style.position = 'absolute';
  printFrame.style.width = '0';
  printFrame.style.height = '0';
  printFrame.style.border = 'none';
  
  // When iframe loads, print its content
  printFrame.onload = function() {
    const printDocument = printFrame.contentWindow.document;
    
    // Add print-specific styles
    const style = printDocument.createElement('style');
    style.innerHTML = `
      @page { size: auto; margin: 5mm; }
      body { padding: 10px; font-family: Arial, sans-serif; }
      .list-group-item {
        display: flex;
        justify-content: space-between;
        padding: 8px 0;
        border-bottom: 1px solid #eee;
      }
      .badge {
        background-color: #007bff !important;
        color: white;
        padding: 3px 8px;
        border-radius: 10px;
        font-size: 12px;
      }
      h5 {
        color: #007bff;
        text-align: center;
        margin-bottom: 15px;
      }
    `;
    
    printDocument.head.appendChild(style);
    printDocument.body.appendChild(printContent.querySelector('.modal-content'));
    
    // Remove unnecessary elements for printing
    const modalFooter = printDocument.querySelector('.modal-footer');
    const modalHeader = printDocument.querySelector('.modal-header button');
    if (modalFooter) modalFooter.remove();
    if (modalHeader) modalHeader.remove();
    
    // Focus and print
    printFrame.contentWindow.focus();
    printFrame.contentWindow.print();
    
    // Clean up
    setTimeout(() => document.body.removeChild(printFrame), 1000);
  };
  
  document.body.appendChild(printFrame);
}



// To update dropdown btn text
function updateDateButton(selectedOption) {
    document.querySelector('#dateDropdown').textContent = selectedOption;
}

// For set goal btn
document.getElementById('setGoalBtn').addEventListener('click', function() {
    // Show the modal
    var modal = new bootstrap.Modal(document.getElementById('nutritionGoalModal'));
    modal.show();
});

// Log Meal BTN on Calendar

function openLogMealForm() {
    const modal = new bootstrap.Modal(document.getElementById('mealModal'));
    
    // Optional: Set mealType hidden input to empty or default
    document.getElementById('mealType').value = '';

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

    // Optionally hide the toggle buttons (if still shown)
    document.querySelectorAll('.mode-btn').forEach(btn => btn.classList.add('d-none'));

    // Set submit button text to "Log Meal"
    const submitText = document.getElementById("submitText");
    if (submitText) submitText.textContent = "Log Meal";

    modal.show();
}

// Function to log a meal
const loggedMeals = {};

function logMeal(mealId, mealData) {
    if (!loggedMeals[mealId]) {
        loggedMeals[mealId] = [];
    }

    loggedMeals[mealId].push(mealData);
    updateMealSummary(mealId);
}

// Function to update the UI
function updateMealSummary(mealId) {
    const mealLogs = loggedMeals[mealId] || [];
    const totalCalories = mealLogs.reduce((sum, log) => sum + log.calories, 0);
    const totalProtein = mealLogs.reduce((sum, log) => sum + log.protein, 0);

    const card = document.getElementById(`${mealId}-card`);
    const summaryEl = card.querySelector('.m-3');
    summaryEl.innerHTML = `
        <span class="md-3 text-muted">${totalCalories} Cal</span>
        <span class="md-3 text-muted"> | </span>
        <span class="md-3 text-muted">${totalProtein}g Protein</span>
    `;

    const listEl = document.getElementById(`${mealId}-list`);
    listEl.innerHTML = mealLogs.map(log => `
        <div class="card shadow-sm mb-2">
            <div class="card-body log-entry position-relative">
                <div class="row align-items-center">
                    <!-- Image -->
                    <div class="col-auto">
                        <img src="${log.photo || 'https://via.placeholder.com/50'}" class="rounded" style="width:50px; height:50px; object-fit:cover;" alt="Meal Image">
                    </div>

                    <!-- Content -->
                    <div class="col">
                        <h6 class="mb-1 fw-bold">${log.name}</h6>
                        <div class="text-muted small">
                            ${log.calories} cal <span class="mx-2">|</span>
                            ${log.time || 'Time N/A'}
                        </div>
                    </div>
                </div>

                <!-- Top-right: Badge -->
                <span class="badge position-absolute top-0 end-0 mt-2 me-2 p-2 custom-badge ${log.mode}">
                  ${log.mode.charAt(0).toUpperCase() + log.mode.slice(1)}
                </span>

                <!-- Bottom-right: Action Icons -->
                <div class="action-buttons position-absolute bottom-0 end-0 mb-2 me-2 d-flex gap-2">
                  <i data-feather="edit-2" style="cursor: pointer;"></i>
                  <i data-feather="trash-2" style="cursor: pointer;"></i>
                </div>

            </div>
        </div>
    `).join('');
    feather.replace(); // Re-apply Feather icons to new content
}

