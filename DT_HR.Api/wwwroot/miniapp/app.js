// Telegram Mini App for HR Check-In Location Verification

class HRCheckInApp {
    constructor() {
        this.tg = window.Telegram.WebApp;
        this.currentLanguage = 'en';
        this.isProcessing = false;
        
        this.init();
    }

    init() {
        // Initialize Telegram Web App
        this.tg.ready();
        this.tg.expand();
        
        // Set theme
        this.applyTelegramTheme();
        
        // Get language from Telegram user
        this.currentLanguage = this.tg.initDataUnsafe?.user?.language_code || 'en';
        
        // Set up localization
        this.updateTexts();
        
        // Set up event listeners
        this.setupEventListeners();
        
        // Check geolocation support
        this.checkGeolocationSupport();
    }

    applyTelegramTheme() {
        // Apply Telegram theme colors if available
        if (this.tg.themeParams) {
            document.documentElement.style.setProperty('--tg-theme-bg-color', this.tg.themeParams.bg_color);
            document.documentElement.style.setProperty('--tg-theme-text-color', this.tg.themeParams.text_color);
            document.documentElement.style.setProperty('--tg-theme-button-color', this.tg.themeParams.button_color);
            document.documentElement.style.setProperty('--tg-theme-button-text-color', this.tg.themeParams.button_text_color);
        }
    }

    updateTexts() {
        const texts = this.getLocalizedTexts();
        
        document.getElementById('app-title').textContent = texts.title;
        document.getElementById('app-subtitle').textContent = texts.subtitle;
        document.getElementById('status-text').textContent = texts.readyStatus;
        document.getElementById('btn-text').textContent = texts.verifyButton;
        document.getElementById('footer-text').textContent = texts.footer;
    }

    getLocalizedTexts() {
        const texts = {
            'en': {
                title: 'HR Check-In',
                subtitle: 'Verify your location to check in',
                readyStatus: 'Ready to verify location',
                verifyButton: 'Verify I\'m at Office',
                footer: 'Secure location verification',
                gettingLocation: 'Getting your location...',
                verifying: 'Verifying location...',
                success: 'Successfully checked in!',
                outsideOffice: 'You are not at the office location',
                locationDenied: 'Location access denied. Please enable location services.',
                locationUnavailable: 'Unable to get your location. Please try again.',
                networkError: 'Network error. Please check your connection.',
                unknownError: 'An unexpected error occurred. Please try again.'
            },
            'ru': {
                title: 'HR Чек-ин',
                subtitle: 'Подтвердите местоположение для входа',
                readyStatus: 'Готов к проверке местоположения',
                verifyButton: 'Подтвердить нахождение в офисе',
                footer: 'Безопасная проверка местоположения',
                gettingLocation: 'Получение вашего местоположения...',
                verifying: 'Проверка местоположения...',
                success: 'Успешно зарегистрированы!',
                outsideOffice: 'Вы находитесь не в офисе',
                locationDenied: 'Доступ к местоположению запрещен. Пожалуйста, включите службы геолокации.',
                locationUnavailable: 'Не удается получить ваше местоположение. Попробуйте еще раз.',
                networkError: 'Ошибка сети. Проверьте соединение.',
                unknownError: 'Произошла непредвиденная ошибка. Попробуйте еще раз.'
            },
            'uz': {
                title: 'HR Kirish',
                subtitle: 'Kirish uchun joylashuvingizni tasdiqlang',
                readyStatus: 'Joylashuvni tekshirishga tayyor',
                verifyButton: 'Ofisda ekanligimni tasdiqlash',
                footer: 'Xavfsiz joylashuv tekshiruvi',
                gettingLocation: 'Joylashuvingiz aniqlanmoqda...',
                verifying: 'Joylashuv tekshirilmoqda...',
                success: 'Muvaffaqiyatli ro\'yxatdan o\'tdingiz!',
                outsideOffice: 'Siz ofis joylashuvida emassiz',
                locationDenied: 'Joylashuv ruxsati rad etildi. Geolokatsiya xizmatlarini yoqing.',
                locationUnavailable: 'Joylashuvingizni aniqlab bo\'lmadi. Qayta urinib ko\'ring.',
                networkError: 'Tarmoq xatosi. Internetni tekshiring.',
                unknownError: 'Kutilmagan xato yuz berdi. Qayta urinib ko\'ring.'
            }
        };

        return texts[this.currentLanguage] || texts['en'];
    }

    setupEventListeners() {
        const verifyButton = document.getElementById('verify-button');
        verifyButton.addEventListener('click', () => this.handleVerifyLocation());
    }

