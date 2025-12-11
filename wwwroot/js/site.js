// LMS Platform JavaScript - Eduma Theme

// ============================================
// Video Player Progress Tracking
// ============================================
function initializeVideoPlayer(videoElement, lessonId, enrollmentId) {
    let progressInterval;
    let lastSavedTime = 0;
    
    videoElement.addEventListener('loadedmetadata', function() {
        // Load saved timestamp
        const savedTime = localStorage.getItem(`lesson_${lessonId}_timestamp`);
        if (savedTime && parseFloat(savedTime) > 0) {
            videoElement.currentTime = parseFloat(savedTime);
            showToast('Resuming from where you left off', 'info');
        }
    });
    
    videoElement.addEventListener('play', function() {
        progressInterval = setInterval(function() {
            if (Math.abs(videoElement.currentTime - lastSavedTime) > 5) {
                saveVideoProgress(lessonId, enrollmentId, videoElement.currentTime);
                lastSavedTime = videoElement.currentTime;
            }
        }, 5000);
    });
    
    videoElement.addEventListener('pause', function() {
        clearInterval(progressInterval);
        saveVideoProgress(lessonId, enrollmentId, videoElement.currentTime);
    });
    
    videoElement.addEventListener('ended', function() {
        clearInterval(progressInterval);
        localStorage.removeItem(`lesson_${lessonId}_timestamp`);
        markLessonComplete(lessonId, enrollmentId);
    });
}

function saveVideoProgress(lessonId, enrollmentId, currentTime) {
    localStorage.setItem(`lesson_${lessonId}_timestamp`, currentTime.toString());
    
    fetch('/api/progress/update', {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json',
            'RequestVerificationToken': getAntiForgeryToken()
        },
        body: JSON.stringify({
            lessonId: lessonId,
            enrollmentId: enrollmentId,
            timestamp: Math.floor(currentTime)
        })
    }).catch(err => console.log('Progress save error:', err));
}

function markLessonComplete(lessonId, enrollmentId) {
    fetch('/api/progress/complete', {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json',
            'RequestVerificationToken': getAntiForgeryToken()
        },
        body: JSON.stringify({
            lessonId: lessonId,
            enrollmentId: enrollmentId
        })
    }).then(response => {
        if (response.ok) {
            showToast('Lesson completed!', 'success');
            updateProgressUI();
        }
    });
}


// ============================================
// Toast Notifications
// ============================================
function showToast(message, type = 'info') {
    const toastContainer = document.getElementById('toast-container') || createToastContainer();
    
    const toast = document.createElement('div');
    toast.className = `toast-notification toast-${type}`;
    toast.innerHTML = `
        <div class="toast-icon">
            <i class="fas fa-${getToastIcon(type)}"></i>
        </div>
        <div class="toast-message">${message}</div>
        <button class="toast-close" onclick="this.parentElement.remove()">
            <i class="fas fa-times"></i>
        </button>
    `;
    
    toastContainer.appendChild(toast);
    
    setTimeout(() => toast.classList.add('show'), 10);
    setTimeout(() => {
        toast.classList.remove('show');
        setTimeout(() => toast.remove(), 300);
    }, 4000);
}

function createToastContainer() {
    const container = document.createElement('div');
    container.id = 'toast-container';
    document.body.appendChild(container);
    return container;
}

function getToastIcon(type) {
    const icons = {
        success: 'check-circle',
        error: 'exclamation-circle',
        warning: 'exclamation-triangle',
        info: 'info-circle'
    };
    return icons[type] || 'info-circle';
}

// ============================================
// Form Validation
// ============================================
function initializeFormValidation() {
    const forms = document.querySelectorAll('.needs-validation');
    
    forms.forEach(form => {
        form.addEventListener('submit', function(event) {
            if (!form.checkValidity()) {
                event.preventDefault();
                event.stopPropagation();
            }
            form.classList.add('was-validated');
        });
    });
}

