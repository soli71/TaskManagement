// SignalR Real-time Notification Manager
class SignalRNotificationManager {
    constructor() {
        this.connection = null;
        this.isConnected = false;
        this.fallbackManager = window.TaskNotifications;
        this.reconnectAttempts = 0;
        this.maxReconnectAttempts = 5;
        this.notificationHistory = [];
        
        // Initialize connection
        this.initializeConnection();
    }

    async initializeConnection() {
        try {
            // Create SignalR connection
            this.connection = new signalR.HubConnectionBuilder()
                .withUrl("/notificationHub")
                .withAutomaticReconnect([0, 2000, 10000, 30000])
                .configureLogging(signalR.LogLevel.Information)
                .build();

            // Setup event handlers
            this.setupEventHandlers();

            // Start connection
            await this.startConnection();
        } catch (error) {
            console.error('SignalR initialization failed:', error);
            this.fallbackToSession();
        }
    }

    setupEventHandlers() {
        // Receive real-time notifications
        this.connection.on("ReceiveNotification", (notification) => {
            console.log('Received SignalR notification:', notification);
            this.displayNotification(notification);
            this.addToHistory(notification);
        });

        // Connection state handlers
        this.connection.onclose(async () => {
            console.log('SignalR connection closed');
            this.isConnected = false;
            await this.attemptReconnect();
        });

        this.connection.onreconnecting(() => {
            console.log('SignalR reconnecting...');
            this.isConnected = false;
        });

        this.connection.onreconnected(() => {
            console.log('SignalR reconnected');
            this.isConnected = true;
            this.reconnectAttempts = 0;
            this.joinUserGroup();
        });
    }

    async startConnection() {
        try {
            await this.connection.start();
            console.log('SignalR connected successfully');
            this.isConnected = true;
            this.reconnectAttempts = 0;
            
            // Join user group for targeted notifications
            await this.joinUserGroup();
            
            // Load pending notifications
            await this.loadPendingNotifications();
        } catch (error) {
            console.error('SignalR connection failed:', error);
            this.fallbackToSession();
        }
    }

    async joinUserGroup() {
        if (this.isConnected) {
            try {
                await this.connection.invoke("JoinUserGroup");
                console.log('Joined user notification group');
            } catch (error) {
                console.error('Failed to join user group:', error);
            }
        }
    }

    async attemptReconnect() {
        if (this.reconnectAttempts < this.maxReconnectAttempts) {
            this.reconnectAttempts++;
            const delay = Math.min(1000 * Math.pow(2, this.reconnectAttempts), 30000);
            
            console.log(`Attempting to reconnect in ${delay}ms... (attempt ${this.reconnectAttempts})`);
            
            setTimeout(async () => {
                try {
                    await this.startConnection();
                } catch (error) {
                    console.error('Reconnection attempt failed:', error);
                    await this.attemptReconnect();
                }
            }, delay);
        } else {
            console.log('Max reconnection attempts reached. Falling back to session-based notifications.');
            this.fallbackToSession();
        }
    }

    fallbackToSession() {
        console.log('Using session-based notification fallback');
        if (this.fallbackManager) {
            // Delegate to session-based manager
            window.TaskNotifications = this.fallbackManager;
        }
    }

    displayNotification(notification) {
        // Use existing notification display logic
        if (this.fallbackManager) {
            this.fallbackManager.show(
                notification.type || 'info',
                notification.title || '',
                notification.message || '',
                {
                    persistent: notification.persistent || false,
                    duration: notification.duration || 5000,
                    actionUrl: notification.actionUrl,
                    actionText: notification.actionText
                }
            );
        }
    }

    addToHistory(notification) {
        notification.receivedAt = new Date();
        this.notificationHistory.unshift(notification);
        
        // Keep only last 100 notifications in memory
        if (this.notificationHistory.length > 100) {
            this.notificationHistory = this.notificationHistory.slice(0, 100);
        }

        // Update notification badge/counter
        this.updateNotificationCounter();
    }

