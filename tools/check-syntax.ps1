$ErrorActionPreference = 'Stop'
$repoRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$checkDir = Join-Path $repoRoot 'artifacts/syntax'
New-Item -ItemType Directory -Force -Path $checkDir | Out-Null
$sdkLine = (& dotnet --list-sdks | Select-Object -Last 1)
if ($sdkLine -notmatch '^(\S+)\s+\[(.+)\]$') { throw 'SDK 경로를 찾을 수 없습니다.' }
$sdkDir = Join-Path $Matches[2] $Matches[1]
$dotnetRoot = [IO.Path]::GetFullPath((Join-Path $Matches[2] '..'))
$refPack = Get-ChildItem -LiteralPath (Join-Path $dotnetRoot 'packs/Microsoft.NETCore.App.Ref') -Directory |
    Where-Object { $_.Name -like '10.*' } | Sort-Object { [version]$_.Name } | Select-Object -Last 1
if (!$refPack) { throw '.NET 10 참조 팩이 없습니다.' }
$references = Get-ChildItem -LiteralPath (Join-Path $refPack.FullName 'ref/net10.0') -Filter '*.dll' -File
$roslyn = Join-Path $sdkDir 'Roslyn/bincore'
$output = Join-Path $checkDir 'StaticSyntaxCheck.dll'
$compilerArgs = @('/nologo', '/noconfig', '/nostdlib+', '/target:exe', "/out:$output")
$compilerArgs += $references | ForEach-Object { "/reference:$($_.FullName)" }
$compilerArgs += @("/reference:$(Join-Path $roslyn 'Microsoft.CodeAnalysis.dll')", "/reference:$(Join-Path $roslyn 'Microsoft.CodeAnalysis.CSharp.dll')")
$compilerArgs += Join-Path $PSScriptRoot 'StaticSyntaxCheck.cs'
$compilerArgs += Join-Path $PSScriptRoot 'StaticTypeCheck.cs'
& dotnet (Join-Path $roslyn 'csc.dll') @compilerArgs
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
@{ runtimeOptions = @{ tfm = 'net10.0'; framework = @{ name = 'Microsoft.NETCore.App'; version = '10.0.0' } } } |
    ConvertTo-Json -Depth 5 | Set-Content -Encoding UTF8 -LiteralPath (Join-Path $checkDir 'StaticSyntaxCheck.runtimeconfig.json')
foreach ($name in @('Microsoft.CodeAnalysis.dll', 'Microsoft.CodeAnalysis.CSharp.dll')) {
    Copy-Item -LiteralPath (Join-Path $roslyn $name) -Destination $checkDir -Force
}
& dotnet $output (Join-Path $repoRoot 'src/Tushonka.Cleaner.Client') (Join-Path $repoRoot 'src/Tushonka.Cleaner.Server') (Join-Path $repoRoot 'src/Shared') (Join-Path $repoRoot 'tests')
exit $LASTEXITCODE
