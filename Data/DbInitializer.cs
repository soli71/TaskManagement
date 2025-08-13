using Microsoft.AspNetCore.Identity;
using TaskManagementMvc.Models;

namespace TaskManagementMvc.Data
{
    public static class DbInitializer
    {
        public static async void Initialize(TaskManagementContext context, UserManager<ApplicationUser> userManager, RoleManager<ApplicationRole> roleManager)
        {
            context.Database.EnsureCreated();

            // Seed Permissions if none exist
            if (!context.Permissions.Any())
            {
                var permissions = new[]
                {
                    // Task Management Permissions
                    new Permission { Name = "مشاهده تسک‌ها", Code = "Tasks.View", Description = "مشاهده لیست تسک‌ها", Group = "مدیریت تسک" },
                    new Permission { Name = "ایجاد تسک", Code = "Tasks.Create", Description = "ایجاد تسک جدید", Group = "مدیریت تسک" },
                    new Permission { Name = "ویرایش تسک", Code = "Tasks.Edit", Description = "ویرایش تسک‌ها", Group = "مدیریت تسک" },
                    new Permission { Name = "حذف تسک", Code = "Tasks.Delete", Description = "حذف تسک‌ها", Group = "مدیریت تسک" },
                    new Permission { Name = "آرشیو تسک", Code = "Tasks.Archive", Description = "آرشیو کردن تسک‌ها", Group = "مدیریت تسک" },

                    // Invoice Management Permissions
                    new Permission { Name = "مشاهده فاکتورها", Code = "Invoices.View", Description = "مشاهده لیست فاکتورها", Group = "مدیریت فاکتور" },
                    new Permission { Name = "ایجاد فاکتور", Code = "Invoices.Create", Description = "ایجاد فاکتور جدید", Group = "مدیریت فاکتور" },
                    new Permission { Name = "ویرایش فاکتور", Code = "Invoices.Edit", Description = "ویرایش فاکتورها", Group = "مدیریت فاکتور" },
                    new Permission { Name = "حذف فاکتور", Code = "Invoices.Delete", Description = "حذف فاکتورها", Group = "مدیریت فاکتور" },

                    // User Management Permissions
                    new Permission { Name = "مشاهده کاربران", Code = "Users.View", Description = "مشاهده لیست کاربران", Group = "مدیریت کاربران" },
                    new Permission { Name = "ایجاد کاربر", Code = "Users.Create", Description = "ایجاد کاربر جدید", Group = "مدیریت کاربران" },
                    new Permission { Name = "ویرایش کاربر", Code = "Users.Edit", Description = "ویرایش کاربران", Group = "مدیریت کاربران" },
                    new Permission { Name = "حذف کاربر", Code = "Users.Delete", Description = "حذف کاربران", Group = "مدیریت کاربران" },

                    // Role Management Permissions
                    new Permission { Name = "مشاهده نقش‌ها", Code = "Roles.View", Description = "مشاهده لیست نقش‌ها", Group = "مدیریت نقش‌ها" },
                    new Permission { Name = "ایجاد نقش", Code = "Roles.Create", Description = "ایجاد نقش جدید", Group = "مدیریت نقش‌ها" },
                    new Permission { Name = "ویرایش نقش", Code = "Roles.Edit", Description = "ویرایش نقش‌ها", Group = "مدیریت نقش‌ها" },
                    new Permission { Name = "حذف نقش", Code = "Roles.Delete", Description = "حذف نقش‌ها", Group = "مدیریت نقش‌ها" },

                    // Permission Management Permissions
                    new Permission { Name = "مشاهده دسترسی‌ها", Code = "Permissions.View", Description = "مشاهده لیست دسترسی‌ها", Group = "مدیریت دسترسی‌ها" },
                    new Permission { Name = "ایجاد دسترسی", Code = "Permissions.Create", Description = "ایجاد دسترسی جدید", Group = "مدیریت دسترسی‌ها" },
                    new Permission { Name = "ویرایش دسترسی", Code = "Permissions.Edit", Description = "ویرایش دسترسی‌ها", Group = "مدیریت دسترسی‌ها" },
                    new Permission { Name = "حذف دسترسی", Code = "Permissions.Delete", Description = "حذف دسترسی‌ها", Group = "مدیریت دسترسی‌ها" },

                    // Email Template Permissions
                    new Permission { Name = "مشاهده قالب‌های ایمیل", Code = "EmailTemplates.View", Description = "مشاهده لیست قالب‌های ایمیل", Group = "مدیریت قالب‌های ایمیل" },
                    new Permission { Name = "ایجاد قالب ایمیل", Code = "EmailTemplates.Create", Description = "ایجاد قالب ایمیل جدید", Group = "مدیریت قالب‌های ایمیل" },
                    new Permission { Name = "ویرایش قالب ایمیل", Code = "EmailTemplates.Edit", Description = "ویرایش قالب‌های ایمیل", Group = "مدیریت قالب‌های ایمیل" },
                    new Permission { Name = "حذف قالب ایمیل", Code = "EmailTemplates.Delete", Description = "حذف قالب‌های ایمیل", Group = "مدیریت قالب‌های ایمیل" },

                    // System Management Permissions
                    new Permission { Name = "مدیریت سیستم", Code = "System.Manage", Description = "دسترسی کامل به تمام بخش‌های سیستم", Group = "مدیریت سیستم" }
                };

                context.Permissions.AddRange(permissions);
                context.SaveChanges();
            }

            // Seed Roles if none exist
            if (!context.Roles.Any())
            {
                var roles = new[]
                {
                    new ApplicationRole { Name = "Admin", Description = "مدیر سیستم با دسترسی کامل", IsActive = true, CreatedAt = DateTime.UtcNow },
                    new ApplicationRole { Name = "Manager", Description = "مدیر پروژه با دسترسی محدود", IsActive = true, CreatedAt = DateTime.UtcNow },
                    new ApplicationRole { Name = "User", Description = "کاربر عادی", IsActive = true, CreatedAt = DateTime.UtcNow }
                };

                foreach (var role in roles)
                {
                    await roleManager.CreateAsync(role);
                }

                // Assign permissions to Admin role
                var adminRole = await roleManager.FindByNameAsync("Admin");
                if (adminRole != null)
                {
                    var allPermissions = context.Permissions.ToList();
                    foreach (var permission in allPermissions)
                    {
                        context.RolePermissions.Add(new RolePermission
                        {
                            RoleId = adminRole.Id,
                            PermissionId = permission.Id,
                            AssignedAt = DateTime.UtcNow,
                            AssignedById = null,
                            IsActive = true
                        });
                    }
                }

                // Assign permissions to Manager role
                var managerRole = await roleManager.FindByNameAsync("Manager");
                if (managerRole != null)
                {
                    var managerPermissions = context.Permissions
                        .Where(p => p.Group == "مدیریت تسک" || p.Group == "مدیریت فاکتور" || p.Group == "مدیریت قالب‌های ایمیل")
                        .ToList();

                    foreach (var permission in managerPermissions)
                    {
                        context.RolePermissions.Add(new RolePermission
                        {
                            RoleId = managerRole.Id,
                            PermissionId = permission.Id,
                            AssignedAt = DateTime.UtcNow,
                            AssignedById = null,
                            IsActive = true
                        });
                    }
                }

                // Assign permissions to User role
                var userRole = await roleManager.FindByNameAsync("User");
                if (userRole != null)
                {
                    var userPermissions = context.Permissions
                        .Where(p => p.Code == "Tasks.View" || p.Code == "Invoices.View")
                        .ToList();

                    foreach (var permission in userPermissions)
                    {
                        context.RolePermissions.Add(new RolePermission
                        {
                            RoleId = userRole.Id,
                            PermissionId = permission.Id,
                            AssignedAt = DateTime.UtcNow,
                            AssignedById = null,
                            IsActive = true
                        });
                    }
                }

                context.SaveChanges();
            }

            // Seed Admin User if none exist
            if (!context.Users.Any())
            {
                var adminUser = new ApplicationUser
                {
                    UserName = "admin",
                    Email = "admin@taskmanagement.com",
                    FullName = "مدیر سیستم",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    EmailConfirmed = true
                };

                var result = await userManager.CreateAsync(adminUser, "Admin123!");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(adminUser, "Admin");
                }
            }