    updateNotificationCounter() {
        const unreadCount = this.notificationHistory.filter(n => !n.isRead).length;
        
        // Update badge in UI
        const badge = document.querySelector('.notification-badge');
        if (badge) {
            badge.textContent = unreadCount > 0 ? unreadCount : '';
            badge.style.display = unreadCount > 0 ? 'block' : 'none';
        }

        // Update page title
        if (unreadCount > 0) {
            document.title = `(${unreadCount}) ${document.title.replace(/^\(\d+\)\s*/, '')}`;
        } else {
            document.title = document.title.replace(/^\(\d+\)\s*/, '');
        }
    }

    async loadPendingNotifications() {
        try {
            const response = await fetch('/api/notifications/pending');
            if (response.ok) {
                const notifications = await response.json();
                notifications.forEach(notification => {
                    this.displayNotification(notification);
                    this.addToHistory(notification);
                });
            }
        } catch (error) {
            console.error('Failed to load pending notifications:', error);
        }
    }

    async markAsRead(notificationId) {
        // Mark locally
        const notification = this.notificationHistory.find(n => n.id === notificationId);
        if (notification) {
            notification.isRead = true;
            notification.readAt = new Date();
            this.updateNotificationCounter();
        }

        // Mark on server
        try {
            await fetch(`/api/notifications/${notificationId}/read`, { method: 'POST' });
        } catch (error) {
            console.error('Failed to mark notification as read:', error);
        }
    }

    async markAllAsRead() {
        // Mark all locally
        this.notificationHistory.forEach(n => {
            n.isRead = true;
            n.readAt = new Date();
        });
        this.updateNotificationCounter();

        // Mark all on server
        try {
            await fetch('/api/notifications/mark-all-read', { method: 'POST' });
        } catch (error) {
            console.error('Failed to mark all notifications as read:', error);
        }
    }

    async clearAll() {
        this.notificationHistory = [];
        this.updateNotificationCounter();

        try {
            await fetch('/api/notifications/clear', { method: 'DELETE' });
        } catch (error) {
            console.error('Failed to clear notifications:', error);
        }
    }

    getNotificationHistory(limit = 50) {
        return this.notificationHistory.slice(0, limit);
    }

    getUnreadNotifications() {
        return this.notificationHistory.filter(n => !n.isRead);
    }

    // API methods for manual notification sending
    async sendToUser(userId, type, title, message, options = {}) {
        try {
            await fetch('/api/notifications/send-to-user', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({
                    userId,
                    type,
                    title,
                    message,
                    ...options
                })
            });
        } catch (error) {
            console.error('Failed to send notification to user:', error);
        }
    }

    async sendToCompany(companyId, type, title, message, options = {}) {
        try {
            await fetch('/api/notifications/send-to-company', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({
                    companyId,
                    type,
                    title,
                    message,
                    ...options
                })
            });
        } catch (error) {
            console.error('Failed to send notification to company:', error);
        }
    }

    async sendToRole(role, type, title, message, options = {}) {
        try {
            await fetch('/api/notifications/send-to-role', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({
                    role,
                    type,
                    title,
                    message,
                    ...options
                })
            });
        } catch (error) {
            console.error('Failed to send notification to role:', error);
        }
    }

    // Test methods
    testConnection() {
        return this.isConnected;
    }

    async testNotification() {
        if (this.isConnected) {
            try {
                await fetch('/api/notifications/test', { method: 'POST' });
            } catch (error) {
                console.error('Failed to send test notification:', error);
            }
        } else {
            console.log('SignalR not connected, using fallback test');
            this.fallbackManager?.show('info', 'Test Notification', 'SignalR connection test (fallback mode)');
        }
    }
}

