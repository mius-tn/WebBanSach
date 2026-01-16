/**
 * 🌸 Sakura Animation - Falling Cherry Blossoms
 * Creates smooth, performant falling sakura petals
 */

(function () {
    'use strict';

    // Configuration
    const CONFIG = {
        petalCount: 50,
        minDuration: 12,
        maxDuration: 20,
        minDelay: 0,
        maxDelay: 8
    };

    /**
     * Initialize sakura animation
     */
    function initSakura() {
        const container = document.getElementById('sakura-container');
        if (!container) {
            console.warn('Sakura container not found');
            return;
        }

        // Create petals
        for (let i = 0; i < CONFIG.petalCount; i++) {
            createPetal(container);
        }
    }

    /**
     * Create a single sakura petal
     */
    function createPetal(container) {
        const petal = document.createElement('div');
        petal.className = 'sakura-petal';

        // Randomize starting position - bias toward right where the tree is
        // 40% of petals fall from the right half where the tree is
        const isRightSide = Math.random() > 0.6;
        const startX = isRightSide ? (60 + Math.random() * 40) : (Math.random() * 100);
        petal.style.left = `${startX}%`;

        // Start from absolute top or slightly random Y to vary the start
        const startY = -Math.random() * 10;
        petal.style.top = `${startY}vh`;

        // Randomize animation duration and delay
        const duration = CONFIG.minDuration + Math.random() * (CONFIG.maxDuration - CONFIG.minDuration);
        const delay = Math.random() * CONFIG.maxDelay;

        petal.style.animationDuration = `${duration}s`;
        petal.style.animationDelay = `${delay}s`;

        // Randomize size slightly
        const size = 12 + Math.random() * 6;
        petal.style.width = `${size}px`;
        petal.style.height = `${size}px`;

        // Randomize opacity
        const opacity = 0.5 + Math.random() * 0.3;
        petal.style.opacity = opacity;

        container.appendChild(petal);
    }

    /**
     * Toggle between login and register forms
     */
    function initFormToggle() {
        const toggleBtns = document.querySelectorAll('.sakura-toggle-btn');
        const formSections = document.querySelectorAll('.sakura-form-section');

        toggleBtns.forEach(btn => {
            btn.addEventListener('click', function () {
                const target = this.getAttribute('data-target');

                // Update active button
                toggleBtns.forEach(b => b.classList.remove('active'));
                this.classList.add('active');

                // Update active form section
                formSections.forEach(section => {
                    if (section.id === target) {
                        section.classList.add('active');
                    } else {
                        section.classList.remove('active');
                    }
                });

                // Update header text
                updateHeader(target);
            });
        });
    }

    /**
     * Update header based on active form
     */
    function updateHeader(target) {
        const title = document.querySelector('.sakura-title');
        const subtitle = document.querySelector('.sakura-subtitle');
        const icon = document.querySelector('.sakura-icon');

        if (target === 'login-form') {
            title.textContent = 'Chào mừng trở lại';
            subtitle.textContent = 'Đăng nhập để tiếp tục';
            icon.textContent = '🌸';
        } else if (target === 'register-form') {
            title.textContent = 'Tạo tài khoản mới';
            subtitle.textContent = 'Tham gia cùng chúng tôi';
            icon.textContent = '🌺';
        }
    }

    /**
     * Password visibility toggle
     */
    function initPasswordToggle() {
        const toggleBtns = document.querySelectorAll('.sakura-password-toggle');

        toggleBtns.forEach(btn => {
            btn.addEventListener('click', function () {
                const input = this.previousElementSibling;
                const icon = this.querySelector('i');

                if (input.type === 'password') {
                    input.type = 'text';
                    icon.classList.remove('bi-eye');
                    icon.classList.add('bi-eye-slash');
                } else {
                    input.type = 'password';
                    icon.classList.remove('bi-eye-slash');
                    icon.classList.add('bi-eye');
                }
            });
        });
    }

    /**
     * Form submission with loading state
     */
    function initFormSubmit() {
        // This function is now empty as its logic is moved to a global listener
        // to ensure it applies to all forms, including dynamically added ones if any.
    }

    // Loading state for form submission
    const forms = document.querySelectorAll('form');
    forms.forEach(form => {
        form.addEventListener('submit', function () {
            const btn = this.querySelector('button[type="submit"]');
            if (btn) {
                btn.classList.add('sakura-btn-loading');
                btn.disabled = true;
            }
        });
    });

    // Email OTP logic
    const btnSendOtp = document.getElementById('btnSendEmailOtp');
    const emailInput = document.getElementById('regEmail');
    const otpGroup = document.getElementById('emailOtpGroup');

    if (btnSendOtp && emailInput && otpGroup) {
        btnSendOtp.addEventListener('click', async function () {
            const email = emailInput.value;
            if (!email || !email.includes('@')) { // Changed '@@' to '@' for valid email check
                alert('Vui lòng nhập email hợp lệ!');
                return;
            }

            btnSendOtp.disabled = true;
            btnSendOtp.innerText = 'Đang gửi...';

            try {
                const response = await fetch('/Account/SendEmailOtp', {
                    method: 'POST',
                    headers: {
                        'Content-Type': 'application/x-www-form-urlencoded', // Changed to x-www-form-urlencoded for URLSearchParams
                        'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]').value
                    },
                    body: new URLSearchParams({ email: email })
                });

                const data = await response.json();
                if (response.ok && data.success) { // Check response.ok for HTTP status
                    alert(data.message);
                    otpGroup.style.display = 'block';
                    btnSendOtp.innerText = 'Gửi lại';

                    // Countdown for resend
                    let seconds = 60;
                    btnSendOtp.disabled = true;
                    const interval = setInterval(() => {
                        seconds--;
                        btnSendOtp.innerText = `Gửi lại (${seconds}s)`;
                        if (seconds <= 0) {
                            clearInterval(interval);
                            btnSendOtp.disabled = false;
                            btnSendOtp.innerText = 'Gửi mã';
                        }
                    }, 1000);
                } else {
                    alert(data.message || 'Có lỗi xảy ra khi gửi mã.');
                    btnSendOtp.disabled = false;
                    btnSendOtp.innerText = 'Gửi mã';
                }
            } catch (err) {
                console.error(err);
                alert('Có lỗi xảy ra khi gửi mã.');
                btnSendOtp.disabled = false;
                btnSendOtp.innerText = 'Gửi mã';
            }
        });
    }

    /**
     * Initialize all features when DOM is ready
     */
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', function () {
            initSakura();
            initFormToggle();
            initPasswordToggle();
            initFormSubmit();
        });
    } else {
        initSakura();
        initFormToggle();
        initPasswordToggle();
        initFormSubmit();
    }
})();
