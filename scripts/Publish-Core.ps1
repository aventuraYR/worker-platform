# Publish-Core.ps1
param(
    [Parameter(Mandatory)]
    [string] $Version   # Ej: 1.0.1
)

$nugetPath = "\\YRMXNT002\NuGet"
$artifactsPath = "Artifacts"
$projects = @(
    "WorkerPlatform.Core.Configuration",
    "WorkerPlatform.Core.Logging",
    "WorkerPlatform.Core.Resilience",
    "WorkerPlatform.Core.Health",
    "WorkerPlatform.Core.Hosting",
    "WorkerPlatform.Core"
)

New-Item -ItemType Directory -Path $artifactsPath -Force | Out-Null

# 1. Limpiar artifacts anteriores
Remove-Item "$artifactsPath\WorkerPlatform.Core*.nupkg" -ErrorAction SilentlyContinue

# 2. Empaquetar cada módulo + agregador con la misma versión
foreach ($project in $projects)
{
    dotnet pack "src\shared\$project" `
        -c Release `
        -o $artifactsPath `
        /p:Version=$Version

    if ($LASTEXITCODE -ne 0)
    {
        Write-Host "ERROR: Falló el empaquetado de $project" -ForegroundColor Red
        exit 1
    }
}

# 3. Copiar paquetes a la carpeta compartida NuGet
Get-ChildItem "$artifactsPath\WorkerPlatform.Core*.$Version.nupkg" |
    Copy-Item -Destination "$nugetPath\" -Force

Write-Host "Publicación completada para versión $Version" -ForegroundColor Green
Write-Host "Paquetes publicados:"
$projects | ForEach-Object { Write-Host " - $_ $Version" }
Write-Host "Actualiza Directory.Packages.props con la versión de WorkerPlatform.Core si corresponde."
