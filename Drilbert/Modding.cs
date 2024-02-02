using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json.Nodes;
using Steamworks;

namespace Drilbert;

public class Mod
{
    public string title;
    public string path;
    public bool local;
    public ulong steamWorkshopId;
}

public class ModLine : MenuLine
{
    public Mod mod;
}

public static class Modding
{
    public static Mod currentMod = null;

    public static Menu modMenu = new Menu();
    public static void updateModMenu()
    {
        List<MenuLine> lines = new List<MenuLine>();
        lines.Add(new MenuLine() { message = "Select mod", type = MenuLine.Type.Label });
        lines.Add(new MenuLine() { message = "", type = MenuLine.Type.Label });

        modMenu.scrollAreaStart = lines.Count;


        foreach (Mod mod in getAllMods())
            lines.Add(new ModLine() { message = mod.title, action =  MenuAction.OpenModPlayOrUpload, type = MenuLine.Type.Button, mod = mod });

        modMenu.scrollAreaEnd = lines.Count;

        modMenu.scrollShowMax = 4;

        lines.Add(new MenuLine() { message = "", type = MenuLine.Type.Label });
        lines.Add(new MenuLine() { message = "Create mod", action = MenuAction.CreateMod, type = MenuLine.Type.Button });
        lines.Add(new MenuLine() { message = "Close", action = MenuAction.CloseMenu, type = MenuLine.Type.Button });

        int previousSelectedIndex = modMenu.selectedIndex;
        modMenu.init(lines, true);
        if (previousSelectedIndex >= 0 && previousSelectedIndex < lines.Count && lines[previousSelectedIndex].isSelectable())
        {
            modMenu.selectedIndex = previousSelectedIndex;
            modMenu.resolveScroll();
        }
    }

    public static void createExampleMod()
    {
        string basePath = Constants.rootPath + "/mods/example";

        for (int i = 0;; i++)
        {
            string outputPath = basePath;
            if (i > 0)
                outputPath += "_" + i;

            if (!Directory.Exists(outputPath))
            {
                Util.copyDirectory(Constants.rootPath + "/template", outputPath);

                if (i > 0)
                {
                    JsonNode json = Util.parseJson(File.ReadAllText(outputPath + "/meta.json"));
                    json["title"] = json["title"].GetValue<string>() + " " + i;
                    File.WriteAllText(outputPath + "/meta.json", json.ToString());
                }

                break;
            }
        }
    }

    public static void ensureModsFolderExists()
    {
        if (!Directory.Exists(Constants.rootPath + "/mods"))
        {
            Directory.CreateDirectory(Constants.rootPath + "/mods");
            createExampleMod();
        }
    }

    public static Menu getModPlayOrUploadMenu(Mod mod)
    {
        List<MenuLine> lines = new List<MenuLine>()
        {
            new MenuLine() { message = mod.title, type = MenuLine.Type.Label },
            new MenuLine() { message = "", type = MenuLine.Type.Label },
            new ModLine() { message = "Play", action = MenuAction.OpenSelectSaveSlotMenuForMod, type = MenuLine.Type.Button, mod = mod},
            new ModLine() { message = "Open Mod Folder", action = MenuAction.OpenModFolder, type = MenuLine.Type.Button, mod = mod},
        };

        if (mod.steamWorkshopId != 0)
            lines.Add(new ModLine() { message = "Open workshop page", action = MenuAction.OpenWorkshopPage, type = MenuLine.Type.Button, mod = mod});

        if (mod.local)
        {
            string message = "Upload to workshop";
            if (mod.steamWorkshopId != 0)
                message = "Upload update to workshop";
            lines.Add(new ModLine() { message = message, action = MenuAction.UploadModToWorkshop, type = MenuLine.Type.Button, mod = mod });
        }

        lines.Add(new ModLine() { message = "Close", action = MenuAction.CloseMenu, type = MenuLine.Type.Button, mod = mod});

        return new Menu(lines, true);
    }