// ============================================
// AJAX Form Submission
// ============================================
function submitFormAjax(formElement, successCallback) {
    const formData = new FormData(formElement);
    const url = formElement.action;
    const method = formElement.method || 'POST';
    
    fetch(url, {
        method: method,
        body: formData,
        headers: {
            'RequestVerificationToken': getAntiForgeryToken()
        }
    })
    .then(response => response.json())
    .then(data => {
        if (data.success) {
            showToast(data.message || 'Success!', 'success');
            if (successCallback) successCallback(data);
        } else {
            showToast(data.error || 'An error occurred', 'error');
        }
    })
    .catch(err => {
        showToast('An error occurred. Please try again.', 'error');
        console.error('Form submission error:', err);
    });
}

// ============================================
// Progress Bar Animation
// ============================================
function animateProgressBars() {
    const progressBars = document.querySelectorAll('.progress-bar[data-progress]');
    
    progressBars.forEach(bar => {
        const progress = bar.dataset.progress;
        bar.style.width = '0%';
        setTimeout(() => {
            bar.style.width = progress + '%';
        }, 100);
    });
}

function updateProgressUI() {
    const progressElements = document.querySelectorAll('[data-progress-update]');
    progressElements.forEach(el => {
        el.classList.add('progress-updated');
        setTimeout(() => el.classList.remove('progress-updated'), 1000);
    });
}

// ============================================
// Utility Functions
// ============================================
function getAntiForgeryToken() {
    const tokenInput = document.querySelector('input[name="__RequestVerificationToken"]');
    return tokenInput ? tokenInput.value : '';
}

function formatTime(seconds) {
    if (isNaN(seconds)) return '0:00';
    const mins = Math.floor(seconds / 60);
    const secs = Math.floor(seconds % 60);
    return `${mins}:${secs.toString().padStart(2, '0')}`;
}

function debounce(func, wait) {
    let timeout;
    return function executedFunction(...args) {
        const later = () => {
            clearTimeout(timeout);
            func(...args);
        };
        clearTimeout(timeout);
        timeout = setTimeout(later, wait);
    };
}

// ============================================
// Search Functionality
// ============================================
function initializeSearch() {
    const searchInputs = document.querySelectorAll('.search-input');
    
    searchInputs.forEach(input => {
        input.addEventListener('input', debounce(function() {
            const query = this.value.trim();
            const targetList = document.querySelector(this.dataset.target);
            
            if (targetList && query.length >= 2) {
                filterList(targetList, query);
            } else if (targetList) {
                showAllItems(targetList);
            }
        }, 300));
    });
}

function filterList(list, query) {
    const items = list.querySelectorAll('[data-searchable]');
    const lowerQuery = query.toLowerCase();
    
    items.forEach(item => {
        const text = item.dataset.searchable.toLowerCase();
        item.style.display = text.includes(lowerQuery) ? '' : 'none';
    });
}

function showAllItems(list) {
    const items = list.querySelectorAll('[data-searchable]');
    items.forEach(item => item.style.display = '');
}

// ============================================
// Smooth Scroll
// ============================================
function initializeSmoothScroll() {
    document.querySelectorAll('a[href^="#"]').forEach(anchor => {
        anchor.addEventListener('click', function(e) {
            e.preventDefault();
            const target = document.querySelector(this.getAttribute('href'));
            if (target) {
                target.scrollIntoView({ behavior: 'smooth', block: 'start' });
            }
        });
    });
}

// ============================================
// Initialize on Page Load
// ============================================
document.addEventListener('DOMContentLoaded', function() {
    // Initialize video players
    const videoPlayers = document.querySelectorAll('.video-player');
    videoPlayers.forEach(video => {
        const lessonId = video.dataset.lessonId;
        const enrollmentId = video.dataset.enrollmentId;
        if (lessonId && enrollmentId) {
            initializeVideoPlayer(video, lessonId, enrollmentId);
        }
    });
    
    // Initialize Bootstrap tooltips
    const tooltipTriggerList = [].slice.call(document.querySelectorAll('[data-bs-toggle="tooltip"]'));
    tooltipTriggerList.map(el => new bootstrap.Tooltip(el));
    
    // Initialize other components
    initializeFormValidation();
    initializeSearch();
    initializeSmoothScroll();
    animateProgressBars();
    
    // Add fade-in animation to cards
    document.querySelectorAll('.card, .stat-card').forEach((card, index) => {
        card.style.animationDelay = `${index * 0.1}s`;
        card.classList.add('fade-in-up');
    });
});

