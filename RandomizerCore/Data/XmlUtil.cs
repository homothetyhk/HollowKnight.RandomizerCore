using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml;

namespace RandomizerCore.Data
{
    public static class XmlUtil
    {
        private static Dictionary<Type, Dictionary<string, FieldInfo>> reflectionCache = new Dictionary<Type, Dictionary<string, FieldInfo>>();

        public static XmlDocument LoadEmbeddedXml(string embeddedResourcePath)
        {
            Stream stream = typeof(XmlUtil).Assembly.GetManifestResourceStream(embeddedResourcePath);
            XmlDocument doc = new XmlDocument();
            doc.Load(stream);
            stream.Dispose();
            return doc;
        }

        /*
        public static void ParseXML(Assembly randoDLL)
        {
            XmlDocument macroXml;
            XmlDocument areaXml;
            XmlDocument roomXml;
            XmlDocument itemXml;
            XmlDocument locationXml;
            XmlDocument waypointXml;
            XmlDocument startLocationXml;

            Stopwatch watch = new Stopwatch();
            watch.Start();

            try
            {
                Stream macroStream = randoDLL.GetManifestResourceStream("RandomizerMod.Resources.macros.xml");
                macroXml = new XmlDocument();
                macroXml.Load(macroStream);
                macroStream.Dispose();

                Stream areaStream = randoDLL.GetManifestResourceStream("RandomizerMod.Resources.areas.xml");
                areaXml = new XmlDocument();
                areaXml.Load(areaStream);
                areaStream.Dispose();

                Stream roomStream = randoDLL.GetManifestResourceStream("RandomizerMod.Resources.rooms.xml");
                roomXml = new XmlDocument();
                roomXml.Load(roomStream);
                roomStream.Dispose();

                Stream itemStream = randoDLL.GetManifestResourceStream("RandomizerMod.Resources.items.xml");
                itemXml = new XmlDocument();
                itemXml.Load(itemStream);
                itemStream.Dispose();

                Stream locationStream = randoDLL.GetManifestResourceStream("RandomizerMod.Resources.locations.xml");
                locationXml = new XmlDocument();
                locationXml.Load(locationStream);
                locationStream.Dispose();

                Stream waypointStream = randoDLL.GetManifestResourceStream("RandomizerMod.Resources.waypoints.xml");
                waypointXml = new XmlDocument();
                waypointXml.Load(waypointStream);
                waypointStream.Dispose();

                Stream startLocationStream = randoDLL.GetManifestResourceStream("RandomizerMod.Resources.startlocations.xml");
                startLocationXml = new XmlDocument();
                startLocationXml.Load(startLocationStream);
                startLocationStream.Dispose();
            }
            catch (Exception e)
            {
                //LogError("Could not load xml streams:\n" + e);
                //return;
                throw e;
            }
            try
            {
                MacroData macros = MacroData.Parse(macroXml.SelectNodes("randomizer/macro"));
                WaypointData waypoints = WaypointData.data = WaypointData.Parse(waypointXml.SelectNodes("randomizer/item"));
                ItemData items = ItemData.data = ItemData.Parse(itemXml.SelectNodes("randomizer/item"));
                LocationData locations = LocationData.data = LocationData.Parse(locationXml.SelectNodes("randomizer/item"));
                TransitionData areaTransitions = TransitionData.areaData = TransitionData.Parse(areaXml.SelectNodes("randomizer/transition"));
                TransitionData roomTransitions = TransitionData.roomData = TransitionData.Parse(roomXml.SelectNodes("randomizer/transition"));
                StartData starts = StartData.data = StartData.Parse(startLocationXml.SelectNodes("randomizer/start"));

                LogicProcessor logicProcessor = new LogicProcessor(macros, items, waypoints, roomTransitions);

                Dictionary<string, LogicDef> logicDefs = new Dictionary<string, LogicDef>();
                logicProcessor.ProcessLocationLogic(locations, logicDefs, LogicMode.Item);
                logicProcessor.ProcessWaypointLogic(waypoints, logicDefs, LogicMode.Item);
                LogicManager.ItemLogicManager = new LogicManager(logicDefs, logicProcessor);

                logicDefs = new Dictionary<string, LogicDef>();
                logicProcessor.ProcessLocationLogic(locations, logicDefs, LogicMode.Area);
                logicProcessor.ProcessTransitionLogic(areaTransitions, logicDefs);
                logicProcessor.ProcessWaypointLogic(waypoints, logicDefs, LogicMode.Area);
                LogicManager.AreaLogicManager = new LogicManager(logicDefs, logicProcessor);

                logicDefs = new Dictionary<string, LogicDef>();
                logicProcessor.ProcessLocationLogic(locations, logicDefs, LogicMode.Room);
                logicProcessor.ProcessTransitionLogic(roomTransitions, logicDefs);
                LogicManager.RoomLogicManager = new LogicManager(logicDefs, logicProcessor);
            }
            catch (Exception e)
            {
                //LogError("Could not parse xml nodes:\n" + e);
                throw e;
            }

            watch.Stop();
            //Log("Parsed items.xml in " + watch.Elapsed.TotalSeconds + " seconds");
        }
        */

        public static Pair<string, T> DeserializeByReflection<T>(XmlNode node) where T : new()
        {
            if (!reflectionCache.TryGetValue(typeof(T), out var fieldDict))
            {
                fieldDict = reflectionCache[typeof(T)] = typeof(T).GetFields().ToDictionary(f => f.Name, f => f);
            }

            string name = node.Attributes["name"].InnerText;
            object def = new T();
            foreach (XmlNode fieldNode in node.ChildNodes)
            {
                if (!fieldDict.TryGetValue(fieldNode.Name, out FieldInfo field)) continue;
                Type type = field.FieldType;
                string stringValue = fieldNode.InnerText;

                if (type == typeof(string))
                {
                    field.SetValue(def, stringValue);
                }
                else if (type == typeof(bool))
                {
                    field.SetValue(def, bool.Parse(stringValue));
                }
                else if (type == typeof(int))
                {
                    field.SetValue(def, int.Parse(stringValue));
                }
                else if (type == typeof(float))
                {
                    field.SetValue(def, float.Parse(stringValue));
                }
                else if (type.IsEnum)
                {
                    field.SetValue(def, Enum.Parse(type, stringValue));
                }

            }

            return new Pair<string, T>(name, (T)def);
        }
    }
}
