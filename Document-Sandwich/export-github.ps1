$ErrorActionPreference = 'Stop'
$root = $PSScriptRoot
$package = Join-Path $root 'GitHub-Package'
$repository = Join-Path $package 'Document-Sandwich'
$releases = Join-Path $package 'Release-Assets'
New-Item -ItemType Directory -Force -Path $repository,$releases | Out-Null
$sourceFiles = @('.gitignore','README.md','build.ps1','build-installer.ps1','export-github.ps1')
$sourceFiles += Get-ChildItem -LiteralPath (Join-Path $root 'src') -File | ForEach-Object { 'src/' + $_.Name }
$sourceFiles += Get-ChildItem -LiteralPath (Join-Path $root 'assets') -File | ForEach-Object { 'assets/' + $_.Name }
$sourceFiles += Get-ChildItem -LiteralPath (Join-Path $root 'installer') -File | ForEach-Object { 'installer/' + $_.Name }
$sourceFiles += Get-ChildItem -LiteralPath (Join-Path $root 'installer/licenses') -File | ForEach-Object { 'installer/licenses/' + $_.Name }
$sourceFiles += Get-ChildItem -LiteralPath (Join-Path $root 'tests/UiSmoke') -File | ForEach-Object { 'tests/UiSmoke/' + $_.Name }
foreach ($relative in $sourceFiles) {
    $source = Join-Path $root $relative
    $target = Join-Path $repository $relative
    New-Item -ItemType Directory -Force -Path (Split-Path -Parent $target) | Out-Null
    Copy-Item -LiteralPath $source -Destination $target -Force
    if ((Get-FileHash -LiteralPath $source).Hash -ne (Get-FileHash -LiteralPath $target).Hash) { throw "Copy verification failed: $relative" }
}
[xml]$project = Get-Content -LiteralPath (Join-Path $root 'src/DocumentSandwich.csproj')
$version = [string]$project.Project.PropertyGroup.Version
$setup = Join-Path $root "dist/Document-Sandwich-Setup-$version.exe"
foreach ($file in @($setup,($setup + '.sha256'))) { Copy-Item -LiteralPath $file -Destination $releases -Force }
$instructions = @'
Document Sandwich - GitHub upload package

Document-Sandwich/  Upload the CONTENTS of this folder as the GitHub repository.
Release-Assets/     Attach the installer and its SHA256 file to a GitHub Release.

The repository includes application source, icon artwork, build scripts,
installer script, third-party notices and automated checks.
Build caches, local settings, test output and previous versions are excluded.
Do not upload the original working folder or dist folder as repository source.
No files have been published to GitHub by this packaging step.
'@
[IO.File]::WriteAllText((Join-Path $package 'UPLOAD-GUIDE.txt'),$instructions)
$manifest = Get-ChildItem -LiteralPath $package -Recurse -File | Where-Object { $_.Name -ne 'PACKAGE-MANIFEST.sha256' } | Sort-Object FullName | ForEach-Object {
    (Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash + '  ' + $_.FullName.Substring($package.Length+1).Replace('\','/')
}
[IO.File]::WriteAllLines((Join-Path $package 'PACKAGE-MANIFEST.sha256'),$manifest)
Write-Output "GitHub package: $package"
Write-Output "Repository files: $($sourceFiles.Count)"