// ============================================
// CSS for Toast Notifications (injected)
// ============================================
const toastStyles = document.createElement('style');
toastStyles.textContent = `
#toast-container {
    position: fixed;
    top: 20px;
    right: 20px;
    z-index: 9999;
    display: flex;
    flex-direction: column;
    gap: 10px;
}

.toast-notification {
    display: flex;
    align-items: center;
    padding: 15px 20px;
    background: white;
    border-radius: 10px;
    box-shadow: 0 5px 20px rgba(0,0,0,0.15);
    transform: translateX(120%);
    transition: transform 0.3s ease;
    min-width: 300px;
}

.toast-notification.show {
    transform: translateX(0);
}

.toast-icon {
    margin-right: 15px;
    font-size: 1.2rem;
}

.toast-success .toast-icon { color: #27ae60; }
.toast-error .toast-icon { color: #e74c3c; }
.toast-warning .toast-icon { color: #f39c12; }
.toast-info .toast-icon { color: #3498db; }

.toast-message {
    flex: 1;
    font-size: 0.95rem;
}

.toast-close {
    background: none;
    border: none;
    color: #999;
    cursor: pointer;
    padding: 5px;
}

.toast-close:hover {
    color: #333;
}
`;
document.head.appendChild(toastStyles);


// ============================================
// Quiz AJAX Submission
// ============================================
function initializeQuizForm() {
    const quizForm = document.getElementById('quiz-form');
    if (!quizForm) return;
    
    quizForm.addEventListener('submit', function(e) {
        e.preventDefault();
        
        const submitBtn = quizForm.querySelector('button[type="submit"]');
        const originalText = submitBtn.innerHTML;
        submitBtn.innerHTML = '<i class="fas fa-spinner fa-spin me-2"></i>Submitting...';
        submitBtn.disabled = true;
        
        const formData = new FormData(quizForm);
        
        fetch(quizForm.action, {
            method: 'POST',
            body: formData,
            headers: {
                'RequestVerificationToken': getAntiForgeryToken()
            }
        })
        .then(response => response.json())
        .then(data => {
            if (data.success) {
                showQuizResults(data);
            } else {
                showToast(data.error || 'Failed to submit quiz', 'error');
                submitBtn.innerHTML = originalText;
                submitBtn.disabled = false;
            }
        })
        .catch(err => {
            console.error('Quiz submission error:', err);
            showToast('An error occurred. Please try again.', 'error');
            submitBtn.innerHTML = originalText;
            submitBtn.disabled = false;
        });
    });
    
    // Handle option selection
    const quizOptions = document.querySelectorAll('.quiz-option');
    quizOptions.forEach(option => {
        option.addEventListener('click', function() {
            const questionGroup = this.closest('.quiz-question');
            questionGroup.querySelectorAll('.quiz-option').forEach(opt => {
                opt.classList.remove('selected');
            });
            this.classList.add('selected');
            
            const radio = this.querySelector('input[type="radio"]');
            if (radio) radio.checked = true;
        });
    });
}

