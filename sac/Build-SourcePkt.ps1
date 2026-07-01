$ErrorActionPreference = "Stop"

$root = Split-Path -Parent $PSScriptRoot
$xamlPath = Join-Path $PSScriptRoot "LeveeOnly.LeftBank.xaml"
$outputDir = Join-Path $root "output"
$packageName = "LeveeOnly.LeftBank.v013"
$toolName = "LeveeOnly_LeftBank_v013"
$packageDir = Join-Path $outputDir "$packageName.source_unzipped"
$pktPath = Join-Path $outputDir "$packageName.source.pkt"
$civilPktPath = Join-Path $outputDir "$packageName.civil3d.pkt"
$guid = "e918b289d4c54ff88397d16e7d4f0040"

if (Test-Path -LiteralPath $packageDir) {
    Remove-Item -LiteralPath $packageDir -Recurse -Force
}
if (Test-Path -LiteralPath $pktPath) {
    Remove-Item -LiteralPath $pktPath -Force
}
if (Test-Path -LiteralPath $civilPktPath) {
    Remove-Item -LiteralPath $civilPktPath -Force
}

New-Item -ItemType Directory -Force -Path $packageDir | Out-Null
Copy-Item -LiteralPath $xamlPath -Destination (Join-Path $packageDir "$guid.xaml") -Force

$atc = @"
<?xml version="1.0"?>
<Category xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance" xmlns:xsd="http://www.w3.org/2001/XMLSchema">
  <ItemID idValue="{e918b289-d4c5-4ff8-8397-d16e7d4f0040}" />
  <Properties>
    <ItemName>W1</ItemName>
    <Images>
      <Image cx="93" cy="123" />
    </Images>
  </Properties>
  <CustomData />
  <Source />
  <Palettes />
  <Packages />
  <Tools>
    <Tool Name="$toolName">
      <ItemID idValue="{e918b289-d4c5-4ff8-8397-d16e7d4f0041}" />
      <Properties>
        <ItemName>$toolName</ItemName>
        <Images>
          <Image cx="64" cy="64" />
        </Images>
        <Description>Left-bank levee with surface-snapped toe scour/apron links, trend-intersection convex/concave markers, and mild-trend confirmed concave protection.</Description>
        <ToolTip>Version: 0.13</ToolTip>
        <Help>
          <HelpFile />
          <HelpCommand />
          <HelpData />
        </Help>
      </Properties>
      <Source />
      <StockToolRef idValue="{7F55AAC0-0256-48D7-BFA5-914702663FDE}" />
      <Data>
        <AeccDbSubassembly>
          <GeometryGenerateMode>UseDotNet</GeometryGenerateMode>
          <DotNetClass Assembly="$guid.dll">Subassembly.$toolName</DotNetClass>
          <Version>0.13</Version>
          <Params>
            <Side DataType="long" TypeInfo="16" DisplayName="Side" Description="Side">-1<Enum><None DisplayName="None">-1</None><Left DisplayName="Left">1</Left><Right DisplayName="Right">0</Right></Enum></Side>
            <CrownWidth DataType="double" TypeInfo="16" DisplayName="Crown Width" Description="Levee crown width">4</CrownWidth>
            <LeveeSideSlope DataType="double" TypeInfo="16" DisplayName="Levee Side Slope" Description="Side slope as grade; 0.5 equals 1V:2H">0.5</LeveeSideSlope>
            <SampleInterval DataType="double" TypeInfo="16" DisplayName="Sample Interval" Description="Horizontal terrain sample spacing from wadi toe toward thalweg">1</SampleInterval>
            <MaxScanDistance DataType="double" TypeInfo="16" DisplayName="Max Scan Distance" Description="Fallback terrain scan distance from wadi toe when thalweg target is not assigned">250</MaxScanDistance>
            <SlopeChangeThreshold DataType="double" TypeInfo="16" DisplayName="Slope Change Threshold" Description="Relative slope trend change that triggers a terrain break">0.1</SlopeChangeThreshold>
            <ToeScourLength DataType="double" TypeInfo="16" DisplayName="Toe Scour Length" Description="Fixed surface-following scour protection length from the wadi-side toe">2</ToeScourLength>
            <ToeApronLength DataType="double" TypeInfo="16" DisplayName="Toe Apron Length" Description="Fixed surface-following apron length after the toe scour segment toward thalweg">5</ToeApronLength>
            <MinMildTrendLength DataType="double" TypeInfo="16" DisplayName="Min Mild Trend Length" Description="Minimum continuing mild run after a concave candidate before accepting the protection break">5</MinMildTrendLength>
            <MinSteepLength DataType="double" TypeInfo="16" DisplayName="Minimum Steep Length" Description="Minimum steep run length needed before a concave break is protected">0.6</MinSteepLength>
            <MildProtectionLength DataType="double" TypeInfo="16" DisplayName="Mild Protection Length" Description="Protection length placed on the confirmed mild surface after a concave break">2</MildProtectionLength>
            <MaxSteepProtectionLength DataType="double" TypeInfo="16" DisplayName="Maximum Steep Protection Length" Description="Maximum protection length extending up the steep face">3</MaxSteepProtectionLength>
            <BreakMarkerSize DataType="double" TypeInfo="16" DisplayName="Break Marker Size" Description="Half-size of visible diamond marker at detected break points">0.5</BreakMarkerSize>
            <MergeDistance DataType="double" TypeInfo="16" DisplayName="Merge Distance" Description="Maximum gap for joining protection runs">5</MergeDistance>
          </Params>
        </AeccDbSubassembly>
        <Units>m</Units>
      </Data>
    </Tool>
  </Tools>
  <StockTools />
