using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace RoomNumerator
{
    [Autodesk.Revit.Attributes.Transaction(Autodesk.Revit.Attributes.TransactionMode.Manual)]
    class RoomNumeratorCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            try
            {
                GetPluginStartInfo().GetAwaiter().GetResult();
            }
            catch
            {
                // телеметрия не должна прерывать команду
            }

            Document doc = commandData.Application.ActiveUIDocument.Document;
            Selection sel = commandData.Application.ActiveUIDocument.Selection;

            List<Room> roomList = GetRoomsFromCurrentSelection(doc, sel);

            if (roomList.Count == 0)
            {
                RoomSelectionFilter selFilter = new RoomSelectionFilter();
                IList<Reference> selRooms = null;
                try
                {
                    selRooms = sel.PickObjects(ObjectType.Element, selFilter, "Выберите помещения!");
                }
                catch (Autodesk.Revit.Exceptions.OperationCanceledException)
                {
                    return Result.Cancelled;
                }

                foreach (Reference roomRef in selRooms)
                {
                    roomList.Add(doc.GetElement(roomRef) as Room);
                }
            }

            //Вызов формы
            RoomNumeratorWPF roomNumeratorWPF = new RoomNumeratorWPF();
            roomNumeratorWPF.ShowDialog();
            if (roomNumeratorWPF.DialogResult != true)
            {
                return Result.Cancelled;
            }

            string numberPrefix = roomNumeratorWPF.NumberPrefix;
            string startFrom = roomNumeratorWPF.StartFrom;
            int formatLength = startFrom.Length;
            bool tryCheck = int.TryParse(startFrom, out int cnt);
            if (!tryCheck) cnt = 1;

            string selectedNumberingDirection = roomNumeratorWPF.SelectedNumberingDirection;
            switch (selectedNumberingDirection)
            {
                case "radioButton_RightAndDown":
                    //Вправо и вниз
                    roomList = roomList.OrderByDescending(r => GetRoomCenter(r)
                        , new CoordinatesBasedComparerXYDown())
                        .ToList();
                    break;
                case "radioButton_DownAndRight":
                    //Вниз и вправо
                    roomList = roomList.OrderByDescending(r => GetRoomCenter(r)
                        , new CoordinatesBasedComparerYXDown())
                        .ToList();
                    break;
                case "radioButton_RightAndUp":
                    //Вправо и вверх
                    roomList = roomList.OrderByDescending(r => GetRoomCenter(r)
                        , new CoordinatesBasedComparerXYUp())
                        .ToList();
                    break;
                case "radioButton_UpAndRight":
                    //Вверх и вправо
                    roomList = roomList.OrderByDescending(r => GetRoomCenter(r)
                        , new CoordinatesBasedComparerYXUp())
                        .ToList();
                    break;
            }

            using (Transaction t = new Transaction(doc))
            {
                t.Start("Нумерация помещений");

                foreach (Room room in roomList)
                {
                    if (numberPrefix == "" || numberPrefix == null)
                    {
                        room.get_Parameter(BuiltInParameter.ROOM_NUMBER).Set($"{cnt}");
                    }
                    else
                    {
                        room.get_Parameter(BuiltInParameter.ROOM_NUMBER).Set($"{numberPrefix}{cnt.ToString($"D{formatLength}")}");
                    }
                    cnt++;
                }

                t.Commit();
            }

            return Result.Succeeded;
        }
        private static List<Room> GetRoomsFromCurrentSelection(Document doc, Selection sel)
        {
            ICollection<ElementId> selectedIds = sel.GetElementIds();
            List<Room> tempRoomsList = new List<Room>();

            foreach (ElementId id in selectedIds)
            {
                Element e = doc.GetElement(id);
                if (e is Room room &&
                    e.Category != null &&
                    e.Category.Id == new ElementId(BuiltInCategory.OST_Rooms))
                {
                    tempRoomsList.Add(room);
                }
            }

            return tempRoomsList;
        }
        private static XYZ GetRoomCenter(Room room)
        {
            BoundingBoxXYZ? bbox = room.get_BoundingBox(null);
            if (bbox != null)
                return (bbox.Max + bbox.Min) * 0.5;
            if (room.Location is LocationPoint lp)
                return lp.Point;
            return XYZ.Zero;
        }
        private static async Task GetPluginStartInfo()
        {
            Assembly thisAssembly = Assembly.GetExecutingAssembly();
            string assemblyName = "RoomNumerator";
            string assemblyNameRus = "Нумератор помещений";
            string? assemblyFolderPath = Path.GetDirectoryName(thisAssembly.Location);
            if (string.IsNullOrEmpty(assemblyFolderPath))
                return;

            int lastBackslashIndex = assemblyFolderPath.LastIndexOf('\\');
            if (lastBackslashIndex < 0)
                return;
            string dllPath = assemblyFolderPath.Substring(0, lastBackslashIndex + 1) + "PluginInfoCollector\\PluginInfoCollector.dll";
            if (!File.Exists(dllPath))
                return;

            Assembly assembly = Assembly.LoadFrom(dllPath);
            Type type = assembly.GetType("PluginInfoCollector.InfoCollector");

            if (type != null)
            {
                // Создание экземпляра класса
                object instance = Activator.CreateInstance(type);

                // Получение метода CollectPluginUsageAsync
                var method = type.GetMethod("CollectPluginUsageAsync");

                if (method != null)
                {
                    // Вызов асинхронного метода через reflection
                    Task task = (Task)method.Invoke(instance, new object[] { assemblyName, assemblyNameRus });
                    await task;  // Ожидание завершения асинхронного метода
                }
            }
        }
    }
}