    checkGeolocationSupport() {
        if (!navigator.geolocation) {
            this.showError('Geolocation is not supported by this browser.');
            return false;
        }
        return true;
    }

    async handleVerifyLocation() {
        if (this.isProcessing) return;
        
        this.isProcessing = true;
        this.setLoadingState('gettingLocation');

        try {
            const position = await this.getCurrentPosition();
            await this.verifyLocationWithServer(position.coords.latitude, position.coords.longitude);
        } catch (error) {
            this.handleLocationError(error);
        } finally {
            this.isProcessing = false;
        }
    }

    getCurrentPosition() {
        return new Promise((resolve, reject) => {
            const options = {
                enableHighAccuracy: true,
                timeout: 10000,
                maximumAge: 0
            };

            navigator.geolocation.getCurrentPosition(resolve, reject, options);
        });
    }

    async verifyLocationWithServer(latitude, longitude) {
        const texts = this.getLocalizedTexts();
        this.setLoadingState('verifying');

        try {
            const response = await fetch('/api/miniapp/checkin', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'X-Telegram-Init-Data': this.tg.initData
                },
                body: JSON.stringify({
                    latitude: latitude,
                    longitude: longitude,
                    timestamp: Date.now()
                })
            });

            const result = await response.json();

            if (response.ok && result.success) {
                this.showSuccess(result.message || texts.success);
                
                // Notify Telegram that the task is complete
                this.tg.showAlert(texts.success, () => {
                    this.tg.close();
                });
            } else {
                this.showError(result.message || texts.outsideOffice);
            }
        } catch (error) {
            console.error('Server verification error:', error);
            this.showError(texts.networkError);
        }
    }

    handleLocationError(error) {
        const texts = this.getLocalizedTexts();
        
        switch (error.code) {
            case error.PERMISSION_DENIED:
                this.showError(texts.locationDenied);
                break;
            case error.POSITION_UNAVAILABLE:
                this.showError(texts.locationUnavailable);
                break;
            case error.TIMEOUT:
                this.showError(texts.locationUnavailable);
                break;
            default:
                this.showError(texts.unknownError);
                break;
        }
    }

    setLoadingState(type) {
        const texts = this.getLocalizedTexts();
        const statusDisplay = document.getElementById('status-display');
        const statusText = document.getElementById('status-text');
        const verifyButton = document.getElementById('verify-button');

        // Hide messages
        this.hideMessages();

        // Update status
        statusDisplay.className = 'status-loading loading';
        statusText.textContent = type === 'gettingLocation' ? texts.gettingLocation : texts.verifying;

        // Disable button
        verifyButton.disabled = true;
        
        // Update status icon to loading spinner
        const statusIcon = statusDisplay.querySelector('.status-icon');
        statusIcon.textContent = '🔄';
    }

    showSuccess(message) {
        const statusDisplay = document.getElementById('status-display');
        const statusText = document.getElementById('status-text');
        const successMessage = document.getElementById('success-message');
        const successText = document.getElementById('success-text');
        const verifyButton = document.getElementById('verify-button');

        // Update status
        statusDisplay.className = 'status-success';
        statusText.textContent = 'Verified!';
        
        // Update status icon
        const statusIcon = statusDisplay.querySelector('.status-icon');
        statusIcon.textContent = '✅';

        // Show success message
        successText.textContent = message;
        successMessage.classList.remove('hidden');

        // Hide button
        verifyButton.style.display = 'none';
    }

    showError(message) {
        const texts = this.getLocalizedTexts();
        const statusDisplay = document.getElementById('status-display');
        const statusText = document.getElementById('status-text');
        const errorMessage = document.getElementById('error-message');
        const errorText = document.getElementById('error-text');
        const verifyButton = document.getElementById('verify-button');

        // Reset status
        statusDisplay.className = 'status-waiting';
        statusText.textContent = texts.readyStatus;
        
        // Reset status icon
        const statusIcon = statusDisplay.querySelector('.status-icon');
        statusIcon.textContent = '📍';

        // Show error message
        errorText.textContent = message;
        errorMessage.classList.remove('hidden');

        // Re-enable button
        verifyButton.disabled = false;

        // Auto-hide error after 5 seconds
        setTimeout(() => {
            errorMessage.classList.add('hidden');
        }, 5000);
    }

    hideMessages() {
        document.getElementById('error-message').classList.add('hidden');
        document.getElementById('success-message').classList.add('hidden');
    }
}

// Initialize the app when the page loads
document.addEventListener('DOMContentLoaded', () => {
    new HRCheckInApp();
});