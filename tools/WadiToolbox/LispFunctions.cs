// NETLOAD helper for driving subassembly import/attach/verification from LISP (via MCP).
// Not part of the shipped .pkt — test tooling only.
//
//   (wadi-import-sub "C:/path/tool.atc" "{GUID}" "ToolName" x y)  -> "OK <handle>" | "ERR ..."
//   (wadi-attach-sub "AssemblyName" "SubassemblyName")            -> "OK" | "ERR ..."
//   (wadi-erase-sub "SubassemblyName")                            -> "OK" | "ERR ..."
//   (wadi-list-asm "AssemblyName")                                -> "sub1|sub2|..." | "ERR ..."
//   (wadi-section-dump "CorridorName" station "C:/out.json")      -> "OK <resolvedStation>" | "ERR ..."
//
// Every function returns a string so failures surface in the MCP result instead of crashing LISP.

using Autodesk.AutoCAD.ApplicationServices.Core;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using Autodesk.Civil.ApplicationServices;
using Autodesk.Civil.DatabaseServices;
using Autodesk.Civil.DatabaseServices.Styles;
using System.Globalization;
using System.Text;

namespace WadiToolbox;

public class LispFunctions
{
    [LispFunction("WADI-IMPORT-SUB")]
    public object ImportSub(ResultBuffer args)
    {
        return Guarded(() =>
        {
            var a = Parse(args, 5);
            var atcPath = (string)a[0];
            var itemId = (string)a[1];
            var name = (string)a[2];
            var location = new Point3d(ToDouble(a[3]), ToDouble(a[4]), 0.0);

            var doc = CivilApplication.ActiveDocument;
            using var tr = StartTransaction();
            var id = doc.SubassemblyCollection.ImportSubassembly(name, atcPath, itemId, location);
            tr.Commit();
            return $"OK {id.Handle}";
        });
    }

    [LispFunction("WADI-ATTACH-SUB")]
    public object AttachSub(ResultBuffer args)
    {
        return Guarded(() =>
        {
            var a = Parse(args, 2);
            var assemblyName = (string)a[0];
            var subassemblyName = (string)a[1];

            var doc = CivilApplication.ActiveDocument;
            using var tr = StartTransaction();
            var assemblyId = FindByName(doc.AssemblyCollection.Cast<ObjectId>(), tr, assemblyName);
            var subassemblyId = FindByName(doc.SubassemblyCollection.Cast<ObjectId>(), tr, subassemblyName);
            if (assemblyId.IsNull) return $"ERR assembly '{assemblyName}' not found";
            if (subassemblyId.IsNull) return $"ERR subassembly '{subassemblyName}' not found";

            var assembly = (Assembly)tr.GetObject(assemblyId, OpenMode.ForWrite);
            assembly.AddSubassembly(subassemblyId);
            tr.Commit();
            return "OK";
        });
    }

    [LispFunction("WADI-ERASE-SUB")]
    public object EraseSub(ResultBuffer args)
    {
        return Guarded(() =>
        {
            var a = Parse(args, 1);
            var name = (string)a[0];
            var doc = CivilApplication.ActiveDocument;
            using var tr = StartTransaction();
            var id = FindByName(doc.SubassemblyCollection.Cast<ObjectId>(), tr, name);
            if (id.IsNull) return $"ERR subassembly '{name}' not found";
            var entity = tr.GetObject(id, OpenMode.ForWrite);
            entity.Erase();
            tr.Commit();
            return "OK";
        });
    }

    [LispFunction("WADI-LIST-ASM")]
    public object ListAsm(ResultBuffer args)
    {
        return Guarded(() =>
        {
            var a = Parse(args, 1);
            var assemblyName = (string)a[0];
            var doc = CivilApplication.ActiveDocument;
            using var tr = StartTransaction();
            var assemblyId = FindByName(doc.AssemblyCollection.Cast<ObjectId>(), tr, assemblyName);
            if (assemblyId.IsNull) return $"ERR assembly '{assemblyName}' not found";
            var assembly = (Assembly)tr.GetObject(assemblyId, OpenMode.ForRead);
            var names = new List<string>();
            foreach (var group in assembly.Groups)
            {
                foreach (ObjectId subId in group.GetSubassemblyIds())
                {
                    var sub = (Autodesk.Civil.DatabaseServices.Subassembly)tr.GetObject(subId, OpenMode.ForRead);
                    names.Add(sub.Name);
                }
            }

            return names.Count == 0 ? "(empty)" : string.Join("|", names);
        });
    }

