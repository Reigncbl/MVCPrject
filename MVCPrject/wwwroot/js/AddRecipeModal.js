// Data Manager - handles data persistence across steps
class DataManager {
    constructor() {
        this.data = {};
    }
            
    saveFormData() {
        // Save all form inputs from current step
        const inputs = document.querySelectorAll('.modal-content-area input, .modal-content-area textarea, .modal-content-area select');
        inputs.forEach(input => {
            this.data[input.id] = input.value;
        });
    }
            
    loadFormData() {
        // Load saved data into form inputs
        const inputs = document.querySelectorAll('.modal-content-area input, .modal-content-area textarea, .modal-content-area select');
        inputs.forEach(input => {
            if (this.data[input.id] !== undefined) {
                        input.value = this.data[input.id];
            }
        });
    }
            
    clearData() {
        this.data = {};
    }
            
    getData() {
        return this.data;
    }
}

// Step interface - defines the contract for each step
class Step {
    constructor(title, content) {
        this.title = title;
        this.content = content;
    }

    getTitle() {
        return this.title;
    }

    getContent() {
        return this.content;
    }

    validate() {
        // Override in subclasses for validation
        return true;
    }
}

// Step One Form
class StepOne extends Step {
    constructor() {
        super("Recipe Overview", `
            <form>
                <!-- Recipe Name, Cooking Time, Servings Row -->
                <div class="row mb-3">
                    <div class="col-md-6 pe-md-2 my-2">
                        <label for="recipeName" class="form-label">Recipe Name *</label>
                        <input type="text" class="form-control" id="recipeName" placeholder="" required>
                    </div>
                    <div class="col-md-4 pe-md-1 my-2">
                        <label for="cookingTime" class="form-label">Cooking Time (Minutes)</label>
                        <input type="number" class="form-control" id="cookingTime" placeholder="">
                    </div>
                    <div class="col-md-2 pe-md-2 my-2">
                        <label for="servings" class="form-label">Servings</label>
                        <input type="number" class="form-control" id="servings" placeholder="">
                    </div>
                </div>

                <!-- Description -->
                <div class="mb-3">
                    <label for="description" class="form-label">Description</label>
                    <textarea class="form-control" id="description" rows="6" placeholder=""></textarea>
                </div>
                            
                <!-- Nutrition Information -->
                <div class="nutrition-row">
                    <div>
                        <label for="calories" class="form-label">Calories</label>
                        <input type="number" class="form-control" id="calories" placeholder="">
                    </div>
                    <div>
                        <label for="protein" class="form-label">Protein</label>
                        <input type="number" class="form-control" id="protein" placeholder="">
                    </div>
                    <div>
                        <label for="carbs" class="form-label">Carbs</label>
                        <input type="number" class="form-control" id="carbs" placeholder="">
                    </div>
                    <div>
                        <label for="fat" class="form-label">Fat</label>
                        <input type="number" class="form-control" id="fat" placeholder="">
                    </div>
                </div>
            </form>
        `);
    }

    validate() {
        const recipeName = document.getElementById('recipeName');
        if (!recipeName || !recipeName.value.trim()) {
            alert('Please enter a recipe name.');
            return false;
        }
        return true;
    }
}

