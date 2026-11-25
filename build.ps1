param([string]$Dotnet = 'dotnet')
$ErrorActionPreference = 'Stop'
Push-Location $PSScriptRoot
try {
    & $Dotnet publish SvgToPng/SvgToPng.csproj -c Release -r win-x64 --self-contained true -p:NuGetAudit=false -o windows-x64/SvgToPng
    if ($LASTEXITCODE -ne 0) { throw 'SVG utility build failed' }
    Copy-Item SvgToPng/LICENSE windows-x64/SvgToPng/LICENSE -Force
    $env:CEWE2PDF_SVG_TOOL = Join-Path $PSScriptRoot 'windows-x64/SvgToPng/SvgToPng.exe'
    foreach ($test in @('CultureTests','IntegrationTests','LegacyTests','DecorationTests','ShadowTests','ShapeTests','BookTests')) {
        $testArgs = @('run','--project',"tests/$test.csproj",'-c','Release','-p:NuGetAudit=false')
        if ($test -ne 'CultureTests') { $testArgs += @('--',"test-artifacts/$test") }
        & $Dotnet @testArgs
        if ($LASTEXITCODE -ne 0) { throw "$test failed" }
    }
    & $Dotnet publish Cewe2pdf/Cewe2pdf.csproj -c Release -r win-x64 --self-contained true -p:PublishReadyToRun=false -p:NuGetAudit=false -o windows-x64
    if ($LASTEXITCODE -ne 0) { throw 'Publish failed' }
    Copy-Item launcher/Fotobuch-starten.cmd windows-x64/Fotobuch-starten.cmd -Force
    Copy-Item README.md windows-x64/ANLEITUNG.md -Force
    Copy-Item licenses windows-x64/licenses -Recurse -Force
    foreach ($notice in @('LICENSE','AUTHORS.md','LICENSE-REVIEW.md','SOURCE-CODE.md','THIRD-PARTY-NOTICES.md','SBOM.spdx.json')) {
        if (Test-Path $notice) { Copy-Item $notice windows-x64/$notice -Force }
    }
} finally { Pop-Location }
