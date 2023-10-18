#region Namespaces
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using Intermediate_Module_02_Challenge.Utils;
using Intermediate_Module_02_Challenge;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Windows.Controls;
using System.Windows.Input;

#endregion

namespace Intermediate_Module_02_Challenge
{
    [Transaction(TransactionMode.Manual)]

    public class Command2 : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            // this is a variable for the Revit application
            UIApplication uiapp = commandData.Application;

            // this is a variable for the current Revit model
            Document doc = uiapp.ActiveUIDocument.Document;

            // Your code goes here
            View curView = doc.ActiveView;
            FilteredElementCollector collector = new FilteredElementCollector(doc);

            List<BuiltInCategory> catList = new List<BuiltInCategory>();
            catList.Add(BuiltInCategory.OST_Rooms);
            catList.Add(BuiltInCategory.OST_Areas);
            catList.Add(BuiltInCategory.OST_Walls);
            catList.Add(BuiltInCategory.OST_Doors);
            catList.Add(BuiltInCategory.OST_LightingFixtures);
            catList.Add(BuiltInCategory.OST_Windows);
            catList.Add(BuiltInCategory.OST_Furniture);

            ElementMulticategoryFilter catFilter = new ElementMulticategoryFilter(catList);
            collector.WherePasses(catFilter).WhereElementIsNotElementType();

            FamilySymbol curRoomTag = new FilteredElementCollector(doc).OfClass(typeof(FamilySymbol))
                .Cast<FamilySymbol>().Where(x => x.FamilyName.Equals("M_Room Tag")).First();

            FamilySymbol curWallTag = new FilteredElementCollector(doc).OfClass(typeof(FamilySymbol))
                .Cast<FamilySymbol>().Where(x => x.FamilyName.Equals("M_Wall Tag")).First();

            FamilySymbol curDoorTag = new FilteredElementCollector(doc).OfClass(typeof(FamilySymbol))
                .Cast<FamilySymbol>().Where(x => x.FamilyName.Equals("M_Door Tag")).First();

            FamilySymbol curLightTag = new FilteredElementCollector(doc).OfClass(typeof(FamilySymbol))
                .Cast<FamilySymbol>().Where(x => x.FamilyName.Equals("M_Lighting Fixture Tag")).First();

            FamilySymbol curWindowTag = new FilteredElementCollector(doc).OfClass(typeof(FamilySymbol))
                .Cast<FamilySymbol>().Where(x => x.FamilyName.Equals("M_Window Tag")).First();

            FamilySymbol curFurnTag = new FilteredElementCollector(doc).OfClass(typeof(FamilySymbol))
                .Cast<FamilySymbol>().Where(x => x.FamilyName.Equals("M_Furniture Tag")).First();

            Dictionary<string, FamilySymbol> tags = new Dictionary<string, FamilySymbol>();
            tags.Add("Rooms", curRoomTag);
            tags.Add("Walls", curWallTag);
            tags.Add("Doors", curDoorTag);
            tags.Add("Lighting Fixtures", curLightTag);
            tags.Add("Windows", curWindowTag);
            tags.Add("Furniture", curFurnTag);
            

            int counter = 0;

            //Initialize GetAllViews from collector class
            List<View> allViews = Collectors.GetAllViews(doc);

            FilteredElementCollector viewCollector = new FilteredElementCollector(doc);

