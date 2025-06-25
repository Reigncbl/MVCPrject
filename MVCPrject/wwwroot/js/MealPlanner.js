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

    // Re-render when window is resized (including split screen scenarios)
    let resizeTimeout;
    window.addEventListener('resize', () => {
        clearTimeout(resizeTimeout);
        resizeTimeout = setTimeout(renderDateSection, 100); // Debounce for performance
    });

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
                    <span class="fw-normal ms-1 meal-label">${parenthesisPart}</span>
                </div>
            <span class="add-btn" style="cursor:pointer;" onclick="openMealModal('${meal.id}')">+</span>
            </div>
            <div class="p-3" id="${meal.id}-list">
                <p class="text-muted">No logged meals yet.</p>
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
        document.getElementById('mealModalLabel').textContent = `Log ${meal.label}`;

        modal.show();
    };

    // MODAL FUNCTIONALITY
  const formContent = document.getElementById("formContent");
  const submitText = document.getElementById("submitText");
  // const submitBtn = document.getElementById("submitBtn");
  const modeButtons = document.querySelectorAll('.mode-btn');
  
  let currentMode = "planned";
  
  function loadForm(mode) {
    if (mode === "planned") {
      submitText.textContent = "Plan Meal";
      formContent.innerHTML = `
        <div class="mb-3">
            <div class="d-flex gap-2">
                <div class="flex-fill">
                    <label for="plannedDate" class="form-label">Date</label>
                    <input type="date" name="plannedDate" id="plannedDate" class="form-control" required>
                </div>
                <div class="flex-fill">
                    <label for="plannedTime" class="form-label">Time</label>
                    <input type="time" name="plannedTime" id="plannedTime" class="form-control" required>
                </div>
            </div>
        </div>

        <div class="mb-3">
          <label for="mealName" class="form-label">What meal do you have in mind?</label>
          <input type="text" name="mealName" id="mealName" class="form-control" placeholder="e.g. Chicken Adobo with Rice" required>
        </div>

        <div class="row">
          <div class="col-3 mb-3">
            <label for="calories" class="form-label">Calories</label>
            <input type="number" name="calories" id="calories" class="form-control" required>
          </div>
          <div class="col-3 mb-3">
            <label for="protein" class="form-label">Protein (g)</label>
            <input type="number" name="protein" id="protein" class="form-control">
          </div>
          <div class="col-3 mb-3">
            <label for="carbs" class="form-label">Carbs (g)</label>
            <input type="number" name="carbs" id="carbs" class="form-control">
          </div>
          <div class="col-3 mb-3">
            <label for="fat" class="form-label">Fat (g)</label>
            <input type="number" name="fat" id="fat" class="form-control">
          </div>
        </div>

        <div class="text-center my-2">
            <label class="form-label fw-normal">OR</label>
        </div>

        <div class="mb-3">
            <label for="mealName" class="form-label">Find a Recipe</label>
            <!-- EDIT LATER FOR SEARCH RECIPES -->
            <input type="text" name="mealName" id="mealName" class="form-control" placeholder="Select a recipe from the list..." required>
        </div>
        
      `;
    } else if (mode === "logged") {
      submitText.textContent = "Log Meal";
      const now = new Date().toISOString().slice(0, 16);
      formContent.innerHTML = `
        <div class="mb-3">
            <div class="d-flex gap-2">
                <div class="flex-fill">
                    <label for="loggedDate" class="form-label">Date</label>
                    <input type="date" name="loggedDate" id="loggedDate" class="form-control" required>
                </div>
                <div class="flex-fill">
                    <label for="loggedTime" class="form-label">Time</label>
                    <input type="time" name="loggedTime" id="loggedTime" class="form-control" required>
                </div>
            </div>
        </div>

        <div class="mb-3">
          <label for="mealName" class="form-label">🍽️ What did you eat?</label>
          <input type="text" name="mealName" id="mealName" class="form-control" placeholder="e.g. Chicken Adobo with Rice" required>
        </div>
        
        <div class="row">
          <div class="col-3 mb-3">
            <label for="calories" class="form-label">Calories</label>
            <input type="number" name="calories" id="calories" class="form-control" required>
          </div>
          <div class="col-3 mb-3">
            <label for="protein" class="form-label">Protein (g)</label>
            <input type="number" name="protein" id="protein" class="form-control">
          </div>
          <div class="col-3 mb-3">
            <label for="carbs" class="form-label">Carbs (g)</label>
            <input type="number" name="carbs" id="carbs" class="form-control">
          </div>
          <div class="col-3 mb-3">
            <label for="fat" class="form-label">Fat (g)</label>
            <input type="number" name="fat" id="fat" class="form-control">
          </div>
        </div>
        
        <div class="mb-3">
          <label for="mealNotes" class="form-label">📝 Notes (Optional)</label>
          <textarea name="mealNotes" id="mealNotes" rows="2" class="form-control" placeholder="How was the meal? Any thoughts..."></textarea>
        </div>
      `;
    }
  }
  
  // Load default form
  loadForm(currentMode);
  
  // Mode toggle functionality
  modeButtons.forEach(btn => {
    btn.addEventListener('click', () => {
      const mode = btn.dataset.mode;
      if (mode !== currentMode) {
        currentMode = mode;
        
        // Update button states
        modeButtons.forEach(b => b.classList.remove('active'));
        btn.classList.add('active');
        
        loadForm(currentMode);
      }
    });
  });


});

// OUTSIDE the DOMContentLoaded event listener

// For generate grocery list btn
function generateGroceryList() {
    alert('Generate grocery list coming soon!');
}

// To update dropdown btn text
function updateDateButton(selectedOption) {
    document.querySelector('#dateDropdown').textContent = selectedOption;
}

// For set goal btn
function setGoal() {
    alert('Set goal functionality coming soon!');
}
