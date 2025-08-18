# UpdateControllersToPermissions.ps1
# اسکریپت برای تبدیل [Authorize(Roles = "")] به [Authorize(Policy = "")]

$rootPath = "."
$controllers = Write-Host "✨ Update controllers completed!" -ForegroundColor Green
Write-Host "⚠️  Reminder: Don't forget to check Program.cs as well." -ForegroundColor Yellow
    "UsersController.cs" = @{
        "Class" = "Permissions.ViewUsers"
        "Create" = "Permissions.CreateUsers"
        "Edit" = "Permissions.EditUsers"
        "Delete" = "Permissions.DeleteUsers"
        "ChangePassword" = "Permissions.ChangeUserPassword"
        "Details" = "Permissions.ViewUserDetails"
        "ToggleActive" = "Permissions.EditUsers"
        "ProjectAccess" = "Permissions.ViewUsers"
    }
    "ProjectsController.cs" = @{
        "Class" = "Permissions.ViewProjects"
        "Create" = "Permissions.CreateProjects"
        "Edit" = "Permissions.EditProjects"
        "Delete" = "Permissions.DeleteProjects"
        "Details" = "Permissions.ViewProjects"
        "ToggleActive" = "Permissions.EditProjects"
        "Tasks" = "Permissions.ViewTasks"
        "Access" = "Permissions.ViewProjectAccess"
        "GrantAccess" = "Permissions.ManageProjectAccess"
        "RevokeAccess" = "Permissions.ManageProjectAccess"
        "ManageAccess" = "Permissions.ManageProjectAccess"
        "AccessHistory" = "Permissions.ViewProjectAccess"
    }
    "TasksController.cs" = @{
        "Class" = "Permissions.ViewTasks"
        "Create" = "Permissions.CreateTasks"
        "Edit" = "Permissions.EditTasks"
        "Delete" = "Permissions.DeleteTasks"
        "Details" = "Permissions.ViewTasks"
        "Archive" = "Permissions.ArchiveTasks"
        "Unarchive" = "Permissions.ArchiveTasks"
        "Archived" = "Permissions.ViewArchivedTasks"
        "ChangeStatus" = "Permissions.ChangeTaskStatus"
        "QuickSave" = "Permissions.EditTasks"
        "QuickView" = "Permissions.ViewTasks"
        "UploadAttachment" = "Permissions.ManageTaskAttachments"
        "DownloadAttachment" = "Permissions.ViewTasks"
        "Board" = "Permissions.ViewTasks"
        "CompletedUninvoiced" = "Permissions.ViewTasks"
    }
    "CompaniesController.cs" = @{
        "Class" = "Permissions.ViewCompany"
        "Create" = "Permissions.ManageCompany"
        "Edit" = "Permissions.ManageCompany"
        "Details" = "Permissions.ViewCompany"
        "ToggleActive" = "Permissions.ManageCompany"
        "Overview" = "Permissions.ViewCompany"
        "MyCompany" = "Permissions.ViewCompany"
        "Users" = "Permissions.ViewCompanyUsers"
        "Projects" = "Permissions.ViewCompanyProjects"
    }
    "RolesController.cs" = @{
        "Class" = "Permissions.ViewRoles"
        "Create" = "Permissions.CreateRoles"
        "Edit" = "Permissions.EditRoles"
        "Delete" = "Permissions.DeleteRoles"
        "Details" = "Permissions.ViewRoles"
        "ToggleActive" = "Permissions.EditRoles"
    }
    "InvoicesController.cs" = @{
        "Class" = "Permissions.ViewInvoices"
        "Create" = "Permissions.CreateInvoices"
        "Edit" = "Permissions.EditInvoices"
        "Details" = "Permissions.ViewInvoices"
    }
    "GradesController.cs" = @{
        "Class" = "Permissions.ViewGrades"
        "Create" = "Permissions.ManageGrades"
        "Edit" = "Permissions.ManageGrades"
        "Delete" = "Permissions.ManageGrades"
        "Details" = "Permissions.ViewGrades"
    }
    "EmailTemplatesController.cs" = @{
        "Class" = "Permissions.ViewEmailTemplates"
        "Create" = "Permissions.ManageEmailTemplates"
        "Edit" = "Permissions.ManageEmailTemplates"
        "Delete" = "Permissions.ManageEmailTemplates"
        "Details" = "Permissions.ViewEmailTemplates"
    }
}

function Update-ControllerFile {
    param(
        [string]$FilePath,
        [hashtable]$PermissionMap
    )
    
    if (-not (Test-Path $FilePath)) {
        Write-Host "File not found: $FilePath" -ForegroundColor Yellow
        return
    }
    
    $content = Get-Content $FilePath -Raw
    $originalContent = $content
    $updated = $false
    
    # تبدیل کلاس کنترلر
    if ($PermissionMap.Class) {
        $pattern = '\[Authorize\(Roles\s*=\s*[^)]+\)\]\s*\n\s*public\s+class\s+\w+Controller'
        $replacement = "[Authorize(Policy = $($PermissionMap.Class))]`npublic class"
        if ($content -match $pattern) {
            $content = $content -replace $pattern, $replacement
            $updated = $true
        }
    }
    
    # تبدیل متدهای کنترلر
    foreach ($method in $PermissionMap.Keys) {
        if ($method -eq "Class") { continue }
        
        $permission = $PermissionMap[$method]
        
        # الگوی جستجو برای متد
        $pattern = '\[Authorize\(Roles\s*=\s*[^)]+\)\]\s*\n\s*public\s+[^{]+\s+' + $method + '\s*\('
        $replacement = "[Authorize(Policy = $permission)]`npublic"
        
        if ($content -match $pattern) {
            $content = $content -replace $pattern, $replacement
            $updated = $true
        }
    }
    
    if ($updated) {
        Set-Content $FilePath $content -Encoding UTF8
        Write-Host "✅ Updated: $FilePath" -ForegroundColor Green
    } else {
        Write-Host "⚪ No changes needed: $FilePath" -ForegroundColor Gray
    }
}

Write-Host "🚀 Starting controller updates for Permission system..." -ForegroundColor Cyan

foreach ($controller in $controllers.Keys) {
    $filePath = Join-Path $rootPath "Controllers" $controller
    $permissionMap = $controllers[$controller]
    
    Write-Host "`n📁 Processing: $controller" -ForegroundColor Yellow
    Update-ControllerFile -FilePath $filePath -PermissionMap $permissionMap
}

Write-Host "`n✨ آپدیت کنترلرها کامل شد!" -ForegroundColor Green
Write-Host "⚠️  یادآوری: فراموش نکنید که Program.cs را نیز بررسی کنید." -ForegroundColor Yellow
