$ErrorActionPreference = 'Stop'

$project = Join-Path $PSScriptRoot 'NetSwitch.csproj'
$output = Join-Path $PSScriptRoot 'artifacts\publish'

dotnet publish $project `
    --configuration Release `
    --runtime win-x64 `
    --self-contained true `
    -p:PublishSingleFile=true `
    -p:DebugType=None `
    --output $output

Write-Host ""
Write-Host "Build completed:" -ForegroundColor Green
Get-ChildItem $output -Filter '*.exe' | Select-Object -ExpandProperty FullName
