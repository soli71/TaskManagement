using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using TaskManagementMvc.Models;
using TaskStatus = TaskManagementMvc.Models.TaskStatus;

namespace TaskManagementMvc.Data
{
    public class TaskManagementContext : IdentityDbContext<ApplicationUser, ApplicationRole, int>
    {
        public TaskManagementContext(DbContextOptions<TaskManagementContext> options)
            : base(options)
        {
        }

        public DbSet<TaskItem> Tasks { get; set; }
        public DbSet<TaskAttachment> TaskAttachments { get; set; }
        public DbSet<TaskHistory> TaskHistories { get; set; }
        public DbSet<Performer> Performers { get; set; }
        public DbSet<Grade> Grades { get; set; }
        public DbSet<Invoice> Invoices { get; set; }
        public DbSet<InvoiceLine> InvoiceLines { get; set; }
        public DbSet<EmailTemplate> EmailTemplates { get; set; }
        public DbSet<InvoiceEmailLog> InvoiceEmailLogs { get; set; }
        public DbSet<InvoiceTelegramLog> InvoiceTelegramLogs { get; set; }
        public DbSet<UserRole> UserRoles { get; set; }
        public DbSet<Permission> Permissions { get; set; }
        public DbSet<RolePermission> RolePermissions { get; set; }
        public DbSet<Company> Companies { get; set; }
        public DbSet<Project> Projects { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Company configuration
            modelBuilder.Entity<Company>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
                entity.Property(e => e.Description).HasMaxLength(1000);
                entity.Property(e => e.Address).HasMaxLength(500);
                entity.Property(e => e.Phone).HasMaxLength(20);
                entity.Property(e => e.Email).HasMaxLength(100);
                entity.Property(e => e.Website).HasMaxLength(200);
                entity.Property(e => e.LogoPath).HasMaxLength(500);
                entity.Property(e => e.IsActive).HasDefaultValue(true);
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            });

            // Project configuration
            modelBuilder.Entity<Project>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
                entity.Property(e => e.Description).HasMaxLength(1000);
                entity.Property(e => e.Code).HasMaxLength(50);
                entity.Property(e => e.Budget).HasColumnType("decimal(18,2)");
                entity.Property(e => e.ActualCost).HasColumnType("decimal(18,2)");
                entity.Property(e => e.Priority).HasDefaultValue(ProjectPriority.Medium);
                entity.Property(e => e.Status).HasDefaultValue(ProjectStatus.Active);
                entity.Property(e => e.IsActive).HasDefaultValue(true);
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