// Step Two Form
class StepTwo extends Step {
    constructor() {
        super("Ingredients & Instructions", `
            <form>
                <div class="row">
                        <!-- Ingredients Section -->
                        <div class="col-md-6">
                            <h5 class="mb-3">Ingredients</h5>
                            <div id="ingredientsContainer">
                                <div class="ingredient-row d-flex align-items-center gap-2">
                                    <input type="text" class="form-control" placeholder="Enter ingredient">
                                    <button class="remove-btn" onclick="removeIngredient(this)">
                                        <i data-feather="x"></i>
                                    </button>
                                </div>
                                <div class="ingredient-row d-flex align-items-center gap-2">
                                    <input type="text" class="form-control" placeholder="Enter ingredient">
                                    <button class="remove-btn" onclick="removeIngredient(this)">
                                        <i data-feather="x"></i>
                                    </button>
                                </div>
                                <div class="ingredient-row d-flex align-items-center gap-2">
                                    <input type="text" class="form-control" placeholder="Enter ingredient">
                                    <button class="remove-btn" onclick="removeIngredient(this)">
                                        <i data-feather="x"></i>
                                    </button>
                                </div>
                            </div>
                            <button type="button" class="add-btn mt-2" onclick="addIngredient()">
                                <i data-feather="plus"></i>
                                Add Ingredients
                            </button>
                        </div>
                        
                        <!-- Instructions Section -->
                        <div class="col-md-6">
                            <h5 class="mb-3">Instructions</h5>
                            <div id="instructionsContainer">
                                <div class="instruction-row">
                                    <div class="instruction-number">1</div>
                                    <div class="d-flex align-items-start gap-2">
                                        <textarea class="form-control" rows="3" placeholder="Enter instruction step"></textarea>
                                        <button class="remove-btn" onclick="removeInstruction(this)">
                                            <i data-feather="x"></i>
                                        </button>
                                    </div>
                                </div>
                                <div class="instruction-row">
                                    <div class="instruction-number">2</div>
                                    <div class="d-flex align-items-start gap-2">
                                        <textarea class="form-control" rows="3" placeholder="Enter instruction step"></textarea>
                                        <button class="remove-btn" onclick="removeInstruction(this)">
                                            <i data-feather="x"></i>
                                        </button>
                                    </div>
                                </div>
                                <div class="instruction-row">
                                    <div class="instruction-number">3</div>
                                    <div class="d-flex align-items-start gap-2">
                                        <textarea class="form-control" rows="3" placeholder="Enter instruction step"></textarea>
                                        <button class="remove-btn" onclick="removeInstruction(this)">
                                            <i data-feather="x"></i>
                                        </button>
                                    </div>
                                </div>
                            </div>
                            <button type="button" class="add-btn mt-2" onclick="addInstruction()">
                                <i data-feather="plus"></i>
                                Add Step
                            </button>
                        </div>
                    </div>
            </form>
        `)
    }

    validate() {
    return window.recipeForm?.validateForm() ?? false;
    }

}

class RecipeForm {
    constructor() {
                this.ingredientsContainer = document.getElementById('ingredientsContainer');
                this.instructionsContainer = document.getElementById('instructionsContainer');
                this.init();
            }

    init() {
                // Bind event listeners
                this.bindEvents();
            }

    bindEvents() {
                // Add event listeners for existing buttons
                document.addEventListener('click', (e) => {
                    if (e.target.closest('.remove-btn')) {
                        const button = e.target.closest('.remove-btn');
                        if (button.getAttribute('onclick')?.includes('removeIngredient')) {
                            this.removeIngredient(button);
                        } else if (button.getAttribute('onclick')?.includes('removeInstruction')) {
                            this.removeInstruction(button);
                        }
                    }
                });
    }

    addIngredient() {
                const newRow = this.createElementFromHTML(`
                    <div class="ingredient-row d-flex align-items-center gap-2">
                        <input type="text" class="form-control" placeholder="Enter ingredient">
                        <button class="remove-btn" onclick="recipeForm.removeIngredient(this)">
                            <i data-feather="x"></i>
                        </button>
                    </div>
                `);
                this.ingredientsContainer.appendChild(newRow);
                feather.replace();
    }
            
    removeIngredient(button) {
                const row = button.parentElement;
                row.remove();
    }
            
    addInstruction() {
                const instructionCount = this.instructionsContainer.children.length + 1;
                const newRow = this.createElementFromHTML(`
                    <div class="instruction-row">
                        <div class="instruction-number">${instructionCount}</div>
                        <div class="d-flex align-items-start gap-2">
                            <textarea class="form-control" rows="3" placeholder="Enter instruction step"></textarea>
                            <button class="remove-btn" onclick="recipeForm.removeInstruction(this)">
                                <i data-feather="x"></i>
                            </button>
                        </div>
                    </div>
                `);
                this.instructionsContainer.appendChild(newRow);
                feather.replace();
    }
            
