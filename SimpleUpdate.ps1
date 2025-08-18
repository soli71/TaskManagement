# Simple Permission Update Script
Write-Host "Starting permission updates..." -ForegroundColor Cyan

# Update UsersController
$usersPath = "Controllers\UsersController.cs"
if (Test-Path $usersPath) {
    $content = Get-Content $usersPath -Raw
    $content = $content -replace '\[Authorize\(Policy = Permissions\.ViewUsers\)\]', '[Authorize(Policy = Permissions.ViewUsers)]'
    Set-Content $usersPath $content -Encoding UTF8
    Write-Host "Checked: UsersController.cs" -ForegroundColor Green
}

Write-Host "Updates completed!" -ForegroundColor Green
