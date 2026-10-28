param([string]$OutputDirectory = "$PSScriptRoot\dist")
$ErrorActionPreference = 'Stop'
dotnet publish "$PSScriptRoot\src\DocumentSandwich.csproj" -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o $OutputDirectory
if ($LASTEXITCODE -ne 0) { throw 'Build failed' }
