param([switch]$SkipApplicationBuild)
$ErrorActionPreference = 'Stop'
$root = $PSScriptRoot
[xml]$project = Get-Content -LiteralPath (Join-Path $root 'src\DocumentSandwich.csproj')
$version = [string]$project.Project.PropertyGroup.Version
$compilerCandidates = @(
    (Join-Path $env:LOCALAPPDATA 'Programs\Inno Setup 6\ISCC.exe'),
    'C:\Program Files (x86)\Inno Setup 6\ISCC.exe',
    'C:\Program Files\Inno Setup 6\ISCC.exe'
)
$compiler = $compilerCandidates | Where-Object { Test-Path -LiteralPath $_ } | Select-Object -First 1
if (-not $compiler) { throw 'Inno Setup 6 is required to build the installer.' }
$payload = Join-Path $root 'dist\installer-payload'
if (-not $SkipApplicationBuild) { & (Join-Path $root 'build.ps1') -OutputDirectory $payload }
$app = Join-Path $payload 'Document Sandwich.exe'
if (-not (Test-Path -LiteralPath $app)) { throw "Application payload missing: $app" }
if ((Get-Item -LiteralPath $app).VersionInfo.ProductVersion.Split('+')[0] -ne $version) { throw 'Application payload version does not match the project.' }

# Ship the notices accompanying the exact runtime packages used by the build.
$licenses = Join-Path $root 'installer\licenses'
$assets = Get-Content -LiteralPath (Join-Path $root 'src\obj\project.assets.json') -Raw | ConvertFrom-Json
$packageRoot = $assets.packageFolders.PSObject.Properties.Name | Select-Object -First 1
$runtimePackages = foreach ($framework in $assets.project.frameworks.PSObject.Properties) {
    foreach ($dependency in $framework.Value.downloadDependencies) {
        if ($dependency.name -match '^Microsoft\.(NETCore|WindowsDesktop)\.App\.Runtime\.win-x64$') {
            $runtimeVersion = ($dependency.version -replace '[\[\] ]','').Split(',')[0]
            $directory = Join-Path $packageRoot ($dependency.name.ToLowerInvariant() + '/' + $runtimeVersion)
            foreach ($notice in Get-ChildItem -LiteralPath $directory -File | Where-Object { $_.Name -match '^(LICENSE|THIRD-PARTY-NOTICES|ThirdPartyNotices)' }) {
                Copy-Item -LiteralPath $notice.FullName -Destination (Join-Path $licenses ($dependency.name + '-' + $runtimeVersion + '-' + $notice.Name)) -Force
            }
        }
    }
}
foreach ($package in $assets.libraries.PSObject.Properties) {
    if ($package.Name -notmatch '^(Microsoft\.NETCore\.App\.Runtime\.win-x64|Microsoft\.WindowsDesktop\.App\.Runtime\.win-x64|Microsoft\.Extensions\.Logging\.Abstractions|Microsoft\.Extensions\.DependencyInjection\.Abstractions|System\.Security\.Cryptography\.Pkcs)/') { continue }
    $directory = Join-Path $packageRoot $package.Value.path
    $label = $package.Name.Replace('/','-')
    foreach ($notice in Get-ChildItem -LiteralPath $directory -File | Where-Object { $_.Name -match '^(LICENSE|THIRD-PARTY-NOTICES|ThirdPartyNotices)' }) {
        Copy-Item -LiteralPath $notice.FullName -Destination (Join-Path $licenses ($label + '-' + $notice.Name)) -Force
    }
}
& $compiler "/DAppVersion=$version" "/DPayloadDir=$payload" (Join-Path $root 'installer\Document-Sandwich.iss')
if ($LASTEXITCODE -ne 0) { throw "Installer compilation failed: $LASTEXITCODE" }
$setup = Join-Path $root "dist\Document-Sandwich-Setup-$version.exe"
$hash = (Get-FileHash -LiteralPath $setup -Algorithm SHA256).Hash
[IO.File]::WriteAllText(($setup + '.sha256'), $hash + '  ' + [IO.Path]::GetFileName($setup) + [Environment]::NewLine)
Write-Output "Created $setup"
Write-Output "SHA256: $hash"