function showQuizResults(data) {
    const resultsContainer = document.getElementById('quiz-results');
    const quizContainer = document.getElementById('quiz-container');
    
    if (resultsContainer && quizContainer) {
        quizContainer.style.display = 'none';
        resultsContainer.style.display = 'block';
        
        resultsContainer.innerHTML = `
            <div class="text-center py-5">
                <div class="result-icon mb-4">
                    <i class="fas fa-${data.passed ? 'trophy' : 'redo'} fa-4x text-${data.passed ? 'warning' : 'secondary'}"></i>
                </div>
                <h2 class="mb-3">${data.passed ? 'Congratulations!' : 'Keep Trying!'}</h2>
                <p class="lead mb-4">Your Score: <strong>${data.score}%</strong></p>
                <div class="progress mb-4" style="height: 25px;">
                    <div class="progress-bar ${data.passed ? 'bg-success' : 'bg-warning'}" 
                         style="width: ${data.score}%">${data.score}%</div>
                </div>
                <p class="text-muted mb-4">
                    ${data.passed 
                        ? 'You have successfully passed this quiz!' 
                        : `You need ${data.passingScore}% to pass. You have ${data.remainingAttempts} attempts remaining.`}
                </p>
                <div class="d-flex justify-content-center gap-3">
                    ${data.passed 
                        ? `<a href="${data.nextLessonUrl || '/Dashboard'}" class="btn btn-primary">
                               <i class="fas fa-arrow-right me-2"></i>Continue
                           </a>`
                        : `<button onclick="location.reload()" class="btn btn-primary">
                               <i class="fas fa-redo me-2"></i>Try Again
                           </button>`}
                    <a href="/Dashboard" class="btn btn-outline-secondary">
                        <i class="fas fa-home me-2"></i>Dashboard
                    </a>
                </div>
            </div>
        `;
    }
}

// ============================================
// Real-time Form Validation
// ============================================
function initializeRealTimeValidation() {
    const forms = document.querySelectorAll('form[data-validate]');
    
    forms.forEach(form => {
        const inputs = form.querySelectorAll('input, textarea, select');
        
        inputs.forEach(input => {
            input.addEventListener('blur', function() {
                validateField(this);
            });
            
            input.addEventListener('input', debounce(function() {
                if (this.classList.contains('is-invalid')) {
                    validateField(this);
                }
            }, 300));
        });
    });
}

function validateField(field) {
    const value = field.value.trim();
    const type = field.type;
    const required = field.hasAttribute('required');
    let isValid = true;
    let errorMessage = '';
    
    // Required check
    if (required && !value) {
        isValid = false;
        errorMessage = 'This field is required';
    }
    
    // Email validation
    else if (type === 'email' && value) {
        const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
        if (!emailRegex.test(value)) {
            isValid = false;
            errorMessage = 'Please enter a valid email address';
        }
    }
    
    // Password validation
    else if (type === 'password' && value) {
        const minLength = parseInt(field.dataset.minLength) || 8;
        if (value.length < minLength) {
            isValid = false;
            errorMessage = `Password must be at least ${minLength} characters`;
        }
    }
    
    // Confirm password
    else if (field.dataset.match) {
        const matchField = document.querySelector(field.dataset.match);
        if (matchField && value !== matchField.value) {
            isValid = false;
            errorMessage = 'Passwords do not match';
        }
    }
    
    // Min/Max length
    else if (field.minLength > 0 && value.length < field.minLength) {
        isValid = false;
        errorMessage = `Minimum ${field.minLength} characters required`;
    }
    
    // Update UI
    updateFieldValidation(field, isValid, errorMessage);
    return isValid;
}

function updateFieldValidation(field, isValid, errorMessage) {
    const feedbackElement = field.nextElementSibling;
    
    field.classList.remove('is-valid', 'is-invalid');
    field.classList.add(isValid ? 'is-valid' : 'is-invalid');
    
    if (feedbackElement && feedbackElement.classList.contains('invalid-feedback')) {
        feedbackElement.textContent = errorMessage;
    }
}

// ============================================
// Loading States
// ============================================
function showLoading(element, text = 'Loading...') {
    const originalContent = element.innerHTML;
    element.dataset.originalContent = originalContent;
    element.innerHTML = `<i class="fas fa-spinner fa-spin me-2"></i>${text}`;
    element.disabled = true;
}

function hideLoading(element) {
    if (element.dataset.originalContent) {
        element.innerHTML = element.dataset.originalContent;
        delete element.dataset.originalContent;
    }
    element.disabled = false;
}