    [LispFunction("WADI-SECTION-DUMP")]
    public object SectionDump(ResultBuffer args)
    {
        return Guarded(() =>
        {
            var a = Parse(args, 3);
            var corridorName = (string)a[0];
            var station = ToDouble(a[1]);
            var outPath = (string)a[2];

            var doc = CivilApplication.ActiveDocument;
            using var tr = StartTransaction();
            var corridorId = FindByName(doc.CorridorCollection.Cast<ObjectId>(), tr, corridorName);
            if (corridorId.IsNull) return $"ERR corridor '{corridorName}' not found";
            var corridor = (Corridor)tr.GetObject(corridorId, OpenMode.ForRead);

            AppliedAssembly? nearest = null;
            var nearestStation = double.NaN;
            var nearestDistance = double.MaxValue;
            foreach (Baseline baseline in corridor.Baselines)
            {
                foreach (BaselineRegion region in baseline.BaselineRegions)
                {
                    var stations = region.SortedStations();
                    for (var i = 0; i < stations.Length; i++)
                    {
                        var distance = Math.Abs(stations[i] - station);
                        if (distance < nearestDistance)
                        {
                            try
                            {
                                var applied = region.AppliedAssemblies[i];
                                nearestDistance = distance;
                                nearestStation = stations[i];
                                nearest = applied;
                            }
                            catch
                            {
                                // Station without applied assembly; skip.
                            }
                        }
                    }
                }
            }

            if (nearest is null) return "ERR no applied assemblies found";

            var json = new StringBuilder();
            json.Append('{').Append($"\"station\":{nearestStation.ToString(CultureInfo.InvariantCulture)},\"subassemblies\":[");
            var firstSub = true;
            foreach (var applied in nearest.GetAppliedSubassemblies())
            {
                if (!firstSub) json.Append(',');
                firstSub = false;
                var subName = "?";
                try
                {
                    var subEntity = (Autodesk.Civil.DatabaseServices.Subassembly)tr.GetObject(applied.SubassemblyId, OpenMode.ForRead);
                    subName = subEntity.Name;
                }
                catch
                {
                    // Applied geometry without resolvable source subassembly.
                }

                json.Append('{').Append($"\"name\":\"{subName}\",\"points\":[");
                var firstPoint = true;
                foreach (var point in applied.Points)
                {
                    if (!firstPoint) json.Append(',');
                    firstPoint = false;
                    var soe = point.StationOffsetElevationToBaseline;
                    json.Append('{')
                        .Append($"\"codes\":\"{string.Join(",", point.CorridorCodes.Cast<string>())}\",")
                        .Append($"\"offset\":{soe.Y.ToString("F4", CultureInfo.InvariantCulture)},")
                        .Append($"\"elev\":{soe.Z.ToString("F4", CultureInfo.InvariantCulture)}")
                        .Append('}');
                }

                json.Append("],\"links\":[");
                var firstLink = true;
                foreach (var link in applied.Links)
                {
                    if (!firstLink) json.Append(',');
                    firstLink = false;
                    json.Append('{')
                        .Append($"\"codes\":\"{string.Join(",", link.CorridorCodes.Cast<string>())}\",")
                        .Append($"\"points\":{link.CalculatedPoints.Count}")
                        .Append('}');
                }

                json.Append("]}");
            }

            json.Append("]}");
            File.WriteAllText(outPath, json.ToString());
            return $"OK {nearestStation.ToString("F3", CultureInfo.InvariantCulture)}";
        });
    }

    [LispFunction("WADI-LIST-ALL")]
    public object ListAll(ResultBuffer args)
    {
        return Guarded(() =>
        {
            var doc = CivilApplication.ActiveDocument;
            using var tr = StartTransaction();
            var sb = new StringBuilder();

            sb.Append("ASSEMBLIES:");
            sb.Append(string.Join(",", doc.AssemblyCollection.Cast<ObjectId>()
                .Select(id => ((Autodesk.Civil.DatabaseServices.Entity)tr.GetObject(id, OpenMode.ForRead)).Name)));
            sb.Append(";SUBASSEMBLIES:");
            sb.Append(string.Join(",", doc.SubassemblyCollection.Cast<ObjectId>()
                .Select(id => ((Autodesk.Civil.DatabaseServices.Entity)tr.GetObject(id, OpenMode.ForRead)).Name)));
            sb.Append(";SURFACES:");
            sb.Append(string.Join(",", doc.GetSurfaceIds().Cast<ObjectId>()
                .Select(id => ((Autodesk.Civil.DatabaseServices.Entity)tr.GetObject(id, OpenMode.ForRead)).Name)));
            sb.Append(";ALIGNMENTS:");
            sb.Append(string.Join(",", doc.GetAlignmentIds().Cast<ObjectId>()
                .Select(id => ((Autodesk.Civil.DatabaseServices.Entity)tr.GetObject(id, OpenMode.ForRead)).Name)));
            sb.Append(";CORRIDORS:");
            foreach (ObjectId corridorId in doc.CorridorCollection.Cast<ObjectId>())
            {
                var corridor = (Corridor)tr.GetObject(corridorId, OpenMode.ForRead);
                sb.Append(corridor.Name).Append('[');
                var assemblyNames = new List<string>();
                foreach (Baseline baseline in corridor.Baselines)
                {
                    foreach (BaselineRegion region in baseline.BaselineRegions)
                    {
                        try
                        {
                            var assembly = (Assembly)tr.GetObject(region.AssemblyId, OpenMode.ForRead);
                            assemblyNames.Add(assembly.Name);
                        }
                        catch
                        {
                            assemblyNames.Add("?");
                        }
                    }
                }

                sb.Append(string.Join(",", assemblyNames)).Append(']');
            }

            return sb.ToString();
        });
    }