// Enhanced notification helpers for SignalR
window.SignalRNotificationHelpers = {
    // Initialize SignalR notifications on page load
    initialize() {
        // Wait for SignalR library to load
        if (typeof signalR !== 'undefined') {
            window.TaskNotifications = new SignalRNotificationManager();
        } else {
            console.warn('SignalR library not loaded, using session-based notifications');
        }
    },

    // Show notification panel/modal
    showNotificationPanel() {
        const notifications = window.TaskNotifications?.getNotificationHistory() || [];
        
        let html = `
            <div class="notification-panel" style="position: fixed; top: 60px; right: 20px; width: 400px; max-height: 500px; background: white; border: 1px solid #ddd; border-radius: 8px; box-shadow: 0 4px 12px rgba(0,0,0,0.15); z-index: 1000; overflow-y: auto;">
                <div class="notification-panel-header" style="padding: 15px; border-bottom: 1px solid #eee; background: #f8f9fa;">
                    <h5 style="margin: 0; display: flex; justify-content: space-between; align-items: center;">
                        <span>اعلان‌ها</span>
                        <div>
                            <button onclick="window.TaskNotifications.markAllAsRead()" class="btn btn-sm btn-outline-primary" style="margin-left: 5px;">همه خوانده شد</button>
                            <button onclick="window.TaskNotifications.clearAll()" class="btn btn-sm btn-outline-danger" style="margin-left: 5px;">پاک کردن</button>
                            <button onclick="this.closest('.notification-panel').remove()" class="btn btn-sm btn-outline-secondary">بستن</button>
                        </div>
                    </h5>
                </div>
                <div class="notification-panel-body" style="padding: 10px;">
        `;

        if (notifications.length === 0) {
            html += '<p style="text-align: center; color: #666; padding: 20px;">اعلانی وجود ندارد</p>';
        } else {
            notifications.forEach(notification => {
                const isUnread = !notification.isRead;
                html += `
                    <div class="notification-item" style="padding: 10px; border-bottom: 1px solid #eee; ${isUnread ? 'background: #f0f8ff;' : ''}" data-id="${notification.id}">
                        <div style="display: flex; justify-content: space-between; align-items: start;">
                            <div style="flex: 1;">
                                <h6 style="margin: 0 0 5px 0; color: ${this.getTypeColor(notification.type)};">${notification.title}</h6>
                                <p style="margin: 0 0 5px 0; font-size: 0.9em; color: #666;">${notification.message}</p>
                                <small style="color: #999;">${this.formatDate(notification.receivedAt || notification.createdAt)}</small>
                            </div>
                            ${isUnread ? '<span style="width: 8px; height: 8px; background: #007bff; border-radius: 50%; margin-right: 10px; margin-top: 5px;"></span>' : ''}
                        </div>
                        ${notification.actionUrl ? `<a href="${notification.actionUrl}" class="btn btn-sm btn-outline-primary" style="margin-top: 10px;">${notification.actionText || 'مشاهده'}</a>` : ''}
                    </div>
                `;
            });
        }

        html += `
                </div>
            </div>
        `;

        // Remove existing panel
        document.querySelector('.notification-panel')?.remove();
        
        // Add new panel
        document.body.insertAdjacentHTML('beforeend', html);

        // Mark notifications as read when clicked
        document.querySelectorAll('.notification-item').forEach(item => {
            item.addEventListener('click', () => {
                const id = item.dataset.id;
                if (id) {
                    window.TaskNotifications?.markAsRead(id);
                    item.style.background = '';
                    item.querySelector('span[style*="background: #007bff"]')?.remove();
                }
            });
        });
    },

    getTypeColor(type) {
        const colors = {
            success: '#28a745',
            error: '#dc3545',
            warning: '#ffc107',
            info: '#17a2b8'
        };
        return colors[type] || '#6c757d';
    },

    formatDate(dateString) {
        if (!dateString) return '';
        const date = new Date(dateString);
        const now = new Date();
        const diffMs = now - date;
        const diffMins = Math.floor(diffMs / 60000);
        const diffHours = Math.floor(diffMins / 60);
        const diffDays = Math.floor(diffHours / 24);

        if (diffMins < 1) return 'همین الان';
        if (diffMins < 60) return `${diffMins} دقیقه پیش`;
        if (diffHours < 24) return `${diffHours} ساعت پیش`;
        if (diffDays < 7) return `${diffDays} روز پیش`;
        
        return date.toLocaleDateString('fa-IR');
    }
};

// Auto-initialize when DOM is ready
document.addEventListener('DOMContentLoaded', () => {
    window.SignalRNotificationHelpers.initialize();
});

// Export for global access
window.SignalRNotificationManager = SignalRNotificationManager;
