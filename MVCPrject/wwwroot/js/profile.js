const currentUserEmail = document.querySelector('meta[name="user-email"]')?.getAttribute('content') || '';

// Initialize page
document.addEventListener('DOMContentLoaded', async function() {
    feather.replace();
    // Stats are already loaded from server-side model, only load recipes dynamically
    await loadMyRecipes();
});

async function loadMyStats() {
    // Only refresh stats if needed (e.g., after an action that might change them)
    // The initial stats are already loaded from the server-side model
    try {
        const response = await fetch(`/Profile/GetUserStats?userEmail=${encodeURIComponent(currentUserEmail)}`);
        const stats = await response.json();
        
        console.log('My stats refreshed:', stats);
        
        // Update the counts in the UI only if they changed
        const recipeCountElement = document.getElementById('myRecipeCount');
        const followingCountElement = document.getElementById('myFollowingCount');
        const followerCountElement = document.getElementById('myFollowerCount');
        
        if (recipeCountElement && stats.recipes !== undefined) {
            recipeCountElement.textContent = stats.recipes;
        }
        if (followingCountElement && stats.following !== undefined) {
            followingCountElement.textContent = stats.following;
        }
        if (followerCountElement && stats.followers !== undefined) {
            followerCountElement.textContent = stats.followers;
        }
    } catch (error) {
        console.error('Error refreshing stats:', error);
        // Keep the server-rendered values if there's an error
    }
}

// Function to refresh stats after actions (like following/unfollowing)
async function refreshStats() {
    await loadMyStats();
}

async function loadMyRecipes() {
    try {
        const response = await fetch(`/Profile/GetUserRecipes?userEmail=${encodeURIComponent(currentUserEmail)}`);
        const result = await response.json();
        
        console.log('My recipes loaded:', result);
        
        const recipesContainer = document.getElementById('recipesContainer');
        
        if (result.success && result.recipes && result.recipes.length > 0) {
            // Clear container (remove sample card)
            recipesContainer.innerHTML = '';
            
            // Create recipe cards
            result.recipes.forEach(recipe => {
                const recipeCard = createMyRecipeCard(recipe);
                recipesContainer.appendChild(recipeCard);
            });
        } else {
            // Keep the sample card or show no recipes message
            console.log('No recipes found, keeping sample card');
        }
        
        // Re-initialize feather icons
        feather.replace();
    } catch (error) {
        console.error('Error loading my recipes:', error);
        // Keep the sample card on error
        feather.replace();
    }
}

function createMyRecipeCard(recipe) {
    const cardDiv = document.createElement('div');
    cardDiv.className = 'card-hover';
    
    cardDiv.innerHTML = `
    <div class="d-flex flex-wrap gap-4 justify-content-start" style="padding-left: 1vh;" id="recipesContainer">
        <div class="card-hover">
            <img src="${recipe.image || '/img/sampleimg.jpg'}" class="img-fluid" alt="${recipe.name}">
            <div class="overlay">
                <h5>${recipe.name}</h5>
                <div class="details gap-5">
                    <span>${recipe.calories || '420'} cal</span>
                    <span>${recipe.totalTime || '45'} mins</span>
                </div>
            </div>
        </div>
    </div>
    `;
    
    // Add click event to navigate to recipe view
    cardDiv.addEventListener('click', function() {
        window.location.href = `/Recipe/View/${recipe.id}`;
    });
    
    return cardDiv;
}

// Show followers modal
async function showFollowersModal() {
    const modal = new bootstrap.Modal(document.getElementById('followersModal'));
    modal.show();
    
    // Load followers data
    await loadFollowers();
}

// Show following modal
async function showFollowingModal() {
    const modal = new bootstrap.Modal(document.getElementById('followingModal'));
    modal.show();
    
    // Load following data
    await loadFollowing();
}

// Load followers list
async function loadFollowers() {
    const container = document.getElementById('followersContainer');
    
    try {
        const response = await fetch(`/Profile/GetUserFollowers?userEmail=${encodeURIComponent(currentUserEmail)}`);
        const result = await response.json();
        
        if (result.success && result.followers && result.followers.length > 0) {
            container.innerHTML = '';
            
            result.followers.forEach(follower => {
                const userCard = createUserCard(follower, 'follower');
                container.appendChild(userCard);
            });
        } else {
            container.innerHTML = `
                <div class="text-center py-5">
                    <i data-feather="users" style="width: 64px; height: 64px;" class="text-muted mb-3"></i>
                    <h5 class="text-muted">No followers yet</h5>
                    <p class="text-muted">You don't have any followers yet.</p>
                </div>
            `;
        }
        
        feather.replace();
    } catch (error) {
        console.error('Error loading followers:', error);
        container.innerHTML = `
            <div class="text-center py-5">
                <i data-feather="alert-circle" style="width: 64px; height: 64px;" class="text-danger mb-3"></i>
                <h5 class="text-muted">Error loading followers</h5>
                <p class="text-muted">Unable to load followers at this time.</p>
            </div>
        `;
        feather.replace();
    }
}

// Load following list
async function loadFollowing() {
    const container = document.getElementById('followingContainer');
    
    try {
        const response = await fetch(`/Profile/GetUserFollowing?userEmail=${encodeURIComponent(currentUserEmail)}`);
        const result = await response.json();
        
        if (result.success && result.following && result.following.length > 0) {
            container.innerHTML = '';
            
            result.following.forEach(user => {
                const userCard = createUserCard(user, 'following');
                container.appendChild(userCard);
            });
        } else {
            container.innerHTML = `
                <div class="text-center py-5">
                    <i data-feather="user-plus" style="width: 64px; height: 64px;" class="text-muted mb-3"></i>
                    <h5 class="text-muted">Not following anyone</h5>
                    <p class="text-muted">You're not following anyone yet.</p>
                </div>
            `;
        }
        
        feather.replace();
    } catch (error) {
        console.error('Error loading following:', error);
        container.innerHTML = `
            <div class="text-center py-5">
                <i data-feather="alert-circle" style="width: 64px; height: 64px;" class="text-danger mb-3"></i>
                <h5 class="text-muted">Error loading following</h5>
                <p class="text-muted">Unable to load following at this time.</p>
            </div>
        `;
        feather.replace();
    }
}

// Create user card for followers/following lists
function createUserCard(user, type) {
    const card = document.createElement('div');
    card.className = 'border-bottom p-3 user-card';
    
    card.innerHTML = `
        <div class="d-flex align-items-center">
            <img src="${user.profileImageUrl || '/img/image.png'}" 
                 alt="${user.name}" 
                 class="rounded-circle me-3" 
                 style="width: 50px; height: 50px; object-fit: cover;">
            <div class="flex-grow-1">
                <h6 class="mb-1 fw-bold">${user.name}</h6>
                <p class="text-muted mb-1 small">@${user.email}</p>
                ${user.bio ? `<p class="text-muted mb-0 small">${user.bio.length > 60 ? user.bio.substring(0, 60) + '...' : user.bio}</p>` : ''}
            </div>
            <div class="ms-3">
                <a href="/Profile/ProfileOthers?email=${encodeURIComponent(user.email)}" 
                   class="btn btn-outline-primary btn-sm">
                    <i data-feather="eye" style="width: 14px; height: 14px;" class="me-1"></i>
                    View Profile
                </a>
            </div>
        </div>
    `;
    
    // Add hover effect
    card.addEventListener('mouseenter', function() {
        this.style.backgroundColor = '#f8f9fa';
    });
    
    card.addEventListener('mouseleave', function() {
        this.style.backgroundColor = '';
    });
    
    return card;
}