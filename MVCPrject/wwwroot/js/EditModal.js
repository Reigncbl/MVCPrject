document.addEventListener('DOMContentLoaded', function () {
    // Feather icon init
    feather.replace();

    // Social Link Add/Remove
    const addLinkBtn = document.getElementById('add-link-btn');
    const linkContainer = document.getElementById('social-links');

    if (addLinkBtn && linkContainer) {
        addLinkBtn.addEventListener('click', function () {
            const newInputGroup = document.createElement('div');
            newInputGroup.classList.add('input-group', 'mb-2');
            newInputGroup.innerHTML = `
                <input type="url" class="form-control rounded-start-3" placeholder="https://...">
                <button type="button" class="btn btn-outline-danger rounded-end-3 remove-link-btn">
                    <i data-feather="x-circle" class="fs-5"></i>
                </button>
            `;
            linkContainer.appendChild(newInputGroup);
            feather.replace();
        });

        linkContainer.addEventListener('click', function (e) {
            if (e.target.closest('.remove-link-btn')) {
                e.target.closest('.input-group').remove();
            }
        });
    }

    // Profile Image Upload & Preview
    const imageInput = document.getElementById('profileImageInput');
    const overlay = document.querySelector('.upload-overlay');
    const preview = document.getElementById('profileImagePreview');

    if (imageInput && overlay && preview) {
        overlay.addEventListener('click', () => imageInput.click());

        imageInput.addEventListener('change', function () {
            const file = this.files[0];
            if (file) {
                const reader = new FileReader();
                reader.onload = function (e) {
                    preview.style.backgroundImage = `url(${e.target.result})`;
                    preview.style.backgroundSize = 'cover';
                    preview.style.backgroundPosition = 'center';
                };
                reader.readAsDataURL(file);
            }
        });
    }

    // Form Submission
    const editProfileForm = document.getElementById('editProfileForm');
    if (editProfileForm) {
        editProfileForm.addEventListener('submit', async function(e) {
            e.preventDefault();
            
            const submitButton = e.target.querySelector('button[type="submit"]');
            const originalButtonText = submitButton.textContent;
            
            // Validate required fields
            const displayName = document.getElementById('displayName').value.trim();
            const username = document.getElementById('username').value.trim();
            
            if (!displayName || !username) {
                showNotification('Please fill in all required fields.', 'error');
                return;
            }
            
            // Set loading state
            submitButton.disabled = true;
            submitButton.innerHTML = '<span class="spinner-border spinner-border-sm me-2" role="status"></span>Saving...';
            
            try {
                // Get base64 image data if an image was selected
                let profileImageBase64 = null;
                const imageFile = imageInput.files[0];
                if (imageFile) {
                    profileImageBase64 = await convertFileToBase64(imageFile);
                }
                
                const requestData = {
                    DisplayName: displayName,
                    Username: username,
                    ProfileImageBase64: profileImageBase64
                };
                
                const response = await fetch('/Profile/UpdateProfile', {
                    method: 'POST',
                    headers: {
                        'Content-Type': 'application/json',
                    },
                    body: JSON.stringify(requestData)
                });
                
                const result = await response.json();
                if (result.success) {
                    showNotification('Profile updated successfully!', 'success');
                    
                    // Update UI
                    document.getElementById('currentUserName').textContent = result.data.displayName;
                    // Update username in the UI
                    const usernameElement = document.querySelector('.text-muted');
                    if (usernameElement) {
                        usernameElement.textContent = '@' + result.data.username;
                    }
                    // Update profile image if changed
                    if (result.data.profileImageUrl) {
                        const profileImg = document.getElementById('mainProfileImage');
                        if (profileImg) {
                            profileImg.src = result.data.profileImageUrl;
                        }
                        
                        // Also update the preview in the modal
                        const preview = document.getElementById('profileImagePreview');
                        if (preview) {
                            preview.style.backgroundImage = `url(${result.data.profileImageUrl})`;
                            preview.style.backgroundSize = 'cover';
                            preview.style.backgroundPosition = 'center';
                        }
                    }
                    
                    // Close modal after a short delay
                    setTimeout(() => {
                        const modal = bootstrap.Modal.getInstance(document.getElementById('editProfileModal'));
                        modal.hide();
                    }, 1500);
                } else {
                    showNotification('Error: ' + result.message, 'error');
                }
            } catch (error) {
                console.error('Error updating profile:', error);
                showNotification('An error occurred while updating the profile.', 'error');
            } finally {
                // Reset button state
                submitButton.disabled = false;
                submitButton.textContent = originalButtonText;
            }
        });
    }

    // Enhanced notification function
    function showNotification(message, type = 'info') {
        // Remove existing notifications
        const existingNotifications = document.querySelectorAll('.profile-notification');
        existingNotifications.forEach(notification => notification.remove());
        
        // Create notification element
        const notification = document.createElement('div');
        notification.className = `alert alert-${type === 'error' ? 'danger' : type === 'success' ? 'success' : 'info'} alert-dismissible fade show profile-notification`;
        notification.style.cssText = `
            position: fixed;
            top: 20px;
            right: 20px;
            z-index: 9999;
            min-width: 300px;
            box-shadow: 0 4px 12px rgba(0,0,0,0.15);
        `;
        
        notification.innerHTML = `
            <div class="d-flex align-items-center">
                <i class="feather-${type === 'success' ? 'check-circle' : type === 'error' ? 'x-circle' : 'info'} me-2"></i>
                <span>${message}</span>
                <button type="button" class="btn-close ms-auto" data-bs-dismiss="alert"></button>
            </div>
        `;
        
        document.body.appendChild(notification);
        
        // Auto-remove after 5 seconds
        setTimeout(() => {
            if (notification.parentNode) {
                notification.remove();
            }
        }, 5000);
    }

    // Helper function to convert file to base64
    function convertFileToBase64(file) {
        return new Promise((resolve, reject) => {
            const reader = new FileReader();
            reader.onload = () => {
                // Remove the data:image/jpeg;base64, prefix
                const base64 = reader.result.split(',')[1];
                resolve(base64);
            };
            reader.onerror = reject;
            reader.readAsDataURL(file);
        });
    }
});