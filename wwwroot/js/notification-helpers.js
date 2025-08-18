/**
 * Task Management Notification Helpers
 * Common notification patterns for the TaskManagement application
 */

window.TaskNotificationHelpers = {
    // Form validation helpers
    showFormErrors: function(errors) {
        if (Array.isArray(errors)) {
            errors.forEach(error => {
                TaskNotifications.error('خطای اعتبارسنجی', error);
            });
        } else if (typeof errors === 'object') {
            Object.keys(errors).forEach(field => {
                const fieldErrors = Array.isArray(errors[field]) ? errors[field] : [errors[field]];
                fieldErrors.forEach(error => {
                    TaskNotifications.error(`خطا در ${field}`, error);
                });
            });
        } else {
            TaskNotifications.showValidationError(errors);
        }
    },

    // AJAX helpers
    handleAjaxError: function(xhr, status, error) {
        let message = 'خطای ناشناخته رخ داده است';
        
        if (xhr.status === 401) {
            TaskNotifications.showAuthError('لطفاً مجدداً وارد سیستم شوید');
            // Redirect to login after delay
            setTimeout(() => {
                window.location.href = '/Account/Login';
            }, 3000);
            return;
        } else if (xhr.status === 403) {
            TaskNotifications.showPermissionError('شما مجوز انجام این عملیات را ندارید');
            return;
        } else if (xhr.status === 404) {
            message = 'منبع درخواستی یافت نشد';
        } else if (xhr.status === 422) {
            // Validation errors
            try {
                const response = JSON.parse(xhr.responseText);
                if (response.errors) {
                    this.showFormErrors(response.errors);
                    return;
                }
                message = response.message || 'داده‌های ورودی نامعتبر است';
            } catch (e) {
                message = 'داده‌های ورودی نامعتبر است';
            }
        } else if (xhr.status >= 500) {
            TaskNotifications.showServerError('خطا در سرور، لطفاً مجدداً تلاش کنید');
            return;
        } else if (xhr.status === 0) {
            TaskNotifications.showNetworkError('لطفاً اتصال اینترنت خود را بررسی کنید');
            return;
        }

        TaskNotifications.error('خطا', message);
    },

    // Task-specific notifications
    taskCreated: function(taskTitle) {
        TaskNotifications.success('تسک ایجاد شد', `تسک "${taskTitle}" با موفقیت ایجاد شد`);
    },

    taskUpdated: function(taskTitle) {
        TaskNotifications.success('تسک به‌روزرسانی شد', `تسک "${taskTitle}" با موفقیت به‌روزرسانی شد`);
    },

    taskDeleted: function(taskTitle) {
        TaskNotifications.success('تسک حذف شد', `تسک "${taskTitle}" با موفقیت حذف شد`);
    },

    taskAssigned: function(taskTitle, assigneeName) {
        TaskNotifications.info('تسک تخصیص داده شد', `تسک "${taskTitle}" به ${assigneeName} تخصیص داده شد`);
    },

    taskCompleted: function(taskTitle) {
        TaskNotifications.success('تسک تکمیل شد', `تسک "${taskTitle}" با موفقیت تکمیل شد! 🎉`);
    },

    taskDeadlineApproaching: function(taskTitle, daysLeft) {
        TaskNotifications.warning('مهلت نزدیک است', `مهلت تسک "${taskTitle}" ${daysLeft} روز دیگر به پایان می‌رسد`);
    },

    taskOverdue: function(taskTitle) {
        TaskNotifications.error('تسک دیرکرد', `مهلت تسک "${taskTitle}" گذشته است`, { persistent: true });
    },

    // Project-specific notifications
    projectCreated: function(projectName) {
        TaskNotifications.success('پروژه ایجاد شد', `پروژه "${projectName}" با موفقیت ایجاد شد`);
    },

    projectCompleted: function(projectName) {
        TaskNotifications.success('پروژه تکمیل شد', `پروژه "${projectName}" با موفقیت تکمیل شد! 🚀`, { persistent: true });
    },

    projectMemberAdded: function(projectName, memberName) {
        TaskNotifications.info('عضو جدید', `${memberName} به پروژه "${projectName}" اضافه شد`);
    },

    // User-specific notifications
    userInvited: function(email) {
        TaskNotifications.success('دعوت‌نامه ارسال شد', `دعوت‌نامه به ${email} ارسال شد`);
    },

    userJoined: function(userName) {
        TaskNotifications.info('عضو جدید', `${userName} به تیم پیوست! 👋`);
    },

    // File-specific notifications
    fileUploaded: function(fileName) {
        TaskNotifications.success('فایل آپلود شد', `فایل "${fileName}" با موفقیت آپلود شد`);
    },

    fileUploadFailed: function(fileName, reason) {
        TaskNotifications.error('خطا در آپلود', `آپلود فایل "${fileName}" ناموفق: ${reason}`);
    },

    // Comment notifications
    commentAdded: function(taskTitle) {
        TaskNotifications.info('نظر جدید', `نظر جدیدی به تسک "${taskTitle}" اضافه شد`);
    },

    // Progress notifications
    showProgress: function(title, message) {
        return TaskNotifications.info(title, message, { persistent: true });
    },

    updateProgress: function(notification, newMessage) {
        if (notification && notification.element) {
            const messageElement = notification.element.querySelector('.notification-message');
            if (messageElement) {
                messageElement.textContent = newMessage;
            }
        }
    },

    hideProgress: function(notification) {
        if (notification) {
            TaskNotifications.remove(notification);
        }
    },

    // Bulk operations
    bulkOperationStart: function(operationType, count) {
        return this.showProgress('در حال پردازش...', `${operationType} ${count} مورد در حال انجام است...`);
    },

    bulkOperationComplete: function(notification, operationType, successCount, failCount) {
        this.hideProgress(notification);
        
        if (failCount === 0) {
            TaskNotifications.success('عملیات تکمیل شد', `${operationType} ${successCount} مورد با موفقیت انجام شد`);
        } else {
            TaskNotifications.warning('عملیات تکمیل شد', `${successCount} مورد موفق، ${failCount} مورد ناموفق`);
        }
    },

    // Connectivity notifications
    connectionLost: function() {
        return TaskNotifications.error('اتصال قطع شد', 'اتصال به سرور قطع شده است. تلاش برای اتصال مجدد...', { persistent: true });
    },

    connectionRestored: function(lostConnectionNotification) {
        if (lostConnectionNotification) {
            this.hideProgress(lostConnectionNotification);
        }
        TaskNotifications.success('اتصال برقرار شد', 'اتصال به سرور برقرار شد');
    },

    // Auto-save notifications
    autoSaveSuccess: function() {
        TaskNotifications.info('ذخیره خودکار', 'تغییرات به صورت خودکار ذخیره شد', { duration: 2000 });
    },

    autoSaveFailed: function() {
        TaskNotifications.warning('خطا در ذخیره', 'ذخیره خودکار ناموفق بود. لطفاً دستی ذخیره کنید');
    }
};