    removeInstruction(button) {
                const row = button.parentElement.parentElement;
                row.remove();
                this.updateInstructionNumbers();
    }
            
    updateInstructionNumbers() {
                const instructions = this.instructionsContainer.children;
                for (let i = 0; i < instructions.length; i++) {
                    const numberDiv = instructions[i].querySelector('.instruction-number');
                    numberDiv.textContent = i + 1;
                }
    }
            
    createElementFromHTML(htmlString) {
                const div = document.createElement('div');
                div.innerHTML = htmlString.trim();
                return div.firstChild;
    }
            
    closeForm() {
                alert('Form closed');
    }
            
    goBack() {
                alert('Going back to previous step');
    }
            
    goNext() {
                // Validate form before proceeding
                if (this.validateForm()) {
                    alert('Proceeding to next step');
                } else {
                    alert('Please fill in all required fields');
                }
    }
            
    validateForm() {
                const ingredients = this.ingredientsContainer.querySelectorAll('input[type="text"]');
                const instructions = this.instructionsContainer.querySelectorAll('textarea');
                
                // Check if at least one ingredient is filled
                const hasIngredients = Array.from(ingredients).some(input => input.value.trim() !== '');
                
                // Check if at least one instruction is filled
                const hasInstructions = Array.from(instructions).some(textarea => textarea.value.trim() !== '');
                
                return hasIngredients && hasInstructions;
    }
            
    getFormData() {
        const ingredients = Array.from(this.ingredientsContainer.querySelectorAll('input[type="text"]'))
                    .map(input => input.value.trim())
                    .filter(value => value !== '');
                    
        const instructions = Array.from(this.instructionsContainer.querySelectorAll('textarea'))
                    .map(textarea => textarea.value.trim())
                    .filter(value => value !== '');
                    
        return {
                    ingredients,
                    instructions
        };
    }
}

function addIngredient() {
    recipeForm.addIngredient();
}
function removeIngredient(button) {
    recipeForm.removeIngredient(button);
}
function addInstruction() {
    recipeForm.addInstruction();
}
function removeInstruction(button) {
    recipeForm.removeInstruction(button);
}

// Step Three Form
class StepThree extends Step {
    constructor() {
        super("Step 3 - Upload Image", `
            <div class="row">
                <div class="col-md-7">
                    <h5 class="mb-4">Upload Image</h5>
                                
                    <div class="upload-area" id="uploadArea" onclick="document.getElementById('fileInput').click()">
                        <div id="uploadContent">
                                <i data-feather="upload-cloud"></i>
                                <p class="mb-2 text-muted">Drag and drop image here or</p>
                                <button type="button" class="btn btn-sm browse-btn">Browse Files</button>
                                <p class="mt-2 text-muted small">PNG, JPG up to 5MB</p>
                        </div>
                        <div id="imagePreview" style="display: none;">
                            <img id="previewImg" class="uploaded-image" alt="Preview">
                            <button type="button" class="btn btn-outline-secondary btn-sm change-image-btn" onclick="changeImage(event)">
                                Change Image
                            </button>
                        </div>
                    </div>
                                
                    <input type="file" id="fileInput" accept="image/*" style="display: none;">
                            </div>
                            
                            <div class="col-md-5">
                                <h6 class="mb-3">Recipe Type</h6>
                                <div class="mb-3">
                                    <select class="form-select" id="recipeType">
                                        <option value="Main Course">Main Course</option>
                                        <option value="Breakfast">Breakfast</option>
                                        <option value="Lunch">Lunch</option>
                                        <option value="Dinner">Dinner</option>
                                        <option value="Snack">Snack</option>
                                        <option value="Dessert">Dessert</option>
                                        <option value="Appetizer">Appetizer</option>
                                        <option value="Side Dish">Side Dish</option>
                                        <option value="Soup">Soup</option>
                                        <option value="Salad">Salad</option>
                                    </select>
                                </div>
                            </div>
                        </div>
        `)
    }
}