</Category>
"@

$cfg = @"
<?xml version="1.0"?>
<Configuration xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance" xmlns:xsd="http://www.w3.org/2001/XMLSchema">
  <CreatedWith>
    <ProductName>Autodesk Subassembly Composer</ProductName>
    <Version>ForMatterhorn</Version>
    <VersionNumber>12.0.842.0</VersionNumber>
  </CreatedWith>
</Configuration>
"@

$pvd = @"
<?xml version="1.0"?>
<PreviewData xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance" xmlns:xsd="http://www.w3.org/2001/XMLSchema">
  <Superelevation>
    <CrossSlopes>
      <PreviewCrossSlope CrossSegmentType="LeftInsideLane" Slope="-0.02" IsDefined="true" />
      <PreviewCrossSlope CrossSegmentType="LeftInsideShoulder" Slope="-0.05" IsDefined="true" />
      <PreviewCrossSlope CrossSegmentType="LeftOutsideLane" Slope="-0.02" IsDefined="true" />
      <PreviewCrossSlope CrossSegmentType="LeftOutsideShoulder" Slope="-0.05" IsDefined="true" />
      <PreviewCrossSlope CrossSegmentType="RightInsideLane" Slope="-0.02" IsDefined="true" />
      <PreviewCrossSlope CrossSegmentType="RightInsideShoulder" Slope="-0.05" IsDefined="true" />
      <PreviewCrossSlope CrossSegmentType="RightOutsideLane" Slope="-0.02" IsDefined="true" />
      <PreviewCrossSlope CrossSegmentType="RightOutsideShoulder" Slope="-0.05" IsDefined="true" />
    </CrossSlopes>
  </Superelevation>
  <Cant>
    <CantParams>
      <PreviewCantParam Name="CantPivotType" Value="CenterLine" />
      <PreviewCantParam Name="LeftRail" Value="" />
      <PreviewCantParam Name="LeftRailDeltaElevation" Value="0" />
      <PreviewCantParam Name="RightRail" Value="" />
      <PreviewCantParam Name="RightRailDeltaElevation" Value="0" />
    </CantParams>
  </Cant>
</PreviewData>
"@

$emd = @"
<?xml version="1.0"?>
<EnumData xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance" xmlns:xsd="http://www.w3.org/2001/XMLSchema">
  <EnumDatas>
    <Groups />
    <DefinedVariables />
  </EnumDatas>
</EnumData>
"@

$contentTypes = @"
<?xml version="1.0" encoding="utf-8"?>
<Types xmlns="http://schemas.openxmlformats.org/package/2006/content-types">
  <Default Extension="atc" ContentType="application/octet" />
  <Default Extension="cfg" ContentType="application/octet" />
  <Default Extension="xaml" ContentType="application/octet" />
  <Default Extension="pvd" ContentType="application/octet" />
  <Default Extension="emd" ContentType="application/octet" />
</Types>
"@