    [LispFunction("WADI-SET-TARGETS")]
    public object SetTargets(ResultBuffer args)
    {
        return Guarded(() =>
        {
            var a = Parse(args, 3);
            var corridorName = (string)a[0];
            var surfaceName = (string)a[1];
            var alignmentName = (string)a[2];

            var doc = CivilApplication.ActiveDocument;
            using var tr = StartTransaction();
            var corridorId = FindByName(doc.CorridorCollection.Cast<ObjectId>(), tr, corridorName);
            if (corridorId.IsNull) return $"ERR corridor '{corridorName}' not found";
            var surfaceId = FindByName(doc.GetSurfaceIds().Cast<ObjectId>(), tr, surfaceName);
            if (surfaceId.IsNull) return $"ERR surface '{surfaceName}' not found";
            var alignmentId = FindByName(doc.GetAlignmentIds().Cast<ObjectId>(), tr, alignmentName);

            var corridor = (Corridor)tr.GetObject(corridorId, OpenMode.ForWrite);
            var assigned = 0;
            foreach (Baseline baseline in corridor.Baselines)
            {
                foreach (BaselineRegion region in baseline.BaselineRegions)
                {
                    var targets = region.GetTargets();
                    foreach (SubassemblyTargetInfo target in targets)
                    {
                        var logicalName = target.LogicalName;
                        if (logicalName.Contains("ExistingGround", StringComparison.OrdinalIgnoreCase))
                        {
                            var ids = new ObjectIdCollection { surfaceId };
                            target.TargetIds = ids;
                            assigned++;
                        }
                        else if (logicalName.Contains("ScanLimitOffset", StringComparison.OrdinalIgnoreCase) && !alignmentId.IsNull)
                        {
                            var ids = new ObjectIdCollection { alignmentId };
                            target.TargetIds = ids;
                            assigned++;
                        }
                    }

                    region.SetTargets(targets);
                }
            }

            tr.Commit();
            return $"OK {assigned}";
        });
    }

    [LispFunction("WADI-REBUILD")]
    public object RebuildCorridor(ResultBuffer args)
    {
        return Guarded(() =>
        {
            var a = Parse(args, 1);
            var corridorName = (string)a[0];
            var doc = CivilApplication.ActiveDocument;
            using var tr = StartTransaction();
            var corridorId = FindByName(doc.CorridorCollection.Cast<ObjectId>(), tr, corridorName);
            if (corridorId.IsNull) return $"ERR corridor '{corridorName}' not found";
            var corridor = (Corridor)tr.GetObject(corridorId, OpenMode.ForWrite);
            corridor.Rebuild();
            tr.Commit();
            return "OK";
        });
    }

    /// <summary>
    /// (wadi-style-codes) — creates colored link/marker styles for every WT_* code and maps
    /// them into ALL code set styles in the drawing, so corridor sections show the colors
    /// regardless of which code set the view uses. ACI colors, editable later in the UI.
    /// </summary>
    [LispFunction("WADI-STYLE-CODES")]
    public object StyleCodes(ResultBuffer args)
    {
        return Guarded(() =>
        {
            var doc = CivilApplication.ActiveDocument;
            var linkColors = new (string Code, short Aci)[]
            {
                ("WT_Crown", 5),        // blue
                ("WT_WadiFace", 4),     // cyan
                ("WT_LandFace", 5),     // blue
                ("WT_Surface", 8),      // gray
                ("WT_Trend", 1),        // red — straight fitted trend lines (W2)
                ("WT_ToeScour", 1),     // red
                ("WT_ToeApron", 40),    // orange
                ("WT_Protection", 6),   // magenta
                ("WT_Concave", 1),      // red
                ("WT_Convex", 3)        // green
            };
            var pointColors = new (string Code, short Aci)[]
            {
                ("WT_CrownPoint", 5),
                ("WT_WadiToe", 4),
                ("WT_LandToe", 5),
                ("WT_Concave", 1),
                ("WT_Convex", 3)
            };

            using var tr = StartTransaction();
            var mapped = 0;

            foreach (var (code, aci) in linkColors)
            {
                var styleId = EnsureLinkStyle(doc, tr, $"{code}_Style", aci);
                mapped += MapCode(doc, tr, SubassemblySubentityStyleType.LinkType, code, styleId);
            }

            foreach (var (code, aci) in pointColors)
            {
                var styleId = EnsureMarkerStyle(doc, tr, $"{code}_Marker", aci);
                mapped += MapCode(doc, tr, SubassemblySubentityStyleType.MarkerType, code, styleId);
            }

            tr.Commit();
            return $"OK mapped {mapped} code entries";
        });
    }

