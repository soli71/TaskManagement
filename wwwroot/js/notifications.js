/**
 * Push Notification System for TaskManagement
 * Supports Persian/Farsi messages with RTL layout
 */

class TaskNotificationManager {
    constructor() {
        this.notifications = [];
        this.maxNotifications = 5;
        this.defaultDuration = 5000;
        this.container = null;
        this.init();
    }

    init() {
        this.createContainer();
        this.setupStyles();
        this.requestPermission();
    }

    createContainer() {
        // Remove existing container if any
        const existing = document.getElementById('notification-container');
        if (existing) {
            existing.remove();
        }

        this.container = document.createElement('div');
        this.container.id = 'notification-container';
        this.container.className = 'notification-container';
        document.body.appendChild(this.container);
    }

    setupStyles() {
        if (document.getElementById('notification-styles')) return;

        const style = document.createElement('style');
        style.id = 'notification-styles';
        style.textContent = `
            .notification-container {
                position: fixed;
                top: 20px;
                right: 20px;
                z-index: 10000;
                max-width: 400px;
                pointer-events: none;
                direction: rtl;
            }

            .notification {
                background: var(--mz-surface, #ffffff);
                border: 1px solid var(--mz-border, #e2e8f0);
                border-radius: var(--mz-radius, 16px);
                box-shadow: var(--mz-shadow-lg, 0 20px 40px rgba(15, 23, 42, 0.12));
                margin-bottom: 12px;
                padding: 16px 20px;
                min-height: 80px;
                display: flex;
                align-items: center;
                gap: 12px;
                pointer-events: auto;
                transform: translateX(450px);
                opacity: 0;
                transition: all 0.3s cubic-bezier(0.4, 0, 0.2, 1);
                font-family: 'Vazirmatn', -apple-system, BlinkMacSystemFont, 'Segoe UI', sans-serif;
                direction: rtl;
                text-align: right;
            }

            .notification.show {
                transform: translateX(0);
                opacity: 1;
            }

            .notification.hide {
                transform: translateX(450px);
                opacity: 0;
                margin-bottom: 0;
                padding-top: 0;
                padding-bottom: 0;
                min-height: 0;
            }

            .notification-icon {
                flex-shrink: 0;
                width: 24px;
                height: 24px;
                border-radius: 50%;
                display: flex;
                align-items: center;
                justify-content: center;
                font-weight: bold;
                font-size: 14px;
                color: white;
            }

            .notification-content {
                flex: 1;
                min-width: 0;
            }

            .notification-title {
                font-weight: 600;
                font-size: 14px;
                color: var(--mz-text, #1e293b);
                margin-bottom: 4px;
                line-height: 1.4;
            }

            .notification-message {
                font-size: 13px;
                color: var(--mz-muted, #64748b);
                line-height: 1.4;
                word-wrap: break-word;
            }

            .notification-close {
                flex-shrink: 0;
                width: 20px;
                height: 20px;
                border: none;
                background: none;
                cursor: pointer;
                color: var(--mz-muted, #64748b);
                font-size: 16px;
                display: flex;
                align-items: center;
                justify-content: center;
                border-radius: 4px;
                transition: background-color 0.2s;
            }

            .notification-close:hover {
                background: var(--mz-border, #e2e8f0);
                color: var(--mz-text, #1e293b);
            }

            .notification-progress {
                position: absolute;
                bottom: 0;
                left: 0;
                right: 0;
                height: 3px;
                background: rgba(255, 255, 255, 0.2);
                border-radius: 0 0 var(--mz-radius, 16px) var(--mz-radius, 16px);
                overflow: hidden;
            }

            .notification-progress-bar {
                height: 100%;
                background: currentColor;
                transform-origin: left;
                transition: transform linear;
            }

            /* Type-specific styles */
            .notification.error {
                border-right: 4px solid var(--mz-danger, #ef4444);
            }
            .notification.error .notification-icon {
                background: var(--mz-danger, #ef4444);
            }
            .notification.error .notification-progress-bar {
                color: var(--mz-danger, #ef4444);
            }

            .notification.warning {
                border-right: 4px solid var(--mz-warning, #f59e0b);
            }
            .notification.warning .notification-icon {
                background: var(--mz-warning, #f59e0b);
            }
            .notification.warning .notification-progress-bar {
                color: var(--mz-warning, #f59e0b);
            }

            .notification.success {
                border-right: 4px solid var(--mz-success, #10b981);
            }
            .notification.success .notification-icon {
                background: var(--mz-success, #10b981);
            }
            .notification.success .notification-progress-bar {
                color: var(--mz-success, #10b981);
            }

            .notification.info {
                border-right: 4px solid var(--mz-primary, #6366f1);
            }
            .notification.info .notification-icon {
                background: var(--mz-primary, #6366f1);
            }
            .notification.info .notification-progress-bar {
                color: var(--mz-primary, #6366f1);
            }

            @media (max-width: 480px) {
                .notification-container {
                    left: 10px;
                    right: 10px;
                    max-width: none;
                }
                
                .notification {
                    transform: translateY(-100px);
                }
                
                .notification.show {
                    transform: translateY(0);
                }
                
                .notification.hide {
                    transform: translateY(-100px);
                }
            }

            /* Dark theme support */
            html[data-theme="dark"] .notification {
                background: var(--mz-surface, #1e293b);
                border-color: var(--mz-border, #334155);
            }
        `;
        document.head.appendChild(style);
    }

    async requestPermission() {
        if ('Notification' in window && Notification.permission === 'default') {
            await Notification.requestPermission();
        }
    }

