using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Steamworks;

namespace Drilbert;


public static class DrilbertSteam
{
    public static bool usingSteam {get; private set;} = false;

    private static Callback<ItemInstalled_t> itemInstalledCallback = null;
    private static Callback<UserSubscribedItemsListChanged_t> subscriptionItemsListChangedCallback = null;

    public static void init()
    {
        string root = Path.GetDirectoryName(Assembly.GetEntryAssembly()!.Location);
        try { File.Delete(root + "/" + "steam_appid.txt"); } catch {}

#if DEBUG
        #if DEMO
            File.WriteAllText(root + "/" + "steam_appid.txt", "2338690");
        #else
            File.WriteAllText(root + "/" + "steam_appid.txt", "2338630");
        #endif
#endif
        usingSteam = SteamAPI.Init();

        Logger.log("Steam init: " + usingSteam);

        if (usingSteam)
        {
            SteamInput.Init(true);

#if !DEMO
            itemInstalledCallback = Callback<ItemInstalled_t>.Create((ItemInstalled_t result) =>
            {
                if (result.m_unAppID.m_AppId != 2338630)
                    return;

                Logger.log("Workshop item " + result.m_nPublishedFileId + " installed");
                Modding.updateModMenu();
            });

            subscriptionItemsListChangedCallback = Callback<UserSubscribedItemsListChanged_t>.Create((UserSubscribedItemsListChanged_t result) =>
            {
                if (result.m_nAppID.m_AppId != 2338630)
                    return;

                Logger.log("Workshop item subscription list changed");
                Modding.updateModMenu();
                startAllWorkshopDownloads();
            });

            userStatsReceivedCallback = Callback<UserStatsReceived_t>.Create((UserStatsReceived_t result) =>
            {
                Logger.log("User stats receieved, m_eResult: " + result.m_eResult);
                haveReceivedUserStats = result.m_eResult == EResult.k_EResultOK;
            });

            SteamUserStats.RequestCurrentStats();
            startAllWorkshopDownloads();
#endif
        }
    }

    public static void getSteamInput(Dictionary<string, GameInputSet> states)
    {
        InputHandle_t[] inputHandles = new InputHandle_t[Steamworks.Constants.STEAM_INPUT_MAX_COUNT];
        int controllerCount = SteamInput.GetConnectedControllers(inputHandles);
        SteamInput.RunFrame();

        InputActionSetHandle_t actionSet = SteamInput.GetActionSetHandle("InGameControls");
        InputDigitalActionHandle_t undoHandle = SteamInput.GetDigitalActionHandle("undo");
        InputDigitalActionHandle_t detonateHandle = SteamInput.GetDigitalActionHandle("detonate");
        InputDigitalActionHandle_t bombHandle = SteamInput.GetDigitalActionHandle("bomb");
        InputDigitalActionHandle_t megadrillHandle = SteamInput.GetDigitalActionHandle("megadrill");
        InputDigitalActionHandle_t resetHandle = SteamInput.GetDigitalActionHandle("reset");
        InputDigitalActionHandle_t menuHandle = SteamInput.GetDigitalActionHandle("menu");
        InputDigitalActionHandle_t upHandle = SteamInput.GetDigitalActionHandle("up");
        InputDigitalActionHandle_t downHandle = SteamInput.GetDigitalActionHandle("down");
        InputDigitalActionHandle_t leftHandle = SteamInput.GetDigitalActionHandle("left");
        InputDigitalActionHandle_t rightHandle = SteamInput.GetDigitalActionHandle("right");
        InputDigitalActionHandle_t menuActivateHandle = SteamInput.GetDigitalActionHandle("menu_activate");
        InputDigitalActionHandle_t menuBackHandle = SteamInput.GetDigitalActionHandle("menu_back");
        InputAnalogActionHandle_t moveHandle = SteamInput.GetAnalogActionHandle("move");

        for (int i = 0; i < controllerCount; i++)
        {
            InputHandle_t inputHandle = inputHandles[i];
            SteamInput.ActivateActionSet(inputHandle, actionSet);

            GameInputSet state = new GameInputSet();
            state.undo = SteamInput.GetDigitalActionData(inputHandle, undoHandle).bState != 0;
            state.detonate = SteamInput.GetDigitalActionData(inputHandle, detonateHandle).bState != 0;
            state.bomb = SteamInput.GetDigitalActionData(inputHandle, bombHandle).bState != 0;
            state.megadrill = SteamInput.GetDigitalActionData(inputHandle, megadrillHandle).bState != 0;
            state.reset = SteamInput.GetDigitalActionData(inputHandle, resetHandle).bState != 0;
            state.menu = SteamInput.GetDigitalActionData(inputHandle, menuHandle).bState != 0;
            state.up = SteamInput.GetDigitalActionData(inputHandle, upHandle).bState != 0;
            state.down = SteamInput.GetDigitalActionData(inputHandle, downHandle).bState != 0;
            state.left = SteamInput.GetDigitalActionData(inputHandle, leftHandle).bState != 0;
            state.right = SteamInput.GetDigitalActionData(inputHandle, rightHandle).bState != 0;
            state.menu_activate = SteamInput.GetDigitalActionData(inputHandle, menuActivateHandle).bState != 0;
            state.menu_back = SteamInput.GetDigitalActionData(inputHandle, menuBackHandle).bState != 0;

            InputAnalogActionData_t move = SteamInput.GetAnalogActionData(inputHandle, moveHandle);
            state.move.x = move.x;
            state.move.y = move.y;

            states["steam_" + inputHandle.m_InputHandle] = state;
        }
    }

