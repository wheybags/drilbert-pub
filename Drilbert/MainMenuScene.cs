using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Drilbert
{
    public class MainMenuScene : Scene
    {
        Tilemap mainMenuLevel = new Tilemap(Constants.rootPath, "levels/main_menu.tmx");

        Thread primeCacheThread = null;
        long cancelPrimeCache = 0;

        RenderTarget2D menuRenderBuffer = null;
        RenderTarget2D mainRenderBuffer = null;

        long startTimeMs = -1;

        public MainMenuScene()
        {
            pushMenu(Menus.mainMenu());
            menuRenderBuffer = new RenderTarget2D(Game1.game.GraphicsDevice, Constants.tileSize * 32, Constants.tileSize * 32);
            mainRenderBuffer = new RenderTarget2D(Game1.game.GraphicsDevice, Constants.tileSize * mainMenuLevel.dimensions.x, Constants.tileSize * mainMenuLevel.dimensions.y);
        }

        public override void start()
        {
            cancelPrimeCache = 0;
            primeCacheThread = new Thread(() =>
            {
                bool old = GameLogic.printUpdateTiming;
                GameLogic.printUpdateTiming = false;

                for (int i = 0; i < mainMenuLoopMoves.Count; i++)
                {
                    GameLogic.evaluate(mainMenuLevel, new MySlice<GameAction>(mainMenuLoopMoves, 0, i+1));
                    if (Interlocked.Read(ref cancelPrimeCache) > 0)
                        break;
                }

                GameLogic.printUpdateTiming = old;
                // Console.WriteLine("PRIME DONE");
            });
            primeCacheThread.Start();
            startTimeMs = -1;
        }

        public override void stop()
        {
            Interlocked.Increment(ref cancelPrimeCache);
            primeCacheThread.Join();
        }

        public override void draw(MySpriteBatch spriteBatch, InputHandler inputHandler, long gameTimeMs)
        {
            if (startTimeMs == -1)
                startTimeMs = gameTimeMs;
            long loopTimeMs = gameTimeMs - startTimeMs;

            float tilesPerSecond = 1.0f;
            long msPerCycle = (long)(1000 / tilesPerSecond * mainMenuLevel.dimensions.x);
            long cyclesElapsed = loopTimeMs / msPerCycle;
            long positionInCycleMs = loopTimeMs - cyclesElapsed * msPerCycle;

            bool renderPlayer = true;

            MySlice<GameAction> previousMoves = new MySlice<GameAction>(mainMenuLoopMoves, 0, 0);
            MySlice<GameAction> currentMoves = new MySlice<GameAction>(mainMenuLoopMoves, 0, 0);
            {
                bool end = false;

                int i = 0;
                long acc = 0;
                while (true)
                {
                    long next = acc + msDelays[i];
                    if (next >= positionInCycleMs)
                        break;

                    acc = next;
                    i++;

                    if (i == msDelays.Length)
                    {
                        end = true;
                        break;
                    }
                }

                if (end)
                {
                    renderPlayer = false;
                    inputHandler.lastinputMs = 0;
                    previousMoves = new MySlice<GameAction>(mainMenuLoopMoves, 0, 0);
                    currentMoves = new MySlice<GameAction>(mainMenuLoopMoves, 0, 0);
                }
                else
                {
                    inputHandler.lastinputMs = cyclesElapsed * msPerCycle + acc;
                    previousMoves = new MySlice<GameAction>(mainMenuLoopMoves, 0, i == 0 ? 0 : i - 1);
                    currentMoves = new MySlice<GameAction>(mainMenuLoopMoves, 0, i);
                }
            }

            AnimationPoint animationPoint;
            {
                EvaluationResult previousResult = null;
                EvaluationResult currentResult = null;
                long animationPointTimeMs = loopTimeMs - inputHandler.lastinputMs;

                while (true)
                {
                    currentResult = GameLogic.evaluate(mainMenuLevel, currentMoves, true);
                    previousResult = GameLogic.evaluate(mainMenuLevel, previousMoves, true);

                    if (currentResult != null && previousResult != null)
                        break;

                    animationPointTimeMs = long.MaxValue;

                    if (currentMoves.length > 1)
                    {
                        currentMoves.length -= 1;
                        previousMoves.length -= 1;
                    }
                    else
                    {
                        Thread.Sleep(1);
                    }
                }

                List<AnimationStage> animationStages = LevelAnimation.calculateAnimationStages(previousResult, currentResult);
                animationPoint = LevelAnimation.calculateAnimationPoint(animationPointTimeMs, animationStages);
            }

            Util.ReleaseAssert(loopTimeMs >= inputHandler.lastinputMs);
            Util.ReleaseAssert(animationPoint.lerpAlpha > -0.99 && animationPoint.lerpAlpha < 1.01);

            GraphicsDevice.SetRenderTarget(mainRenderBuffer);
            LevelRender.render(spriteBatch,
                               loopTimeMs,
                               animationPoint.stage.startTilemap,
                               animationPoint.stage.endTilemap,
                               animationPoint.lerpAlpha,
                               animationPoint.stage.shake,
                               renderPlayer,
                               LevelRender.EdgeEffect.None);

            GraphicsDevice.SetRenderTarget(menuRenderBuffer);
            Rect menuArea = Render.renderMenu(spriteBatch, loopTimeMs, getCurrentMenu());

            GraphicsDevice.SetRenderTarget(null);
            GraphicsDevice.Clear(new Color(0, 0, 0, 255));
            GraphicsDevice.Clear(Constants.outsideLevelBackgroundColor);
            spriteBatch.Begin(SpriteSortMode.Deferred, Render.alphaBlend, SamplerState.PointWrap);

            float renderScale = ((float) Window.ClientBounds.Width) / (24 * Constants.tileSize);

            {
                float gameBrightness = 0.5f;

                Vec2f targetSize = mainRenderBuffer.Bounds.f().size * renderScale;
                Vec2f offset = new Vec2f(0, Window.ClientBounds.Height / 2 - targetSize.y / 2);

                float positionInCycleNormalised = (float)positionInCycleMs / (float)msPerCycle;
                float uvOffsetX = mainRenderBuffer.Width * positionInCycleNormalised + Constants.tileSize * 3;

                spriteBatch.r(mainRenderBuffer).size(targetSize).pos(offset).offsetUv(new Vec2f(uvOffsetX, 0)).color(Color.White*gameBrightness).draw();
            }

            {
                float menuRenderScale = MathF.Max(1.0f, MathF.Floor(renderScale));
                Vec2f targetSize = menuArea.size * menuRenderScale;
                Vec2f pos = (Window.ClientBounds.f().size / 2 - targetSize / 2).rounded();
                spriteBatch.r(menuRenderBuffer).size(targetSize).pos(pos).uv(menuArea).draw();
            }

            spriteBatch.End();
        }

        List<GameAction> mainMenuLoopMoves = GameLogic.gameActionsFromString("RRRRRRRRRRURURRRDDRDRDRRRRRRRRDRRLRLLUUUUUUURRRRRRRRRRRRDDRRRBLDTURRRRRRRRRRURRRURUUUUUURRRDDDDDDDRUUURRDRUUUURRRDDDDDRRRRRRRRR");

        int[] msDelays = {
            983,
            133,
            134,
            133,
            133,
            134,
            133,
            133,
            134,
            133,
            417,
            233,
            300,
            300,
            367,
            316,
            350,
            417,
            500,
            400,
            333,
            367,
            450,
            133,
            134,
            133,
            133,
            134,
            1316,
            1584,
            316,
            334,
            450,
            350,
            866,
            284,
            900,
            283,
            133,
            134,
            133,
            133,
            134,
            133,
            233,
            134,
            133,
            133,
            134,
            133,
            133,
            134,
            133,
            133,
            134,
            133,
            367,
            366,
            650,
            417,
            433,
            350,
            267,
            350,
            367,
            1333,
            383,
            134,
            583,
            133,
            134,
            750,
            133,
            133,
            134,
            133,
            533,
            1384,
            133,
            233,
            434,
            266,
            300,
            134,
            133,
            133,
            134,
            666,
            267,
            383,
            367,
            383,
            234,
            183,
            167,
            200,
            183,
            233,
            334,
            333,
            417,
            350,
            316,
            434,
            316,
            334,
            783,
            267,
            283,
            417,
            266,
            267,
            183,
            450,
            317,
            200,
            167,
            216,
            367,
            133,
            134,
            133,
            133,
            134,
            133,
            733,
            134 + 1000,
        };
    }
}