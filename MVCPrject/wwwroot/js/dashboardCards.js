const nutritionData = [
    { label: 'Calories', value: 1354, percent: 70 },
    { label: 'Protein', value: 1354, percent: 50 },
    { label: 'Fats', value: 1354, percent: 20 },
    { label: 'Carbohydrates', value: 1354, percent: 90 }
];

const container = document.getElementById("dashboard-cards");

nutritionData.forEach(item => {
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
