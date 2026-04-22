using Autodesk.Revit.DB.Structure;
using Autodesk.Revit.UI;
using RevitMCPCommandSet.Commands;
using RevitMCPCommandSet.Models.Common;
using System.IO;
using System.Reflection;

namespace RevitMCPCommandSet.Utils
{
    public static class ProjectUtils
    {
        /// <summary>
        /// Generic helper for creating a family instance
        /// </summary>
        /// <param name="doc">Current document</param>
        /// <param name="familySymbol">Family type</param>
        /// <param name="locationPoint">Location point</param>
        /// <param name="locationLine">Reference line</param>
        /// <param name="baseLevel">Base level</param>
        /// <param name="topLevel">Second level (used for TwoLevelsBased placement)</param>
        /// <param name="baseOffset">Base offset (ft)</param>
        /// <param name="topOffset">Top offset (ft)</param>
        /// <param name="faceDirection">Facing direction</param>
        /// <param name="handDirection">Hand direction</param>
        /// <param name="view">View</param>
        /// <returns>The created family instance, or null on failure</returns>
        public static FamilyInstance CreateInstance(
            this Document doc,
            FamilySymbol familySymbol,
            XYZ locationPoint = null,
            Line locationLine = null,
            Level baseLevel = null,
            Level topLevel = null,
            double baseOffset = -1,
            double topOffset = -1,
            XYZ faceDirection = null,
            XYZ handDirection = null,
            View view = null)
        {
            // Basic parameter checks
            if (doc == null)
                throw new ArgumentNullException($"Required parameter {typeof(Document)} {nameof(doc)} is missing!");
            if (familySymbol == null)
                throw new ArgumentNullException($"Required parameter {typeof(FamilySymbol)} {nameof(familySymbol)} is missing!");

            // Activate the family symbol
            if (!familySymbol.IsActive)
                familySymbol.Activate();

            FamilyInstance instance = null;

            // Choose the creation method based on the family's placement type
            switch (familySymbol.Family.FamilyPlacementType)
            {
                // Families based on a single level (e.g. Metric Generic Model)
                case FamilyPlacementType.OneLevelBased:
                    if (locationPoint == null)
                        throw new ArgumentNullException($"Required parameter {typeof(XYZ)} {nameof(locationPoint)} is missing!");
                    // With level information
                    if (baseLevel != null)
                    {
                        instance = doc.Create.NewFamilyInstance(
                            locationPoint,                  // Physical position where the instance will be placed
                            familySymbol,                   // The FamilySymbol representing the instance type to insert
                            baseLevel,                      // The Level used as the base level of the object
                            StructuralType.NonStructural);  // Specifies the component type when the component is structural
                    }
                    // Without level information
                    else
                    {
                        instance = doc.Create.NewFamilyInstance(
                            locationPoint,                  // Physical position where the instance will be placed
                            familySymbol,                   // The FamilySymbol representing the instance type to insert
                            StructuralType.NonStructural);  // Specifies the component type when the component is structural
                    }
                    break;

                // Families based on a single level and a host (e.g. doors and windows)
                case FamilyPlacementType.OneLevelBasedHosted:
                    if (locationPoint == null)
                        throw new ArgumentNullException($"Required parameter {typeof(XYZ)} {nameof(locationPoint)} is missing!");
                    // Automatically find the nearest host element
                    Element host = doc.GetNearestHostElement(locationPoint, familySymbol);
                    if (host == null)
                        throw new ArgumentNullException($"No valid host element found!");
                    // Placement direction is determined by the host's creation direction
                    // With level information
                    if (baseLevel != null)
                    {
                        instance = doc.Create.NewFamilyInstance(
                            locationPoint,                  // Physical position where the instance will be placed on the specified level
                            familySymbol,                   // The FamilySymbol representing the instance type to insert
                            host,                           // The host object the instance will be embedded in
                            baseLevel,                      // The Level used as the base level of the object
                            StructuralType.NonStructural);  // Specifies the component type when the component is structural
                    }
                    // Without level information
                    else
                    {
                        instance = doc.Create.NewFamilyInstance(
                            locationPoint,                  // Physical position where the instance will be placed on the specified level
                            familySymbol,                   // The FamilySymbol representing the instance type to insert
                            host,                           // The host object the instance will be embedded in
                            StructuralType.NonStructural);  // Specifies the component type when the component is structural
                    }
                    break;

                // Families based on two levels (e.g. columns)
                case FamilyPlacementType.TwoLevelsBased:
                    if (locationPoint == null)
                        throw new ArgumentNullException($"Required parameter {typeof(XYZ)} {nameof(locationPoint)} is missing!");
                    if (baseLevel == null)
                        throw new ArgumentNullException($"Required parameter {typeof(Level)} {nameof(baseLevel)} is missing!");
                    // Determine whether this is a structural column or an architectural column
                    StructuralType structuralType = StructuralType.NonStructural;
                    if (familySymbol.Category.Id.IntegerValue == (int)BuiltInCategory.OST_StructuralColumns)
                        structuralType = StructuralType.Column;
                    instance = doc.Create.NewFamilyInstance(
                        locationPoint,              // Physical position where the instance will be placed
                        familySymbol,               // The FamilySymbol representing the instance type to insert
                        baseLevel,                  // The Level used as the base level of the object
                        structuralType);            // Specifies the component type when the component is structural
                    // Set base level, top level, base offset, and top offset
                    if (instance != null)
                    {
                        // Set the column's base and top levels
                        if (baseLevel != null)
                        {
                            Parameter baseLevelParam = instance.get_Parameter(BuiltInParameter.FAMILY_BASE_LEVEL_PARAM);
                            if (baseLevelParam != null)
                                baseLevelParam.Set(baseLevel.Id);
                        }
                        if (topLevel != null)
                        {
                            Parameter topLevelParam = instance.get_Parameter(BuiltInParameter.FAMILY_TOP_LEVEL_PARAM);
                            if (topLevelParam != null)
                                topLevelParam.Set(topLevel.Id);
                        }
                        // Get the base offset parameter
                        if (baseOffset != -1)
                        {
                            Parameter baseOffsetParam = instance.get_Parameter(BuiltInParameter.FAMILY_BASE_LEVEL_OFFSET_PARAM);
                            if (baseOffsetParam != null && baseOffsetParam.StorageType == StorageType.Double)
                            {
                                // Convert mm to Revit internal units
                                double baseOffsetInternal = baseOffset;
                                baseOffsetParam.Set(baseOffsetInternal);
                            }
                        }
                        // Get the top offset parameter
                        if (topOffset != -1)
                        {
                            Parameter topOffsetParam = instance.get_Parameter(BuiltInParameter.FAMILY_TOP_LEVEL_OFFSET_PARAM);
                            if (topOffsetParam != null && topOffsetParam.StorageType == StorageType.Double)
                            {
                                // Convert mm to Revit internal units
                                double topOffsetInternal = topOffset;
                                topOffsetParam.Set(topOffsetInternal);
                            }
                        }
                    }
                    break;

                // Families that are view-specific (e.g. detail components)
                case FamilyPlacementType.ViewBased:
                    if (locationPoint == null)
                        throw new ArgumentNullException($"Required parameter {typeof(XYZ)} {nameof(locationPoint)} is missing!");
                    instance = doc.Create.NewFamilyInstance(
                        locationPoint,  // Origin of the family instance. When created in a ViewPlan, the origin is projected onto the plan view
                        familySymbol,   // The FamilySymbol representing the instance type to insert
                        view);          // The 2D view in which the family instance is placed
                    break;

                // Work-plane-based families (e.g. face-based Generic Model, including face-based and wall-based)
                case FamilyPlacementType.WorkPlaneBased:
                    if (locationPoint == null)
                        throw new ArgumentNullException($"Required parameter {typeof(XYZ)} {nameof(locationPoint)} is missing!");
                    // Get the nearest host face
                    Reference hostFace = doc.GetNearestFaceReference(locationPoint, 1000 / 304.8);
                    if (hostFace == null)
                        throw new ArgumentNullException($"No valid host element found!");
                    if (faceDirection == null || faceDirection == XYZ.Zero)
                    {
                        var result = doc.GenerateDefaultOrientation(hostFace);
                        faceDirection = result.FacingOrientation;
                    }
                    // Create the family instance on the face using a point and direction
                    instance = doc.Create.NewFamilyInstance(
                        hostFace,               // Reference to the face
                        locationPoint,          // The point on the face where the instance will be placed
                        faceDirection,          // Vector defining the orientation of the family instance. Note that this direction defines the rotation of the instance on the face and cannot be parallel to the face normal
                        familySymbol);          // The FamilySymbol representing the instance type to insert. This FamilySymbol must represent a family whose FamilyPlacementType is WorkPlaneBased
                    break;

                // Line-based families on a work plane (e.g. line-based Generic Model)
                case FamilyPlacementType.CurveBased:
                    if (locationLine == null)
                        throw new ArgumentNullException($"Required parameter {typeof(Line)} {nameof(locationLine)} is missing!");

                    // Get the nearest host face (no tolerance)
                    Reference lineHostFace = doc.GetNearestFaceReference(locationLine.Evaluate(0.5, true), 1e-5);
                    if (lineHostFace != null)
                    {
                        instance = doc.Create.NewFamilyInstance(
                            lineHostFace,   // Reference to the face
                            locationLine,   // The curve that the family instance is based on
                            familySymbol);  // A FamilySymbol representing the type of instance to insert. This Symbol must represent a family whose FamilyPlacementType is WorkPlaneBased or CurveBased
                    }
                    else
                    {
                        instance = doc.Create.NewFamilyInstance(
                            locationLine,                   // The curve that the family instance is based on
                            familySymbol,                   // A FamilySymbol representing the type of instance to insert. This Symbol must represent a family whose FamilyPlacementType is WorkPlaneBased or CurveBased
                            baseLevel,                      // A Level to serve as the base level of the object
                            StructuralType.NonStructural);  // Specifies the component type when the component is structural
                    }
                    if (instance != null)
                    {
                        // Get the base offset parameter
                        if (baseOffset != -1)
                        {
                            Parameter baseOffsetParam = instance.get_Parameter(BuiltInParameter.INSTANCE_FREE_HOST_OFFSET_PARAM);
                            if (baseOffsetParam != null && baseOffsetParam.StorageType == StorageType.Double)
                            {
                                // Convert mm to Revit internal units
                                double baseOffsetInternal = baseOffset;
                                baseOffsetParam.Set(baseOffsetInternal);
                            }
                        }
                    }
                    break;

                // Line-based families in a specific view (e.g. detail components)
                case FamilyPlacementType.CurveBasedDetail:
                    if (locationLine == null)
                        throw new ArgumentNullException($"Required parameter {typeof(Line)} {nameof(locationLine)} is missing!");
                    if (view == null)
                        throw new ArgumentNullException($"Required parameter {typeof(View)} {nameof(view)} is missing!");
                    instance = doc.Create.NewFamilyInstance(
                        locationLine,   // Line position of the family instance. The line must lie in the plane of the view
                        familySymbol,   // The FamilySymbol representing the instance type to insert
                        view);          // The 2D view in which the family instance is placed
                    break;

                // Structural curve-driven families (e.g. beams, braces, slanted columns)
                case FamilyPlacementType.CurveDrivenStructural:
                    if (locationLine == null)
                        throw new ArgumentNullException($"Required parameter {typeof(Line)} {nameof(locationLine)} is missing!");
                    if (baseLevel == null)
                        throw new ArgumentNullException($"Required parameter {typeof(Level)} {nameof(baseLevel)} is missing!");
                    instance = doc.Create.NewFamilyInstance(
                        locationLine,                   // The curve that the family instance is based on
                        familySymbol,                   // A FamilySymbol representing the type of instance to insert. This Symbol must represent a family whose FamilyPlacementType is WorkPlaneBased or CurveBased
                        baseLevel,                      // A Level to serve as the base level of the object
                        StructuralType.Beam);           // Specifies the component type when the component is structural
                    break;

                // Adaptive families (e.g. Adaptive Generic Model, curtain panels)
                case FamilyPlacementType.Adaptive:
                    throw new NotImplementedException("Creation method for FamilyPlacementType.Adaptive is not implemented!");

                default:
                    break;
            }
            return instance;
        }

