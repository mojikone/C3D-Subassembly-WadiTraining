// Single source of truth for every point/link code the W2 trend-finder subassembly emits.
// Map these in the Civil 3D Code Set Style (or run the toolbox (wadi-style-codes)).

namespace WadiTrend.Core;

public static class WadiCodes
{
    // Levee body links
    public const string Crown = "WT_Crown";
    public const string WadiFace = "WT_WadiFace";
    public const string LandFace = "WT_LandFace";

    // Ground-following link across the analysis run
    public const string Surface = "WT_Surface";

    // Fitted trend lines, drawn junction-to-junction (the deliverable of W2)
    public const string Trend = "WT_Trend";

    // Break markers at trend-line intersections
    public const string ConcaveMarker = "WT_Concave";
    public const string ConvexMarker = "WT_Convex";

    // Named points
    public const string CrownPoint = "WT_CrownPoint";
    public const string WadiToePoint = "WT_WadiToe";
    public const string LandToePoint = "WT_LandToe";
}