// UI Manager - for DOM Manipulation
class UIManager {
    constructor() {
        this.modalTitle = document.getElementById("recipeModalLabel");
        this.modalBody = document.querySelector('.modal-content-area');
        this.nextBtn = document.getElementById('nextStepBtn');
        this.backBtn = document.getElementById('backBtn');
        this.closeBtn = document.getElementById('closeBtn');
        this.stepCircles = document.querySelectorAll('.step-circle');
        this.lineOne = document.getElementById('lineOne');
        this.lineTwo = document.getElementById('lineTwo');
    }

    updateTitle(title) {
        this.modalTitle.textContent = title;
    }

    updateContent(content) {
                this.modalBody.innerHTML = content;
            }
            
            updateButtonText(text) {
                this.nextBtn.textContent = text;
            }
            
            showBackButton() {
                this.backBtn.style.display = 'inline-block';
            }
            
            hideBackButton() {
                this.backBtn.style.display = 'none';
            }
    
    updateStepIndicator(currentStep) {
        this.stepCircles.forEach((circle, index) => {
            const stepNumber = index + 1;

            if (stepNumber <= currentStep) {
                circle.classList.remove('step-inactive');
                circle.classList.add('step-active');
            } else {
                circle.classList.remove('step-active');
                circle.classList.add('step-inactive');
            }
        });

        this.toggleStepLine(currentStep);
    }

    closeModal() {
        bootstrap.Modal.getInstance(document.getElementById('addRecipeModal')).hide();
    }

    toggleNavigationButtons(currentStep) {
        if (currentStep === 1) {
            // Show Close, hide Back on Step 1
            this.closeBtn.style.display = 'inline-block';
            this.backBtn.style.display = 'none';
        }  else {
            // Hide Close, show Back on Steps 2 and 3
            this.closeBtn.style.display = 'none';
            this.backBtn.style.display = 'inline-block';
        }
    }

    toggleStepLine(currentStep) {
        if (currentStep === 1) {
            this.lineOne.classList.remove('line-active');
            this.lineTwo.classList.remove('line-active');
        } else if (currentStep === 2) {
            this.lineOne.classList.add('line-active');
            this.lineTwo.classList.remove('line-active');
        }  else if (currentStep === 3) {
            this.lineTwo.classList.add('line-active');
        }
    }
}

// Step Factory - creates step instances
class StepFactory {
    static createStep(stepNumber) {
        switch(stepNumber) {
            case 1:
                return new StepOne();
            case 2:
                return new StepTwo();
            case 3:
                return new StepThree();
            default:
                throw new Error(`Invalid step number: ${stepNumber}`);
        }
    }
}

// Wizard Controller - manages the overall step flow
class WizardController {
    constructor(uiManager, dataManager, totalSteps = 3) {
        this.uiManager = uiManager;
        this.dataManager = dataManager;
        this.totalSteps = totalSteps;
        this.currentStep = 1;
        this.steps = new Map();
                
        // Initialize all steps
        for (let i = 1; i <= totalSteps; i++) {
            this.steps.set(i, StepFactory.createStep(i));
        }
                
        this.initializeWizard();
    }
            
    initializeWizard() {
        this.updateUI();
        this.bindEvents();
    }
            
    updateUI() {
        // Save current form data before switching steps
        this.dataManager.saveFormData();

        // Load data for the current step
        const step = this.steps.get(this.currentStep);
        this.uiManager.updateTitle(step.getTitle());
        this.uiManager.updateContent(step.getContent());
        this.uiManager.updateStepIndicator(this.currentStep);

        // Load saved data into the new step
        setTimeout(() => {
            this.dataManager.loadFormData();

            // Initialize RecipeForm only on Step 2
            if (this.currentStep === 2) {
                window.recipeForm = new RecipeForm(); // Set globally so onclick handlers still work
            }
        }, 0);
                
        // Update button text
        const buttonText = this.currentStep === this.totalSteps ? 'Finish' : 'Next';
        this.uiManager.updateButtonText(buttonText);
                
        // Toggle Close/Back buttons based on step
        this.uiManager.toggleNavigationButtons(this.currentStep);

        // Refresh Feather Icons
        feather.replace();
    }
            
