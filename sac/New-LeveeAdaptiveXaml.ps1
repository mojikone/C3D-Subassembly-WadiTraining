$ErrorActionPreference = "Stop"

$outPath = Join-Path $PSScriptRoot "LeveeOnly.LeftBank.xaml"
$sampleCount = 250
$activityId = 1

function Escape-XamlText {
    param([string]$Text)
    return [System.Security.SecurityElement]::Escape($Text)
}

function Expr {
    param([string]$Text)
    $normalized = $Text.Replace("&amp;", "&").Replace("&lt;", "<").Replace("&gt;", ">")
    return [System.Security.SecurityElement]::Escape($normalized)
}

function CodesList {
    param(
        [string]$Owner,
        [string[]]$Codes
    )
    if (-not $Codes -or $Codes.Count -eq 0) {
        return ""
    }

    $items = ($Codes | ForEach-Object { "            <InArgument x:TypeArguments=`"x:String`">$(Escape-XamlText $_)</InArgument>" }) -join "`r`n"
    return @"
          <$Owner>
            <scg:List x:TypeArguments="InArgument(x:String)" Capacity="8">
$items
            </scg:List>
          </$Owner>
"@
}

function AssignVar {
    param(
        [string]$Type,
        [string]$Name,
        [string]$Value,
        [string]$Display
    )
    $id = $script:activityId++
    $variableType = switch ($Type) {
        "x:Double" { "Double" }
        "x:Int32" { "Integer" }
        "x:String" { "String" }
        default { "Double" }
    }
    return @"
        <asa2:InternalVariableDefine ActivityId="$id" DisplayName="$(Escape-XamlText $Display) &lt;$variableType&gt;" OldVariableName="" ShowErrors="True" SubassemblyErrorCenter="[SubassemblyErrorCenter]" SubassemblyRunMode="[SubassemblyRunMode]" VariableName="$(Escape-XamlText $Name)" VariableType="$variableType">
          <asa2:InternalVariableDefine.DefaultValue>
            <InArgument x:TypeArguments="$Type">$(Expr $Value)</InArgument>
          </asa2:InternalVariableDefine.DefaultValue>
          <asa2:InternalVariableDefine.Variable>
            <OutArgument x:TypeArguments="$Type">[$Name]</OutArgument>
          </asa2:InternalVariableDefine.Variable>
        </asa2:InternalVariableDefine>
"@
}

function CreatePointDelta {
    param(
        [string]$Point,
        [string]$FromPoint,
        [string]$Dx,
        [string]$Dy,
        [string]$Display,
        [string[]]$PointCodes = @(),
        [bool]$AutoLink = $false,
        [string]$LinkName = "",
        [string[]]$LinkCodes = @()
    )
    $id = $script:activityId++
    $fromAttr = if ($FromPoint) { "FromPoint=`"$(Escape-XamlText $FromPoint)`"" } else { "FromPoint=`"{x:Null}`"" }
    $auto = if ($AutoLink) { "True" } else { "False" }
    $autoName = if ($LinkName) { "AutoLinkGeometryName=`"$(Escape-XamlText $LinkName)`"" } else { "AutoLinkGeometryName=`"{x:Null}`"" }
    $autoCodes = if ($AutoLink) { CodesList "asa2:CreatePoint.AutoLinkCodes" $LinkCodes } else { "" }
    $pointCodesXml = CodesList "asa2:CreatePoint.PointCodes" $PointCodes
    return @"
        <asa2:CreatePoint ActivityId="$id" ApplyAOR="False" AutoLink="$auto" $autoName DisplayName="$(Escape-XamlText $Display)" $fromAttr Geometry="[Geometry]" PointNumber="$(Escape-XamlText $Point)" Positioning="DeltaXAndDeltaY" ShowErrors="True" Side="[Side]" SubassemblyErrorCenter="[SubassemblyErrorCenter]" SubassemblyRunMode="[SubassemblyRunMode]">
          <asa2:CreatePoint.Arguments>
            <InArgument x:TypeArguments="x:Double" x:Key="DeltaX1">$(Expr $Dx)</InArgument>
            <InArgument x:TypeArguments="x:Double" x:Key="DeltaY1">$(Expr $Dy)</InArgument>
          </asa2:CreatePoint.Arguments>
$autoCodes
$pointCodesXml
        </asa2:CreatePoint>
"@
}

function CreatePointSlopeToSurface {
    param(
        [string]$Point,
        [string]$FromPoint,
        [string]$Slope,
        [string]$Reverse,
        [string]$LayoutDx,
        [string]$Display,
        [string[]]$PointCodes,
        [string]$LinkName,
        [string[]]$LinkCodes
    )
    $id = $script:activityId++
    $autoCodes = CodesList "asa2:CreatePoint.AutoLinkCodes" $LinkCodes
    $pointCodesXml = CodesList "asa2:CreatePoint.PointCodes" $PointCodes
    return @"
        <asa2:CreatePoint ActivityId="$id" ApplyAOR="False" AutoLink="True" AutoLinkGeometryName="$(Escape-XamlText $LinkName)" DisplayName="$(Escape-XamlText $Display)" FromPoint="$(Escape-XamlText $FromPoint)" Geometry="[Geometry]" PointNumber="$(Escape-XamlText $Point)" Positioning="SlopeToSurface" ShowErrors="True" Side="[Side]" SubassemblyErrorCenter="[SubassemblyErrorCenter]" SubassemblyRunMode="[SubassemblyRunMode]">
          <asa2:CreatePoint.Arguments>
            <InArgument x:TypeArguments="x:Double" x:Key="Slope3">$(Expr $Slope)</InArgument>
            <InArgument x:TypeArguments="x:Boolean" x:Key="ReverseSlopeDirection3">$Reverse</InArgument>
            <InArgument x:TypeArguments="asw:SurfaceTarget" x:Key="SurfaceTarget3">[ExistingGround]</InArgument>
            <InArgument x:TypeArguments="x:Double" x:Key="DeltaXForLayout3">$(Expr $LayoutDx)</InArgument>
            <InArgument x:TypeArguments="x:Boolean" x:Key="ShowErrors">True</InArgument>
          </asa2:CreatePoint.Arguments>
$autoCodes
$pointCodesXml
        </asa2:CreatePoint>
"@
}

function CreateAuxDeltaXOnSurface {
    param(
        [string]$Point,
        [string]$FromPoint,
        [string]$Dx,
        [string]$Display
    )
    $id = $script:activityId++
    return @"
        <asa2:CreatePoint AutoLinkCodes="{x:Null}" ActivityId="$id" ApplyAOR="False" AutoLink="False" AutoLinkGeometryName="{x:Null}" DisplayName="$(Escape-XamlText $Display)" FromPoint="$(Escape-XamlText $FromPoint)" Geometry="[Geometry]" PointNumber="$(Escape-XamlText $Point)" Positioning="DeltaXOnSurface" ShowErrors="False" Side="[Side]" SubassemblyErrorCenter="[SubassemblyErrorCenter]" SubassemblyRunMode="[SubassemblyRunMode]">
          <asa2:CreatePoint.Arguments>
            <InArgument x:TypeArguments="x:Double" x:Key="DeltaX4">$(Expr $Dx)</InArgument>
            <InArgument x:TypeArguments="asw:SurfaceTarget" x:Key="SurfaceTarget4">[ExistingGround]</InArgument>
            <InArgument x:TypeArguments="asw:OffsetTarget" x:Key="OffsetTarget4" />
            <InArgument x:TypeArguments="x:Double" x:Key="DeltaYForLayout4">0</InArgument>
          </asa2:CreatePoint.Arguments>
        </asa2:CreatePoint>
"@
}

function CreatePointDeltaXOnSurface {
    param(
        [string]$Point,
        [string]$FromPoint,
        [string]$Dx,
        [string]$Display,
        [string[]]$PointCodes = @(),
        [bool]$ShowErrors = $false
    )
    $id = $script:activityId++
    $show = if ($ShowErrors) { "True" } else { "False" }
    $pointCodesXml = CodesList "asa2:CreatePoint.PointCodes" $PointCodes
    return @"
        <asa2:CreatePoint AutoLinkCodes="{x:Null}" ActivityId="$id" ApplyAOR="False" AutoLink="False" AutoLinkGeometryName="{x:Null}" DisplayName="$(Escape-XamlText $Display)" FromPoint="$(Escape-XamlText $FromPoint)" Geometry="[Geometry]" PointNumber="$(Escape-XamlText $Point)" Positioning="DeltaXOnSurface" ShowErrors="$show" Side="[Side]" SubassemblyErrorCenter="[SubassemblyErrorCenter]" SubassemblyRunMode="[SubassemblyRunMode]">
          <asa2:CreatePoint.Arguments>
            <InArgument x:TypeArguments="x:Double" x:Key="DeltaX4">$(Expr $Dx)</InArgument>
            <InArgument x:TypeArguments="asw:SurfaceTarget" x:Key="SurfaceTarget4">[ExistingGround]</InArgument>
            <InArgument x:TypeArguments="asw:OffsetTarget" x:Key="OffsetTarget4" />
            <InArgument x:TypeArguments="x:Double" x:Key="DeltaYForLayout4">0</InArgument>
          </asa2:CreatePoint.Arguments>
$pointCodesXml
        </asa2:CreatePoint>
"@
}

function CreateLink {
    param(
        [string]$Name,
        [string]$Start,
        [string]$End,
        [string]$Display,
        [string[]]$Codes
    )
    $id = $script:activityId++
    $codesXml = CodesList "asa2:CreateLink.LinkCodes" $Codes
    return @"
        <asa2:CreateLink ActivityId="$id" ApplyAOR="False" DisplayName="$(Escape-XamlText $Display)" EndPoint="$(Escape-XamlText $End)" Geometry="[Geometry]" IsEnabled="True" LinkNumber="$(Escape-XamlText $Name)" ShowErrors="True" StartPoint="$(Escape-XamlText $Start)" SubassemblyErrorCenter="[SubassemblyErrorCenter]" SubassemblyRunMode="[SubassemblyRunMode]">
$codesXml
        </asa2:CreateLink>
"@
}

function CreateStripSurface {
    param(
        [string]$Name,
        [string]$StartOffset,
        [string]$EndOffset,
        [string]$StartPoint,
        [string]$EndPoint,
        [string]$Display,
        [string[]]$LinkCodes,
        [string[]]$StartPointCodes = @(),
        [string[]]$EndPointCodes = @()
    )
    $id = $script:activityId++
    $startOffsetExpr = ToStripSurfaceOffset $StartOffset
    $endOffsetExpr = ToStripSurfaceOffset $EndOffset
    $linkCodesXml = CodesList "asa2:StripSurfaceActivity.LinkCodes" $LinkCodes
    $startCodesXml = CodesList "asa2:StripSurfaceActivity.StartPointCodes" $StartPointCodes
    $endCodesXml = CodesList "asa2:StripSurfaceActivity.EndPointCodes" $EndPointCodes
    return @"
        <asa2:StripSurfaceActivity EndOffsetTarget="{x:Null}" StartOffsetTarget="{x:Null}" ActivityId="$id" Depth="0" DepthForLayoutMode="[-P3.Y]" DisplayName="$(Escape-XamlText $Display)" EndOffset="$(Expr $endOffsetExpr)" EndPointName="$(Escape-XamlText $EndPoint)" Geometry="[Geometry]" LinkNumber="$(Escape-XamlText $Name)" ShowErrors="True" Side="[Side]" StartOffset="$(Expr $startOffsetExpr)" StartPointName="$(Escape-XamlText $StartPoint)" SubassemblyErrorCenter="[SubassemblyErrorCenter]" SubassemblyRunMode="[SubassemblyRunMode]" Surface="[ExistingGround]">
$linkCodesXml
$startCodesXml
$endCodesXml
        </asa2:StripSurfaceActivity>
"@
}

function ToStripSurfaceOffset {
    param([string]$Offset)
    $text = $Offset.Trim()
    if ($text.StartsWith("[") -and $text.EndsWith("]")) {
        $inner = $text.Substring(1, $text.Length - 2)
        return "[$inner + P3.X]"
    }
    return "[$text + P3.X]"
}

function CreateBreakMarkerActivities {
    param(
        [string]$Prefix,
        [string]$Center,
        [string[]]$Codes
    )
    return @(
        (CreatePointDelta "$($Prefix)T" $Center "0" "[BreakMarkerSize]" "$Prefix marker top" $Codes),
        (CreatePointDelta "$($Prefix)R" $Center "[BreakMarkerSize]" "0" "$Prefix marker right" $Codes),
        (CreatePointDelta "$($Prefix)B" $Center "0" "[-BreakMarkerSize]" "$Prefix marker bottom" $Codes),
        (CreatePointDelta "$($Prefix)L" $Center "[-BreakMarkerSize]" "0" "$Prefix marker left" $Codes),
        (CreateLink "$($Prefix)M1" "$($Prefix)T" "$($Prefix)R" "$Prefix marker" $Codes),
        (CreateLink "$($Prefix)M2" "$($Prefix)R" "$($Prefix)B" "$Prefix marker" $Codes),
        (CreateLink "$($Prefix)M3" "$($Prefix)B" "$($Prefix)L" "$Prefix marker" $Codes),
        (CreateLink "$($Prefix)M4" "$($Prefix)L" "$($Prefix)T" "$Prefix marker" $Codes)
    )
}

function IfBlock {
    param(
        [string]$Condition,
        [string]$Display,
        [string[]]$Then,
        [string[]]$Else = @()
    )
    $thenBody = ($Then -join "`r`n")
    $elseText = ""
    if ($Else -and $Else.Count -gt 0) {
        $elseBody = ($Else -join "`r`n")
        $elseText = @"
          <If.Else>
            <Sequence DisplayName="$(Escape-XamlText "$Display Else")">
$elseBody
            </Sequence>
          </If.Else>
"@
    }

    return @"
        <If Condition="$(Expr "[$Condition]")" DisplayName="$(Escape-XamlText $Display)">
          <If.Then>
            <Sequence DisplayName="$(Escape-XamlText "$Display Then")">
$thenBody
            </Sequence>
          </If.Then>
$elseText
        </If>
"@
}

$nodeId = 0
$flowNodes = New-Object System.Collections.Generic.List[string]
function New-FlowName {
    $name = "__ReferenceID$script:nodeId"
    $script:nodeId++
    return $name
}

function AddFlowStep {
    param([string]$Activity, [string]$Next = "")
    $name = New-FlowName
    $nextText = ""
    if ($Next) {
        $nextText = @"
        <FlowStep.Next>
          <x:Reference>$Next</x:Reference>
        </FlowStep.Next>
"@
    }
    $script:flowNodes.Add(@"
      <FlowStep x:Name="$name">
$Activity
$nextText
      </FlowStep>
"@)
    return $name
}

function AddFlowDecision {
    param([string]$Condition, [string]$TrueNode, [string]$FalseNode = "")
    $name = New-FlowName
    $falseText = ""
    if ($FalseNode) {
        $falseText = @"
        <FlowDecision.False>
          <x:Reference>$FalseNode</x:Reference>
        </FlowDecision.False>
"@
    }
    $script:flowNodes.Add(@"
      <FlowDecision x:Name="$name" Condition="$(Expr "[$Condition]")">
        <FlowDecision.True>
          <x:Reference>$TrueNode</x:Reference>
        </FlowDecision.True>
$falseText
      </FlowDecision>
"@)
    return $name
}

function ChainSteps {
    param([string[]]$Activities, [string]$Next = "")
    $node = $Next
    for ($idx = $Activities.Count - 1; $idx -ge 0; $idx--) {
        $node = AddFlowStep $Activities[$idx] $node
    }
    return $node
}

function BuildCandidateProtectionNode {
    param([int]$I, [string]$CurrentPoint, [string]$Next)
    $steepHorizontal = "Math.Min(Math.Max(CandidateSteepLength, 0), MaxSteepProtectionLength / Math.Sqrt(1 + CandidateSteepSlope * CandidateSteepSlope))"
    $protStartX = "(CandidateX - ($steepHorizontal))"
    $intersectionX = "Math.Min(Math.Max((((CandidateMildStartY - MildTrendSlope * CandidateMildStartX) - (RefY - CandidateSteepSlope * RefX)) / (CandidateSteepSlope - MildTrendSlope)), CandidateSteepEndX), CandidateMildStartX)"
    $mergeCondition = "HasProtection = 1 AndAlso (ProtectionStartX - LastProtectionEndX) <= MergeDistance"

    $afterMerge = ChainSteps @(
        (CreateLink "PRS$I" "PS$I" "CC$I" "PRS$I steep protection" @("ProtectionCyan", "Protection", "ProtectionSteep")),
        (CreateLink "PRM$I" "CC$I" "PM$I" "PRM$I mild protection" @("ProtectionCyan", "Protection", "ProtectionMild")),
        (AssignVar "x:Int32" "HasProtection" "1" "HasProtection = 1"),
        (AssignVar "x:Double" "LastProtectionStartX" "[If(LastProtectionEndX > 0 AndAlso (ProtectionStartX - LastProtectionEndX) <= MergeDistance, LastProtectionStartX, ProtectionStartX)]" "LastProtectionStartX = merged/start"),
        (AssignVar "x:Double" "LastProtectionEndX" "[ProtectionEndX]" "LastProtectionEndX = PM$I"),
        (AssignVar "x:Double" "SurfaceRunStartX" "[ProtectionEndX]" "SurfaceRunStartX = PM$I"),
        (AssignVar "x:Double" "RefX" "[CandidateX]" "RefX = confirmed concave"),
        (AssignVar "x:Double" "RefY" "[CandidateY]" "RefY = confirmed concave"),
        (AssignVar "x:Double" "PrevSlope" "[MildTrendSlope]" "PrevSlope = confirmed mild trend"),
        (AssignVar "x:Int32" "CandidateActive" "0" "CandidateActive = 0"),
        (AssignVar "x:Int32" "HavePrevSlope" "1" "HavePrevSlope = 1 after protection")
    ) $Next
    $mergeGap = AddFlowDecision "ProtectionStartX - LastProtectionEndX > 0.001" (ChainSteps @(
        (CreatePointDeltaXOnSurface "MGS$I" "P3" "[LastProtectionEndX - P3.X]" "MGS$I merge start on surface" @("ProtectionCyan") $false),
        (CreatePointDeltaXOnSurface "MGE$I" "P3" "[ProtectionStartX - P3.X]" "MGE$I merge end on surface" @("ProtectionCyan") $false),
        (CreateLink "MG$I" "MGS$I" "MGE$I" "MG$I merged protection" @("ProtectionCyan", "Protection", "ProtectionMerge"))
    ) $afterMerge) $afterMerge
    $surfaceGap = AddFlowDecision "ProtectionStartX - SurfaceRunStartX > 0.001" (ChainSteps @(
        (CreatePointDeltaXOnSurface "SFS$I" "P3" "[SurfaceRunStartX - P3.X]" "SFS$I surface start" @("SurfaceYellow") $false),
        (CreatePointDeltaXOnSurface "SFE$I" "P3" "[ProtectionStartX - P3.X]" "SFE$I surface end" @("SurfaceYellow") $false),
        (CreateLink "SF$I" "SFS$I" "SFE$I" "SF$I ground surface" @("SurfaceYellow", "ExistingGround"))
    ) $afterMerge) $afterMerge
    $preProtectionNode = AddFlowDecision $mergeCondition $mergeGap $surfaceGap

    $markerActivities = CreateBreakMarkerActivities "CCM$I" "CC$I" @("ConcaveBreakMarker", "ProtectionCyan", "BreakMarker")
    return ChainSteps (@(
        (AssignVar "x:Double" "CandidateX" "[$intersectionX]" "CandidateX = trend intersection $I"),
        (AssignVar "x:Double" "CandidateY" "[RefY + CandidateSteepSlope * (CandidateX - RefX)]" "CandidateY = trend intersection $I"),
        (AssignVar "x:Double" "CandidateSteepLength" "[CandidateX - RefX]" "CandidateSteepLength = intersection"),
        (AssignVar "x:Double" "ProtectionStartX" "[$protStartX]" "ProtectionStartX = $I"),
        (AssignVar "x:Double" "ProtectionEndX" "[CandidateX + MildProtectionLength]" "ProtectionEndX = $I"),
        (CreatePointDeltaXOnSurface "CC$I" $CurrentPoint "[CandidateX - $CurrentPoint.X]" "CC$I confirmed concave trend intersection on surface" @("ProtectionCyan", "ConcaveBreak", "ProtectionBreak") $true),
        (CreatePointDeltaXOnSurface "PS$I" "CC$I" "[ProtectionStartX - CandidateX]" "PS$I protection start on surface" @("ProtectionCyan", "ProtectionStart") $false),
        (CreatePointDeltaXOnSurface "PM$I" "CC$I" "[ProtectionEndX - CandidateX]" "PM$I protection end on surface" @("ProtectionCyan", "ProtectionEnd") $false)
    ) + $markerActivities) $preProtectionNode
}

function BuildSampleNode {
    param([int]$I, [string]$Next)
    $ap = "AP$I"
    $prev = if ($I -eq 1) { "P3" } else { "AP$($I - 1)" }
    if ($I -eq 1) {
        $sampleChain = ChainSteps @(
            (CreateAuxDeltaXOnSurface $ap $prev "[SampleInterval]" "$ap 1m ground sample"),
            (AssignVar "x:Double" "LastSampleX" "[$ap.X]" "LastSampleX = $ap"),
            (AssignVar "x:Double" "CurrSlope" "[If(Math.Abs($ap.X - RefX) < 0.000001, 0, ($ap.Y - RefY) / ($ap.X - RefX))]" "CurrSlope $ap"),
            (AssignVar "x:Double" "LocalSlope" "[CurrSlope]" "LocalSlope AP1"),
            (AssignVar "x:Double" "PrevSlope" "[CurrSlope]" "PrevSlope = AP1"),
            (AssignVar "x:Int32" "HavePrevSlope" "1" "HavePrevSlope = 1")
        ) $Next
    } else {
        $breakPoint = "AP$($I - 1)"
        $trendEnd = if ($I -eq 2) { "P3" } else { "AP$($I - 2)" }
        $afterCandidate = ChainSteps @(
            (AssignVar "x:Int32" "CandidateActive" "1" "CandidateActive = 1"),
            (AssignVar "x:Double" "CandidateX" "[$breakPoint.X]" "CandidateX = $breakPoint"),
            (AssignVar "x:Double" "CandidateY" "[$breakPoint.Y]" "CandidateY = $breakPoint"),
            (AssignVar "x:Double" "CandidateSteepEndX" "[$breakPoint.X]" "CandidateSteepEndX = $breakPoint"),
            (AssignVar "x:Double" "CandidateSteepEndY" "[$breakPoint.Y]" "CandidateSteepEndY = $breakPoint"),
            (AssignVar "x:Double" "CandidateMildStartX" "[$ap.X]" "CandidateMildStartX = $ap"),
            (AssignVar "x:Double" "CandidateMildStartY" "[$ap.Y]" "CandidateMildStartY = $ap"),
            (AssignVar "x:Double" "CandidateSteepSlope" "[PrevSlope]" "CandidateSteepSlope = PrevSlope"),
            (AssignVar "x:Double" "CandidateMildSlope" "[LocalSlope]" "CandidateMildSlope = LocalSlope"),
            (AssignVar "x:Double" "CandidateSteepLength" "[$breakPoint.X - RefX]" "CandidateSteepLength = $breakPoint - Ref"),
            (AssignVar "x:Int32" "HavePrevSlope" "1" "HavePrevSlope = 1 after candidate")
        ) $Next
        $ignoredBreakUpdate = ChainSteps @(
            (AssignVar "x:Double" "RefX" "[$breakPoint.X]" "RefX = ignored break"),
            (AssignVar "x:Double" "RefY" "[$breakPoint.Y]" "RefY = ignored break"),
            (AssignVar "x:Double" "PrevSlope" "[LocalSlope]" "PrevSlope = ignored break"),
            (AssignVar "x:Int32" "CandidateActive" "0" "CandidateActive = 0 ignored break"),
            (AssignVar "x:Int32" "HavePrevSlope" "1" "HavePrevSlope = 1 ignored break")
        ) $Next
        $convexRawX = "(((($ap.Y - LocalSlope * $ap.X) - (RefY - ConvexBeforeSlope * RefX)) / (ConvexBeforeSlope - LocalSlope)))"
        $afterBreak = ChainSteps (@(
            (AssignVar "x:Double" "ConvexBeforeSlope" "[If(Math.Abs($trendEnd.X - RefX) < 0.000001, PrevSlope, ($trendEnd.Y - RefY) / ($trendEnd.X - RefX))]" "ConvexBeforeSlope $I"),
            (AssignVar "x:Double" "ConvexX" "[If(Math.Abs(ConvexBeforeSlope - LocalSlope) < 0.000001, $breakPoint.X, Math.Min(Math.Max($convexRawX, $trendEnd.X), $ap.X))]" "ConvexX = trend intersection $I"),
            (AssignVar "x:Double" "ConvexY" "[RefY + ConvexBeforeSlope * (ConvexX - RefX)]" "ConvexY = trend intersection $I"),
            (CreatePointDeltaXOnSurface "CV$I" $ap "[ConvexX - $ap.X]" "CV$I ignored convex trend intersection" @("ConvexBreakMarker", "ConvexBreak", "IgnoredBreak") $true)
        ) + (CreateBreakMarkerActivities "CVM$I" "CV$I" @("ConvexBreakMarker", "IgnoredBreak", "BreakMarker")) + @(
            (AssignVar "x:Double" "RefX" "[ConvexX]" "RefX = convex intersection"),
            (AssignVar "x:Double" "RefY" "[ConvexY]" "RefY = convex intersection"),
            (AssignVar "x:Double" "PrevSlope" "[LocalSlope]" "PrevSlope = LocalSlope after break"),
            (AssignVar "x:Int32" "CandidateActive" "0" "CandidateActive = 0 after convex"),
            (AssignVar "x:Int32" "HavePrevSlope" "1" "HavePrevSlope = 1 after break")
        )) $Next
        $convexCondition = "LocalSlope < PrevSlope AndAlso Math.Abs(LocalSlope) > Math.Abs(PrevSlope)"
        $afterProtection = AddFlowDecision $convexCondition $afterBreak $ignoredBreakUpdate
        $candidateCondition = "PrevSlope < LocalSlope AndAlso Math.Abs(PrevSlope) > Math.Abs(LocalSlope) AndAlso ($breakPoint.X - RefX) >= MinSteepLength AndAlso $breakPoint.X >= FixedToeProtectionEndX"
        $protectNode = AddFlowDecision $candidateCondition $afterCandidate $afterProtection
        $noBreak = ChainSteps @(
            (AssignVar "x:Double" "PrevSlope" "[CurrSlope]" "PrevSlope = CurrSlope no break"),
            (AssignVar "x:Int32" "HavePrevSlope" "1" "HavePrevSlope = 1 no break")
        ) $Next
        $breakCondition = "HavePrevSlope = 1 AndAlso Math.Abs(CurrSlope - PrevSlope) >= SlopeChangeThreshold * Math.Max(Math.Abs(PrevSlope), 0.001)"
        $breakDecision = AddFlowDecision $breakCondition $protectNode $noBreak
        $candidateReject = ChainSteps @(
            (AssignVar "x:Int32" "CandidateActive" "0" "CandidateActive = 0 rejected"),
            (AssignVar "x:Double" "RefX" "[CandidateMildStartX]" "RefX = rejected mild start"),
            (AssignVar "x:Double" "RefY" "[CandidateMildStartY]" "RefY = rejected mild start"),
            (AssignVar "x:Double" "PrevSlope" "[MildTrendSlope]" "PrevSlope = rejected candidate"),
            (AssignVar "x:Int32" "HavePrevSlope" "1" "HavePrevSlope = 1 rejected")
        ) $Next
        $mildIsValid = "CandidateSteepSlope < MildTrendSlope AndAlso Math.Abs(CandidateSteepSlope) > Math.Abs(MildTrendSlope)"
        $rejectCondition = "($ap.X - CandidateMildStartX) >= MinMildTrendLength AndAlso Not($mildIsValid)"
        $candidateWait = AddFlowDecision $rejectCondition $candidateReject $Next
        $confirmCondition = "CandidateActive = 1 AndAlso ($ap.X - CandidateMildStartX) >= MinMildTrendLength AndAlso $mildIsValid AndAlso CandidateSteepLength >= MinSteepLength AndAlso CandidateMildStartX + MildProtectionLength <= ScanLimitX AndAlso Math.Abs(CandidateSteepSlope - MildTrendSlope) > 0.000001"
        $confirmDecision = AddFlowDecision "CandidateActive = 1" (AddFlowDecision $confirmCondition (BuildCandidateProtectionNode $I $ap $Next) $candidateWait) $breakDecision
        $sampleChain = ChainSteps @(
            (CreateAuxDeltaXOnSurface $ap $prev "[SampleInterval]" "$ap 1m ground sample"),
            (AssignVar "x:Double" "LastSampleX" "[$ap.X]" "LastSampleX = $ap"),
            (AssignVar "x:Double" "CurrSlope" "[If(Math.Abs($ap.X - RefX) < 0.000001, 0, ($ap.Y - RefY) / ($ap.X - RefX))]" "CurrSlope $ap"),
            (AssignVar "x:Double" "LocalSlope" "[If(Math.Abs($ap.X - $breakPoint.X) < 0.000001, 0, ($ap.Y - $breakPoint.Y) / ($ap.X - $breakPoint.X))]" "LocalSlope $breakPoint to $ap"),
            (AssignVar "x:Double" "MildTrendSlope" "[If(CandidateActive = 1 AndAlso Math.Abs($ap.X - CandidateMildStartX) >= 0.000001, ($ap.Y - CandidateMildStartY) / ($ap.X - CandidateMildStartX), LocalSlope)]" "MildTrendSlope $ap")
        ) $confirmDecision
    }
    $scanCondition = "P3.X + ($I * SampleInterval) <= ScanLimitX"
    return AddFlowDecision $scanCondition $sampleChain $Next
}

$finishNode = AddFlowDecision "ScanLimitX - SurfaceRunStartX > 0.001" (ChainSteps @(
    (CreatePointDeltaXOnSurface "SFFS" "P3" "[SurfaceRunStartX - P3.X]" "SFFS final surface start" @("SurfaceYellow") $false),
    (CreatePointDeltaXOnSurface "SFFE" "P3" "[ScanLimitX - P3.X]" "SFFE final surface end" @("SurfaceYellow") $false),
    (CreateLink "SF_Final" "SFFS" "SFFE" "SF final ground surface" @("SurfaceYellow", "ExistingGround"))
)) ""

$sampleNode = $finishNode
for ($i = $sampleCount; $i -ge 1; $i--) {
    $sampleNode = BuildSampleNode $i $sampleNode
}

$flowStartNode = ChainSteps @(
    (CreatePointDelta "P1" $null "0" "0" "P1 Origin" @("Crown", "WadiCrownEdge")),
    (CreatePointDelta "P2" "P1" "[-CrownWidth]" "0" "P2 Crown Back and L_Crown" @("Crown", "LandCrownEdge") $true "L_Crown" @("Top", "Crown")),
    (CreatePointSlopeToSurface "P3" "P1" "[-LeveeSideSlope]" "False" "10" "P3 Wadi Toe and L_WadiFace" @("WadiToe") "L_WadiFace" @("Top", "WadiFace")),
    (CreatePointSlopeToSurface "P4" "P2" "[-LeveeSideSlope]" "True" "-10" "P4 Land Toe and L_LandFace" @("LandToe") "L_LandFace" @("Top", "LandFace")),
    (AssignVar "x:Double" "ScanLimitX" "[If(ThalwegOffset.IsValid, ThalwegOffset.Offset, P3.X + MaxScanDistance)]" "ScanLimitX"),
    (AssignVar "x:Double" "ToeScourEndX" "[Math.Min(P3.X + ToeScourLength, ScanLimitX)]" "ToeScourEndX"),
    (AssignVar "x:Double" "FixedToeProtectionEndX" "[Math.Min(P3.X + ToeScourLength + ToeApronLength, ScanLimitX)]" "FixedToeProtectionEndX"),
    (CreatePointDeltaXOnSurface "TSCE" "P3" "[ToeScourEndX - P3.X]" "TSCE toe scour end" @("ProtectionCyan", "ToeScourEnd") $false),
    (CreateLink "TSC" "P3" "TSCE" "TSC toe scour protection" @("ProtectionCyan", "Protection", "ToeScourProtection")),
    (CreatePointDeltaXOnSurface "TAPE" "P3" "[FixedToeProtectionEndX - P3.X]" "TAPE toe apron end" @("ProtectionCyan", "ToeApronEnd") $false),
    (CreateLink "TAP" "TSCE" "TAPE" "TAP toe apron protection" @("ProtectionCyan", "Protection", "ToeApronProtection")),
    (AssignVar "x:Double" "SurfaceRunStartX" "[FixedToeProtectionEndX]" "SurfaceRunStartX = fixed toe protection end"),
    (AssignVar "x:Double" "LastSampleX" "[P3.X]" "LastSampleX = P3"),
    (AssignVar "x:Double" "RefX" "[P3.X]" "RefX = P3"),
    (AssignVar "x:Double" "RefY" "[P3.Y]" "RefY = P3"),
    (AssignVar "x:Double" "PrevSlope" "0" "PrevSlope = 0"),
    (AssignVar "x:Int32" "HavePrevSlope" "0" "HavePrevSlope = 0"),
    (AssignVar "x:Int32" "HasProtection" "[If(FixedToeProtectionEndX - P3.X > 0.001, 1, 0)]" "HasProtection = fixed toe protection"),
    (AssignVar "x:Double" "LastProtectionStartX" "[P3.X]" "LastProtectionStartX = fixed toe start"),
    (AssignVar "x:Double" "LastProtectionEndX" "[FixedToeProtectionEndX]" "LastProtectionEndX = fixed toe end"),
    (AssignVar "x:Double" "ProtectionStartX" "0" "ProtectionStartX = 0"),
    (AssignVar "x:Double" "ProtectionEndX" "0" "ProtectionEndX = 0"),
    (AssignVar "x:Int32" "CandidateActive" "0" "CandidateActive = 0"),
    (AssignVar "x:Double" "CandidateX" "0" "CandidateX = 0"),
    (AssignVar "x:Double" "CandidateY" "0" "CandidateY = 0"),
    (AssignVar "x:Double" "CandidateSteepEndX" "0" "CandidateSteepEndX = 0"),
    (AssignVar "x:Double" "CandidateSteepEndY" "0" "CandidateSteepEndY = 0"),
    (AssignVar "x:Double" "CandidateMildStartX" "0" "CandidateMildStartX = 0"),
    (AssignVar "x:Double" "CandidateMildStartY" "0" "CandidateMildStartY = 0"),
    (AssignVar "x:Double" "CandidateSteepSlope" "0" "CandidateSteepSlope = 0"),
    (AssignVar "x:Double" "CandidateMildSlope" "0" "CandidateMildSlope = 0"),
    (AssignVar "x:Double" "CandidateSteepLength" "0" "CandidateSteepLength = 0"),
    (AssignVar "x:Double" "MildTrendSlope" "0" "MildTrendSlope = 0"),
    (AssignVar "x:Double" "ConvexBeforeSlope" "0" "ConvexBeforeSlope = 0"),
    (AssignVar "x:Double" "ConvexX" "0" "ConvexX = 0"),
    (AssignVar "x:Double" "ConvexY" "0" "ConvexY = 0")
) $sampleNode

$flowNodesText = $flowNodes -join "`r`n"

$xaml = @"
<Activity mc:Ignorable="sads sap"
 xmlns="http://schemas.microsoft.com/netfx/2009/xaml/activities"
 xmlns:asa="clr-namespace:Autodesk.SubassemblyComposer.API;assembly=Subassembly.API"
 xmlns:asa1="clr-namespace:Autodesk.SubassemblyComposer.API;assembly=Subassembly.WorkflowEngine"
 xmlns:asa2="clr-namespace:Autodesk.SubassemblyComposer.ActivityLibrary;assembly=Subassembly.ActivityLibrary"
 xmlns:asw="clr-namespace:Autodesk.SubassemblyComposer.WorkflowEngine;assembly=Subassembly.WorkflowEngine"
 xmlns:av="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
 xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
 xmlns:mva="clr-namespace:Microsoft.VisualBasic.Activities;assembly=System.Activities"
 xmlns:s="clr-namespace:System;assembly=mscorlib"
 xmlns:s1="clr-namespace:System;assembly=System"
 xmlns:s2="clr-namespace:System;assembly=System.Core"
 xmlns:s3="clr-namespace:System;assembly=System.ServiceModel"
 xmlns:sa="clr-namespace:System.Activities;assembly=System.Activities"
 xmlns:sads="http://schemas.microsoft.com/netfx/2010/xaml/activities/debugger"
 xmlns:sap="http://schemas.microsoft.com/netfx/2009/xaml/activities/presentation"
 xmlns:scg="clr-namespace:System.Collections.Generic;assembly=mscorlib"
 xmlns:this="clr-namespace:"
 xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
 x:Class="Subassembly"
 this:Subassembly.Side="[new EnumType(-1, &quot;None&quot;, &quot;Side&quot;)]"
 this:Subassembly.ExistingGround="[new PreviewSurfaceTarget(&quot;ExistingGround&quot;, True, 1000, -5, -1000, -5)]"
 this:Subassembly.ThalwegOffset="[new PreviewOffsetTarget(&quot;ThalwegOffset&quot;, True, 50)]"
 this:Subassembly.CrownWidth="4"
 this:Subassembly.LeveeSideSlope="0.5"
 this:Subassembly.SampleInterval="1"
 this:Subassembly.MaxScanDistance="250"
 this:Subassembly.SlopeChangeThreshold="0.1"
 this:Subassembly.ToeScourLength="2"
 this:Subassembly.ToeApronLength="5"
 this:Subassembly.MildProtectionLength="2"
 this:Subassembly.MinMildTrendLength="5"
 this:Subassembly.MinSteepLength="0.6"
 this:Subassembly.MaxSteepProtectionLength="3"
 this:Subassembly.BreakMarkerSize="0.5"
 this:Subassembly.MergeDistance="5">
  <x:Members>
    <x:Property Name="Geometry" Type="InOutArgument(asw:Geometry)" />
    <x:Property Name="SubassemblyErrorCenter" Type="InOutArgument(asw:SubassemblyErrorCenter)" />
    <x:Property Name="SubassemblyRunMode" Type="InOutArgument(asw:SubassemblyRunMode)" />
    <x:Property Name="Side" Type="InArgument(asw:EnumType)" />
    <x:Property Name="ExistingGround" Type="InArgument(asw:SurfaceTarget)">
      <x:Property.Attributes>
        <asw:EnabledFlag2Attribute EnabledFlag="True" />
        <asw:DisplayName2Attribute DisplayName="Existing Ground" />
      </x:Property.Attributes>
    </x:Property>
    <x:Property Name="ThalwegOffset" Type="InArgument(asw:OffsetTarget)">
      <x:Property.Attributes>
        <asw:EnabledFlag2Attribute EnabledFlag="True" />
        <asw:DisplayName2Attribute DisplayName="Thalweg Offset" />
      </x:Property.Attributes>
    </x:Property>
    <x:Property Name="CrownWidth" Type="InArgument(x:Double)" />
    <x:Property Name="LeveeSideSlope" Type="InArgument(x:Double)" />
    <x:Property Name="SampleInterval" Type="InArgument(x:Double)" />
    <x:Property Name="MaxScanDistance" Type="InArgument(x:Double)" />
    <x:Property Name="SlopeChangeThreshold" Type="InArgument(x:Double)" />
    <x:Property Name="ToeScourLength" Type="InArgument(x:Double)" />
    <x:Property Name="ToeApronLength" Type="InArgument(x:Double)" />
    <x:Property Name="MildProtectionLength" Type="InArgument(x:Double)" />
    <x:Property Name="MinMildTrendLength" Type="InArgument(x:Double)" />
    <x:Property Name="MinSteepLength" Type="InArgument(x:Double)" />
    <x:Property Name="MaxSteepProtectionLength" Type="InArgument(x:Double)" />
    <x:Property Name="BreakMarkerSize" Type="InArgument(x:Double)" />
    <x:Property Name="MergeDistance" Type="InArgument(x:Double)" />
  </x:Members>
  <mva:VisualBasic.Settings>Assembly references and imported namespaces serialized as XML namespaces</mva:VisualBasic.Settings>
  <Flowchart mva:VisualBasic.Settings="Assembly references and imported namespaces serialized as XML namespaces">
    <Flowchart.Variables>
      <Variable x:TypeArguments="x:Double" Name="RefX" />
      <Variable x:TypeArguments="x:Double" Name="RefY" />
      <Variable x:TypeArguments="x:Double" Name="PrevSlope" />
      <Variable x:TypeArguments="x:Double" Name="CurrSlope" />
      <Variable x:TypeArguments="x:Double" Name="LocalSlope" />
      <Variable x:TypeArguments="x:Int32" Name="HavePrevSlope" />
      <Variable x:TypeArguments="x:Int32" Name="HasProtection" />
      <Variable x:TypeArguments="x:Double" Name="ScanLimitX" />
      <Variable x:TypeArguments="x:Double" Name="LastSampleX" />
      <Variable x:TypeArguments="x:Double" Name="SurfaceRunStartX" />
      <Variable x:TypeArguments="x:Double" Name="ToeScourEndX" />
      <Variable x:TypeArguments="x:Double" Name="FixedToeProtectionEndX" />
      <Variable x:TypeArguments="x:Double" Name="LastProtectionStartX" />
      <Variable x:TypeArguments="x:Double" Name="LastProtectionEndX" />
      <Variable x:TypeArguments="x:Double" Name="ProtectionStartX" />
      <Variable x:TypeArguments="x:Double" Name="ProtectionEndX" />
      <Variable x:TypeArguments="x:Int32" Name="CandidateActive" />
      <Variable x:TypeArguments="x:Double" Name="CandidateX" />
      <Variable x:TypeArguments="x:Double" Name="CandidateY" />
      <Variable x:TypeArguments="x:Double" Name="CandidateSteepEndX" />
      <Variable x:TypeArguments="x:Double" Name="CandidateSteepEndY" />
      <Variable x:TypeArguments="x:Double" Name="CandidateMildStartX" />
      <Variable x:TypeArguments="x:Double" Name="CandidateMildStartY" />
      <Variable x:TypeArguments="x:Double" Name="CandidateSteepSlope" />
      <Variable x:TypeArguments="x:Double" Name="CandidateMildSlope" />
      <Variable x:TypeArguments="x:Double" Name="CandidateSteepLength" />
      <Variable x:TypeArguments="x:Double" Name="MildTrendSlope" />
      <Variable x:TypeArguments="x:Double" Name="ConvexBeforeSlope" />
      <Variable x:TypeArguments="x:Double" Name="ConvexX" />
      <Variable x:TypeArguments="x:Double" Name="ConvexY" />
      <Variable x:TypeArguments="asw:EnumType" Default="[new EnumType(1, &quot;Left&quot;)]" Modifiers="ReadOnly" Name="Left" />
      <Variable x:TypeArguments="asw:EnumType" Default="[new EnumType(11, &quot;No&quot;)]" Modifiers="ReadOnly" Name="No" />
      <Variable x:TypeArguments="asw:EnumType" Default="[new EnumType(-1, &quot;None&quot;)]" Modifiers="ReadOnly" Name="None" />
      <Variable x:TypeArguments="asw:EnumType" Default="[new EnumType(0, &quot;Right&quot;)]" Modifiers="ReadOnly" Name="Right" />
      <Variable x:TypeArguments="asw:EnumType" Default="[new EnumType(10, &quot;Yes&quot;)]" Modifiers="ReadOnly" Name="Yes" />
    </Flowchart.Variables>
    <Flowchart.StartNode>
      <x:Reference>$flowStartNode</x:Reference>
    </Flowchart.StartNode>
$flowNodesText
  </Flowchart>
</Activity>
"@

Set-Content -LiteralPath $outPath -Value $xaml -Encoding UTF8
Get-Item -LiteralPath $outPath