    show(options) {
        const {
            type = 'info',
            title = '',
            message = '',
            duration = this.defaultDuration,
            persistent = false,
            actions = []
        } = options;

        // Remove oldest notification if we've reached the limit
        if (this.notifications.length >= this.maxNotifications) {
            this.remove(this.notifications[0]);
        }

        const notification = this.createNotificationElement({
            type,
            title,
            message,
            duration,
            persistent,
            actions
        });

        this.notifications.push(notification);
        this.container.appendChild(notification.element);

        // Trigger animation
        setTimeout(() => {
            notification.element.classList.add('show');
        }, 10);

        // Auto-remove if not persistent
        if (!persistent && duration > 0) {
            notification.timer = setTimeout(() => {
                this.remove(notification);
            }, duration);

            // Add progress bar
            this.addProgressBar(notification, duration);
        }

        // Show browser notification if permission granted
        this.showBrowserNotification({ type, title, message });

        return notification;
    }

    createNotificationElement({ type, title, message, persistent, actions }) {
        const element = document.createElement('div');
        element.className = `notification ${type}`;

        const iconSymbols = {
            error: '✕',
            warning: '⚠',
            success: '✓',
            info: 'ℹ'
        };

        element.innerHTML = `
            <div class="notification-icon">${iconSymbols[type] || 'ℹ'}</div>
            <div class="notification-content">
                ${title ? `<div class="notification-title">${this.escapeHtml(title)}</div>` : ''}
                <div class="notification-message">${this.escapeHtml(message)}</div>
            </div>
            <button class="notification-close" aria-label="بستن اعلان">×</button>
            ${!persistent ? '<div class="notification-progress"><div class="notification-progress-bar"></div></div>' : ''}
        `;

        const notification = {
            element,
            type,
            title,
            message,
            timer: null
        };

        // Add close button event
        element.querySelector('.notification-close').addEventListener('click', () => {
            this.remove(notification);
        });

        // Add click event for actions
        if (actions.length > 0) {
            element.addEventListener('click', (e) => {
                if (e.target.classList.contains('notification-close')) return;
                actions[0].handler();
                this.remove(notification);
            });
            element.style.cursor = 'pointer';
        }

        return notification;
    }

    addProgressBar(notification, duration) {
        const progressBar = notification.element.querySelector('.notification-progress-bar');
        if (progressBar) {
            progressBar.style.transition = `transform ${duration}ms linear`;
            setTimeout(() => {
                progressBar.style.transform = 'scaleX(0)';
            }, 10);
        }
    }

    remove(notification) {
        if (!notification || !notification.element) return;

        const index = this.notifications.indexOf(notification);
        if (index > -1) {
            this.notifications.splice(index, 1);
        }

        if (notification.timer) {
            clearTimeout(notification.timer);
        }

        notification.element.classList.add('hide');
        setTimeout(() => {
            if (notification.element.parentNode) {
                notification.element.parentNode.removeChild(notification.element);
            }
        }, 300);
    }

    showBrowserNotification({ type, title, message }) {
        if ('Notification' in window && Notification.permission === 'granted') {
            const notification = new Notification(title || 'سیستم مدیریت وظایف', {
                body: message,
                icon: '/favicon.ico',
                tag: 'taskmanagement-' + Date.now()
            });

            notification.onclick = () => {
                window.focus();
                notification.close();
            };

            setTimeout(() => notification.close(), 5000);
        }
    }

    escapeHtml(text) {
        const div = document.createElement('div');
        div.textContent = text;
        return div.innerHTML;
    }

    // Convenience methods
    error(title, message, options = {}) {
        return this.show({ type: 'error', title, message, ...options });
    }

    warning(title, message, options = {}) {
        return this.show({ type: 'warning', title, message, ...options });
    }

    success(title, message, options = {}) {
        return this.show({ type: 'success', title, message, ...options });
    }

    info(title, message, options = {}) {
        return this.show({ type: 'info', title, message, ...options });
    }

    clear() {
        this.notifications.forEach(notification => this.remove(notification));
    }

    // Error-specific methods with Persian messages
    showAuthError(message = 'خطای احراز هویت رخ داده است') {
        return this.error('خطای احراز هویت', message, { persistent: true });
    }

    showValidationError(message = 'لطفاً اطلاعات وارد شده را بررسی کنید') {
        return this.error('خطای اعتبارسنجی', message);
    }

    showServerError(message = 'خطا در ارتباط با سرور') {
        return this.error('خطای سرور', message, { persistent: true });
    }

    showPermissionError(message = 'شما مجوز لازم برای این عملیات را ندارید') {
        return this.error('خطای دسترسی', message);
    }

    showNetworkError(message = 'لطفاً اتصال اینترنت خود را بررسی کنید') {
        return this.error('خطای شبکه', message, { persistent: true });
    }
}

// Global instance
window.TaskNotifications = new TaskNotificationManager();

// jQuery integration for easier use
if (typeof $ !== 'undefined') {
    $.extend({
        notify: function(options) {
            return window.TaskNotifications.show(options);
        },
        notifyError: function(title, message, options) {
            return window.TaskNotifications.error(title, message, options);
        },
        notifySuccess: function(title, message, options) {
            return window.TaskNotifications.success(title, message, options);
        },
        notifyWarning: function(title, message, options) {
            return window.TaskNotifications.warning(title, message, options);
        },
        notifyInfo: function(title, message, options) {
            return window.TaskNotifications.info(title, message, options);
        }
    });
}

// Global error handler
window.addEventListener('error', function(event) {
    window.TaskNotifications.showServerError(`خطای JavaScript: ${event.message}`);
});

// Unhandled promise rejection handler
window.addEventListener('unhandledrejection', function(event) {
    window.TaskNotifications.showServerError(`خطای Promise: ${event.reason}`);
});
