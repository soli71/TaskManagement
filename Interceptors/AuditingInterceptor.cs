using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace TaskManagementMvc.Interceptors
{
    /// <summary>
    /// SaveChanges interceptor to populate audit timestamp fields in memory instead of relying on DB defaults.
    /// </summary>
    public class AuditingInterceptor : SaveChangesInterceptor
    {
        private static readonly string[] CreationFieldNames =
        {
            "CreatedAt", "AssignedAt", "GrantedAt", "ChangedAt", "UploadedAt", "SentAt", "RunStartedAt"
        };

        private static readonly string[] UpdateFieldNames = { "UpdatedAt" };

        private static DateTime UtcNow => DateTime.UtcNow;

        public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
        {
            if (eventData.Context != null)
            {
                ApplyAuditValues(eventData.Context.ChangeTracker);
            }
            return base.SavingChanges(eventData, result);
        }

        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            if (eventData.Context != null)
            {
                ApplyAuditValues(eventData.Context.ChangeTracker);
            }
            return base.SavingChangesAsync(eventData, result, cancellationToken);
        }

        private void ApplyAuditValues(ChangeTracker changeTracker)
        {
            var now = UtcNow;

            foreach (var entry in changeTracker.Entries())
            {
                if (entry.State == EntityState.Added)
                {
                    foreach (var name in CreationFieldNames)
                    {
                        var prop = entry.Properties.FirstOrDefault(p => p.Metadata.Name == name);
                        if (prop == null) continue;

                        if (prop.Metadata.ClrType == typeof(DateTime))
                        {
                            if ((DateTime)prop.CurrentValue! == default)
                                prop.CurrentValue = now;
                        }
                        else if (prop.Metadata.ClrType == typeof(DateTime?))
                        {
                            if (prop.CurrentValue == null)
                                prop.CurrentValue = now;
                        }
                    }
                    var updatedProp = entry.Properties.FirstOrDefault(p => p.Metadata.Name == "UpdatedAt");
                    if (updatedProp != null)
                        updatedProp.CurrentValue = now;
                }
                else if (entry.State == EntityState.Modified)
                {
                    foreach (var name in UpdateFieldNames)
                    {
                        var prop = entry.Properties.FirstOrDefault(p => p.Metadata.Name == name);
                        if (prop != null)
                            prop.CurrentValue = now;
                    }
                }
            }
        }
    }
}
