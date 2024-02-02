using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Drilbert
{
    public unsafe class InGameScene: Scene
    {
        public GameState gameState = new GameState();
        private float lastLerpAlpha = 0;
        private bool wasDead = false;

        RenderTarget2D mainRenderBuffer = null;
        RenderTarget2D topHudRenderBuffer = null;
        RenderTarget2D menuRenderBuffer = null;

        private Direction lastDirection = Direction.None;

        public InGameScene()
        {
            topHudRenderBuffer = new RenderTarget2D(Game1.game.GraphicsDevice, Constants.tileSize * 32, Constants.tileSize * 32);
            menuRenderBuffer = new RenderTarget2D(Game1.game.GraphicsDevice, Constants.tileSize * 32, Constants.tileSize * 32);
        }

        public override void update(long gameTimeMs, InputHandler inputHandler)
        {
            Tilemap tilemap = GameLogic.evaluate(gameState).tilemaps.Last();
            if (getCurrentMenu() == null && tilemap.win)
            {
                Logger.log("Solution: " + GameLogic.gameActionsToString(GameLogic.trimGameActionsToLastRunAfterReset(new MySlice<GameAction>(gameState.moves))));

                Menu winMenu = Menus.levelCompleteMenu();
                winMenu.onClose = MusicManager.stopVictorySequence;
                winMenu.inputBlockTime = gameTimeMs + (long)(1000 * 0.5);
                pushMenu(winMenu);
                MusicManager.playVictorySequence();

                if (Modding.currentMod == null)
                {
                    if (gameState.originalLevel.path == "levels/teach_bomb_push.tmx")
                    {
                        int uid = gameState.originalLevel.get(2, 9)->tileIdentity;
                        if (tilemap.get(2, 9)->tileIdentity == uid)
                            DrilbertSteam.unlockAchievement("RAILGUNT");
                    }

                    if (tilemap.dead)
                        DrilbertSteam.unlockAchievement("NECROBERT");

                    if (gameState.originalLevel.path == "levels/heartbreaker.tmx")
                        DrilbertSteam.unlockAchievement("HOWCOULDYOU");

                }
            }

            blockedDirections.Clear();
            if (getCurrentMenu() == null)
            {
                Grip grip = GameLogic.getGrip(tilemap);
                foreach (Direction moveDirection in new Direction[]{Direction.Up, Direction.Down, Direction.Left, Direction.Right})
                {
                    Vec2i move = GameLogic.directionToVector(moveDirection);

                    Vec2i newPosition = tilemap.playerPosition + move;

                    if (!tilemap.isPointValid(newPosition))
                        continue;

                    TileId targetTileId = tilemap.get(newPosition)->tileId;

                    bool targetUnpassable = GameLogic.tileIsSolid(targetTileId) && targetTileId != Constants.dirtTileId && !Constants.diamondIds.Contains(targetTileId.val);
                    if (targetUnpassable || !GameLogic.checkMoveDisallowJump(tilemap, grip, moveDirection, targetTileId))
                        blockedDirections.Add(moveDirection);
                }
            }

            GameLogic.getGrip(tilemap);

            AnimationPoint animationPoint;
            {
                EvaluationResult previousResult = GameLogic.evaluate(gameState.originalLevel, new MySlice<GameAction>(gameState.moves, 0, gameState.moves.Count - 1));
                EvaluationResult currentResult = GameLogic.evaluate(gameState.originalLevel, new MySlice<GameAction>(gameState.moves));
                List<AnimationStage> animationStages = LevelAnimation.calculateAnimationStages(previousResult, currentResult);
                animationPoint = LevelAnimation.calculateAnimationPoint(gameTimeMs - inputHandler.lastinputMs, animationStages);
            }

            rumbleLeft = animationPoint.stage.shake ? 0.5f : 0;
            rumbleRight = animationPoint.stage.shake ? 0.5f : 0;

            MusicManager.dynamicPitch = animationPoint.stage.endTilemap.dead ? -0.5f : 0.0f;
            MusicManager.dynamicVolume = getCurrentMenu() != null && !getCurrentMenu().isAudioSettings ? Sounds.decibelToLinear(-20) : 1.0f;
        }

        public override void processInput(long gameTimeMs, InputHandler inputHandler)
        {
            AnimationPoint animationPoint;
            {
                EvaluationResult previousResult = GameLogic.evaluate(gameState.originalLevel, new MySlice<GameAction>(gameState.moves, 0, gameState.moves.Count - 1));
                EvaluationResult currentResult = GameLogic.evaluate(gameState);
                List<AnimationStage> animationStages = LevelAnimation.calculateAnimationStages(previousResult, currentResult);
                animationPoint = LevelAnimation.calculateAnimationPoint(gameTimeMs - inputHandler.lastinputMs, animationStages);
            }

            bool tryAddMove(GameState state, GameAction action, bool allowErrorSound = true)
            {
                bool retval = GameLogic.tryAddMove(state, action);
                if (!retval && allowErrorSound)
                    Sounds.soundEffects[SoundId.Error].Play();

                return retval;
            }

            Input directionToInput(Direction direction)
            {
                switch (direction)
                {
                    case Direction.Up: return Input.Up;
                    case Direction.Down: return Input.Down;
                    case Direction.Left: return Input.Left;
                    case Direction.Right: return Input.Right;
                }
                Util.ReleaseAssert(false);
                return Input.Up;
            }

            GameAction directionToGameAction(Direction direction)
            {
                switch (direction)
                {
                    case Direction.Up: return GameAction.Up;
                    case Direction.Down: return GameAction.Down;
                    case Direction.Left: return GameAction.Left;
                    case Direction.Right: return GameAction.Right;
                }
                Util.ReleaseAssert(false);
                return GameAction.Up;
            }

            if (animationPoint.isDone)
            {
                Direction newDirection = Direction.None;

                void handleDirection(Direction direction)
                {
                    if (inputHandler.input.currentState.getInput(directionToInput(direction)) && (lastDirection == direction || lastDirection == Direction.None))
                        newDirection = direction;
                }

                handleDirection(Direction.Up);
                handleDirection(Direction.Down);
                handleDirection(Direction.Left);
                handleDirection(Direction.Right);

                if (lastDirection != Direction.None)
                {
                    bool activeWithRepeat = inputHandler.input.activeWithRepeat(directionToInput(lastDirection));
                    bool allowErrorSound = inputHandler.input.downThisFrame(directionToInput(lastDirection));

                    if (activeWithRepeat && tryAddMove(gameState, directionToGameAction(lastDirection), allowErrorSound))
                        inputHandler.lastinputMs = gameTimeMs;
                }

                lastDirection = newDirection;

                if (inputHandler.input.downThisFrame(Input.BombDrop) && tryAddMove(gameState, GameAction.BombDrop))
                    inputHandler.lastinputMs = gameTimeMs;

                if (inputHandler.input.downThisFrame(Input.BombTrigger) && tryAddMove(gameState, GameAction.BombTrigger))
                    inputHandler.lastinputMs = gameTimeMs;

                if (inputHandler.input.downThisFrame(Input.MegadrillDrop) && tryAddMove(gameState, GameAction.MegadrillDrop))
                    inputHandler.lastinputMs = gameTimeMs;
            }

            if (inputHandler.input.activeWithRepeat(Input.Undo))
            {
                if (GameLogic.tryUndo(gameState))
                    inputHandler.lastinputMs = 0;
                else
                    Sounds.soundEffects[SoundId.Error].Play();
            }

            if (inputHandler.input.downThisFrame(Input.Reset))
            {
                GameLogic.reset(gameState);
                inputHandler.lastinputMs = 0;
            }

            if (inputHandler.input.downThisFrame(Input.Pause))
            {
                pushMenu(Menus.pauseMenu());
            }
        }

        public override void menuActivate(InputHandler inputHandler)
        {
            switch (getCurrentMenu().getSelectedLine().action)
            {
                case MenuAction.Continue:
                {
                    gameState.moves.Clear();
                    popCurrentMenu();
                    Game1.game.levelSelectScene.returnToLevelSelect(true);
                    Sounds.menuActivate.Play();
                    break;
                }
                case MenuAction.Restart:
                {
                    gameState.moves.Clear();
                    inputHandler.lastinputMs = 0;
                    popCurrentMenu();
                    Sounds.menuActivate.Play();
                    break;
                }

                case MenuAction.ExitLevel:
                {
                    gameState.moves.Clear();
                    popCurrentMenu();
                    Game1.game.levelSelectScene.returnToLevelSelect(false);
                    Sounds.menuActivate.Play();
                    break;
                }

                default:
                    base.menuActivate(inputHandler);
                    break;
            }
        }

        Rect renderTopHud(MySpriteBatch spriteBatch, long gameTimeMs, Tilemap state)
        {
            GraphicsDevice.Clear(new Color(0, 0, 0, 0));

            int widthUsed = 0;
            spriteBatch.Begin(SpriteSortMode.Deferred, Render.alphaBlend);
            {
                string scoreString = "";
                if (state.maxLoot > 0)
                    scoreString += "• " + state.currentLoot + "/" + state.maxLoot;
                if (state.maxDiamonds > 0)
                {
                    if (scoreString.Length > 0)
                        scoreString += "  ";
                    scoreString += "◆ " + state.currentDiamonds + "/" + state.maxDiamonds;
                }

                string inventoryString = "";
                if (state.currentBombs > 0)
                {
                    if (inventoryString.Length > 0)
                        inventoryString += "  ";
                    inventoryString += "Ⓑ " + state.currentBombs;
                }

                if (state.currentMegadrills > 0)
                {
                    if (inventoryString.Length > 0)
                        inventoryString += "  ";
                    inventoryString += "◇ " + state.currentMegadrills;
                }

                if (inventoryString.Length > 0)
                    scoreString += "  ";

                int textWidth = Textures.drilbertFont.measureText(scoreString) + Textures.drilbertFont.measureText(inventoryString);

                int tilesRequired;
                {
                    float temp = (float) textWidth / (float) Constants.tileSize;
                    temp += 0.5f * 2; // allow a horizontal margin
                    tilesRequired = (int)MathF.Ceiling(temp);
                }

                widthUsed = tilesRequired * Constants.tileSize;

                Render.renderTile(spriteBatch, gameTimeMs, new RenderTileId(Constants.hudBorderLeftId), new Vec2i(0, 0));
                for (int x = 1; x < tilesRequired - 1; x++)
                    Render.renderTile(spriteBatch, gameTimeMs, new RenderTileId(Constants.hudBorderCentreId), new Vec2i(x, 0));
                Render.renderTile(spriteBatch, gameTimeMs, new RenderTileId(Constants.hudBorderRightId), new Vec2i(tilesRequired - 1, 0));

                float textX = ((float)widthUsed / 2.0f) - ((float) textWidth / 2.0f);
                float textY = ((float)Constants.tileSize / 2.0f) - ((float)Textures.drilbertFont.lineHeight / 2.0f);
                Vec2i textPos = new Vec2i((int)MathF.Round(textX), (int)MathF.Round(textY));

                bool blinkCoins = state.get(state.playerPosition)->tileId == Constants.levelEndTileId &&
                                  (state.currentLoot < state.maxLoot || state.currentDiamonds < state.maxDiamonds) &&
                                  gameTimeMs % 500 < 250;

                if (blinkCoins)
                    textPos.x += Textures.drilbertFont.measureText(scoreString);
                else
                    textPos = Textures.drilbertFont.draw(scoreString, textPos, spriteBatch);


                Textures.drilbertFont.draw(inventoryString, textPos, spriteBatch);
            }
            spriteBatch.End();
            return new Rect(0, 0, widthUsed, Constants.tileSize);
        }

        TextureSlice? getTutorialPrompt(long gameTimeMs, InputHandler inputHandler, Tilemap state)
        {
            Animation animation = null;

            if (state.prompt == "movement")
            {
                animation = Textures.controlsMove;
            }
            else if (state.prompt == "undo_reset")
            {
                if (inputHandler.input.getLastInputDeviceUsed() == InputState.InputDeviceType.Keyboard)
                    animation = Textures.controlsUndoResetKeyboard;
                if (inputHandler.input.getLastInputDeviceUsed() == InputState.InputDeviceType.Gamepad)
                    animation = state.dead ? Textures.controlsUndoResetXboxNoColor : Textures.controlsUndoResetXbox;
            }
            else if (state.prompt == "bomb")
            {
                if (inputHandler.input.getLastInputDeviceUsed() == InputState.InputDeviceType.Keyboard)
                    animation = Textures.controlsBombKeyboard;
                if (inputHandler.input.getLastInputDeviceUsed() == InputState.InputDeviceType.Gamepad)
                    animation = Textures.controlsBombXbox;
            }
            else if (state.prompt == "megadrill")
            {
                if (inputHandler.input.getLastInputDeviceUsed() == InputState.InputDeviceType.Keyboard)
                    animation = Textures.controlsMegadrillKeyboard;
                if (inputHandler.input.getLastInputDeviceUsed() == InputState.InputDeviceType.Gamepad)
                    animation = Textures.controlsMegadrillXbox;
            }

            if (animation != null)
                return new TextureSlice(){ texture = animation.getCurrentFrame(gameTimeMs), area = new Rect(0, 0, animation.Width, animation.Height)};

            return null;
        }

        public override void draw(MySpriteBatch spriteBatch, InputHandler inputHandler, long gameTimeMs)
        {
            AnimationPoint animationPoint;
            {
                EvaluationResult previousResult = GameLogic.evaluate(gameState.originalLevel, new MySlice<GameAction>(gameState.moves, 0, gameState.moves.Count - 1));
                EvaluationResult currentResult = GameLogic.evaluate(gameState.originalLevel, new MySlice<GameAction>(gameState.moves));
                List<AnimationStage> animationStages = LevelAnimation.calculateAnimationStages(previousResult, currentResult);
                animationPoint = LevelAnimation.calculateAnimationPoint(gameTimeMs - inputHandler.lastinputMs, animationStages);
            }

            Util.ReleaseAssert(gameTimeMs >= inputHandler.lastinputMs);
            Util.ReleaseAssert(animationPoint.lerpAlpha > -0.99 && animationPoint.lerpAlpha < 1.01);

            if (lastLerpAlpha > animationPoint.lerpAlpha)
            {
                foreach (SoundId sound in animationPoint.stage.endTilemap.soundEffects)
                    Sounds.soundEffects[sound].Play();

                if (animationPoint.stage.shake)
                    Sounds.soundEffects[SoundId.BigMovement].Play();
            }
            lastLerpAlpha = animationPoint.lerpAlpha;

            if (!wasDead && animationPoint.stage.endTilemap.dead)
                Sounds.soundEffects[SoundId.Death].Play();
            wasDead = animationPoint.stage.endTilemap.dead;

            if (mainRenderBuffer == null || mainRenderBuffer.Width != gameState.originalLevel.dimensions.x * Constants.tileSize || mainRenderBuffer.Height != gameState.originalLevel.dimensions.y * Constants.tileSize)
                mainRenderBuffer = new RenderTarget2D(GraphicsDevice, gameState.originalLevel.dimensions.x * Constants.tileSize, gameState.originalLevel.dimensions.y * Constants.tileSize);

            GraphicsDevice.SetRenderTarget(mainRenderBuffer);
            LevelRender.render(spriteBatch,
                               getCurrentMenu() == null ? gameTimeMs : 0,
                               animationPoint.stage.startTilemap,
                               animationPoint.stage.endTilemap,
                               animationPoint.lerpAlpha,
                               animationPoint.stage.shake,
                               true,
                               LevelRender.EdgeEffect.Dither);

            // using (var f = System.IO.File.OpenWrite("C:\\users\\wheybags\\desktop\\out.png"))
            //     mainRenderBuffer.SaveAsPng(f, mainRenderBuffer.Width, mainRenderBuffer.Height);

            Rect? menuArea = null;
            if (getCurrentMenu() != null)
            {
                GraphicsDevice.SetRenderTarget(menuRenderBuffer);
                menuArea = Render.renderMenu(spriteBatch, gameTimeMs, getCurrentMenu());
            }

            GraphicsDevice.SetRenderTarget(topHudRenderBuffer);
            Rect topHudArea = renderTopHud(spriteBatch, gameTimeMs, animationPoint.stage.endTilemap);

            TextureSlice? tutorialPrompt = getTutorialPrompt(gameTimeMs, inputHandler, animationPoint.stage.endTilemap);

            GraphicsDevice.SetRenderTarget(null);
            GraphicsDevice.Clear(Constants.outsideLevelBackgroundColor);
            spriteBatch.Begin(SpriteSortMode.Deferred, Render.alphaBlend, SamplerState.PointWrap);

            float topPadUnscaled = topHudArea.h;
            float bottomPadUnscaled = topPadUnscaled;
            if (tutorialPrompt.HasValue)
                bottomPadUnscaled = Math.Max(bottomPadUnscaled, tutorialPrompt.Value.area.h);

            float renderScale;
            {
                float xScale = ((float) Window.ClientBounds.Width) / (mainRenderBuffer.Width + Constants.tileSize * 1);
                float yScale = ((float) Window.ClientBounds.Height) / (mainRenderBuffer.Height + topPadUnscaled + bottomPadUnscaled);
                renderScale = (int) Math.Max(1.0f, Math.Floor(Math.Min(xScale, yScale)));
            }

            float gameBrightness = getCurrentMenu() != null ? 0.5f : 1.0f;
            Color gameTint = animationPoint.stage.endTilemap.dead ? new Color(255,0,0) : Color.White;
            gameTint *= gameBrightness;

            {
                Vec2f targetSize = mainRenderBuffer.Bounds.f().size * renderScale;

                float spaceBetweenTopAndBottomHud = Window.ClientBounds.Height - (bottomPadUnscaled + topPadUnscaled) * renderScale;
                Vec2f pos = new Vec2f(
                    Window.ClientBounds.f().w/2 - targetSize.x/2,                                   // centre on screen horizontally
                    topPadUnscaled * renderScale + spaceBetweenTopAndBottomHud/2 - targetSize.y/2   // centre on space left after top and bottom hud vertically
                ).rounded();

                spriteBatch.r(mainRenderBuffer).pos(pos).size(targetSize).color(gameTint).draw();
            }

            if (menuArea != null)
            {
                Vec2f targetSize = menuArea.Value.size * renderScale;
                Vec2f pos = (Window.ClientBounds.f().size/2 - targetSize/2).rounded();
                spriteBatch.r(menuRenderBuffer).pos(pos).size(targetSize).uv(menuArea.Value).draw();
            }
            else
            {
                // top hud
                {
                    Vec2f targetSize = topHudArea.size * renderScale;
                    Vec2f pos = new Vec2f(Window.ClientBounds.Width/2f - targetSize.x/2f, 0).rounded();
                    spriteBatch.r(topHudRenderBuffer).pos(pos).size(targetSize).uv(topHudArea).color(gameTint).draw();
                }

                // undo reset prompt
                if (animationPoint.stage.endTilemap.prompt != "undo_reset")
                {
                    bool render = true;
                    if (animationPoint.stage.endTilemap.dead)
                        render = gameTimeMs % 1000 < 500;

                    if (render)
                    {
                        Texture2D texture = Textures.controlsUndoResetTopKeyboard;
                        if (inputHandler.input.getLastInputDeviceUsed() == InputState.InputDeviceType.Gamepad)
                            texture = Textures.controlsUndoResetTopXbox;

                        Vec2f targetSize = new Vec2f(texture.Width, texture.Height) * renderScale;
                        Vec2f pos = new Vec2f(Window.ClientBounds.Width - targetSize.x, 0).rounded();
                        spriteBatch.r(texture).pos(pos).size(targetSize).draw();
                    }
                }

                // bottom hud
                if (tutorialPrompt.HasValue)
                {
                    TextureSlice hud = tutorialPrompt.Value;
                    Vec2f targetSize = hud.area.size * renderScale;
                    Vec2f pos = new Vec2f(Window.ClientBounds.Width / 2f - targetSize.x / 2f, Window.ClientBounds.Height - targetSize.y).rounded();
                    spriteBatch.r(hud.texture).pos(pos).size(targetSize).uv(hud.area).color(gameTint).draw();
                }
            }

            spriteBatch.End();
        }
    }
}