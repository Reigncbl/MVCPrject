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

        for (let i = -3; i <= 3; i++) {
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

    // MEAL LOGS

    const meals = [
    { id: 'almusal', label: 'Almusal (Breakfast)' },
    { id: 'tanghalian', label: 'Tanghalian (Lunch)' },
    { id: 'meryenda', label: 'Meryenda (Snack)' },
    { id: 'hapunan', label: 'Hapunan (Dinner)' }
  ];

    const row = document.getElementById('meal-row');

    meals.forEach(meal => {
        const html = `
        <div class="col-md-6 mb-4">
            <div class="card meal-card" id="${meal.id}-card">
            <div class="meal-header">
                ${meal.label}
                <span class="add-btn" style="cursor:pointer;" onclick="alert('Form coming soon!')">+</span>
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
});
