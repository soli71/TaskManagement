using Microsoft.AspNetCore.Identity;
using TaskManagementMvc.Models;

namespace TaskManagementMvc.Data
{
    public static class DbInitializer
    {
        public static async Task Initialize(TaskManagementContext context, UserManager<ApplicationUser> userManager, RoleManager<ApplicationRole> roleManager)
        {
            // Database is migrated externally (Program.cs). Do not call EnsureCreated with migrations.

            // Seed Permissions if none exist
            if (!context.Permissions.Any())
            {
                var permissions = new[]
                {
                    // System Management Permissions
                    new Permission { Name = "مدیریت سیستم", Code = Permissions.ManageSystem, Description = "دسترسی کامل به تمام بخش‌های سیستم", Group = "مدیریت سیستم" },
                    new Permission { Name = "مشاهده لاگ‌های سیستم", Code = Permissions.ViewSystemLogs, Description = "مشاهده لاگ‌های سیستم", Group = "مدیریت سیستم" },
                    new Permission { Name = "مدیریت تمام شرکت‌ها", Code = Permissions.ManageAllCompanies, Description = "مدیریت تمام شرکت‌ها", Group = "مدیریت سیستم" },
                    new Permission { Name = "مدیریت تمام کاربران", Code = Permissions.ManageAllUsers, Description = "مدیریت تمام کاربران", Group = "مدیریت سیستم" },
                    new Permission { Name = "مدیریت نقش‌های سیستم", Code = Permissions.ManageSystemRoles, Description = "مدیریت نقش‌های سیستم", Group = "مدیریت سیستم" },

                    // Company Management Permissions
                    new Permission { Name = "مدیریت شرکت", Code = Permissions.ManageCompany, Description = "مدیریت شرکت", Group = "مدیریت شرکت" },
                    new Permission { Name = "مشاهده شرکت", Code = Permissions.ViewCompany, Description = "مشاهده اطلاعات شرکت", Group = "مدیریت شرکت" },
                    new Permission { Name = "مدیریت کاربران شرکت", Code = Permissions.ManageCompanyUsers, Description = "مدیریت کاربران شرکت", Group = "مدیریت شرکت" },
                    new Permission { Name = "مشاهده کاربران شرکت", Code = Permissions.ViewCompanyUsers, Description = "مشاهده کاربران شرکت", Group = "مدیریت شرکت" },
                    new Permission { Name = "مدیریت پروژه‌های شرکت", Code = Permissions.ManageCompanyProjects, Description = "مدیریت پروژه‌های شرکت", Group = "مدیریت شرکت" },
                    new Permission { Name = "مشاهده پروژه‌های شرکت", Code = Permissions.ViewCompanyProjects, Description = "مشاهده پروژه‌های شرکت", Group = "مدیریت شرکت" },

                    // Project Management Permissions
                    new Permission { Name = "مدیریت پروژه‌ها", Code = Permissions.ManageProjects, Description = "مدیریت پروژه‌ها", Group = "مدیریت پروژه" },
                    new Permission { Name = "مشاهده پروژه‌ها", Code = Permissions.ViewProjects, Description = "مشاهده پروژه‌ها", Group = "مدیریت پروژه" },
                    new Permission { Name = "ایجاد پروژه", Code = Permissions.CreateProjects, Description = "ایجاد پروژه جدید", Group = "مدیریت پروژه" },
                    new Permission { Name = "ویرایش پروژه", Code = Permissions.EditProjects, Description = "ویرایش پروژه‌ها", Group = "مدیریت پروژه" },
                    new Permission { Name = "حذف پروژه", Code = Permissions.DeleteProjects, Description = "حذف پروژه‌ها", Group = "مدیریت پروژه" },
                    new Permission { Name = "مدیریت دسترسی پروژه", Code = Permissions.ManageProjectAccess, Description = "مدیریت دسترسی به پروژه‌ها", Group = "مدیریت پروژه" },
                    new Permission { Name = "مشاهده دسترسی پروژه", Code = Permissions.ViewProjectAccess, Description = "مشاهده دسترسی‌های پروژه", Group = "مدیریت پروژه" },

                    // Task Management Permissions
                    new Permission { Name = "مدیریت وظایف", Code = Permissions.ManageTasks, Description = "مدیریت وظایف", Group = "مدیریت تسک" },
                    new Permission { Name = "مشاهده وظایف", Code = Permissions.ViewTasks, Description = "مشاهده وظایف", Group = "مدیریت تسک" },
                    new Permission { Name = "ایجاد وظیفه", Code = Permissions.CreateTasks, Description = "ایجاد وظیفه جدید", Group = "مدیریت تسک" },
                    new Permission { Name = "ویرایش وظیفه", Code = Permissions.EditTasks, Description = "ویرایش وظایف", Group = "مدیریت تسک" },
                    new Permission { Name = "حذف وظیفه", Code = Permissions.DeleteTasks, Description = "حذف وظایف", Group = "مدیریت تسک" },
                    new Permission { Name = "آرشیو وظایف", Code = Permissions.ArchiveTasks, Description = "آرشیو کردن وظایف", Group = "مدیریت تسک" },
                    new Permission { Name = "تغییر وضعیت وظیفه", Code = Permissions.ChangeTaskStatus, Description = "تغییر وضعیت وظایف", Group = "مدیریت تسک" },
                    new Permission { Name = "مشاهده وظایف آرشیو شده", Code = Permissions.ViewArchivedTasks, Description = "مشاهده وظایف آرشیو شده", Group = "مدیریت تسک" },
                    new Permission { Name = "مدیریت ضمائم وظیفه", Code = Permissions.ManageTaskAttachments, Description = "مدیریت ضمائم وظایف", Group = "مدیریت تسک" },

                    // User Management Permissions
                    new Permission { Name = "مشاهده کاربران", Code = Permissions.ViewUsers, Description = "مشاهده لیست کاربران", Group = "مدیریت کاربران" },
                    new Permission { Name = "ایجاد کاربر", Code = Permissions.CreateUsers, Description = "ایجاد کاربر جدید", Group = "مدیریت کاربران" },
                    new Permission { Name = "ویرایش کاربر", Code = Permissions.EditUsers, Description = "ویرایش کاربران", Group = "مدیریت کاربران" },
                    new Permission { Name = "حذف کاربر", Code = Permissions.DeleteUsers, Description = "حذف کاربران", Group = "مدیریت کاربران" },
                    new Permission { Name = "تغییر رمز عبور کاربر", Code = Permissions.ChangeUserPassword, Description = "تغییر رمز عبور کاربران", Group = "مدیریت کاربران" },
                    new Permission { Name = "مدیریت نقش‌های کاربر", Code = Permissions.ManageUserRoles, Description = "مدیریت نقش‌های کاربران", Group = "مدیریت کاربران" },
                    new Permission { Name = "مشاهده جزئیات کاربر", Code = Permissions.ViewUserDetails, Description = "مشاهده جزئیات کاربران", Group = "مدیریت کاربران" },

                    // Role Management Permissions
                    new Permission { Name = "مشاهده نقش‌ها", Code = Permissions.ViewRoles, Description = "مشاهده لیست نقش‌ها", Group = "مدیریت نقش‌ها" },
                    new Permission { Name = "ایجاد نقش", Code = Permissions.CreateRoles, Description = "ایجاد نقش جدید", Group = "مدیریت نقش‌ها" },
                    new Permission { Name = "ویرایش نقش", Code = Permissions.EditRoles, Description = "ویرایش نقش‌ها", Group = "مدیریت نقش‌ها" },
                    new Permission { Name = "حذف نقش", Code = Permissions.DeleteRoles, Description = "حذف نقش‌ها", Group = "مدیریت نقش‌ها" },

                    // Grade Management Permissions
                    new Permission { Name = "مدیریت رتبه‌ها", Code = Permissions.ManageGrades, Description = "مدیریت رتبه‌ها", Group = "مدیریت رتبه‌ها" },
                    new Permission { Name = "مشاهده رتبه‌ها", Code = Permissions.ViewGrades, Description = "مشاهده رتبه‌ها", Group = "مدیریت رتبه‌ها" },

                    // Invoice Management Permissions
                    new Permission { Name = "مدیریت فاکتورها", Code = Permissions.ManageInvoices, Description = "مدیریت فاکتورها", Group = "مدیریت فاکتور" },
                    new Permission { Name = "مشاهده فاکتورها", Code = Permissions.ViewInvoices, Description = "مشاهده فاکتورها", Group = "مدیریت فاکتور" },
                    new Permission { Name = "ایجاد فاکتور", Code = Permissions.CreateInvoices, Description = "ایجاد فاکتور جدید", Group = "مدیریت فاکتور" },
                    new Permission { Name = "ویرایش فاکتور", Code = Permissions.EditInvoices, Description = "ویرایش فاکتورها", Group = "مدیریت فاکتور" },

                    // Email Template Permissions
                    new Permission { Name = "مدیریت قالب‌های ایمیل", Code = Permissions.ManageEmailTemplates, Description = "مدیریت قالب‌های ایمیل", Group = "مدیریت قالب‌های ایمیل" },
                    new Permission { Name = "مشاهده قالب‌های ایمیل", Code = Permissions.ViewEmailTemplates, Description = "مشاهده قالب‌های ایمیل", Group = "مدیریت قالب‌های ایمیل" }
                };

                context.Permissions.AddRange(permissions);
                context.SaveChanges();
            }

            // Seed Roles if none exist
            if (!context.Roles.Any())
            {
                var roles = new[]
                {
                    new ApplicationRole { Name = Roles.SystemAdmin, Description = "مدیر سیستم با دسترسی کامل به تمام بخش‌ها", IsActive = true, CreatedAt = DateTime.UtcNow },
                    new ApplicationRole { Name = Roles.CompanyManager, Description = "مدیر شرکت با دسترسی به مدیریت کاربران و پروژه‌های شرکت", IsActive = true, CreatedAt = DateTime.UtcNow },
                    new ApplicationRole { Name = Roles.ProjectManager, Description = "مدیر پروژه با دسترسی به مدیریت پروژه و وظایف", IsActive = true, CreatedAt = DateTime.UtcNow },
                    new ApplicationRole { Name = Roles.Employee, Description = "کارمند با دسترسی به عملیات پایه", IsActive = true, CreatedAt = DateTime.UtcNow }
                };

                foreach (var role in roles)
                {
                    await roleManager.CreateAsync(role);
                }

                // Assign permissions to Admin role
                var adminRole = await roleManager.FindByNameAsync(Roles.SystemAdmin);
                if (adminRole != null)
                {
                    var adminPermissions = Permissions.RolePermissions[Roles.SystemAdmin];
                    foreach (var permissionCode in adminPermissions)
                    {
                        var permission = context.Permissions.FirstOrDefault(p => p.Code == permissionCode);
                        if (permission != null)
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
                }

                // Assign permissions to CompanyManager role
                var companyManagerRole = await roleManager.FindByNameAsync(Roles.CompanyManager);
                if (companyManagerRole != null)
                {
                    var companyManagerPermissions = Permissions.RolePermissions[Roles.CompanyManager];
                    foreach (var permissionCode in companyManagerPermissions)
                    {
                        var permission = context.Permissions.FirstOrDefault(p => p.Code == permissionCode);
                        if (permission != null)
                        {
                            context.RolePermissions.Add(new RolePermission
                            {
                                RoleId = companyManagerRole.Id,
                                PermissionId = permission.Id,
                                AssignedAt = DateTime.UtcNow,
                                AssignedById = null,
                                IsActive = true
                            });
                        }
                    }
                }

                // Assign permissions to ProjectManager role
                var projectManagerRole = await roleManager.FindByNameAsync(Roles.ProjectManager);
                if (projectManagerRole != null)
                {
                    var projectManagerPermissions = Permissions.RolePermissions[Roles.ProjectManager];
                    foreach (var permissionCode in projectManagerPermissions)
                    {
                        var permission = context.Permissions.FirstOrDefault(p => p.Code == permissionCode);
                        if (permission != null)
                        {
                            context.RolePermissions.Add(new RolePermission
                            {
                                RoleId = projectManagerRole.Id,
                                PermissionId = permission.Id,
                                AssignedAt = DateTime.UtcNow,
                                AssignedById = null,
                                IsActive = true
                            });
                        }
                    }
                }

                // Assign permissions to Employee role
                var employeeRole = await roleManager.FindByNameAsync(Roles.Employee);
                if (employeeRole != null)
                {
                    var employeePermissions = Permissions.RolePermissions[Roles.Employee];
                    foreach (var permissionCode in employeePermissions)
                    {
                        var permission = context.Permissions.FirstOrDefault(p => p.Code == permissionCode);
                        if (permission != null)
                        {
                            context.RolePermissions.Add(new RolePermission
                            {
                                RoleId = employeeRole.Id,
                                PermissionId = permission.Id,
                                AssignedAt = DateTime.UtcNow,
                                AssignedById = null,
                                IsActive = true
                            });
                        }
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
                    await userManager.AddToRoleAsync(adminUser, Roles.SystemAdmin);
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
                    },
                    new EmailTemplate
                    {
                        Name = "قالب فاکتور با جمع افراد",
                        Subject = "فاکتور شماره {{InvoiceNumber}} - مجموع نفرات",
                        Body = "RAZOR:InvoicePerformerTotals",
                        Description = "نمایش جمع ساعات و مبالغ هر فرد + اطلاعات بانکی (شبا و کارت)",
                        IsActive = true,
                        AvailableVariables = "{{InvoiceNumber}}, {{TotalAmount}}, {{IssueDate}}, {{TaskList}}, {{LineCount}}, {{PerformerTotals}}"
                    }
                };

                context.EmailTemplates.AddRange(templates);
                context.SaveChanges();
            }
        }
    }
}
