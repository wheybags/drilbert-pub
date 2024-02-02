using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Drilbert
{
    public class LevelSection : List<Tilemap>
    {
        public string name;
        public LevelSection leftParent = null;
        public LevelSection rightParent = null;
    }

    public static class Levels
    {
        public static List<LevelSection> allSections = null;
        public static IEnumerable<Tilemap> allLevels => allSections.SelectMany(x => x);

        static Levels()
        {
            load();
        }

        public static void load(string rootPath = null)
        {
            if (rootPath == null)
                rootPath = Constants.rootPath;

            string strData = File.ReadAllText(rootPath + "/levels/levels.json");
            JsonNode data = Util.parseJson(strData);

            Dictionary<string, LevelSection> sectionMap = new Dictionary<string, LevelSection>();
            allSections = new List<LevelSection>();

            foreach (var sectionItem in data.AsArray())
            {
                string name = sectionItem["name"].GetValue<string>();
                LevelSection section = new LevelSection() { name = name };

                foreach (var levelPathItem in sectionItem["levels"].AsArray())
                    section.Add(new Tilemap(rootPath,levelPathItem.GetValue<string>()));

                sectionMap[name] = section;
                allSections.Add(section);
            }

            foreach (var sectionItem in data.AsArray())
            {
                LevelSection section = sectionMap[sectionItem["name"].GetValue<string>()];

                if (sectionItem.AsObject().ContainsKey("parent_left"))
                    section.leftParent = sectionMap[sectionItem["parent_left"].GetValue<string>()];

                if (sectionItem.AsObject().ContainsKey("parent_right"))
                    section.rightParent = sectionMap[sectionItem["parent_right"].GetValue<string>()];
            }


            Util.ReleaseAssert(allSections[0].leftParent == null && allSections[0].rightParent == null);

#if DEMO
            sectionMap["bomb"].RemoveRange(1, sectionMap["bomb"].Count - 1);
            sectionMap["bomb"].Add(null);
            sectionMap["bomb"].Add(null);

            sectionMap["megadrill"].RemoveRange(1, sectionMap["megadrill"].Count - 1);
            sectionMap["megadrill"].Add(null);
            sectionMap["megadrill"].Add(null);

            sectionMap["final"].Clear();
            sectionMap["final"].Add(null);
#endif
        }
    }
}