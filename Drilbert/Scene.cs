using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Drilbert
{
    public abstract class Scene
    {
        public List<Direction> blockedDirections = new List<Direction>();
        private List<Menu> menuStack = new List<Menu>();
        public bool suppressMenuCloseThisTick = false;

        public float rumbleLeft { get; protected set; } = 0;
        public float rumbleRight { get; protected set; } = 0;

        protected static GraphicsDevice GraphicsDevice => Game1.game.GraphicsDevice;
        protected static GameWindow Window => Game1.game.Window;

        public Menu getCurrentMenu()
        {
            return menuStack.Count == 0 ? null : menuStack[menuStack.Count - 1];
        }

        public void pushMenu(Menu menu)
        {
            menuStack.Add(menu);
            suppressMenuCloseThisTick = true;
        }

        public void popCurrentMenu()
        {
            if (menuStack.Count > 0)
            {
                Menu menu = menuStack.Last();
                menuStack.RemoveAt(menuStack.Count - 1);
                menu.onClose?.Invoke();
            }
        }

        public virtual void start() {}
        public virtual void stop() {}
        public virtual void update(long gameTimeMs, InputHandler inputHandler) {}
        public virtual void processInput(long gameTimeMs, InputHandler inputHandler) {}
        public virtual void menuActivate(InputHandler inputHandler)
        {
            MenuLine selectedLine = getCurrentMenu().getSelectedLine();
            if (selectedLine == null)
                return;

            switch (selectedLine.action)
            {
                case MenuAction.Nothing:
                    break;
                case MenuAction.CloseMenu:
                {
                    popCurrentMenu();
                    Sounds.menuActivate.Play();
                    break;
                }
#if DEMO
                case MenuAction.Quit:
                {
                    pushMenu(Menus.callToActionOnQuitMenu());
                    Sounds.menuActivate.Play();
                    break;
                }
                case MenuAction.OpenStorePage:
                {
                    Util.openStorePage();
                    Sounds.menuActivate.Play();
                    break;
                }
                case MenuAction.ReallyQuit:
#else
                case MenuAction.Quit:
#endif
                {
                    Sounds.menuActivate.Play();
                    Game1.game.Exit();
                    break;
                }
                case MenuAction.MainMenu:
                {
                    popCurrentMenu();
                    Game1.game.setScene(Game1.game.mainMenuScene);
                    Sounds.menuActivate.Play();
                    break;
                }
                case MenuAction.OpenSettings:
                {
                    pushMenu(Menus.settingsMenu());
                    Sounds.menuActivate.Play();
                    break;
                }
                case MenuAction.OpenModding:
                {
                    Modding.ensureModsFolderExists();
                    pushMenu(Menus.moddingMenu());
                    Sounds.menuActivate.Play();
                    break;
                }
                case MenuAction.OpenSourceCode:
                {
                    string sourcePath = Path.Join(Constants.rootPath, "src");
                    Util.openFolder(sourcePath);
                    Sounds.menuActivate.Play();
                    break;
                }
                case MenuAction.OpenTiled:
                {
                    string projectPath = Path.Join(Constants.rootPath, "Drilbert.tiled-project");

                    string sessionPath = Path.Join(Constants.rootPath, "Drilbert.tiled-session");
                    if (!File.Exists(sessionPath))
                        File.Copy(sessionPath + ".template", sessionPath);

                    if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                    {
                        string batPath = Constants.rootPath + "\\Tiled\\run_tiled.bat";
                        string tiledExePath = Constants.rootPath + "\\Tiled\\win\\tiled.exe";

                        // Tiled doesn't like something about the steam environment (exits with code 1) - so we bounce it through explorer
                        // Bat file is necessary because you can't pass params through explorer
                        File.WriteAllText(batPath, "\"" + tiledExePath + "\" \"" + projectPath + "\"");
                        Process.Start("explorer.exe", batPath);
                    }
                    else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                    {
                        // What is this stupid shit?
                        // Well, I make builds on windows, and windows kinda doesn't have symlinks the way unices do.
                        // Unfortunately, some frameworks inside Tiled.app require symlinks, and so here we are.
                        if (!Directory.Exists(Constants.rootPath + "/Tiled/osx/Tiled.app"))
                        {
                            var psi = new ProcessStartInfo("unzip", "Tiled.app.zip");
                            psi.WorkingDirectory = Constants.rootPath + "/Tiled/osx";
                            psi.UseShellExecute = false;
                            psi.CreateNoWindow = true;
                            Process.Start(psi).WaitForExit();
                        }

                        Process.Start("/usr/bin/open", new string[] { "-a", Constants.rootPath + "/Tiled/osx/Tiled.app", "--args", projectPath });
                    }
                    else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                    {
                        string tiledPath = Constants.rootPath + "/Tiled/linux/Tiled-1.10.2_Linux_Qt-6_x86_64.AppImage";

                        // try
                        // {
                        //     using (Process proc = Process.Start("chmod", "+x \"" + tiledPath + "\""))
                        //         proc.WaitForExit();
                        //
                        //     Process tiledProcess = Process.Start(tiledPath, projectPath);
                        //     if (tiledProcess.WaitForExit(2000))
                        //         throw new Exception();
                        // }
                        // catch (Exception)
                        {
                            string message = "Problems with the linux steam runtime make it impossible to start the level editor (Tiled) from inside drilbert.";
                            message += "\nYou can run the editor manually, it is located at " + tiledPath;
                            message += "\nOpen the example session at " + projectPath;
                            NativeFuncs.SDL_ShowSimpleMessageBox(NativeFuncs.SDL_MESSAGEBOX_INFORMATION, "Running Tiled editor failed", message, IntPtr.Zero);
                        }
                    }
                    else
                    {
                        Util.ReleaseAssert(false);
                    }
                    Sounds.menuActivate.Play();
                    break;
                }
                case MenuAction.OpenModdingGuide:
                {
                    Util.openWebpage("https://steamcommunity.com/sharedfiles/filedetails/?id=3089186208");
                    Sounds.menuActivate.Play();
                    break;
                }
                case MenuAction.BrowseWorkshop:
                {
                    Util.openWebpage("https://steamcommunity.com/app/2338630/workshop/");
                    Sounds.menuActivate.Play();
                    break;
                }
                case MenuAction.OpenModsListMenu:
                {
                    DrilbertSteam.startAllWorkshopDownloads();
                    Modding.updateModMenu();
                    pushMenu(Modding.modMenu);
                    Sounds.menuActivate.Play();
                    break;
                }
                case MenuAction.CreateMod:
                {
                    Modding.createExampleMod();
                    Modding.updateModMenu();
                    Sounds.menuActivate.Play();
                    break;
                }
                case MenuAction.OpenSelectSaveSlotMenu:
                {
                    Modding.activateMod(null);
                    pushMenu(Game1.game.levelSelectScene.createSaveSlotMenu());
                    Sounds.menuActivate.Play();
                    break;
                }
                case MenuAction.OpenDeleteSaveSlotMenu:
                {
                    pushMenu(Game1.game.levelSelectScene.createDeleteSaveSlotMenu());
                    Sounds.menuActivate.Play();
                    break;
                }
                case MenuAction.DeleteSaveSlot:
                {
                    LevelSelectScene.DeleteSaveSlotMenuLine deleteLine = (LevelSelectScene.DeleteSaveSlotMenuLine)getCurrentMenu().getSelectedLine();
                    Game1.game.levelSelectScene.deleteSaveSlot(deleteLine.index);

                    popCurrentMenu();
                    popCurrentMenu();
                    pushMenu(Game1.game.levelSelectScene.createSaveSlotMenu());
                    pushMenu(Game1.game.levelSelectScene.createDeleteSaveSlotMenu());

                    Sounds.menuActivate.Play();
                    break;
                }
                case MenuAction.OpenSelectSaveSlotMenuForMod:
                {
                    ModLine line = (ModLine)getCurrentMenu().getSelectedLine();
                    try
                    {
                        Modding.activateMod(line.mod);
                    }
                    catch (Exception e)
                    {
                        Modding.activateMod(null);
                        Logger.log("Error loading mod");
                        Logger.log(e.ToString());
                        NativeFuncs.SDL_ShowSimpleMessageBox(0x00000010 /* SDL_MESSAGEBOX_ERROR */, "Error loading mod", e.ToString(), IntPtr.Zero);
                        break;
                    }

                    pushMenu(Game1.game.levelSelectScene.createSaveSlotMenu());
                    Sounds.menuActivate.Play();
                    break;
                }
                case MenuAction.OpenModPlayOrUpload:
                {
                    ModLine line = (ModLine)getCurrentMenu().getSelectedLine();
                    pushMenu(Modding.getModPlayOrUploadMenu(line.mod));
                    Sounds.menuActivate.Play();
                    break;
                }
                case MenuAction.OpenModFolder:
                {
                    ModLine line = (ModLine)getCurrentMenu().getSelectedLine();
                    Util.openFolder(line.mod.path);
                    Sounds.menuActivate.Play();
                    break;
                }
                case MenuAction.UploadModToWorkshop:
                {
                    ModLine line = (ModLine)getCurrentMenu().getSelectedLine();

                    Modding.updateModUploadMenu("Uploading...", false);
                    DrilbertSteam.tryUploadWorkshopItemAsync(line.mod.path, (string status, bool error) =>
                    {
                        Modding.updateModUploadMenu(status, true);
                    });

                    pushMenu(Modding.modUploadProgressMenu);

                    Sounds.menuActivate.Play();
                    break;
                }
                case MenuAction.OpenWorkshopPage:
                {
                    ModLine line = (ModLine)getCurrentMenu().getSelectedLine();
                    Util.openWebpage("https://steamcommunity.com/sharedfiles/filedetails/?id=" + line.mod.steamWorkshopId);
                    Sounds.menuActivate.Play();
                    break;
                }
                case MenuAction.OpenControlsMenu:
                {
                    pushMenu(Menus.controlsMenu());
                    Sounds.menuActivate.Play();
                    break;
                }
                case MenuAction.OpenSoundSettings:
                {
                    pushMenu(Menus.soundOptionsMenu());
                    Sounds.menuActivate.Play();
                    break;
                }
                case MenuAction.Credits:
                {
                    pushMenu(Menus.creditsMenu());
                    Sounds.menuActivate.Play();
                    break;
                }
                case MenuAction.StartGame:
                {
                    Game1.game.levelSelectScene.saveSlotIndex = ((LevelSelectScene.SaveSlotMenuLine)getCurrentMenu().getSelectedLine()).index;
                    Game1.game.levelSelectScene.tryLoadSavedGame();

                    popCurrentMenu();

                    if (Modding.currentMod == null && !Game1.game.levelSelectScene.hasCompletedFirstLevel())
                        Game1.game.setScene(new IntroScene());
                    else
                        Game1.game.setScene(Game1.game.levelSelectScene);

                    Sounds.menuActivate.Play();
                    break;
                }
                case MenuAction.ToggleFullscreen:
                {
                    Game1.game.toggleFullscreen();
                    popCurrentMenu();
                    pushMenu(Menus.settingsMenu());
                    Sounds.menuActivate.Play();
                    break;
                }
                case MenuAction.OpenDiscord:
                {
                    Util.openWebpage("https://discord.com/invite/RWtg6RCqqG");
                    Sounds.menuActivate.Play();
                    break;
                }
                default:
                    throw new Exception("unhandled");
            }
        }
        public abstract void draw(MySpriteBatch spriteBatch, InputHandler inputHandler, long gameTimeMs);
    }
}