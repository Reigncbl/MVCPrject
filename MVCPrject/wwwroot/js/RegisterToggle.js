const toggleBtn = document.getElementById("toggleMode");
const formContent = document.getElementById("formContent");
const heroContainer = document.querySelector(".hero-container");

let isLogin = true;

function swapFormContent() {
    formContent.classList.add("fade-out");

    setTimeout(() => {
        heroContainer.classList.toggle("reverse");
        isLogin = !isLogin;

        formContent.innerHTML = isLogin
            ? `
                <h2 class="fw-bold mb-4">Welcome Back!</h2>
                <form class="d-flex flex-column justify-content-between h-100">
                    <div>
                        <div class="mb-3 text-start">
                            <label for="username" class="form-label fs-4 fw-bolder">Username</label>
                            <input type="text" id="username" class="form-control fs-5 p-2"
                                placeholder="Username" required style="background-color: #D9D9D9;" />
                        </div>

                        <div class="mb-4 text-start">
                            <label for="password" class="form-label fs-4 fw-bolder">Password</label>
                            <input type="password" id="password" class="form-control fs-5 p-2"
                                placeholder="Password" required style="background-color: #D9D9D9;" />
                        </div>
                        <div class="mb-4 text-start">
                            <a href="#" class="toggle-link fw-bold">Forgot password?</a>
                        </div>
                        <div class="mb-4 text-start">
                            <input type="checkbox" id="remember" class="form-check-input" />
                            <label for="remember" class="form-label fs-6 fw-light">Remember me</label>
                        </div>
                    </div>
                    <div>
                        <button type="submit" class="btn w-100 fs-5 fw-bold mb-3 text-black"
                            style="background-color: #FBAE1F;" asp-controller="Landing" asp-action="Index">
                            Login
                        </button>
                    </div>
                </form>
                <p class="mt-3 small">
                    Don't have an account?
                    <a id="toggleMode" class="toggle-link fw-bold">Register here</a>
                </p>
            `
            : `
                <h2 class="fw-bold mb-4">Create Account</h2>
                <form class="d-flex flex-column justify-content-between h-100">
                    <div>
                        <div class="mb-3 text-start">
                            <label for="name" class="form-label fs-4 fw-bolder">Full Name</label>
                            <input type="text" class="form-control fs-5 p-2" id="name" placeholder="Enter your full name" required style="background-color: #D9D9D9;" />
                        </div>

                        <div class="mb-3 text-start">
                            <label for="email" class="form-label fs-4 fw-bolder">Email address</label>
                            <input type="email" class="form-control fs-5 p-2" id="email" placeholder="Enter your email" required style="background-color: #D9D9D9;" />
                        </div>

                        <div class="mb-3 text-start">
                            <label for="password" class="form-label fs-4 fw-bolder">Password</label>
                            <input type="password" class="form-control fs-5 p-2" id="password" placeholder="Enter a password" required style="background-color: #D9D9D9;" />
                        </div>

                        <div class="mb-4 text-start">
                            <label for="confirm" class="form-label fs-4 fw-bolder">Confirm Password</label>
                            <input type="password" class="form-control fs-5 p-2" id="confirm" placeholder="Confirm your password" required style="background-color: #D9D9D9;" />
                        </div>
                        <div class="mb-4 text-start">
                            <input type="checkbox" id="remember" class="form-check-input" />
                            <label for="remember" class="form-label fs-6 fw-light">Accept Terms and Condition</label>
                        </div>
                    </div>

                    <div>
                        <button type="submit" class="btn w-100 fs-5 fw-bold mb-3 text-black" style="background-color: #FBAE1F;">Register</button>
                    </div>
                </form>
                <p class="mt-3 small">
                    Already have an account?
                    <a id="toggleMode" class="toggle-link fw-bold">Login here</a>
                </p>

            `;

        formContent.classList.remove("fade-out");

        // Rebind the event
        document.getElementById("toggleMode").addEventListener("click", (e) => {
            e.preventDefault();
            swapFormContent();
        });
    }, 300);
}

toggleBtn.addEventListener("click", (e) => {
    e.preventDefault();
    swapFormContent();
});
