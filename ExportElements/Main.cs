using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Newtonsoft.Json;
using ExportElements.Models;



namespace ExportElements
{

    [Transaction(TransactionMode.Manual)]
        [Regeneration(RegenerationOption.Manual)]
    public class Main : IExternalCommand
    {
        // private HttpClient _client;

        static readonly HttpClient _client = new HttpClient();

        IList<ElementData> GetAllElements(Document doc)
        {
            ElementClassFilter FamilyInstanceFilter = new ElementClassFilter(typeof(FamilyInstance));
            FilteredElementCollector FamilyInstanceCollector = new FilteredElementCollector(doc);
            IList<Element> ElementsCollection = FamilyInstanceCollector.WherePasses(FamilyInstanceFilter).ToElements();
            IList<ElementData> AllModelElements = new List<ElementData>();

            foreach (Element e in ElementsCollection)
            {
                if ((null != e.Category)
                && (null != e.LevelId)
                && (null != e.get_Geometry(new Options()))
                )
                {
                    ElementData elData = new ElementData();
                    elData.Id = e.Id.IntegerValue;
                    elData.Name = e.Name;
                    elData.CategoryEnumName = ((BuiltInCategory)e.Category.Id.IntegerValue).ToString();
                    elData.CategoryLocalizedName = e.Category.Name;

                    AllModelElements.Add(elData);
                }
            }
            return AllModelElements;
        }

        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {

            UIApplication uiapp = commandData.Application;
            Document doc = uiapp.ActiveUIDocument.Document;

            IList<ElementData> elems = GetAllElements(doc);

            ProjectDoc projectDoc = new ProjectDoc();
            projectDoc.Title = doc.Title;
            projectDoc.Data = JsonConvert.SerializeObject(elems);

            string json = JsonConvert.SerializeObject(projectDoc);

            var content = new StringContent(json, Encoding.UTF8, "application/json");

            try
            {
                var result = _client.PostAsync("http://localhost:8080/api/Projects", content).GetAwaiter().GetResult();
            }
            catch (HttpRequestException e)
            {
                TaskDialog.Show("Warning", e.Message);
            }

            return Result.Succeeded;
        }
    }
}

