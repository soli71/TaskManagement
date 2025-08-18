# PowerShell script to update role references in the codebase
Write-Host "Updating role references..." -ForegroundColor Green

# Define file patterns to search
$filePatterns = @(
    "Controllers\*.cs",
    "Data\*.cs", 
    "Services\*.cs",
    "Views\**\*.cshtml"
)

# Define replacements
$replacements = @{
    "Roles\.Admin" = "Roles.SystemAdmin"
    "Roles\.Manager" = "Roles.CompanyManager" 
    "Roles\.User" = "Roles.Employee"
    '"Admin"' = '"SystemAdmin"'
    '"Manager"' = '"CompanyManager"'
    '"User"' = '"Employee"'
    "'Admin'" = "'SystemAdmin'"
    "'Manager'" = "'CompanyManager'" 
    "'User'" = "'Employee'"
}

$totalReplacements = 0

foreach ($pattern in $filePatterns) {
    $files = Get-ChildItem -Path $pattern -Recurse -ErrorAction SilentlyContinue
    
    foreach ($file in $files) {
        if ($file.PSIsContainer) { continue }
        
        $content = Get-Content -Path $file.FullName -Raw -ErrorAction SilentlyContinue
        if (-not $content) { continue }
        
        $originalContent = $content
        $fileChanged = $false
        
        foreach ($old in $replacements.Keys) {
            $new = $replacements[$old]
            if ($content -match $old) {
                $content = $content -replace $old, $new
                $fileChanged = $true
            }
        }
        
        if ($fileChanged) {
            Set-Content -Path $file.FullName -Value $content -NoNewline
            Write-Host "Updated: $($file.FullName)" -ForegroundColor Yellow
            $totalReplacements++
        }
    }
}

Write-Host "Completed! Updated $totalReplacements files." -ForegroundColor Green
