# PowerShell script to replace hardcoded role strings with Roles class references

# Define the files to process (exclude migrations and generated files)
$files = Get-ChildItem -Path "." -Include "*.cs" -Recurse | 
    Where-Object { 
        $_.FullName -notlike "*\Migrations\*" -and 
        $_.FullName -notlike "*\bin\*" -and 
        $_.FullName -notlike "*\obj\*" -and
        $_.Name -ne "Roles.cs"
    }

Write-Host "Processing $($files.Count) files..."

foreach ($file in $files) {
    $content = Get-Content $file.FullName -Raw
    $originalContent = $content
    
    # Replace hardcoded role strings with Roles class references
    $content = $content -replace 'User\.IsInRole\("Admin"\)', 'User.IsInRole(Roles.Admin)'
    $content = $content -replace 'User\.IsInRole\("Manager"\)', 'User.IsInRole(Roles.Manager)'
    $content = $content -replace 'User\.IsInRole\("User"\)', 'User.IsInRole(Roles.User)'
    
    # Replace in Authorize attributes
    $content = $content -replace '\[Authorize\(Roles = "Admin"\)\]', '[Authorize(Roles = Roles.Admin)]'
    $content = $content -replace '\[Authorize\(Roles = "Manager"\)\]', '[Authorize(Roles = Roles.Manager)]'
    $content = $content -replace '\[Authorize\(Roles = "User"\)\]', '[Authorize(Roles = Roles.User)]'
    
    # Replace in role manager calls
    $content = $content -replace 'FindByNameAsync\("Admin"\)', 'FindByNameAsync(Roles.Admin)'
    $content = $content -replace 'FindByNameAsync\("Manager"\)', 'FindByNameAsync(Roles.Manager)'
    $content = $content -replace 'FindByNameAsync\("User"\)', 'FindByNameAsync(Roles.User)'
    
    # Replace in role comparisons
    $content = $content -replace 'r\.Name != "Admin"', 'r.Name != Roles.Admin'
    $content = $content -replace 'r\.Name != "Manager"', 'r.Name != Roles.Manager'
    $content = $content -replace 'r\.Name != "User"', 'r.Name != Roles.User'
    
    # Replace string role assignments
    $content = $content -replace 'Name = "Admin"', 'Name = Roles.Admin'
    $content = $content -replace 'Name = "Manager"', 'Name = Roles.Manager'
    $content = $content -replace 'Name = "User"', 'Name = Roles.User'
    
    if ($content -ne $originalContent) {
        Set-Content $file.FullName $content -NoNewline
        Write-Host "Updated: $($file.FullName)"
    }
}

Write-Host "Role replacement completed!"
