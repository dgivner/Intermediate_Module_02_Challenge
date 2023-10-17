using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Intermediate_Module_02_Challenge
{
    [Transaction(TransactionMode.Manual)]
    internal class Command4 : IExternalCommand
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
            catList.Add(BuiltInCategory.OST_LightingFixtures);

            ElementMulticategoryFilter catFilter = new ElementMulticategoryFilter(catList);
            collector.WherePasses(catFilter).WhereElementIsNotElementType();

            FamilySymbol curRoomTag = new FilteredElementCollector(doc).OfClass(typeof(FamilySymbol))
                .Cast<FamilySymbol>().Where(x => x.FamilyName.Equals("M_Room Tag")).First();

            FamilySymbol curLightTag = new FilteredElementCollector(doc).OfClass(typeof(FamilySymbol))
                .Cast<FamilySymbol>().Where(x => x.FamilyName.Equals("M_Lighting Fixture Tag")).First();

            Dictionary<string, FamilySymbol> tags = new Dictionary<string, FamilySymbol>();
            tags.Add("Rooms", curRoomTag);
            tags.Add("Lighting Fixtures", curLightTag);

            int counter = 0;

            using (Transaction t = new Transaction(doc))
            {
                t.Start("Tag Rooms & Lights in RCP View");
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

                    FamilySymbol curTagType = tags[curElem.Category.Name];

                    Reference curRef = new Reference(curElem);

                    //Place Tag
                    if (IsElementTagged(curView, curElem) == false)
                    {
                        IndependentTag newTag = IndependentTag.Create(doc, curTagType.Id, curView.Id, curRef, false,
                            TagOrientation.Horizontal, instPoint);

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
            string buttonInternalName = "btnCommand4";
            string buttonTitle = "RCP Tags";

            ButtonDataClass myButtonData4 = new ButtonDataClass(
                buttonInternalName,
                buttonTitle,
                MethodBase.GetCurrentMethod().DeclaringType?.FullName,
                Properties.Resources.Blue_32,
                Properties.Resources.Blue_16,
                "This tool tags all lights and rooms in RCP views.");

            return myButtonData4.Data;
        }
    }
}
