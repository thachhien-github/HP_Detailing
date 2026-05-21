$files = @{
    'SettingsController.cs' = 'Admin'
    'StaffController.cs' = 'Admin'
    'AnalyticsController.cs' = 'Admin'
    'CatalogController.cs' = 'Admin'
    'WarehouseController.cs' = 'Admin, QuanLyKho'
    'TicketsController.cs' = 'Admin, ThuNgan'
    'FinancialController.cs' = 'Admin, ThuNgan'
}

foreach ($file in $files.Keys) {
    $path = Join-Path 'Controllers' $file
    if (Test-Path $path) {
        $content = Get-Content $path
        $role = $files[$file]
        $replacement = "[Authorize(Roles = `"$role`")]"
        $newContent = $content -replace '\[Authorize\]', $replacement
        Set-Content -Path $path -Value $newContent
        Write-Host "Updated $file"
    }
}