$civilContentTypes = @"
<?xml version="1.0" encoding="utf-8"?>
<Types xmlns="http://schemas.openxmlformats.org/package/2006/content-types">
  <Default Extension="atc" ContentType="application/octet" />
  <Default Extension="cfg" ContentType="application/octet" />
  <Default Extension="dll" ContentType="application/octet" />
  <Default Extension="xaml" ContentType="application/octet" />
  <Default Extension="pvd" ContentType="application/octet" />
  <Default Extension="emd" ContentType="application/octet" />
</Types>
"@

Set-Content -LiteralPath (Join-Path $packageDir "$guid.atc") -Value $atc -Encoding UTF8
Set-Content -LiteralPath (Join-Path $packageDir "$guid.cfg") -Value $cfg -Encoding UTF8
Set-Content -LiteralPath (Join-Path $packageDir "$guid.pvd") -Value $pvd -Encoding UTF8
Set-Content -LiteralPath (Join-Path $packageDir "$guid.emd") -Value $emd -Encoding UTF8
Set-Content -LiteralPath (Join-Path $packageDir "[Content_Types].xml") -Value $contentTypes -Encoding UTF8

Add-Type -AssemblyName System.IO.Compression.FileSystem
[IO.Compression.ZipFile]::CreateFromDirectory($packageDir, $pktPath)

$wrapperSource = @"
using System;
using System.IO;
using System.Reflection;
using System.Windows.Forms;

[assembly: AssemblyVersion("13.8.280.0")]

namespace Subassembly
{
    public class $toolName
    {
        private static object subassemblyProxy;
        private static MethodInfo drawMethod;
        private static MethodInfo getLogicalNamesMethod;
        private static MethodInfo getInputParametersMethod;
        private static MethodInfo getOutputParametersMethod;
        private static Exception initializeException;

        static $toolName()
        {
            try
            {
                string acadFolder = Path.GetDirectoryName(Application.ExecutablePath);
                string runtimeDll = Path.Combine(acadFolder, "C3D", "SACRuntime", "Subassembly.CivilRuntime.dll");
                if (!File.Exists(runtimeDll))
                {
                    runtimeDll = Path.Combine(acadFolder, "Subassembly.CivilRuntime.dll");
                }

                Assembly runtimeAssembly = Assembly.LoadFrom(runtimeDll);
                Type proxyType = runtimeAssembly.GetType("Autodesk.SubassemblyComposer.CivilRuntime.SubassemblyProxy", true);

                drawMethod = proxyType.GetMethod("Draw");
                getLogicalNamesMethod = proxyType.GetMethod("GetLogicalNames");
                getInputParametersMethod = proxyType.GetMethod("GetInputParameters");
                getOutputParametersMethod = proxyType.GetMethod("GetOutputParameters");

                string packetMemberFile = Assembly.GetExecutingAssembly().Location;
                subassemblyProxy = Activator.CreateInstance(proxyType, new object[] { packetMemberFile });
            }
            catch (Exception ex)
            {
                initializeException = ex;
            }
        }

        private static void EnsureInitialized()
        {
            if (initializeException != null)
            {
                throw initializeException;
            }
        }

        public void Draw()
        {
            EnsureInitialized();
            drawMethod.Invoke(subassemblyProxy, null);
        }

        public void GetLogicalNames()
        {
            EnsureInitialized();
            getLogicalNamesMethod.Invoke(subassemblyProxy, null);
        }

        public void GetInputParameters()
        {
            EnsureInitialized();
            getInputParametersMethod.Invoke(subassemblyProxy, null);
        }

        public void GetOutputParameters()
        {
            EnsureInitialized();
            getOutputParametersMethod.Invoke(subassemblyProxy, null);
        }
    }
}
"@

$dllPath = Join-Path $packageDir "$guid.dll"
if (Test-Path -LiteralPath $dllPath) {
    Remove-Item -LiteralPath $dllPath -Force
}
Add-Type -TypeDefinition $wrapperSource -OutputAssembly $dllPath -ReferencedAssemblies @("System.dll", "System.Windows.Forms.dll")

Set-Content -LiteralPath (Join-Path $packageDir "[Content_Types].xml") -Value $civilContentTypes -Encoding UTF8
[IO.Compression.ZipFile]::CreateFromDirectory($packageDir, $civilPktPath)

Get-Item -LiteralPath $pktPath
Get-Item -LiteralPath $civilPktPath