        /// <summary>
        /// Generate default facing and hand orientations (by default the long edge is HandOrientation and the short edge is FacingOrientation)
        /// </summary>
        /// <param name="hostFace"></param>
        /// <returns></returns>
        public static (XYZ FacingOrientation, XYZ HandOrientation) GenerateDefaultOrientation(this Document doc, Reference hostFace)
        {
            var facingOrientation = new XYZ();  // Facing direction: orientation of the family's positive Y axis after loading
            var handOrientation = new XYZ();    // Hand direction: orientation of the family's positive X axis after loading

            // Step 1: Get the face object from the Reference
            Face face = doc.GetElement(hostFace.ElementId).GetGeometryObjectFromReference(hostFace) as Face;

            // Step 2: Get the face profile
            List<Curve> profile = null;
            // Profile collection; each sublist represents a complete closed profile, the first usually being the outer profile
            List<List<Curve>> profiles = new List<List<Curve>>();
            // Get all edge loops (outer profile and possibly inner holes)
            EdgeArrayArray edgeLoops = face.EdgeLoops;
            // Iterate over each edge loop
            foreach (EdgeArray loop in edgeLoops)
            {
                List<Curve> currentLoop = new List<Curve>();
                // Get every edge in the loop
                foreach (Edge edge in loop)
                {
                    Curve curve = edge.AsCurve();
                    currentLoop.Add(curve);
                }
                // Add the loop to the result collection if it has edges
                if (currentLoop.Count > 0)
                {
                    profiles.Add(currentLoop);
                }
            }
            // The first loop is typically the outer profile
            if (profiles != null && profiles.Any())
                profile = profiles.FirstOrDefault();

            // Step 3: Get the face normal
            XYZ faceNormal = null;
            // If the face is planar, the normal property can be retrieved directly
            if (face is PlanarFace planarFace)
                faceNormal = planarFace.FaceNormal;

            // Step 4: Get the two valid primary directions of the face (right-hand-rule compliant)
            var result = face.GetMainDirections();
            var primaryDirection = result.PrimaryDirection;
            var secondaryDirection = result.SecondaryDirection;

            // By default the long-edge direction is HandOrientation and the short-edge direction is FacingOrientation
            facingOrientation = primaryDirection;
            handOrientation = secondaryDirection;

            // Check right-hand-rule compliance (thumb: HandOrientation, index: FacingOrientation, middle: FaceNormal)
            if (!facingOrientation.IsRightHandRuleCompliant(handOrientation, faceNormal))
            {
                var newHandOrientation = facingOrientation.GenerateIndexFinger(faceNormal);
                if (newHandOrientation != null)
                {
                    handOrientation = newHandOrientation;
                }
            }

            return (facingOrientation, handOrientation);
        }