    private static ObjectId EnsureLinkStyle(CivilDocument doc, Transaction tr, string name, short aci)
    {
        ObjectId styleId;
        try
        {
            styleId = doc.Styles.LinkStyles[name];
        }
        catch
        {
            styleId = doc.Styles.LinkStyles.Add(name);
        }

        var style = (Autodesk.Civil.DatabaseServices.Styles.LinkStyle)tr.GetObject(styleId, OpenMode.ForWrite);
        var color = Autodesk.AutoCAD.Colors.Color.FromColorIndex(Autodesk.AutoCAD.Colors.ColorMethod.ByAci, aci);
        foreach (var display in new[] { style.GetDisplayStyleSection(), style.GetDisplayStylePlan(), style.GetDisplayStyleModel() })
        {
            display.Color = color;
            display.Visible = true;
        }

        return styleId;
    }

    private static ObjectId EnsureMarkerStyle(CivilDocument doc, Transaction tr, string name, short aci)
    {
        ObjectId styleId;
        try
        {
            styleId = doc.Styles.MarkerStyles[name];
        }
        catch
        {
            styleId = doc.Styles.MarkerStyles.Add(name);
        }

        var style = (Autodesk.Civil.DatabaseServices.Styles.MarkerStyle)tr.GetObject(styleId, OpenMode.ForWrite);
        var color = Autodesk.AutoCAD.Colors.Color.FromColorIndex(Autodesk.AutoCAD.Colors.ColorMethod.ByAci, aci);
        foreach (var display in new[] { style.GetMarkerDisplayStyleSection(), style.GetMarkerDisplayStylePlan(), style.GetMarkerDisplayStyleModel() })
        {
            display.Color = color;
            display.Visible = true;
        }

        return styleId;
    }

    private static int MapCode(CivilDocument doc, Transaction tr, SubassemblySubentityStyleType styleType, string code, ObjectId styleId)
    {
        var mapped = 0;
        foreach (ObjectId codeSetId in doc.Styles.CodeSetStyles)
        {
            var codeSet = (CodeSetStyle)tr.GetObject(codeSetId, OpenMode.ForWrite);
            var found = false;
            try
            {
                foreach (CodeSetStyleItem item in codeSet)
                {
                    if (item.StyleType == styleType &&
                        string.Equals(item.Code, code, StringComparison.OrdinalIgnoreCase))
                    {
                        item.CodeStyleId = styleId;
                        found = true;
                        mapped++;
                        break;
                    }
                }
            }
            catch
            {
                // enumeration hiccup; try Add below
            }

            if (!found)
            {
                try
                {
                    codeSet.Add(code, styleId);
                    mapped++;
                }
                catch
                {
                    // Code set may not accept this entry; skip.
                }
            }
        }

        return mapped;
    }

    // ----- helpers -----

    private static Transaction StartTransaction()
    {
        return Application.DocumentManager.MdiActiveDocument.Database.TransactionManager.StartTransaction();
    }

    private static ObjectId FindByName(IEnumerable<ObjectId> ids, Transaction tr, string name)
    {
        foreach (var id in ids)
        {
            var entity = tr.GetObject(id, OpenMode.ForRead);
            var entityName = entity switch
            {
                Autodesk.Civil.DatabaseServices.Entity civil => civil.Name,
                _ => null
            };
            if (string.Equals(entityName, name, StringComparison.OrdinalIgnoreCase))
            {
                return id;
            }
        }

        return ObjectId.Null;
    }

    private static List<object> Parse(ResultBuffer args, int expected)
    {
        var values = new List<object>();
        foreach (var tv in args)
        {
            if (tv.TypeCode != (int)LispDataType.Nil)
            {
                values.Add(tv.Value);
            }
        }

        if (values.Count < expected)
        {
            throw new InvalidOperationException($"expected {expected} arguments, got {values.Count}");
        }

        return values;
    }

    private static double ToDouble(object value) => Convert.ToDouble(value, CultureInfo.InvariantCulture);

    private static string Guarded(Func<string> body)
    {
        try
        {
            return body();
        }
        catch (System.Exception ex)
        {
            return $"ERR {ex.GetType().Name}: {ex.Message}";
        }
    }
}