            // Seed Email Templates if none exist
            if (!context.EmailTemplates.Any())
            {
                var templates = new[]
                {
                    new EmailTemplate
                    {
                        Name = "قالب فاکتور ساده",
                        Subject = "فاکتور شماره {{InvoiceNumber}}",
                        Body = @"<div dir='rtl' style='font-family: Tahoma, Arial, sans-serif;'>
                            <h2>فاکتور شماره {{InvoiceNumber}}</h2>
                            <p>سلام،</p>
                            <p>فاکتور شماره {{InvoiceNumber}} با مبلغ {{TotalAmount}} صادر شده است.</p>
                            <p>تاریخ: {{IssueDate}}</p>
                            <p>با تشکر</p>
                        </div>",
                        Description = "قالب ساده برای ارسال فاکتور",
                        IsActive = true,
                        AvailableVariables = "{{InvoiceNumber}}, {{TotalAmount}}, {{IssueDate}}"
                    },
                    new EmailTemplate
                    {
                        Name = "قالب فاکتور تفصیلی",
                        Subject = "فاکتور تفصیلی شماره {{InvoiceNumber}}",
                        Body = @"<div dir='rtl' style='font-family: Tahoma, Arial, sans-serif;'>
                            <h2>فاکتور شماره {{InvoiceNumber}}</h2>
                            <p>سلام،</p>
                            <p>فاکتور شماره {{InvoiceNumber}} با جزئیات زیر صادر شده است:</p>
                            <h3>جزئیات تسک‌ها:</h3>
                            {{TaskList}}
                            <p>مبلغ کل: <strong>{{TotalAmount}}</strong></p>
                            <p>تاریخ: {{IssueDate}}</p>
                            <p>تعداد تسک‌ها: {{LineCount}}</p>
                            <p>با تشکر</p>
                        </div>",
                        Description = "قالب تفصیلی با جزئیات کامل تسک‌ها",
                        IsActive = true,
                        AvailableVariables = "{{InvoiceNumber}}, {{TotalAmount}}, {{IssueDate}}, {{TaskList}}, {{LineCount}}"
                    }
                };

                context.EmailTemplates.AddRange(templates);
                context.SaveChanges();
            }
        }
    }
}
