let isFollowing = false;
let profileUserId = '';
let profileUserEmail = '';
let currentUserEmail = '';

// Initialize variables from meta tags
function initializeVariables() {
    profileUserId = document.querySelector('meta[name="profile-user-id"]')?.getAttribute('content') || '';
    profileUserEmail = document.querySelector('meta[name="profile-user-email"]')?.getAttribute('content') || '';
    currentUserEmail = document.querySelector('meta[name="current-user-email"]')?.getAttribute('content') || '';
    
    // Debug logging
    console.log('Profile User Email:', profileUserEmail);
    console.log('Current User Email:', currentUserEmail);
    console.log('User is authenticated:', document.querySelector('meta[name="user-authenticated"]')?.getAttribute('content') === 'true');
}

// Initialize follow status when page loads
document.addEventListener('DOMContentLoaded', async function() {
    initializeVariables();
    feather.replace();
    await checkFollowStatus();
    await loadUserStats();
    await loadUserRecipes();
    
    // Initialize Bootstrap tabs
    const triggerTabList = [].slice.call(document.querySelectorAll('#profileTabs button'));
    triggerTabList.forEach(function (triggerEl) {
        const tabTrigger = new bootstrap.Tab(triggerEl);
        triggerEl.addEventListener('click', function (event) {
            event.preventDefault();
            tabTrigger.show();
        });
    });
});

async function checkFollowStatus() {
    try {
        const response = await fetch(`/Profile/IsFollowing?followeeEmail=${encodeURIComponent(profileUserEmail)}`);
        const result = await response.json();
        isFollowing = result.isFollowing;
        updateFollowButton();
    } catch (error) {
        console.error('Error checking follow status:', error);
        // Default to not following if there's an error
        isFollowing = false;
        updateFollowButton();
    }
}

async function loadUserStats() {
    try {
        const response = await fetch(`/Profile/GetUserStats?userEmail=${encodeURIComponent(profileUserEmail)}`);
        const stats = await response.json();
        
        console.log('User stats loaded:', stats);
        
        // Update the counts in the UI
        document.getElementById('recipeCount').textContent = stats.recipes;
        document.getElementById('followingCount').textContent = stats.following;
        document.getElementById('followerCount').textContent = stats.followers;
    } catch (error) {
        console.error('Error loading user stats:', error);
        // Keep default values if there's an error
    }
}

async function loadUserRecipes() {
    try {
        const response = await fetch(`/Profile/GetUserRecipes?userEmail=${encodeURIComponent(profileUserEmail)}`);
        const result = await response.json();
        
        console.log('User recipes loaded:', result);
        
        const recipesContainer = document.getElementById('recipesContainer');
        
        if (result.success && result.recipes && result.recipes.length > 0) {
            // Clear loading spinner
            recipesContainer.innerHTML = '';
            
            // Create recipe cards
            result.recipes.forEach(recipe => {
                const recipeCard = createRecipeCard(recipe);
                recipesContainer.appendChild(recipeCard);
            });
        } else {
            // Show no recipes message
            recipesContainer.innerHTML = `
                <div class="col-12 text-center py-5">
                    <i data-feather="book-open" style="width: 64px; height: 64px;" class="text-muted mb-3"></i>
                    <h4 class="text-muted">No recipes yet</h4>
                    <p class="text-muted">This user hasn't shared any recipes yet.</p>
                </div>
            `;
            feather.replace();
        }
    } catch (error) {
        console.error('Error loading user recipes:', error);
        const recipesContainer = document.getElementById('recipesContainer');
        recipesContainer.innerHTML = `
            <div class="col-12 text-center py-5">
                <i data-feather="alert-circle" style="width: 64px; height: 64px;" class="text-danger mb-3"></i>
                <h4 class="text-muted">Error loading recipes</h4>
                <p class="text-muted">Unable to load recipes at this time.</p>
            </div>
        `;
        feather.replace();
    }
}