// ============================================
// Confirmation Dialogs
// ============================================
function confirmAction(message, callback) {
    const modal = document.createElement('div');
    modal.className = 'modal fade';
    modal.innerHTML = `
        <div class="modal-dialog modal-dialog-centered">
            <div class="modal-content">
                <div class="modal-header">
                    <h5 class="modal-title">Confirm Action</h5>
                    <button type="button" class="btn-close" data-bs-dismiss="modal"></button>
                </div>
                <div class="modal-body">
                    <p>${message}</p>
                </div>
                <div class="modal-footer">
                    <button type="button" class="btn btn-secondary" data-bs-dismiss="modal">Cancel</button>
                    <button type="button" class="btn btn-primary" id="confirm-btn">Confirm</button>
                </div>
            </div>
        </div>
    `;
    
    document.body.appendChild(modal);
    const bsModal = new bootstrap.Modal(modal);
    
    modal.querySelector('#confirm-btn').addEventListener('click', function() {
        bsModal.hide();
        if (callback) callback();
    });
    
    modal.addEventListener('hidden.bs.modal', function() {
        modal.remove();
    });
    
    bsModal.show();
}

// ============================================
// File Upload Preview
// ============================================
function initializeFileUpload() {
    const fileInputs = document.querySelectorAll('input[type="file"][data-preview]');
    
    fileInputs.forEach(input => {
        input.addEventListener('change', function() {
            const previewContainer = document.querySelector(this.dataset.preview);
            if (!previewContainer) return;
            
            previewContainer.innerHTML = '';
            
            Array.from(this.files).forEach(file => {
                const preview = document.createElement('div');
                preview.className = 'file-preview-item';
                
                if (file.type.startsWith('image/')) {
                    const reader = new FileReader();
                    reader.onload = function(e) {
                        preview.innerHTML = `
                            <img src="${e.target.result}" alt="${file.name}" class="img-thumbnail" style="max-height: 100px;">
                            <span class="file-name">${file.name}</span>
                        `;
                    };
                    reader.readAsDataURL(file);
                } else {
                    preview.innerHTML = `
                        <i class="fas fa-file fa-2x text-muted"></i>
                        <span class="file-name">${file.name}</span>
                        <span class="file-size">(${formatFileSize(file.size)})</span>
                    `;
                }
                
                previewContainer.appendChild(preview);
            });
        });
    });
}

function formatFileSize(bytes) {
    if (bytes === 0) return '0 Bytes';
    const k = 1024;
    const sizes = ['Bytes', 'KB', 'MB', 'GB'];
    const i = Math.floor(Math.log(bytes) / Math.log(k));
    return parseFloat((bytes / Math.pow(k, i)).toFixed(2)) + ' ' + sizes[i];
}

// ============================================
// Countdown Timer (for quizzes)
// ============================================
function initializeCountdown(elementId, seconds, onComplete) {
    const element = document.getElementById(elementId);
    if (!element) return;
    
    let remaining = seconds;
    
    const interval = setInterval(function() {
        remaining--;
        
        const mins = Math.floor(remaining / 60);
        const secs = remaining % 60;
        element.textContent = `${mins}:${secs.toString().padStart(2, '0')}`;
        
        if (remaining <= 60) {
            element.classList.add('text-danger');
        }
        
        if (remaining <= 0) {
            clearInterval(interval);
            if (onComplete) onComplete();
        }
    }, 1000);
    
    return interval;
}

// ============================================
// Initialize Additional Components
// ============================================
document.addEventListener('DOMContentLoaded', function() {
    initializeQuizForm();
    initializeRealTimeValidation();
    initializeFileUpload();
    
    // Initialize delete confirmations
    document.querySelectorAll('[data-confirm]').forEach(btn => {
        btn.addEventListener('click', function(e) {
            e.preventDefault();
            const message = this.dataset.confirm || 'Are you sure?';
            const href = this.href;
            
            confirmAction(message, function() {
                if (href) window.location.href = href;
            });
        });
    });
});