                entity.HasOne(e => e.Company)
                    .WithMany(e => e.Projects)
                    .HasForeignKey(e => e.CompanyId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.ProjectManager)
                    .WithMany(e => e.ManagedProjects)
                    .HasForeignKey(e => e.ProjectManagerId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            // ApplicationUser configuration
            modelBuilder.Entity<ApplicationUser>(entity =>
            {
                entity.Property(e => e.FullName).HasMaxLength(100);
                entity.Property(e => e.Notes).HasMaxLength(500);
                entity.Property(e => e.IsActive).HasDefaultValue(true);
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
                entity.Property(e => e.CompanyRole).HasDefaultValue(CompanyRole.User);

                entity.HasOne(e => e.Company)
                    .WithMany(e => e.Users)
                    .HasForeignKey(e => e.CompanyId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            // TaskItem configuration
            modelBuilder.Entity<TaskItem>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Title).IsRequired().HasMaxLength(200);
                entity.Property(e => e.Description).HasMaxLength(1000);
                entity.Property(e => e.Status).HasDefaultValue(TaskStatus.InProgress);
                entity.Property(e => e.Priority).HasDefaultValue(TaskPriority.Medium);
                entity.Property(e => e.Hours).HasDefaultValue(0);
                entity.Property(e => e.IsArchived).HasDefaultValue(false);
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

                entity.HasOne(e => e.Performer)
                    .WithMany(e => e.Tasks)
                    .HasForeignKey(e => e.PerformerId)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasOne(e => e.CreatedBy)
                    .WithMany(e => e.CreatedTasks)
                    .HasForeignKey(e => e.CreatedById)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasOne(e => e.UpdatedBy)
                    .WithMany()
                    .HasForeignKey(e => e.UpdatedById)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasOne(e => e.Project)
                    .WithMany(e => e.Tasks)
                    .HasForeignKey(e => e.ProjectId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            // Performer configuration
            modelBuilder.Entity<Performer>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Description).HasMaxLength(500);
                entity.Property(e => e.Email).HasMaxLength(100);
                entity.Property(e => e.Phone).HasMaxLength(20);
                entity.Property(e => e.IsActive).HasDefaultValue(true);
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

                entity.HasOne(e => e.Company)
                    .WithMany(e => e.Performers)
                    .HasForeignKey(e => e.CompanyId)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasOne(e => e.Grade)
                    .WithMany()
                    .HasForeignKey(e => e.GradeId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            // Grade configuration
            modelBuilder.Entity<Grade>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Description).HasMaxLength(500);
                entity.Property(e => e.HourlyRate).HasColumnType("decimal(18,2)");
                entity.Property(e => e.IsActive).HasDefaultValue(true);
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETDATE()");

                entity.HasOne(e => e.Company)
                    .WithMany(e => e.Grades)
                    .HasForeignKey(e => e.CompanyId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            // Invoice configuration
            modelBuilder.Entity<Invoice>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.InvoiceNumber).IsRequired().HasMaxLength(50);
                entity.Property(e => e.Description).HasMaxLength(1000);
                entity.Property(e => e.CustomerEmail).HasMaxLength(100);
                entity.Property(e => e.CustomerName).HasMaxLength(200);
                entity.Property(e => e.CustomerAddress).HasMaxLength(500);
                entity.Property(e => e.Status).HasDefaultValue(InvoiceStatus.Draft);
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETDATE()");

                entity.HasOne(e => e.CreatedBy)
                    .WithMany(e => e.CreatedInvoices)
                    .HasForeignKey(e => e.CreatedById)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasOne(e => e.UpdatedBy)
                    .WithMany()
                    .HasForeignKey(e => e.UpdatedById)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasOne(e => e.Project)
                    .WithMany(e => e.Invoices)
                    .HasForeignKey(e => e.ProjectId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            // InvoiceLine configuration
            modelBuilder.Entity<InvoiceLine>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Title).IsRequired().HasMaxLength(200);
                entity.Property(e => e.PerformerName).HasMaxLength(100);
                entity.Property(e => e.GradeName).HasMaxLength(100);
                entity.Property(e => e.HourlyRate).HasColumnType("decimal(18,2)");
                entity.Property(e => e.Hours).HasDefaultValue(0);
                entity.Property(e => e.Amount).HasColumnType("decimal(18,2)");

                entity.HasOne(e => e.Invoice)
                    .WithMany(e => e.Lines)
                    .HasForeignKey(e => e.InvoiceId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.TaskItem)
                    .WithMany()
                    .HasForeignKey(e => e.TaskItemId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            // TaskAttachment configuration
            modelBuilder.Entity<TaskAttachment>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.FileName).IsRequired().HasMaxLength(255);
                entity.Property(e => e.FilePath).IsRequired().HasMaxLength(500);
                entity.Property(e => e.UploadedAt).HasDefaultValueSql("GETDATE()");

                entity.HasOne(e => e.Task)
                    .WithMany(e => e.Attachments)
                    .HasForeignKey(e => e.TaskId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.UploadedBy)
                    .WithMany(e => e.TaskAttachments)
                    .HasForeignKey(e => e.UploadedById)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            // TaskHistory configuration
            modelBuilder.Entity<TaskHistory>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Field).IsRequired().HasMaxLength(100);
                entity.Property(e => e.OldValue).HasMaxLength(1000);
                entity.Property(e => e.NewValue).HasMaxLength(1000);
                entity.Property(e => e.ChangedAt).HasDefaultValueSql("GETDATE()");

                entity.HasOne(e => e.Task)
                    .WithMany(e => e.HistoryEntries)
                    .HasForeignKey(e => e.TaskId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.ChangedBy)
                    .WithMany(e => e.TaskHistories)
                    .HasForeignKey(e => e.ChangedById)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            // EmailTemplate configuration
            modelBuilder.Entity<EmailTemplate>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Subject).IsRequired().HasMaxLength(200);
                entity.Property(e => e.Body).IsRequired();
                entity.Property(e => e.Description).HasMaxLength(500);
                entity.Property(e => e.AvailableVariables).HasMaxLength(1000);
                entity.Property(e => e.IsActive).HasDefaultValue(true);
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETDATE()");
            });

            // InvoiceEmailLog configuration
            modelBuilder.Entity<InvoiceEmailLog>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.ToEmail).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Subject).IsRequired().HasMaxLength(200);
                entity.Property(e => e.Body).IsRequired();
                entity.Property(e => e.Error).HasMaxLength(1000);
                entity.Property(e => e.SentAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

                entity.HasOne(e => e.Invoice)
                    .WithMany(e => e.EmailLogs)
                    .HasForeignKey(e => e.InvoiceId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.SentBy)
                    .WithMany()
                    .HasForeignKey(e => e.SentById)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            // InvoiceTelegramLog configuration
            modelBuilder.Entity<InvoiceTelegramLog>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.ChatId).IsRequired().HasMaxLength(100);
                entity.Property(e => e.MessageText).IsRequired();
                entity.Property(e => e.Error).HasMaxLength(1000);
                entity.Property(e => e.SentAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

                entity.HasOne(e => e.Invoice)
                    .WithMany(e => e.TelegramLogs)
                    .HasForeignKey(e => e.InvoiceId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.SentBy)
                    .WithMany()
                    .HasForeignKey(e => e.SentById)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            // Permission configuration
            modelBuilder.Entity<Permission>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Code).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Description).HasMaxLength(500);
                entity.Property(e => e.Group).HasMaxLength(100);
                entity.Property(e => e.IsActive).HasDefaultValue(true);
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            });

            // ApplicationRole configuration
            modelBuilder.Entity<ApplicationRole>(entity =>
            {
                entity.Property(e => e.Description).HasMaxLength(500);
                entity.Property(e => e.IsActive).HasDefaultValue(true);
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            });

            // UserRole configuration
            modelBuilder.Entity<UserRole>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => new { e.UserId, e.RoleId }).IsUnique();
                entity.Property(e => e.AssignedAt).HasDefaultValueSql("GETDATE()");
                entity.Property(e => e.Notes).HasMaxLength(500);
                entity.Property(e => e.IsActive).HasDefaultValue(true);

                entity.HasOne(e => e.User)
                    .WithMany(e => e.UserRoles)
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.Role)
                    .WithMany()
                    .HasForeignKey(e => e.RoleId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.AssignedBy)
                    .WithMany()
                    .HasForeignKey(e => e.AssignedById)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            // RolePermission configuration
            modelBuilder.Entity<RolePermission>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => new { e.RoleId, e.PermissionId }).IsUnique();
                entity.Property(e => e.AssignedAt).HasDefaultValueSql("GETDATE()");
                entity.Property(e => e.IsActive).HasDefaultValue(true);

                entity.HasOne(e => e.Role)
                    .WithMany()
                    .HasForeignKey(e => e.RoleId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.Permission)
                    .WithMany()
                    .HasForeignKey(e => e.PermissionId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.AssignedBy)
                    .WithMany()
                    .HasForeignKey(e => e.AssignedById)
                    .OnDelete(DeleteBehavior.SetNull);
            });
        }
    }
}