// Extend TaskNotifications with helper methods
Object.assign(TaskNotifications, TaskNotificationHelpers);

// Auto-setup for common scenarios
document.addEventListener('DOMContentLoaded', function() {
    // Setup AJAX error handling
    if (typeof $ !== 'undefined') {
        $(document).ajaxError(function(event, xhr, settings, error) {
            // Don't show notifications for silent requests
            if (settings.silent !== true) {
                TaskNotificationHelpers.handleAjaxError(xhr, 'error', error);
            }
        });

        // Setup form validation
        $(document).on('submit', 'form[data-notify-errors="true"]', function(e) {
            const form = $(this);
            const errors = [];
            
            form.find('.is-invalid').each(function() {
                const input = $(this);
                const error = input.siblings('.invalid-feedback').text() || 
                             input.next('.invalid-feedback').text() || 
                             'مقدار وارد شده نامعتبر است';
                errors.push(error);
            });

            if (errors.length > 0) {
                e.preventDefault();
                TaskNotificationHelpers.showFormErrors(errors);
            }
        });
    }

    // Setup connection monitoring
    let wasOnline = navigator.onLine;
    let connectionNotification = null;

    window.addEventListener('online', function() {
        if (!wasOnline) {
            TaskNotificationHelpers.connectionRestored(connectionNotification);
            connectionNotification = null;
        }
        wasOnline = true;
    });

    window.addEventListener('offline', function() {
        wasOnline = false;
        connectionNotification = TaskNotificationHelpers.connectionLost();
    });
});