            ICollection<Element> collection = viewCollector.OfClass(typeof(View)).ToElements();
            using (Transaction t = new Transaction(doc))
            {
                t.Start("Tag Elements in all Views.");
                // Iterate through all views
                foreach (Element e in collection)
                {
                    View view = e as View;
                    // Check if the view is null or is a template
                    if (null == view || view.IsTemplate)
                        continue;
                    // Now you can work with the view

                    foreach (Element curElem in collector)
                    {
                        XYZ instPoint;
                        LocationPoint locPoint;
                        LocationCurve locCurve;

                        Location curLoc = curElem.Location;

                        if (curLoc == null) continue;

                        locPoint = curLoc as LocationPoint;
                        if (locPoint != null)
                        {
                            instPoint = locPoint.Point;
                        }
                        else
                        {
                            locCurve = curLoc as LocationCurve;
                            Curve curCurve = locCurve.Curve;

                            instPoint = GetMidpointBetweenTwoPoints(curCurve.GetEndPoint(0), curCurve.GetEndPoint(1));
                        }

                        FamilySymbol curTagType = null;
                        if (tags.ContainsKey(curElem.Category.Name))
                        {
                            curTagType = tags[curElem.Category.Name];
                        }
                        else if (curElem.Category.Name == "Area")
                        {
                            if (view.ViewType == ViewType.AreaPlan)
                            {
                                if (IsElementTagged(curView, curElem) == false)
                                {
                                    if (curElem.Category.Name == "Area")
                                    {
                                        ViewPlan curAreaPlan = curView as ViewPlan;
                                        Area curArea = curElem as Area;

                                        AreaTag curAreaTag =
                                            doc.Create.NewAreaTag(curAreaPlan, curArea, new UV(instPoint.X, instPoint.Y));
                                        curAreaTag.TagHeadPosition = new XYZ(instPoint.X, instPoint.Y, 0);
                                        curAreaTag.HasLeader = false;
                                    }
                                }
                            }
                        }

                        else
                        {
                            continue;
                        }
                        //FamilySymbol curTagType = tags[curElem.Category.Name];
                        Reference curRef = new Reference(curElem);

                        if (view.ViewType == ViewType.FloorPlan)
                        {
                            if (IsElementTagged(curView, curElem) == false)
                            {
                                IndependentTag newTag = IndependentTag.Create(doc, curTagType.Id, curView.Id, curRef,
                                    false,
                                    TagOrientation.Horizontal, instPoint);
                                if (curElem.Category.Name == "Windows")
                                {
                                    newTag.TagHeadPosition = new XYZ(instPoint.X, 3, 0);
                                }

                                if (curElem.Category.Name == "Walls")
                                {
                                    Wall curWall = curElem as Wall;
                                    WallType curWallType = curWall.WallType;
                                    if (curWallType.Kind == WallKind.Curtain)
                                    {
                                        newTag.TagHeadPosition = instPoint;
                                    }
                                }

                                if (curElem.Category.Name == "Rooms")
                                {
                                    //ViewPlan curSecView = curView as ViewPlan;
                                    newTag.TagHeadPosition = new XYZ(instPoint.X, instPoint.Y, 0);
                                }

                                if (curElem.Category.Name == "Windows")
                                {
                                    Element curWindow = curElem as Element;
                                    newTag.TagHeadPosition = new XYZ(instPoint.X, 3, 0);
                                }
                            }
                        }

                        if (view.ViewType == ViewType.CeilingPlan)
                        {
                            if (IsElementTagged(curView, curElem) == false)
                            {
                                IndependentTag newTag = IndependentTag.Create(doc, curTagType.Id, curView.Id, curRef,
                                    false,
                                    TagOrientation.Horizontal, instPoint);
                                if (curElem.Category.Name == "Lighting Fixtures")
                                {
                                    Element curLightingFixture = curElem as Element;
                                    newTag.TagHeadPosition = new XYZ(instPoint.X, instPoint.Y, 0);
                                }

                                if (curElem.Category.Name == "Rooms")
                                {
                                    Element curRoom = curElem as Element;
                                    newTag.TagHeadPosition = new XYZ(instPoint.X, instPoint.Y, 0);
                                }
                            }
                        }

                        

                        if (view.ViewType == ViewType.Section)
                        {
                            IndependentTag newTag = IndependentTag.Create(doc, curTagType.Id, curView.Id, curRef, false,
                                TagOrientation.Horizontal, instPoint);
                            if (curElem.Category.Name == "Rooms")
                            {
                                //ViewPlan curSecView = curView as ViewPlan;
                                newTag.TagHeadPosition = new XYZ(instPoint.X, instPoint.Y, 3);
                            }

                            counter++;
                        }
                    }
                }

                t.Commit();

                TaskDialog.Show("Complete", $"Placed {counter} tags in all views.");
            }
            return Result.Succeeded;
        }

        
        private bool IsElementTagged(View curView, Element curElem)
        {
            FilteredElementCollector collector = new FilteredElementCollector(curElem.Document, curView.Id);
            collector.OfClass(typeof(IndependentTag)).WhereElementIsNotElementType();

            foreach (IndependentTag curTag in collector)
            {
                List<ElementId> taggedIds = curTag.GetTaggedLocalElementIds().ToList();

                foreach (ElementId taggedId in taggedIds)
                {
                    if (taggedId.Equals(curElem.Id))
                        return true;
                }
            }

            return false;
        }

        private XYZ GetMidpointBetweenTwoPoints(XYZ start, XYZ end)
        {
            XYZ midPoint = new XYZ((start.X + end.X) / 2, (start.Y + end.Y) / 2, (start.Z + end.Z) / 2);
            return midPoint;
        }

        internal static PushButtonData GetButtonData()
        {
            // use this method to define the properties for this command in the Revit ribbon
            string buttonInternalName = "btnCommand2";
            string buttonTitle = "Tag All Views";

            ButtonDataClass myButtonData2 = new ButtonDataClass(
                buttonInternalName,
                buttonTitle,
                MethodBase.GetCurrentMethod().DeclaringType?.FullName,
                Properties.Resources.Blue_32,
                Properties.Resources.Blue_16,
                "This tool tags elements in all views.");

            return myButtonData2.Data;
        }
    }
}