    public static Menu modUploadProgressMenu = new Menu();

    public static void updateModUploadMenu(string status, bool done)
    {
        List<MenuLine> lines = new List<MenuLine>()
        {
            new MenuLine() { message = "Uploading mod...", type = MenuLine.Type.Label },
            new MenuLine() { message = "", type = MenuLine.Type.Label },
            new MenuLine() { message = status, type = MenuLine.Type.Label },
        };

        if (done)
            lines.Add(new MenuLine() { message = "Close", action = MenuAction.CloseMenu, type = MenuLine.Type.Button});

        modUploadProgressMenu.init(lines, done);
    }

    public static void activateMod(Mod mod = null)
    {
        if (mod != null)
            Logger.log("Activating mod \"" + mod.title + "\" from \""+ mod.path + "\"");
        else
            Logger.log("Deactivating mods");

        Levels.load(mod?.path);
        currentMod = mod;

        Game1.game.levelSelectScene = new LevelSelectScene();
    }

    public static List<Mod> getAllMods()
    {
        List<Mod> mods = new List<Mod>();

        foreach (Mod mod in getLocalMods())
            mods.Add(mod);

        foreach (Mod mod in getWorkshopMods())
            mods.Add(mod);

        return mods;
    }

    public static List<Mod> getLocalMods()
    {
        List<Mod> mods = new List<Mod>();

        foreach (string path in Directory.GetDirectories(Path.Join(Constants.rootPath, "mods")))
        {
            Mod mod = new Mod();
            mod.path = path;
            mod.local = true;

            try
            {
                string strData = File.ReadAllText(path + "/meta.json");
                JsonNode data = Util.parseJson(strData);

                mod.title = data["title"].GetValue<string>();

                if (data.AsObject().ContainsKey("steamWorkshopId"))
                    mod.steamWorkshopId = ulong.Parse(data["steamWorkshopId"].GetValue<string>());

                mods.Add(mod);
            }
            catch (Exception e)
            {
                Logger.log("Couldn't load local mod from " + path+ ":");
                Logger.log(e.ToString());
            }
        }

        return mods;
    }

    public static List<Mod> getWorkshopMods()
    {
        List<Mod> mods = new List<Mod>();

        if (!DrilbertSteam.usingSteam)
            return mods;

        foreach (PublishedFileId_t itemId in DrilbertSteam.getSubscribedWorkshopItemIds())
        {
            uint state = SteamUGC.GetItemState(itemId);

            // bool subscribed = (state & (uint)EItemState.k_EItemStateSubscribed) != 0;
            // bool legacyItem = (state & (uint)EItemState.k_EItemStateLegacyItem) != 0;
            bool installed = (state & (uint)EItemState.k_EItemStateInstalled) != 0;
            // bool needsUpdate = (state & (uint)EItemState.k_EItemStateNeedsUpdate) != 0;
            // bool downloading = (state & (uint)EItemState.k_EItemStateDownloading) != 0;
            // bool downloadPending = (state & (uint)EItemState.k_EItemStateDownloadPending) != 0;

            bool success = SteamUGC.GetItemInstallInfo(itemId, out ulong sizeOnDisk, out string path, 8192, out uint timeStamp);

            if (!installed)
                continue;

            Mod mod = new Mod();
            mod.path = path;
            mod.local = false;
            mod.steamWorkshopId = itemId.m_PublishedFileId;

            try
            {
                string strData = File.ReadAllText(path + "/meta.json");
                JsonNode data = Util.parseJson(strData);

                mod.title = "⚙ " + data["title"].GetValue<string>() + " ⚙";

                mods.Add(mod);
            }
            catch (Exception e)
            {
                Logger.log("Couldn't load workshop mod " + itemId.m_PublishedFileId + ":");
                Logger.log(e.ToString());
            }
        }

        mods.Sort(((a, b) => a.title.CompareTo(b.title)));

        return mods;
    }
}