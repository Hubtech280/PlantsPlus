param([Parameter(Mandatory=$true)][string]$GameDirectory)
$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path $PSScriptRoot -Parent
Set-Location -LiteralPath $repoRoot
$GameDirectory = (Resolve-Path -LiteralPath $GameDirectory).Path
$project = Join-Path $repoRoot 'src/PlantsPlus/PlantsPlus.csproj'
$version = ([xml](Get-Content -LiteralPath $project -Raw)).Project.PropertyGroup.Version | Where-Object { $_ } | Select-Object -First 1
$referenceRoot = Join-Path $GameDirectory 'MelonLoader'
foreach ($relative in @('net6/MelonLoader.dll','Il2CppAssemblies/Assembly-CSharp.dll')) {
    if (!(Test-Path -LiteralPath (Join-Path $referenceRoot $relative))) { throw "Missing game reference: $relative" }
}
if (!(Test-Path -LiteralPath (Join-Path $GameDirectory 'Mods/CustomizeLib.MelonLoader.dll'))) { throw 'Install CustomizeLib.MelonLoader 4.0.5.2-ml.5 first.' }
& dotnet build $project -c Release "-p:PVZF_GAME_DIR=$GameDirectory" '-clp:ErrorsOnly'
if ($LASTEXITCODE -ne 0) { throw 'Mod build failed.' }
$dll = Join-Path $repoRoot 'src/build/PvZFusionPlus.dll'
& dotnet run --project tools/PatchAudit/PatchAudit.csproj -c Release -- $dll $GameDirectory
if ($LASTEXITCODE -ne 0) { throw 'Patch audit failed.' }
& dotnet run --project tools/TextAudit/TextAudit.csproj -c Release
if ($LASTEXITCODE -ne 0) { throw 'Text audit failed.' }
& dotnet run --project tools/FavoritesAudit/FavoritesAudit.csproj -c Release -- $dll
if ($LASTEXITCODE -ne 0) { throw 'Favorites audit failed.' }
$artifactRoot = Join-Path $repoRoot ('artifacts/PvZFusionPlus-' + $version + '-' + (Get-Date -Format 'yyyyMMdd-HHmmss'))
if (Test-Path -LiteralPath $artifactRoot) { throw 'Artifact directory already exists; refusing overwrite.' }
$package = Join-Path $artifactRoot 'package'
$mods = Join-Path $package 'Mods'
New-Item -ItemType Directory -Path $mods -Force | Out-Null
Copy-Item -LiteralPath $dll -Destination $mods
foreach ($name in @('README.md','README_FR.md','NOTICE.md','CHANGELOG.md','GITHUB_RELEASE_NOTES.md')) {
    Copy-Item -LiteralPath (Join-Path $repoRoot $name) -Destination $package
}
$dllHash = (Get-FileHash -LiteralPath $dll -Algorithm SHA256).Hash.ToLowerInvariant()
$manifest = [ordered]@{ release="PvZ Fusion+ $version"; target_game='PvZ Fusion 4.0.5.2'; loader='MelonLoader 0.7.3'; customize_lib='4.0.5.2-ml.5'; binary='Mods/PvZFusionPlus.dll'; binary_size=(Get-Item -LiteralPath $dll).Length; binary_sha256=$dllHash; known_issue='Zombie damage/armor-loss ArgumentOutOfRangeException reports remain unresolved.' }
$manifest | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $package 'RELEASE_MANIFEST.json') -Encoding UTF8
$zipName = "PvZFusionPlus-$version.zip"
$zip = Join-Path $artifactRoot $zipName
Compress-Archive -Path (Join-Path $package '*') -DestinationPath $zip -CompressionLevel Optimal
$zipHash = (Get-FileHash -LiteralPath $zip -Algorithm SHA256).Hash.ToLowerInvariant()
@("$zipHash  $zipName", "$dllHash  Mods/PvZFusionPlus.dll") | Set-Content -LiteralPath (Join-Path $artifactRoot 'SHA256SUMS.txt') -Encoding ASCII
Write-Output "RELEASE_ZIP=$zip"
Write-Output "SHA256=$zipHash"
Write-Output 'No game launched; no DLL installed.'