        /// <summary>
        /// Get the Reference to the face nearest to a point
        /// </summary>
        /// <param name="doc">Current document</param>
        /// <param name="location">Target point position</param>
        /// <param name="radius">Search radius (internal units)</param>
        /// <returns>Reference to the nearest face, or null if not found</returns>
        public static Reference GetNearestFaceReference(this Document doc, XYZ location, double radius = 1000 / 304.8)
        {
            try
            {
                // Tolerance handling
                location = new XYZ(location.X, location.Y, location.Z + 0.1 / 304.8);

                // Create or obtain a 3D view
                View3D view3D = null;
                FilteredElementCollector collector = new FilteredElementCollector(doc)
                    .OfClass(typeof(View3D));

                foreach (View3D v in collector)
                {
                    if (!v.IsTemplate)
                    {
                        view3D = v;
                        break;
                    }
                }

                if (view3D == null)
                {
                    using (Transaction trans = new Transaction(doc, "Create 3D View"))
                    {
                        trans.Start();
                        ViewFamilyType vft = new FilteredElementCollector(doc)
                            .OfClass(typeof(ViewFamilyType))
                            .Cast<ViewFamilyType>()
                            .FirstOrDefault(x => x.ViewFamily == ViewFamily.ThreeDimensional);

                        if (vft != null)
                        {
                            view3D = View3D.CreateIsometric(doc, vft.Id);
                        }
                        trans.Commit();
                    }
                }

                if (view3D == null)
                {
                    TaskDialog.Show("Error", "Unable to create or obtain a 3D view");
                    return null;
                }

                // Set up rays in 6 directions
                XYZ[] directions = new XYZ[]
                {
                  XYZ.BasisX,    // +X
                  -XYZ.BasisX,   // -X
                  XYZ.BasisY,    // +Y
                  -XYZ.BasisY,   // -Y
                  XYZ.BasisZ,    // +Z
                  -XYZ.BasisZ    // -Z
                };

                // Create filters
                ElementClassFilter wallFilter = new ElementClassFilter(typeof(Wall));
                ElementClassFilter floorFilter = new ElementClassFilter(typeof(Floor));
                ElementClassFilter ceilingFilter = new ElementClassFilter(typeof(Ceiling));
                ElementClassFilter instanceFilter = new ElementClassFilter(typeof(FamilyInstance));

                // Combined filter
                LogicalOrFilter categoryFilter = new LogicalOrFilter(
                    new ElementFilter[] { wallFilter, floorFilter, ceilingFilter, instanceFilter });


                // 1. Simplest: filter for all instance elements
                //ElementFilter filter = new ElementIsElementTypeFilter(true);

                // Create the ray-tracing reference intersector
                ReferenceIntersector refIntersector = new ReferenceIntersector(categoryFilter,
                    FindReferenceTarget.Face, view3D);
                refIntersector.FindReferencesInRevitLinks = true; // Use if faces in linked files need to be found

                double minDistance = double.MaxValue;
                Reference nearestFace = null;

                foreach (XYZ direction in directions)
                {
                    // Fire a ray from the current position
                    IList<ReferenceWithContext> references = refIntersector.Find(location, direction);

                    foreach (ReferenceWithContext rwc in references)
                    {
                        double distance = rwc.Proximity; // Distance to the face

                        // If within range and closer than current best
                        if (distance <= radius && distance < minDistance)
                        {
                            minDistance = distance;
                            nearestFace = rwc.GetReference();
                        }
                    }
                }

                return nearestFace;
            }
            catch (Exception ex)
            {
                TaskDialog.Show("Error", $"Error getting nearest face: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Get the nearest element that can serve as a host
        /// </summary>
        /// <param name="doc">Current document</param>
        /// <param name="location">Target point position</param>
        /// <param name="familySymbol">Family type used to determine the host type</param>
        /// <param name="radius">Search radius (internal units)</param>
        /// <returns>Nearest host element, or null if not found</returns>
        public static Element GetNearestHostElement(this Document doc, XYZ location, FamilySymbol familySymbol, double radius = 5.0)
        {
            try
            {
                // Basic parameter checks
                if (doc == null || location == null || familySymbol == null)
                    return null;

                // Get the family's hosting behavior parameter
                Parameter hostParam = familySymbol.Family.get_Parameter(BuiltInParameter.FAMILY_HOSTING_BEHAVIOR);
                int hostingBehavior = hostParam?.AsInteger() ?? 0;

                // Create or obtain a 3D view
                View3D view3D = null;
                FilteredElementCollector viewCollector = new FilteredElementCollector(doc)
                    .OfClass(typeof(View3D));
                foreach (View3D v in viewCollector)
                {
                    if (!v.IsTemplate)
                    {
                        view3D = v;
                        break;
                    }
                }

                if (view3D == null)
                {
                    using (Transaction trans = new Transaction(doc, "Create 3D View"))
                    {
                        trans.Start();
                        ViewFamilyType vft = new FilteredElementCollector(doc)
                            .OfClass(typeof(ViewFamilyType))
                            .Cast<ViewFamilyType>()
                            .FirstOrDefault(x => x.ViewFamily == ViewFamily.ThreeDimensional);

                        if (vft != null)
                        {
                            view3D = View3D.CreateIsometric(doc, vft.Id);
                        }
                        trans.Commit();
                    }
                }

                if (view3D == null)
                {
                    TaskDialog.Show("Error", "Unable to create or obtain a 3D view");
                    return null;
                }

                // Create a class filter based on the hosting behavior
                ElementFilter classFilter;
                switch (hostingBehavior)
                {
                    case 1: // Wall based
                        classFilter = new ElementClassFilter(typeof(Wall));
                        break;
                    case 2: // Floor based
                        classFilter = new ElementClassFilter(typeof(Floor));
                        break;
                    case 3: // Ceiling based
                        classFilter = new ElementClassFilter(typeof(Ceiling));
                        break;
                    case 4: // Roof based
                        classFilter = new ElementClassFilter(typeof(RoofBase));
                        break;
                    default:
                        return null; // Unsupported host type
                }

                // Set up rays in 6 directions
                XYZ[] directions = new XYZ[]
                {
                    XYZ.BasisX,    // +X
                    -XYZ.BasisX,   // -X
                    XYZ.BasisY,    // +Y
                    -XYZ.BasisY,   // -Y
                    XYZ.BasisZ,    // +Z
                    -XYZ.BasisZ    // -Z
                };

                // Create the ray-tracing reference intersector
                ReferenceIntersector refIntersector = new ReferenceIntersector(classFilter,
                    FindReferenceTarget.Element, view3D);
                refIntersector.FindReferencesInRevitLinks = true; // Use if elements in linked files need to be found

                double minDistance = double.MaxValue;
                Element nearestHost = null;

                foreach (XYZ direction in directions)
                {
                    // Fire a ray from the current position
                    IList<ReferenceWithContext> references = refIntersector.Find(location, direction);

                    foreach (ReferenceWithContext rwc in references)
                    {
                        double distance = rwc.Proximity; // Distance to the element

                        // If within range and closer than current best
                        if (distance <= radius && distance < minDistance)
                        {
                            minDistance = distance;
                            nearestHost = doc.GetElement(rwc.GetReference().ElementId);
                        }
                    }
                }

                return nearestHost;
            }
            catch (Exception ex)
            {
                TaskDialog.Show("Error", $"Error getting nearest host element: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Highlight the specified face
        /// </summary>
        /// <param name="doc">Current document</param>
        /// <param name="faceRef">Reference of the face to highlight</param>
        /// <param name="duration">Highlight duration in milliseconds (default 3000)</param>
        public static void HighlightFace(this Document doc, Reference faceRef)
        {
            if (faceRef == null) return;

            // Get the solid fill pattern
            FillPatternElement solidFill = new FilteredElementCollector(doc)
                .OfClass(typeof(FillPatternElement))
                .Cast<FillPatternElement>()
                .FirstOrDefault(x => x.GetFillPattern().IsSolidFill);

            if (solidFill == null)
            {
                TaskDialog.Show("Error", "Solid fill pattern not found");
                return;
            }

            // Create the highlight override settings
            OverrideGraphicSettings ogs = new OverrideGraphicSettings();
            ogs.SetSurfaceForegroundPatternColor(new Color(255, 0, 0)); // Red
            ogs.SetSurfaceForegroundPatternId(solidFill.Id);
            ogs.SetSurfaceTransparency(0); // Opaque

            // Apply the highlight
            doc.ActiveView.SetElementOverrides(faceRef.ElementId, ogs);
        }

        /// <summary>
        /// Extract the two primary direction vectors of a face
        /// </summary>
        /// <param name="face">Input face</param>
        /// <returns>A tuple containing the primary and secondary directions</returns>
        /// <exception cref="ArgumentNullException">Thrown when the face is null</exception>
        /// <exception cref="ArgumentException">Thrown when the face profile is insufficient to form a valid shape</exception>
        /// <exception cref="InvalidOperationException">Thrown when no valid directions can be extracted</exception>
        public static (XYZ PrimaryDirection, XYZ SecondaryDirection) GetMainDirections(this Face face)
        {
            // 1. Parameter validation
            if (face == null)
                throw new ArgumentNullException(nameof(face), "Face cannot be null");

            // 2. Get the face normal, used for later perpendicular-vector calculations
            XYZ faceNormal = face.ComputeNormal(new UV(0.5, 0.5));

            // 3. Get the outer profile of the face
            EdgeArrayArray edgeLoops = face.EdgeLoops;
            if (edgeLoops.Size == 0)
                throw new ArgumentException("The face has no valid edge loops", nameof(face));

            // The first loop is typically the outer profile
            EdgeArray outerLoop = edgeLoops.get_Item(0);

            // 4. Compute direction vectors and lengths for each edge
            List<XYZ> edgeDirections = new List<XYZ>();  // Unit direction vector of each edge
            List<double> edgeLengths = new List<double>(); // Length of each edge

            foreach (Edge edge in outerLoop)
            {
                Curve curve = edge.AsCurve();
                XYZ startPoint = curve.GetEndPoint(0);
                XYZ endPoint = curve.GetEndPoint(1);

                // Vector from start to end
                XYZ direction = endPoint - startPoint;
                double length = direction.GetLength();

                // Ignore edges that are too short (possibly caused by coincident vertices or numerical precision)
                if (length > 1e-10)
                {
                    edgeDirections.Add(direction.Normalize());  // Store normalized direction vector
                    edgeLengths.Add(length);                    // Store edge length
                }
            }

            if (edgeDirections.Count < 4) // Ensure at least four edges
            {
                throw new ArgumentException("The provided face does not have enough edges to form a valid shape", nameof(face));
            }

            // 5. Group edges with similar directions
            List<List<int>> directionGroups = new List<List<int>>();  // Direction groups; each holds edge indices

            for (int i = 0; i < edgeDirections.Count; i++)
            {
                bool foundGroup = false;
                XYZ currentDirection = edgeDirections[i];

                // Try to add the current edge to an existing direction group
                for (int j = 0; j < directionGroups.Count; j++)
                {
                    var group = directionGroups[j];
                    // Compute the group's weighted average direction
                    XYZ groupAvgDir = CalculateWeightedAverageDirection(group, edgeDirections, edgeLengths);

                    // Check whether the current direction is similar to the group's average (including reverse direction)
                    double dotProduct = Math.Abs(groupAvgDir.DotProduct(currentDirection));
                    if (dotProduct > 0.8) // Within approximately 30 degrees is considered similar
                    {
                        group.Add(i);  // Add the current edge index to this direction group
                        foundGroup = true;
                        break;
                    }
                }

                // If the current edge is not similar to any existing group, create a new group
                if (!foundGroup)
                {
                    List<int> newGroup = new List<int> { i };
                    directionGroups.Add(newGroup);
                }
            }

            // 6. Compute total weight (sum of edge lengths) and average direction for each group
            List<double> groupWeights = new List<double>();
            List<XYZ> groupDirections = new List<XYZ>();

            foreach (var group in directionGroups)
            {
                // Sum of edge lengths in the group
                double totalLength = 0;
                foreach (int edgeIndex in group)
                {
                    totalLength += edgeLengths[edgeIndex];
                }
                groupWeights.Add(totalLength);

                // Compute the group's weighted average direction
                groupDirections.Add(CalculateWeightedAverageDirection(group, edgeDirections, edgeLengths));
            }

            // 7. Sort by weight to extract the primary directions
            int[] sortedIndices = Enumerable.Range(0, groupDirections.Count)
                .OrderByDescending(i => groupWeights[i])
                .ToArray();

            // 8. Build the result
            if (groupDirections.Count >= 2)
            {
                // At least two direction groups: take the two highest-weighted groups as primary and secondary
                int primaryIndex = sortedIndices[0];
                int secondaryIndex = sortedIndices[1];

                return (
                    PrimaryDirection: groupDirections[primaryIndex],      // Primary direction
                    SecondaryDirection: groupDirections[secondaryIndex]   // Secondary direction
                );
            }
            else if (groupDirections.Count == 1)
            {
                // Only one direction group: manually construct a secondary direction perpendicular to the primary
                XYZ primaryDirection = groupDirections[0];
                // Use the cross product of the face normal and primary direction to create a perpendicular vector
                XYZ secondaryDirection = faceNormal.CrossProduct(primaryDirection).Normalize();

                return (
                    PrimaryDirection: primaryDirection,         // Primary direction
                    SecondaryDirection: secondaryDirection      // Artificially constructed perpendicular secondary direction
                );
            }
            else
            {
                // Unable to extract valid directions (very rare)
                throw new InvalidOperationException("Unable to extract valid directions from the face");
            }
        }

        /// <summary>
        /// Compute the weighted average direction of a set of edges based on their lengths
        /// </summary>
        /// <param name="edgeIndices">Indices of the edges</param>
        /// <param name="directions">Direction vectors of all edges</param>
        /// <param name="lengths">Lengths of all edges</param>
        /// <returns>Normalized weighted average direction vector</returns>
        public static XYZ CalculateWeightedAverageDirection(List<int> edgeIndices, List<XYZ> directions, List<double> lengths)
        {
            if (edgeIndices.Count == 0)
                return null;

            double sumX = 0, sumY = 0, sumZ = 0;
            XYZ referenceDir = directions[edgeIndices[0]];  // Use the first direction in the group as reference

            foreach (int i in edgeIndices)
            {
                XYZ currentDir = directions[i];

                // Compute dot product with the reference direction to determine whether to invert
                double dot = referenceDir.DotProduct(currentDir);

                // If the direction is opposite (negative dot), invert the vector before accumulating
                // This ensures vectors in the same group point consistently and do not cancel each other out
                double factor = (dot >= 0) ? lengths[i] : -lengths[i];

                // Accumulate vector components (with weight)
                sumX += currentDir.X * factor;
                sumY += currentDir.Y * factor;
                sumZ += currentDir.Z * factor;
            }

            // Build the combined vector and normalize
            XYZ avgDir = new XYZ(sumX, sumY, sumZ);
            double magnitude = avgDir.GetLength();

            // Guard against the zero vector
            if (magnitude < 1e-10)
                return referenceDir;  // Fall back to the reference direction

            return avgDir.Normalize();  // Return the normalized direction vector
        }

        /// <summary>
        /// Determine whether three vectors satisfy the right-hand rule and are strictly perpendicular to each other
        /// </summary>
        /// <param name="thumb">Thumb direction vector</param>
        /// <param name="indexFinger">Index-finger direction vector</param>
        /// <param name="middleFinger">Middle-finger direction vector</param>
        /// <param name="tolerance">Tolerance for the check, default 1e-6</param>
        /// <returns>True if the three vectors satisfy the right-hand rule and are mutually perpendicular; otherwise false</returns>
        public static bool IsRightHandRuleCompliant(this XYZ thumb, XYZ indexFinger, XYZ middleFinger, double tolerance = 1e-6)
        {
            // Check whether the three vectors are mutually perpendicular (all dot products near zero)
            double dotThumbIndex = Math.Abs(thumb.DotProduct(indexFinger));
            double dotThumbMiddle = Math.Abs(thumb.DotProduct(middleFinger));
            double dotIndexMiddle = Math.Abs(indexFinger.DotProduct(middleFinger));

            bool areOrthogonal = (dotThumbIndex <= tolerance) &&
                                  (dotThumbMiddle <= tolerance) &&
                                  (dotIndexMiddle <= tolerance);

            // Only check the right-hand rule if the three vectors are mutually perpendicular
            if (!areOrthogonal)
                return false;

            // Compute the dot product of the cross-product vector with the thumb to determine right-hand-rule compliance
            XYZ crossProduct = indexFinger.CrossProduct(middleFinger);
            double rightHandTest = crossProduct.DotProduct(thumb);

            // A positive dot product indicates right-hand-rule compliance
            return rightHandTest > tolerance;
        }

        /// <summary>
        /// Generate an index-finger direction that satisfies the right-hand rule, given the thumb and middle-finger directions
        /// </summary>
        /// <param name="thumb">Thumb direction vector</param>
        /// <param name="middleFinger">Middle-finger direction vector</param>
        /// <param name="tolerance">Tolerance for perpendicularity check, default 1e-6</param>
        /// <returns>The generated index-finger direction vector, or null if the input vectors are not perpendicular</returns>
        public static XYZ GenerateIndexFinger(this XYZ thumb, XYZ middleFinger, double tolerance = 1e-6)
        {
            // Normalize the input vectors first
            XYZ normalizedThumb = thumb.Normalize();
            XYZ normalizedMiddleFinger = middleFinger.Normalize();

            // Check whether the two vectors are perpendicular (dot product near zero)
            double dotProduct = normalizedThumb.DotProduct(normalizedMiddleFinger);

            // If the absolute dot product exceeds the tolerance, the vectors are not perpendicular
            if (Math.Abs(dotProduct) > tolerance)
            {
                return null;
            }

            // Compute the index-finger direction via cross product and negate it
            XYZ indexFinger = normalizedMiddleFinger.CrossProduct(normalizedThumb).Negate();

            // Return the normalized index-finger direction vector
            return indexFinger.Normalize();
        }

        /// <summary>
        /// Create or retrieve a level at the specified elevation
        /// </summary>
        /// <param name="doc">Revit document</param>
        /// <param name="elevation">Level elevation (ft)</param>
        /// <param name="levelName">Level name</param>
        /// <returns></returns>
        public static Level CreateOrGetLevel(this Document doc, double elevation, string levelName)
        {
            // First, check whether a level at the specified elevation already exists
            Level existingLevel = new FilteredElementCollector(doc)
                .OfClass(typeof(Level))
                .Cast<Level>()
                .FirstOrDefault(l => Math.Abs(l.Elevation - elevation) < 0.1 / 304.8);

            if (existingLevel != null)
                return existingLevel;

            // Create a new level
            Level newLevel = Level.Create(doc, elevation);
            // Set the level name
            Level namesakeLevel = new FilteredElementCollector(doc)
                 .OfClass(typeof(Level))
                 .Cast<Level>()
                 .FirstOrDefault(l => l.Name == levelName);
            if (namesakeLevel != null)
            {
                levelName = $"{levelName}_{newLevel.Id.IntegerValue.ToString()}";
            }
            newLevel.Name = levelName;

            return newLevel;
        }

        /// <summary>
        /// Find the level nearest to a given elevation
        /// </summary>
        /// <param name="doc">Current Revit document</param>
        /// <param name="height">Target elevation (Revit internal units)</param>
        /// <returns>The level closest to the target elevation, or null if the document contains no levels</returns>
        public static Level FindNearestLevel(this Document doc, double height)
        {
            if (doc == null)
                throw new ArgumentNullException(nameof(doc), "Document cannot be null");

            // Use LINQ directly to get the closest level
            return new FilteredElementCollector(doc)
                .OfClass(typeof(Level))
                .Cast<Level>()
                .OrderBy(level => Math.Abs(level.Elevation - height))
                .FirstOrDefault();
        }

        ///// <summary>
        ///// Refresh the view with an optional delay
        ///// </summary>
        //public static void Refresh(this Document doc, int waitingTime = 0, bool allowOperation = true)
        //{
        //    UIApplication uiApp = new UIApplication(doc.Application);
        //    UIDocument uiDoc = uiApp.ActiveUIDocument;

        //    // Check whether the document can be modified
        //    if (uiDoc.Document.IsModifiable)
        //    {
        //        // Update the model
        //        uiDoc.Document.Regenerate();
        //    }
        //    // Refresh the UI
        //    uiDoc.RefreshActiveView();

        //    // Delay
        //    if (waitingTime != 0)
        //    {
        //        System.Threading.Thread.Sleep(waitingTime);
        //    }

        //    // Allow the user to perform non-safe operations
        //    if (allowOperation)
        //    {
        //        System.Windows.Forms.Application.DoEvents();
        //    }
        //}

        /// <summary>
        /// Save the specified message to a file on the desktop (overwrites by default)
        /// </summary>
        /// <param name="message">The message content to save</param>
        /// <param name="fileName">Target file name</param>
        public static void SaveToDesktop(this string message, string fileName = "temp.json", bool isAppend = false)
        {
            // Ensure the file name has an extension
            if (!Path.HasExtension(fileName))
            {
                fileName += ".txt"; // Add .txt by default
            }

            // Get the desktop path
            string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);

            // Build the full file path
            string filePath = Path.Combine(desktopPath, fileName);

            // Write the file (overwrite mode)
            using (StreamWriter sw = new StreamWriter(filePath, isAppend))
            {
                sw.WriteLine($"{message}");
            }
        }

    }
}
