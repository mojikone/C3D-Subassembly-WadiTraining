param(
    [string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"

$root = Resolve-Path (Join-Path $PSScriptRoot "..")
$project = Join-Path $root "src\WadiTrainingSubassembly\WadiTrainingSubassembly.csproj"
$output = Join-Path $root "output"
$staging = Join-Path $output "pkt-staging"
$packet = Join-Path $output "WadiTrainingLevee_W2.pkt"
$atc = Join-Path $staging "WadiTrainingLevee_W2.atc"
$dllSource = Join-Path $root "src\WadiTrainingSubassembly\bin\$Configuration\net8.0-windows\WadiTrainingSubassembly.dll"
$dllTarget = Join-Path $staging "WadiTrainingSubassembly.dll"

dotnet build $project -c $Configuration -v:minimal

if (Test-Path -LiteralPath $staging) {
    Remove-Item -LiteralPath $staging -Recurse -Force
}
New-Item -ItemType Directory -Force -Path $staging | Out-Null
Copy-Item -LiteralPath $dllSource -Destination $dllTarget -Force

$xml = @'
<?xml version="1.0" encoding="utf-8"?>
<Category xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance" xmlns:xsd="http://www.w3.org/2001/XMLSchema">
  <ItemID idValue="{2B5953A7-B56A-43C0-93D0-703FA1C522E1}" />
  <Properties>
    <ItemName>WadiTraining W2</ItemName>
    <Images>
      <Image cx="93" cy="123" />
    </Images>
    <Description>Terrain-adaptive wadi levee and scour-protection subassemblies.</Description>
    <AccessRight>1</AccessRight>
    <Time createdUniversalDateTime="2026-07-01T00:00:00" modifiedUniversalDateTime="2026-07-01T00:00:00" />
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
    <Tool Name="WadiTrainingLevee_W2">
      <ItemID idValue="{5750E652-3474-488E-81D7-76D8CA7377F6}" />
      <Properties>
        <ItemName>WadiTrainingLevee_W2</ItemName>
        <Images>
          <Image cx="64" cy="64" />
        </Images>
        <Description>Draws a levee, follows existing ground toward the thalweg, detects concave and convex trend-line breaks, and places protection only at concave breaks.</Description>
        <ToolTip>Version: W2.1</ToolTip>
        <Keywords>_WadiTrainingLevee_W2 subassembly levee scour protection wadi</Keywords>
        <Help>
          <HelpFile />
          <HelpCommand />
          <HelpData />
        </Help>
        <Time createdUniversalDateTime="2026-07-01T00:00:00" modifiedUniversalDateTime="2026-07-01T00:00:00" />
      </Properties>
      <Source />
      <StockToolRef idValue="{7F55AAC0-0256-48D7-BFA5-914702663FDE}" />
      <Data>
        <AeccDbSubassembly>
          <GeometryGenerateMode>UseDotNet</GeometryGenerateMode>
          <ConditionalSubassembly>false</ConditionalSubassembly>
          <DotNetClass Assembly="WadiTrainingSubassembly.dll">Subassembly.WadiTrainingLevee</DotNetClass>
          <Version>W2.1</Version>
          <Content DownloadLocation="" />
          <Params>
            <Version DataType="String" DisplayName="Version" Description="Internal package version.">W2.1</Version>
            <Side DataType="Long" TypeInfo="16" DisplayName="Side" Description="Civil 3D side selector for one-bank placement. None uses Bank Mode.">-1
              <Enum>
                <None DisplayName="None">-1</None>
                <Right DisplayName="Right">0</Right>
                <Left DisplayName="Left">1</Left>
              </Enum>
            </Side>
            <BankMode DataType="Long" DisplayName="Bank Mode" Description="Right or Left uses the insertion point as the wadi-side crown. Both uses bank crown offset targets from the centerline.">0
              <Enum>
                <Right DisplayName="Right">0</Right>
                <Left DisplayName="Left">1</Left>
                <Both DisplayName="Both">2</Both>
              </Enum>
            </BankMode>
            <CrownWidth DataType="Double" TypeInfo="16" DisplayName="Crown Width" Description="Horizontal crown width.">4.0</CrownWidth>
            <LeveeSideSlope DataType="Double" TypeInfo="10" DisplayName="Levee Side Slope" Description="Side slope as vertical over horizontal grade; 0.5 means 1V:2H.">0.5</LeveeSideSlope>
            <MaxScanDistance DataType="Double" TypeInfo="16" DisplayName="Max Scan Distance" Description="Maximum ground scan distance if thalweg target is not found.">250.0</MaxScanDistance>
            <AnalysisSampleInterval DataType="Double" TypeInfo="16" DisplayName="Analysis Sample Interval" Description="Spacing used only for trend analysis; surface links are drawn from Civil surface sampling.">0.5</AnalysisSampleInterval>
            <TrendWindowLength DataType="Double" TypeInfo="16" DisplayName="Trend Window Length" Description="Length on each side used to fit slope trend lines.">5.0</TrendWindowLength>
            <MinMildTrendLength DataType="Double" TypeInfo="16" DisplayName="Min Mild Trend Length" Description="Minimum fitted length of the milder surface before a break is accepted.">5.0</MinMildTrendLength>
            <MinSteepTrendLength DataType="Double" TypeInfo="16" DisplayName="Min Steep Trend Length" Description="Minimum fitted length of the steeper face before a break is accepted.">0.6</MinSteepTrendLength>
            <SlopeChangeThreshold DataType="Double" DisplayName="Slope Change Threshold" Description="Minimum steep-to-mild ratio change. Use 0.20 for 20 percent.">0.20</SlopeChangeThreshold>
            <MaxTrendResidual DataType="Double" TypeInfo="16" DisplayName="Max Trend Residual" Description="Maximum RMS vertical fit error allowed for either trend line.">0.25</MaxTrendResidual>
            <MinBreakSpacing DataType="Double" TypeInfo="16" DisplayName="Min Break Spacing" Description="Suppresses duplicate same-kind break markers inside this spacing.">5.0</MinBreakSpacing>
            <MildProtectionLength DataType="Double" TypeInfo="16" DisplayName="Mild Protection Length" Description="Surface length to protect on the mild side of a concave break.">2.0</MildProtectionLength>
            <MaxSteepProtectionLength DataType="Double" TypeInfo="16" DisplayName="Max Steep Protection Length" Description="Maximum surface length to protect up the steep side of a concave break.">3.0</MaxSteepProtectionLength>
            <MergeDistance DataType="Double" TypeInfo="16" DisplayName="Merge Distance" Description="Protection intervals closer than this are drawn as one continuous run.">5.0</MergeDistance>
            <ToeScourLength DataType="Double" TypeInfo="16" DisplayName="Toe Scour Length" Description="Surface length of scour protection from the wadi-side toe toward the thalweg.">2.0</ToeScourLength>
            <ToeApronLength DataType="Double" TypeInfo="16" DisplayName="Toe Apron Length" Description="Surface length of apron protection after the toe scour segment.">2.0</ToeApronLength>
            <BreakMarkerSize DataType="Double" TypeInfo="16" DisplayName="Break Marker Size" Description="Visible diamond marker size for detected break points.">0.5</BreakMarkerSize>
            <ShowConvexMarkers DataType="Long" DisplayName="Show Convex Markers" Description="Shows convex breaks for debugging; convex breaks do not receive protection.">1
              <Enum>
                <No DisplayName="No">0</No>
                <Yes DisplayName="Yes">1</Yes>
              </Enum>
            </ShowConvexMarkers>
          </Params>
        </AeccDbSubassembly>
        <Units>m</Units>
      </Data>
    </Tool>
  </Tools>
  <StockTools />
</Category>
'@

Set-Content -LiteralPath $atc -Value $xml -Encoding UTF8

if (Test-Path -LiteralPath $packet) {
    Remove-Item -LiteralPath $packet -Force
}

Add-Type -AssemblyName System.IO.Compression.FileSystem
[System.IO.Compression.ZipFile]::CreateFromDirectory($staging, $packet)
Write-Host "Created $packet"