    public static List<PublishedFileId_t> getSubscribedWorkshopItemIds()
    {
        if (!usingSteam)
            return new List<PublishedFileId_t>();

        uint numSubscribedItems = SteamUGC.GetNumSubscribedItems();
        if (numSubscribedItems > 0)
        {
            PublishedFileId_t[] items = new PublishedFileId_t[numSubscribedItems];
            uint itemIdsRetrieved = SteamUGC.GetSubscribedItems(items, (uint)items.Length);
            return items.Take((int)itemIdsRetrieved).ToList();
        }

        return new List<PublishedFileId_t>();
    }

    public static void startAllWorkshopDownloads()
    {
        if (!usingSteam)
            return;

        foreach (PublishedFileId_t itemId in getSubscribedWorkshopItemIds())
        {
            uint state = SteamUGC.GetItemState(itemId);
            bool installed = (state & (uint)EItemState.k_EItemStateInstalled) != 0;
            if (!installed)
            {
                Logger.log("Starting download of workshop item " + itemId);
                SteamUGC.DownloadItem(itemId, true);
            }
        }
    }

    private static bool haveReceivedUserStats = false;
    private static Callback<UserStatsReceived_t> userStatsReceivedCallback = null;

    public static void unlockAchievement(string id)
    {
#if !DEMO
        if (!usingSteam || !haveReceivedUserStats)
            return;

        SteamUserStats.GetAchievement(id, out bool alreadySet);

        if (!alreadySet)
        {
            Logger.log("achievement unlocked: " + id);
            SteamUserStats.SetAchievement(id);
            SteamUserStats.StoreStats();
        }
#endif
    }

    public static void runCallbacks()
    {
        if (usingSteam)
            SteamAPI.RunCallbacks();
    }

    // need to be kept here to stop callbacks being GCed
    private static CallResult<CreateItemResult_t> createItemCallResult = null;
    private static CallResult<SubmitItemUpdateResult_t> submitItemUpdateCallResult = null;

