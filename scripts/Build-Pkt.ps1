# Builds the W2 trend-finder DLL and packages it with a generated .atc into WadiTrend_C2.pkt.
#   powershell -NoProfile -ExecutionPolicy Bypass -File scripts\Build-Pkt.ps1
# Keep in sync: parameter names/defaults <-> Civil\ParameterSet.cs; DotNetClass <-> SubassemblyEntry.cs.

param(
    [string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"

$root = Resolve-Path (Join-Path $PSScriptRoot "..")
$project = Join-Path $root "src\WadiTrendSubassembly\WadiTrendSubassembly.csproj"
$output = Join-Path $root "output"
$staging = Join-Path $output "pkt-staging"
$packet = Join-Path $output "WadiTrend_C2.pkt"
$atc = Join-Path $staging "WadiTrend_C2.atc"
$dllSource = Join-Path $root "src\WadiTrendSubassembly\bin\$Configuration\net8.0-windows\WadiTrendSubassembly_C21.dll"
$dllTarget = Join-Path $staging "WadiTrendSubassembly_C21.dll"

dotnet build $project -c $Configuration -v:minimal
if ($LASTEXITCODE -ne 0) { throw "dotnet build failed" }

# Best-effort cleanup: files from previous versions may be locked by a running Civil 3D
# that loaded them; leave those in place (the .atc only references the current DLL name).
if (Test-Path -LiteralPath $staging) {
    Get-ChildItem -LiteralPath $staging -File | ForEach-Object {
        try { Remove-Item -LiteralPath $_.FullName -Force -ErrorAction Stop } catch {}
    }
}
New-Item -ItemType Directory -Force -Path $staging | Out-Null
Copy-Item -LiteralPath $dllSource -Destination $dllTarget -Force

$xml = @"
<?xml version="1.0" encoding="utf-8"?>
<Category xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance" xmlns:xsd="http://www.w3.org/2001/XMLSchema">
  <ItemID idValue="{80F0C0B1-04E9-40A4-8762-F463E1ADFCA4}" />
  <Properties>
    <ItemName>WadiTrend C2</ItemName>
    <Images>
      <Image cx="93" cy="123" />
    </Images>
    <Description>Wadi levee trend-line finder: fits straight terrain trends, marks concave/convex breaks at their intersections (Claude C2).</Description>
    <AccessRight>1</AccessRight>
    <Time createdUniversalDateTime="2026-07-02T00:00:00" modifiedUniversalDateTime="2026-07-02T00:00:00" />
  </Properties>
  <CustomData />
  <Source>
    <Publisher>
      <PublisherName>Renardet</PublisherName>
    </Publisher>
  </Source>
  <Palettes />
  <Packages />
  <Tools>
    <Tool Name="WadiTrend_OneSide_C2">
      <ItemID idValue="{FCDDFB39-3ED1-4AEA-B959-6D3FA99E42D6}" />
      <Properties>
        <ItemName>WadiTrend_OneSide_C2</ItemName>
        <Images>
          <Image cx="64" cy="64" />
        </Images>
        <Description>One-side wadi levee trend finder. Fits long straight trend lines to the scanned ground and marks concave (protect) / convex (debug) breaks at trend intersections.</Description>
        <ToolTip>Version: C2.1 Trend Finder</ToolTip>
        <Keywords>_WadiTrend_OneSide_C2 subassembly levee trend wadi</Keywords>
        <Help>
          <HelpFile />
          <HelpCommand />
          <HelpData />
        </Help>
        <Time createdUniversalDateTime="2026-07-02T00:00:00" modifiedUniversalDateTime="2026-07-02T00:00:00" />
      </Properties>
      <Source />
      <StockToolRef idValue="{7F55AAC0-0256-48D7-BFA5-914702663FDE}" />
      <Data>
        <AeccDbSubassembly>
          <GeometryGenerateMode>UseDotNet</GeometryGenerateMode>
          <ConditionalSubassembly>false</ConditionalSubassembly>
          <DotNetClass Assembly="WadiTrendSubassembly_C21.dll">Subassembly.WadiTrendOneSide</DotNetClass>
          <Version>C2.1</Version>
          <Content DownloadLocation="" />
          <Params>
            <Side DataType="Long" TypeInfo="16" DisplayName="Side" Description="Which side of the baseline the wadi is on; scan direction.">0
              <Enum>
                <Right DisplayName="Right">0</Right>
                <Left DisplayName="Left">1</Left>
              </Enum>
            </Side>
            <Version DataType="String" DisplayName="Version" Description="Internal package version.">C2.1</Version>
            <CrownWidth DataType="Double" TypeInfo="16" DisplayName="Crown Width" Description="Horizontal crown width (m).">4.0</CrownWidth>
            <LeveeSideSlope DataType="Double" TypeInfo="10" DisplayName="Levee Side Slope" Description="Side slope as vertical/horizontal grade; 0.5 means 1V:2H.">0.5</LeveeSideSlope>
            <MaxScanDistance DataType="Double" TypeInfo="16" DisplayName="Max Scan Distance" Description="Maximum ground scan distance (m) when no scan limit target is assigned.">250.0</MaxScanDistance>
            <AnalysisSampleInterval DataType="Double" TypeInfo="16" DisplayName="Analysis Sample Interval" Description="Ground sampling spacing (m) for the analysis; drawn surface links follow the actual surface.">0.5</AnalysisSampleInterval>
            <MaxTrendResidual DataType="Double" TypeInfo="16" DisplayName="Max Trend Residual" Description="Maximum RMS vertical error (m) a single trend line may have. Larger = fewer, longer trends.">0.15</MaxTrendResidual>
            <MinTrendLength DataType="Double" TypeInfo="16" DisplayName="Min Trend Length" Description="Trends shorter than this (m) are absorbed/dropped; rounded transitions between long trends are bridged.">5.0</MinTrendLength>
            <SlopeChangeThreshold DataType="Double" DisplayName="Slope Change Threshold" Description="Grade difference below which two trends merge, dimensionless: 0.05 = 5%. Also the minimum steep-side grade for classification.">0.05</SlopeChangeThreshold>
            <BreakMarkerSize DataType="Double" TypeInfo="16" DisplayName="Break Marker Size" Description="Diamond marker size (m) at detected breaks.">0.5</BreakMarkerSize>
            <ShowConvexMarkers DataType="Long" DisplayName="Show Convex Markers" Description="Convex breaks are debug-only; turn off to hide them.">1
              <Enum>
                <No DisplayName="No">0</No>
                <Yes DisplayName="Yes">1</Yes>
              </Enum>
            </ShowConvexMarkers>
            <ShowTrendLines DataType="Long" DisplayName="Show Trend Lines" Description="Draws the fitted trend lines junction-to-junction (code WT_Trend).">1
              <Enum>
                <No DisplayName="No">0</No>
                <Yes DisplayName="Yes">1</Yes>
              </Enum>
            </ShowTrendLines>
          </Params>
        </AeccDbSubassembly>
        <Units>m</Units>
      </Data>
    </Tool>
  </Tools>
  <StockTools />
</Category>
"@

Set-Content -LiteralPath $atc -Value $xml -Encoding UTF8

if (Test-Path -LiteralPath $packet) {
    Remove-Item -LiteralPath $packet -Force
}

Add-Type -AssemblyName System.IO.Compression.FileSystem
[System.IO.Compression.ZipFile]::CreateFromDirectory($staging, $packet)
Write-Host "Created $packet"
