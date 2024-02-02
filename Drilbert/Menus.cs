using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace Drilbert
{
    public enum MenuAction
    {
        Nothing,
        Continue,
        Restart,
        ExitLevel,
        OpenSelectSaveSlotMenu,
        OpenDeleteSaveSlotMenu,
        DeleteSaveSlot,
        StartGame,
        OpenModPlayOrUpload,
        OpenSelectSaveSlotMenuForMod,
        OpenModFolder,
        UploadModToWorkshop,
        OpenWorkshopPage,
        Quit,
        MainMenu,
        CloseMenu,
        OpenSettings,
        OpenModding,
        OpenSourceCode,
        OpenTiled,
        OpenModdingGuide,
        BrowseWorkshop,
        OpenModsListMenu,
        OpenControlsMenu,
        OpenSoundSettings,
        ToggleFullscreen,
        Credits,
        CreateMod,
        OpenDiscord,
#if DEMO
        ReallyQuit,
        OpenStorePage,
#endif
    }

    public class MenuLine
    {
        public enum Type
        {
            Label,
            Button,
            Slider,
        }

        public string message;
        public List<Animation> animation = null;
        public MenuAction action = MenuAction.Nothing;
        public Type type;
        public bool forceTextFixedWidth = false;
        public int forceLineHeight = 0;
        public Color textTint = Color.White;

        public Func<float> getSliderVal;
        public Action<float> setSliderVal;
        public int sliderStepCount = 20;

        public bool isSelectable()
        {
            return type == Type.Button || type == Type.Slider;
        }
    }

    public class Menu
    {
        public List<MenuLine> lines;
        public int selectedIndex = -1;
        public long inputBlockTime = 0;
        public long lastChangeMs = 0;
        public bool closable;
        public bool isAudioSettings = false;

        public int scrollAreaStart = -1;
        public int scrollAreaEnd = -1;
        public int scrollShowMax = -1;
        public int scrollOffset = 0;

        public Action onClose;

        public Menu(List<MenuLine> lines, bool closable)
        {
            init(lines, closable);
        }

        public Menu() {}

        public void init(List<MenuLine> lines, bool closable)
        {
            this.closable = closable;
            this.lines = lines;
            selectedIndex = -1;
            for (int i = 0; i < this.lines.Count; i++)
            {
                if (this.lines[i].isSelectable())
                {
                    selectedIndex = i;
                    break;
                }
            }

            if (scrollAreaStart != -1)
            {
                scrollOffset = selectedIndex - (scrollShowMax / 2);
                scrollOffset = Math.Max(0, scrollOffset);
            }
        }

        public MenuLine getSelectedLine()
        {
            return selectedIndex == -1 ? null : lines[selectedIndex];
        }

        public void moveSelectedIndex(long gameTimeMs, int direction)
        {
            Util.ReleaseAssert(selectedIndex >= 0);

            if (inputBlockTime + (long)(1000 * 0.3) > gameTimeMs)
                return;

            while (true)
            {
                selectedIndex = selectedIndex + direction;

                if (selectedIndex == lines.Count)
                    selectedIndex = 0;
                if (selectedIndex == -1)
                    selectedIndex = lines.Count - 1;

                if (lines[selectedIndex].isSelectable())
                    break;
            }

            resolveScroll();
            lastChangeMs = gameTimeMs;

            Sounds.menuSelect.Play();
        }

        public void resolveScroll()
        {
            if (scrollAreaStart != -1)
            {
                if (selectedIndex - scrollOffset >= scrollShowMax)
                    scrollOffset = selectedIndex - scrollShowMax + 1;
                if (selectedIndex < scrollOffset)
                    scrollOffset = selectedIndex;

                if (scrollOffset > scrollAreaEnd - scrollShowMax)
                    scrollOffset = scrollAreaEnd - scrollShowMax;
                if (scrollOffset < scrollAreaStart)
                    scrollOffset = scrollAreaStart;
            }
        }
    }

    public static class Menus
    {
        public static Menu levelCompleteMenu() => new Menu(new List<MenuLine>()
        {
            new MenuLine() { message = "Level complete!", type = MenuLine.Type.Label },
            new MenuLine() { message = "", type = MenuLine.Type.Label },
            new MenuLine() { message = "Continue", action = MenuAction.Continue, type = MenuLine.Type.Button },
            new MenuLine() { message = "Restart", action = MenuAction.Restart, type = MenuLine.Type.Button },
        }, false);

        public static Menu mainMenu() => new Menu(new List<MenuLine>()
        {
            new MenuLine() { animation = new List<Animation>(){Textures.logo}, type = MenuLine.Type.Label },
            new MenuLine() { message = "", type = MenuLine.Type.Label },
            new MenuLine() { message = "Start game", action = MenuAction.OpenSelectSaveSlotMenu, type = MenuLine.Type.Button },
            new MenuLine() { message = "Settings", action = MenuAction.OpenSettings, type = MenuLine.Type.Button },
#if DEMO
            new MenuLine() { message = "Modding", textTint = new Color(1.0f, 1.0f, 1.0f, 0.5f), type = MenuLine.Type.Label },
#else
            new MenuLine() { message = "Modding", action = MenuAction.OpenModding, type = MenuLine.Type.Button },
#endif
            new MenuLine() { message = "Discord", action = MenuAction.OpenDiscord, type = MenuLine.Type.Button },
            new MenuLine() { message = "Credits", action = MenuAction.Credits, type = MenuLine.Type.Button },
            new MenuLine() { message = "Exit game", action = MenuAction.Quit, type = MenuLine.Type.Button },
        }, false);

        public static Menu moddingMenu() => new Menu(new List<MenuLine>()
        {
            new MenuLine() { message = "Modding", type = MenuLine.Type.Label },
            new MenuLine() { message = "", type = MenuLine.Type.Label },
            new MenuLine() { message = "Mods", action = MenuAction.OpenModsListMenu, type = MenuLine.Type.Button},
            new MenuLine() { message = "Browse workshop mods", action = MenuAction.BrowseWorkshop, type = MenuLine.Type.Button},
            new MenuLine() { message = "Mod Creation guide", action = MenuAction.OpenModdingGuide, type = MenuLine.Type.Button},
            new MenuLine() { message = "Level editor", action = MenuAction.OpenTiled, type = MenuLine.Type.Button },
            new MenuLine() { message = "Source code", action = MenuAction.OpenSourceCode, type = MenuLine.Type.Button },
            new MenuLine() { message = "Close", action = MenuAction.CloseMenu, type = MenuLine.Type.Button },
        }, true);

        public static Menu levelSelectPauseMenu() => new Menu(new List<MenuLine>()
        {
            new MenuLine() { animation = new List<Animation>(){Textures.logo}, type = MenuLine.Type.Label },
            new MenuLine() { message = "", type = MenuLine.Type.Label },
            new MenuLine() { message = "Continue", action = MenuAction.CloseMenu, type = MenuLine.Type.Button },
            new MenuLine() { message = "Settings", action = MenuAction.OpenSettings, type = MenuLine.Type.Button },
            new MenuLine() { message = "Controls", action = MenuAction.OpenControlsMenu, type = MenuLine.Type.Button },
            new MenuLine() { message = Modding.currentMod == null ? "Main menu" : "Exit mod", action = MenuAction.MainMenu, type = MenuLine.Type.Button },
            new MenuLine() { message = "Exit game", action = MenuAction.Quit, type = MenuLine.Type.Button },
        }, true);

        public static Menu pauseMenu() => new Menu(new List<MenuLine>()
        {
            new MenuLine() { animation = new List<Animation>(){Textures.logo}, type = MenuLine.Type.Label },
            new MenuLine() { message = "", type = MenuLine.Type.Label },
            new MenuLine() { message = "Resume", action = MenuAction.CloseMenu, type = MenuLine.Type.Button },
            new MenuLine() { message = "Settings", action = MenuAction.OpenSettings, type = MenuLine.Type.Button },
            new MenuLine() { message = "Controls", action = MenuAction.OpenControlsMenu, type = MenuLine.Type.Button },
            new MenuLine() { message = "Restart level", action = MenuAction.Restart, type = MenuLine.Type.Button },
            new MenuLine() { message = "Exit Level", action = MenuAction.ExitLevel, type = MenuLine.Type.Button },
            new MenuLine() { message = "Exit game", action = MenuAction.Quit, type = MenuLine.Type.Button },
        }, true);

        public static Menu settingsMenu() => new  Menu(new List<MenuLine>()
        {
            new MenuLine() { message = "Settings", type = MenuLine.Type.Label },
            new MenuLine() { message = "", type = MenuLine.Type.Label },
            new MenuLine() { message = "Fullscreen " + (GameSettings.fullscreen ? "ON" : "OFF"), action = MenuAction.ToggleFullscreen, type = MenuLine.Type.Button },
            new MenuLine() { message = "Sound Settings", action = MenuAction.OpenSoundSettings, type = MenuLine.Type.Button },
            new MenuLine() { message = "", type = MenuLine.Type.Label },
            new MenuLine() { message = "Close", action = MenuAction.CloseMenu, type = MenuLine.Type.Button },
        }, true);

        public static Menu controlsMenu()
        {
            List<Animation> controlsAnimations = new List<Animation>() { Textures.controls };

            if (Game1.game.levelSelectScene.hasSeenBomb())
                controlsAnimations.Add(new Animation(Textures.controlsBombOverlay));

            if (Game1.game.levelSelectScene.hasSeenMegadrill())
                controlsAnimations.Add(new Animation(Textures.controlsDrillOverlay));

            return new Menu(new List<MenuLine>()
            {
                new MenuLine() { animation = controlsAnimations, type = MenuLine.Type.Label },
                new MenuLine() { message = "Close", action = MenuAction.CloseMenu, type = MenuLine.Type.Button },
            }, true);
        }

        private static float maxDb = 0;
        private static float minDb = -50;

        public static Menu soundOptionsMenu() => new Menu(new List<MenuLine>()
        {
            new MenuLine() { message = "Sound Settings", type = MenuLine.Type.Label },
            new MenuLine() { message = "", type = MenuLine.Type.Label },
            new MenuLine() {message = "Master ", type = MenuLine.Type.Slider, forceTextFixedWidth = true, getSliderVal = () =>
            {
                if (GameSettings.masterVolume == 0)
                    return 0;
                float db = Sounds.linearToDecibel(GameSettings.masterVolume);
                return (db - minDb) / (maxDb - minDb);
            }, setSliderVal = (f =>
            {
                float db = f * (maxDb - minDb) + minDb;
                float linear = f == 0 ? 0 : Sounds.decibelToLinear(db);
                Logger.log("Setting master volume to " + db + " db / " + linear + " linear");
                GameSettings.masterVolume = linear;
            })},
            new MenuLine() {message = "Music  ", type = MenuLine.Type.Slider, forceTextFixedWidth = true, getSliderVal = () =>
            {
                if (GameSettings.musicVolume == 0)
                    return 0;
                float db = Sounds.linearToDecibel(GameSettings.musicVolume);
                return (db - minDb) / (maxDb - minDb);
            }, setSliderVal = (f =>
            {
                float db = f * (maxDb - minDb) + minDb;
                float linear = f == 0 ? 0 : Sounds.decibelToLinear(db);
                Logger.log("Setting music volume to " + db + " db / " + linear + " linear");
                GameSettings.musicVolume = linear;
            })},
            new MenuLine() { message = "", type = MenuLine.Type.Label },
            new MenuLine() { message = "Close", action = MenuAction.CloseMenu, type = MenuLine.Type.Button },
        }, true) { isAudioSettings = true};

        public static Menu creditsMenu() => new Menu(new List<MenuLine>()
        {
            new MenuLine() {animation = new List<Animation>(){Textures.logo}, type = MenuLine.Type.Label },
            new MenuLine() {message = "", type = MenuLine.Type.Label},
            new MenuLine() {message = "A game by Tom Mason", type = MenuLine.Type.Label},
            new MenuLine() {message = "Music by Nicole Marie T", type = MenuLine.Type.Label},
            new MenuLine() {message = "", forceLineHeight = 4, type = MenuLine.Type.Label},
            new MenuLine() {message = "--- Special Thanks ---", type = MenuLine.Type.Label},
            new MenuLine() {message = "", forceLineHeight = 4, type = MenuLine.Type.Label},
            new MenuLine() {message = "Ben Buckton", type = MenuLine.Type.Label},
            new MenuLine() {message = "", forceLineHeight = 2, type = MenuLine.Type.Label},
            new MenuLine() {message = "Frankfurt indies group", type = MenuLine.Type.Label},
            new MenuLine() {message = "", forceLineHeight = 2, type = MenuLine.Type.Label},
            new MenuLine() {message = "My friends and colleagues at powder", type = MenuLine.Type.Label},
            new MenuLine() {message = "", forceLineHeight = 2, type = MenuLine.Type.Label},
            new MenuLine() {message = "Tiled editor", type = MenuLine.Type.Label},
            new MenuLine() {message = "", forceLineHeight = 2, type = MenuLine.Type.Label},
            new MenuLine() {message = "Aseprite", type = MenuLine.Type.Label},
            new MenuLine() {message = "", type = MenuLine.Type.Label},
            new MenuLine() {message = "Close", action = MenuAction.CloseMenu, type = MenuLine.Type.Button },
        }, true);

#if DEMO
        public static Menu callToActionOnQuitMenu() => new Menu(new List<MenuLine>()
        {
            new MenuLine() { message = "Before you go, would you like", type = MenuLine.Type.Label},
            new MenuLine() { message = "to wishlist on steam?", type = MenuLine.Type.Label},
            new MenuLine() { message = "", type = MenuLine.Type.Label},
            new MenuLine() { message = "Yeah!", type = MenuLine.Type.Button, action = MenuAction.OpenStorePage },
            new MenuLine() { message = "Quit", type = MenuLine.Type.Button, action = MenuAction.ReallyQuit },
        }, false);
#endif
    }
}