    canProceed() {
        const step = this.steps.get(this.currentStep);
        return step.validate();
    }
            
    nextStep() {
        if (!this.canProceed()) {
            return false;
        }
                
        if (this.currentStep < this.totalSteps) {
            this.currentStep++;
            this.updateUI();
            return true;
        } else {
            this.finish();
            return false;
        }
    }
            
    previousStep() {
        if (this.currentStep > 1) {
            this.currentStep--;
            this.updateUI();
            return true;
        }
        return false;
    }
            
    async finish() {
        // Save final form data
        this.dataManager.saveFormData();
        
        const formData = this.dataManager.getData();
        console.log('Recipe completed!');
        console.log('Final data:', formData);

        // Collect ingredients and instructions from Step 2
        const ingredients = [];
        const instructions = [];
        
        if (window.recipeForm) {
            const recipeFormData = window.recipeForm.getFormData();
            ingredients.push(...recipeFormData.ingredients);
            instructions.push(...recipeFormData.instructions);
        }

        // Prepare the request payload
        const requestData = {
            recipeName: formData.recipeName || '',
            description: formData.description || '',
            recipeAuthor: formData.recipeAuthor || 'Anonymous',
            recipeType: formData.recipeType || 'Main Course',
            servings: parseInt(formData.servings) || null,
            cookingTime: parseInt(formData.cookingTime) || null,
            calories: parseInt(formData.calories) || null,
            protein: parseInt(formData.protein) || null,
            carbs: parseInt(formData.carbs) || null,
            fat: parseInt(formData.fat) || null,
            ingredients: ingredients,
            instructions: instructions,
            imageUrl: formData.imageUrl || null
        };

        try {
            // Show loading state
            this.uiManager.updateButtonText('Saving...');
            this.uiManager.nextBtn.disabled = true;

            // Submit to server
            const response = await fetch('/Recipe/Add', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                },
                body: JSON.stringify(requestData)
            });

            const result = await response.json();

            if (result.success) {
                // Show success message
                this.showToast('Recipe added successfully!', 'success');
                
                // Close modal and reset
                this.uiManager.closeModal();
                this.reset();
                
                // Optionally redirect to the new recipe or refresh the page
                setTimeout(() => {
                    window.location.reload();
                }, 1000);
            } else {
                // Show error message
                this.showToast(result.message || 'Failed to add recipe. Please try again.', 'error');
                
                // Reset button state
                this.uiManager.updateButtonText('Finish');
                this.uiManager.nextBtn.disabled = false;
            }
        } catch (error) {
            console.error('Error submitting recipe:', error);
            this.showToast('An error occurred while adding the recipe. Please try again.', 'error');
            
            // Reset button state
            this.uiManager.updateButtonText('Finish');
            this.uiManager.nextBtn.disabled = false;
        }
    }

    showToast(message, type) {
        // Create toast element
        const toast = document.createElement('div');
        toast.className = `alert alert-${type === 'success' ? 'success' : type === 'warning' ? 'warning' : 'danger'} position-fixed`;
        toast.style.cssText = 'top: 20px; right: 20px; z-index: 9999; min-width: 300px;';
        toast.textContent = message;

        // Add to page
        document.body.appendChild(toast);

        // Remove after 3 seconds
        setTimeout(() => {
            toast.remove();
        }, 3000);
    }

                
    reset() {
        this.currentStep = 1;
        this.dataManager.clearData();
        this.updateUI();
        
    }
            
    bindEvents() {
        // Next button click
        this.uiManager.nextBtn.addEventListener('click', () => {
            this.nextStep();
        });
                
        // Back button click
        this.uiManager.backBtn.addEventListener('click', () => {
            this.previousStep();
        });
                
        // Modal close reset
        document.getElementById('addRecipeModal').addEventListener('hidden.bs.modal', () => {
            this.reset();
        });
    }
}

// Initialize the application
document.addEventListener('DOMContentLoaded', () => {
    // Initialize Feather icons
    feather.replace();
            
    const uiManager = new UIManager();
    const dataManager = new DataManager();
    const wizard = new WizardController(uiManager, dataManager);
});