    public static void tryUploadWorkshopItemAsync(string path, Action<string, bool> completionCallback)
    {
        UGCUpdateHandle_t updateHandle = UGCUpdateHandle_t.Invalid;

        string strData = null;
        try
        {
            strData = File.ReadAllText(path + "/meta.json");
        }
        catch (Exception)
        {
            completionCallback("Error, can't read meta.json", false);
            return;
        }

        JsonNode metaData = null;
        try
        {
            metaData = Util.parseJson(strData);
        }
        catch (Exception)
        {
            completionCallback("Error, invalid meta.json", false);
            return;
        }

        PublishedFileId_t publishedFileId = new PublishedFileId_t(0);
        if (metaData.AsObject().ContainsKey("steamWorkshopId"))
            publishedFileId = new PublishedFileId_t(ulong.Parse(metaData["steamWorkshopId"].GetValue<string>()));

        if (!metaData.AsObject().ContainsKey("title"))
        {
            completionCallback("Error, missing title in meta.json", false);
            return;
        }

        string title = metaData["title"].GetValue<string>();

        // Prepare a clean folder for uploading
        string uploadFolder = path + "/upload_temp";
        {
            if (Directory.Exists(uploadFolder))
                Directory.Delete(uploadFolder, true);

            Directory.CreateDirectory(uploadFolder);

            Util.copyDirectory(path, uploadFolder, ignorePatterns: new List<Regex>()
            {
                new Regex(@"^upload_temp$"),
                new Regex(@"^save\.json$"),
                new Regex(@"^meta\.json$"),
            });

            JsonNode metaDataCopy = Util.parseJson(strData);
            if (metaDataCopy.AsObject().ContainsKey("steamWorkshopId"))
                metaDataCopy.AsObject().Remove("steamWorkshopId");

            File.WriteAllText(uploadFolder + "/meta.json", metaDataCopy.ToJsonString(new JsonSerializerOptions() { WriteIndented = true }));
        }

        void submitUpdate()
        {
            updateHandle = SteamUGC.StartItemUpdate(SteamUtils.GetAppID(), publishedFileId);

            SteamUGC.SetItemTitle(updateHandle, title);
            SteamUGC.SetItemContent(updateHandle, uploadFolder);
            SteamUGC.SetItemPreview(updateHandle, Path.Join(uploadFolder, "cover.png"));

            SteamAPICall_t submitItemUpdateCall = SteamUGC.SubmitItemUpdate(updateHandle, null);
            submitItemUpdateCallResult.Set(submitItemUpdateCall);
        }

        submitItemUpdateCallResult = CallResult<SubmitItemUpdateResult_t>.Create((SubmitItemUpdateResult_t result, bool failure) =>
        {
            if (result.m_eResult != EResult.k_EResultOK || failure)
            {
                completionCallback("Upload failed!", false);
                return;
            }

            try { Directory.Delete(uploadFolder, true); } catch (Exception) {}
            Util.openWebpage("https://steamcommunity.com/sharedfiles/filedetails/?id=" + publishedFileId);

            completionCallback("Upload succeeded!", false);
            Logger.log("Upload succeeded!");
        });

        createItemCallResult = CallResult<CreateItemResult_t>.Create((CreateItemResult_t result, bool failure) =>
        {
            if (result.m_eResult != EResult.k_EResultOK || failure)
            {
                completionCallback("Failed to create workshop item", false);
                return;
            }

            publishedFileId = result.m_nPublishedFileId;

            metaData["steamWorkshopId"] = result.m_nPublishedFileId.ToString();
            File.WriteAllText(path + "/meta.json", metaData.ToJsonString(new JsonSerializerOptions() {WriteIndented = true}));

            submitUpdate();
        });

        if (publishedFileId.m_PublishedFileId == 0)
        {
            SteamAPICall_t createItemCall = SteamUGC.CreateItem(SteamUtils.GetAppID(), EWorkshopFileType.k_EWorkshopFileTypeCommunity);
            createItemCallResult.Set(createItemCall);
        }
        else
        {
            submitUpdate();
        }
    }

    public static bool tryShowStorePage()
    {
        if (SteamUtils.IsOverlayEnabled())
        {
            SteamFriends.ActivateGameOverlayToStore(new AppId_t(2338630), EOverlayToStoreFlag.k_EOverlayToStoreFlag_None);
            return true;
        }

        return false;
    }

    public static bool tryShowUrlInOverlay(string url)
    {
        if (SteamUtils.IsOverlayEnabled())
        {
            SteamFriends.ActivateGameOverlayToWebPage(url);
            return true;
        }

        return false;
    }
}