function createRecipeCard(recipe) {
    const col = document.createElement('div');
    col.className = 'col-md-6 col-lg-4 mb-4';
    
    col.innerHTML = `
        <div class="card h-100 shadow-sm border-0 rounded-3 card-hover">
            <div class="position-relative">
                <img src="${recipe.image || '/img/default-recipe.jpg'}" 
                     class="card-img-top" 
                     alt="${recipe.name}"
                     style="height: 200px; object-fit: cover;">
                <span class="badge bg-primary position-absolute top-0 start-0 m-3 rounded-pill px-3 py-2">
                    ${recipe.type || 'Recipe'}
                </span>
                ${recipe.totalTime ? `
                <span class="badge bg-dark position-absolute top-0 end-0 m-3 rounded-pill px-3 py-2">
                    <i data-feather="clock" style="width: 14px; height: 14px;" class="me-1"></i>
                    ${recipe.totalTime}min
                </span>
                ` : ''}
            </div>
            <div class="card-body d-flex flex-column">
                <h5 class="card-title fw-bold mb-2">${recipe.name}</h5>
                <p class="card-text text-muted mb-3 flex-grow-1" style="font-size: 0.9rem;">
                    ${recipe.description ? (recipe.description.length > 100 ? recipe.description.substring(0, 100) + '...' : recipe.description) : 'No description available'}
                </p>
                <div class="d-flex justify-content-between align-items-center mb-3">
                    <small class="text-muted">
                        <i data-feather="users" style="width: 14px; height: 14px;" class="me-1"></i>
                        ${recipe.servings || 'N/A'} servings
                    </small>
                    <small class="text-muted">
                        <i data-feather="list" style="width: 14px; height: 14px;" class="me-1"></i>
                        ${recipe.ingredientCount} ingredients
                    </small>
                </div>
                <a href="/Recipe/View/${recipe.id}" class="btn btn-custom-red btn-sm w-100">
                    <i data-feather="eye" style="width: 14px; height: 14px;" class="me-2"></i>
                    View Recipe
                </a>
            </div>
        </div>
    `;
    
    // Re-initialize feather icons for the new card
    setTimeout(() => feather.replace(), 100);
    
    return col;
}

async function toggleFollow() {
    const followBtn = document.getElementById('followBtn');
    const followText = document.getElementById('followText');
    
    // Disable button during request
    followBtn.disabled = true;
    followText.textContent = isFollowing ? 'Unfollowing...' : 'Following...';
    
    try {
        console.log('toggleFollow called - currentUserEmail:', currentUserEmail);
        console.log('toggleFollow called - profileUserEmail:', profileUserEmail);
        
        if (!currentUserEmail || currentUserEmail === '') {
            showToast('Please log in to follow users', 'error');
            console.error('Current user email is empty or null');
            return;
        }
        
        if (!profileUserEmail || profileUserEmail === '') {
            showToast('Profile user email not found', 'error');
            console.error('Profile user email is empty or null');
            return;
        }
        
        const endpoint = isFollowing ? '/Profile/UnFollow' : '/Profile/Follow';
        console.log('Making request to:', endpoint);
        console.log('Request body:', {
            followerEmail: currentUserEmail,
            followeeEmail: profileUserEmail
        });
        
        const response = await fetch(endpoint, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
            },
            body: JSON.stringify({
                followerEmail: currentUserEmail,
                followeeEmail: profileUserEmail
            })
        });
        
        const result = await response.json();
        console.log('Response:', result);
        
        if (result.success) {
            isFollowing = !isFollowing;
            updateFollowButton();
            
            // Reload user stats to reflect the change
            await loadUserStats();
            
            showToast(isFollowing ? 'Successfully followed user!' : 'Successfully unfollowed user!', 'success');
        } else {
            console.error('Follow/Unfollow failed:', result.message);
            showToast(result.message || 'Failed to update follow status', 'error');
        }
    } catch (error) {
        console.error('Error toggling follow:', error);
        showToast('An error occurred. Please try again.', 'error');
    } finally {
        followBtn.disabled = false;
    }
}

function updateFollowButton() {
    const followBtn = document.getElementById('followBtn');
    const followText = document.getElementById('followText');
    
    // Enable the button once we have the follow status
    followBtn.disabled = false;
    
    if (isFollowing) {
        followBtn.className = 'btn btn-outline-danger btn-lg px-4 py-2 shadow-sm';
        followText.innerHTML = '<i data-feather="user-minus" class="me-2" style="width: 16px; height: 16px;"></i>Unfollow';
    } else {
        followBtn.className = 'btn btn-custom-red btn-lg px-4 py-2 shadow-sm';
        followText.innerHTML = '<i data-feather="user-plus" class="me-2" style="width: 16px; height: 16px;"></i>Follow';
    }
    
    // Re-initialize feather icons for the new icons
    feather.replace();
}

function showToast(message, type) {
    // Create toast element
    const toast = document.createElement('div');
    toast.className = `alert alert-${type === 'success' ? 'success' : type === 'warning' ? 'warning' : 'danger'} position-fixed`;
    toast.style.cssText = 'top: 20px; right: 20px; z-index: 9999; min-width: 300px; opacity: 0.9;';
    toast.textContent = message;
    
    // Add to page
    document.body.appendChild(toast);
    
    // Remove after 3 seconds
    setTimeout(() => {
        toast.remove();
    }, 3000);
}