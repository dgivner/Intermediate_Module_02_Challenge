#region Namespaces
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using Autodesk.Revit.DB.Architecture;

#endregion

namespace Intermediate_Module_02_Challenge
{
    [Transaction(TransactionMode.Manual)]
    public class Command1 : IExternalCommand
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
            //catList.Add(BuiltInCategory.OST_Areas);
            catList.Add(BuiltInCategory.OST_Walls);
            catList.Add(BuiltInCategory.OST_Doors);
            //catList.Add(BuiltInCategory.OST_LightingFixtures);
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

            //FamilySymbol curLightTag = new FilteredElementCollector(doc).OfClass(typeof(FamilySymbol))
            //    .Cast<FamilySymbol>().Where(x => x.FamilyName.Equals("M_Lighting Fixture Tag")).First();

            FamilySymbol curWindowTag = new FilteredElementCollector(doc).OfClass(typeof(FamilySymbol))
                .Cast<FamilySymbol>().Where(x => x.FamilyName.Equals("M_Window Tag")).First();

            FamilySymbol curFurnTag = new FilteredElementCollector(doc).OfClass(typeof(FamilySymbol))
                .Cast<FamilySymbol>().Where(x => x.FamilyName.Equals("M_Furniture Tag")).First();

            Dictionary<string, FamilySymbol> tags = new Dictionary<string, FamilySymbol>();
            tags.Add("Rooms", curRoomTag);
            tags.Add("Walls", curWallTag);
            tags.Add("Doors", curDoorTag);
            //tags.Add("Lighting Fixtures", curLightTag);
            tags.Add("Windows", curWindowTag);
            tags.Add("Furniture", curFurnTag);

            int counter = 0;

            using (Transaction t = new Transaction(doc))
            {
                t.Start("Tag All Elements in View");
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

                    if (curElem.Category.Name == "Walls")
                    {
                        Wall curWall = curElem as Wall;
                        WallType curWallType = curWall.WallType;
                    }

                    //ViewType cureViewType = curView.ViewType;
                    //if (cureViewType == ViewType.AreaPlan)
                    //{
                    //    TaskDialog.Show("Area View", "This is an Area View Plan");
                    //}

                    FamilySymbol curTagType = tags[curElem.Category.Name];

                    Reference curRef = new Reference(curElem);

                    
                    //Place Tag
                    if (IsElementTagged(curView, curElem) == false)
                    {
                        IndependentTag newTag = IndependentTag.Create(doc, curTagType.Id, curView.Id, curRef, false, TagOrientation.Horizontal, instPoint);

                        if (curView.ViewType == ViewType.FloorPlan)
                        {
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

                        //if (curView.ViewType == ViewType.Section)
                        //{
                        //    //Section View Tag Location
                        //    if (curElem.Category.Name == "Rooms")
                        //    {
                        //        newTag.TagHeadPosition = new XYZ(instPoint.X, instPoint.Y, 3);
                        //    }
                        //}
                        
                        
                    }
                    
                }
                t.Commit();
            }

            TaskDialog.Show("Complete", $"Placed {counter} tags in the current view");
            


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
            string buttonInternalName = "btnCommand1";
            string buttonTitle = "Button 1";

            ButtonDataClass myButtonData1 = new ButtonDataClass(
                buttonInternalName,
                buttonTitle,
                MethodBase.GetCurrentMethod().DeclaringType?.FullName,
                Properties.Resources.Blue_32,
                Properties.Resources.Blue_16,
                "This is a tooltip for Button 1");

            return myButtonData1.Data;
        }
    }
}
