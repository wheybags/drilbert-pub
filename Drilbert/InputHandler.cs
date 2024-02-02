using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Xna.Framework.Input;

namespace Drilbert
{
    public class InputHandler
    {
        public InputState input = new InputState();

        public long reloadTexturesMs = -9999;
        public long lastinputMs = 0;

        private bool toggledFullscreen = false;

        public void update(long gameTimeMs)
        {
            input.update(gameTimeMs);

            if (input.currentState.getInput(Input.ToggleFullscreen))
            {
                if (!toggledFullscreen)
                {
                    Game1.game.toggleFullscreen();
                    toggledFullscreen = true;
                }
            }
            else
            {
                toggledFullscreen = false;
            }
        }

        public void processDebugInput(GameState gameState, long gameTimeMs)
        {
            if (input.downThisFrame(Keys.D0) && gameState.originalLevel != null)
            {
                string path;
                {
                    string basePath = Constants.rootPath + "/" + gameState.originalLevel.path;
                    basePath = basePath.Substring(0, basePath.Length - 4);
                    int num = 0;
                    do
                    {
                        if (num == 0)
                            path = basePath + "Output.tmx";
                        else
                            path = basePath + "Output" + num + ".tmx";
                        num++;
                    } while (File.Exists(path));
                }

                EvaluationResult currentResult = GameLogic.evaluate(gameState);

                string moves = GameLogic.gameActionsToString(new MySlice<GameAction>(gameState.moves));
                File.WriteAllText(path, currentResult.tilemaps.Last().save(new Dictionary<string, object>()
                {
                    {"moves", moves}
                }));
            }

            if (input.downThisFrame(Keys.D9))
            {
                reloadTexturesMs = gameTimeMs;
                Textures.loadTextures();
                Sounds.loadSoundEffects();
            }

            if (input.downThisFrame(Keys.D8) && Game1.game.getScene() == Game1.game.inGameScene)
            {
                Game1.game.inGameScene.gameState.originalLevel.reload();
                GameLogic.evaluationCache.Clear();

                MySlice<GameAction> oldActions = GameLogic.trimGameActionsToLastRunAfterReset(new MySlice<GameAction>(gameState.moves));
                gameState.moves = new List<GameAction>();
                foreach (GameAction move in oldActions)
                {
                    gameState.moves.Add(move);
                    if (GameLogic.evaluate(gameState) == null)
                        gameState.moves.RemoveAt(gameState.moves.Count - 1);
                }
            }

#if DEBUG
            if (input.downThisFrame(Keys.D1) && Game1.game.getScene() == Game1.game.levelSelectScene)
            {
                Game1.game.levelSelectScene.resetProgress();
                Game1.game.levelSelectScene.unlockSelectedLevelRecursive();
            }
            if (input.downThisFrame(Keys.D2) && Game1.game.getScene() == Game1.game.levelSelectScene)
            {
                Game1.game.levelSelectScene.unlockSelectedLevelRecursive();
            }
#endif
        }

        public void processMenuInput(Scene scene, long gameTimeMs)
        {
            Menu currentMenu = scene.getCurrentMenu();
            Util.DebugAssert(currentMenu != null);

            if (input.downThisFrame(Input.MenuActivate))
                scene.menuActivate(this);

            if (input.activeWithRepeat(Input.Up, Constants.menuKeyRepeatMs))
                currentMenu.moveSelectedIndex(gameTimeMs, -1);
            if (input.activeWithRepeat(Input.Down, Constants.menuKeyRepeatMs))
                currentMenu.moveSelectedIndex(gameTimeMs, 1);

            if (currentMenu.getSelectedLine() != null && currentMenu.getSelectedLine().type == MenuLine.Type.Slider)
            {
                bool left = input.activeWithRepeat(Input.Left, Constants.menuKeyRepeatMs);
                bool right = input.activeWithRepeat(Input.Right, Constants.menuKeyRepeatMs);
                if (left || right)
                {
                    MenuLine line = currentMenu.getSelectedLine();
                    float step = 1.0f / ((float)line.sliderStepCount);
                    if (left)
                        line.setSliderVal(Math.Max(line.getSliderVal() - step, 0.0f));
                    if (right)
                        line.setSliderVal(Math.Min(line.getSliderVal() + step, 1.0f));
                }
            }

            if (!scene.suppressMenuCloseThisTick && input.downThisFrame(Input.MenuBack) && scene.getCurrentMenu().closable)
            {
                scene.popCurrentMenu();
                Sounds.menuClose.Play();
            }

            scene.suppressMenuCloseThisTick = false;
        }
    }
}