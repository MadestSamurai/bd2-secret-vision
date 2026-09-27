param([switch]$Locked)
$ErrorActionPreference='Stop'
$restoreArgs=@();if($Locked){$restoreArgs+='--locked-mode'}
foreach($project in @('desktop/BD2SecretVision.Desktop.csproj','tests/BD2SecretVision.Tests.csproj','compatibility-tests/BD2SecretVision.Compatibility.Tests.csproj')){
    dotnet restore (Join-Path $PSScriptRoot $project) @restoreArgs --nologo
    if($LASTEXITCODE -ne 0){throw "Restore failed: $project"}
}
dotnet build (Join-Path $PSScriptRoot 'desktop/BD2SecretVision.Desktop.csproj') -c Release --no-restore --nologo
if($LASTEXITCODE -ne 0){throw 'Build failed'}
foreach($project in @('tests','compatibility-tests')){
    dotnet run --project (Join-Path $PSScriptRoot $project) -c Release --no-restore
    if($LASTEXITCODE -ne 0){throw "Tests failed: $project"}
}
