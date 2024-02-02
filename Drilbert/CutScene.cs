using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Drilbert;

public abstract class CutScene : Scene
{
    protected RenderTarget2D mainRenderBuffer = null;

    public CutScene()
    {
        mainRenderBuffer = new RenderTarget2D(Game1.game.GraphicsDevice, Constants.levelWidth * Constants.tileSize, Constants.levelHeight * Constants.tileSize);
    }

    public override void start()
    {
        do
        {
            advanceStep();
        } while (currentStep.done(Time.getMs(), currentStepStartTimeMs));
    }

    internal abstract class CutsceneStep
    {
        public abstract bool done(long nowMs, long startMs);
        public virtual Color getOverlayColor(long nowMs, long startMs) { return Color.Transparent; }
    }

    internal class ColorFadeStep : CutsceneStep
    {
        public long lengthMs;
        public Color start;
        public Color end;

        private float getAlpha(long nowMs, long startMs)
        {
            long elapsed = nowMs - startMs;
            return Math.Clamp(((float)elapsed) / ((float)lengthMs), 0.0f, 1.0f);
        }

        public override bool done(long nowMs, long startMs)
        {
            return getAlpha(nowMs, startMs) >= 1.0f;
        }

        public override Color getOverlayColor(long nowMs, long startMs)
        {
            float alpha = getAlpha(nowMs, startMs);
            return new Color(start.ToVector4() * (1.0f - alpha) + end.ToVector4() * alpha);
        }
    }

    internal class DialogStep : CutsceneStep
    {
        public enum Speaker
        {
            None,
            Drilbert,
            Drilfather,
        }

        public Speaker speaker;
        public string overrideName = null;
        public bool showPortrait = true;
        public string[] lines;
        public Color overlayColor = Color.Transparent;

        public int charsToShow(long now, long start)
        {
            return (int)((now - start) / 1000f * 20.0f);
        }

        public bool allTextDone(long now, long start)
        {
            int totalChars = lines.Select(line => line.Length).Sum();
            return charsToShow(now, start) >= totalChars;
        }

        public override bool done(long nowMs, long startMs)
        {
            return false;
        }

        public override Color getOverlayColor(long nowMs, long startMs)
        {
            return overlayColor;
        }
    }

    internal class PushSpriteStep : CutsceneStep
    {
        public Animation sprite;
        public Vec2f pos;
        public Vec2f scale = new Vec2f(1,1);
        public bool flipHorizontal = false;
        public int insertIndex = -1;
        public Color tint = Color.White;

        public override bool done(long nowMs, long startMs)
        {
            return true;
        }
    }

    internal class RemoveSpriteStep : CutsceneStep
    {
        public int spriteIndex;

        public override bool done(long nowMs, long startMs)
        {
            return true;
        }
    }

    internal class AnimateSpriteStep : CutsceneStep
    {
        public long lengthMs;
        public int spriteIndex;
        public Vec2f start;
        public Vec2f end;

        private float getAlpha(long nowMs, long startMs)
        {
            long elapsed = nowMs - startMs;
            return Math.Clamp(((float)elapsed) / ((float)lengthMs), 0.0f, 1.0f);
        }

        public override bool done(long nowMs, long startMs)
        {
            return getAlpha(nowMs, startMs) >= 1.0f;
        }

        public Vec2f calculateSpritePosition(long nowMs, long startMs)
        {
            float alpha = getAlpha(nowMs, startMs);
            return start * (1.0f - alpha) + end * alpha;
        }
    }

    internal CutsceneStep[] sequence;

    private int currentStepIndex = -1;
    private CutsceneStep currentStep => currentStepIndex < sequence.Length ? sequence[currentStepIndex] : null;
    private long currentStepStartTimeMs = 0;
    private bool dialogForcedEnd = false;

    private class SpriteData
    {
        public Animation sprite;
        public Vec2f pos;
        public Vec2f scale;
        public bool flipHorizontal;
        public Color tint;
    };
    private List<SpriteData> spriteStack = new List<SpriteData>();

    private void advanceStep()
    {
        currentStepIndex++;
        currentStepStartTimeMs = Time.getMs();
        dialogForcedEnd = false;

        if (currentStep is PushSpriteStep)
        {
            PushSpriteStep sprite = (PushSpriteStep)currentStep;
            spriteStack.Insert(sprite.insertIndex == -1 ? spriteStack.Count : sprite.insertIndex,
                               new SpriteData() {
                                 sprite = sprite.sprite,
                                 pos = sprite.pos,
                                 scale = sprite.scale,
                                 flipHorizontal = sprite.flipHorizontal,
                                 tint = sprite.tint,
                               });
        }
        else if (currentStep is RemoveSpriteStep)
        {
            RemoveSpriteStep remove = (RemoveSpriteStep)currentStep;
            spriteStack.RemoveAt(remove.spriteIndex);
        }

        if (currentStep == null)
        {
            onEnd();
        }
    }

    protected abstract void onEnd();

    public override void processInput(long gameTimeMs, InputHandler inputHandler)
    {
        if (currentStep is DialogStep && inputHandler.input.downThisFrame(Input.MenuActivate))
        {
            if (!dialogForcedEnd && !(currentStep as DialogStep).allTextDone(gameTimeMs, currentStepStartTimeMs))
                dialogForcedEnd = true;
            else
                advanceStep();
        }
    }

    public override void update(long gameTimeMs, InputHandler inputHandler)
    {
        while (currentStep != null && currentStep.done(gameTimeMs, currentStepStartTimeMs))
            advanceStep();

        if (currentStep is AnimateSpriteStep)
        {
            AnimateSpriteStep animateSpriteStep = (AnimateSpriteStep)currentStep;
            spriteStack[animateSpriteStep.spriteIndex].pos = animateSpriteStep.calculateSpritePosition(gameTimeMs, currentStepStartTimeMs);
        }
    }

    public override void draw(MySpriteBatch spriteBatch, InputHandler inputHandler, long gameTimeMs)
    {
        GraphicsDevice.SetRenderTarget(mainRenderBuffer);
        GraphicsDevice.Clear(Color.Red);
        spriteBatch.Begin(SpriteSortMode.Deferred, Render.alphaBlend, SamplerState.PointWrap);

        foreach (SpriteData sprite in spriteStack)
        {
            spriteBatch.r(sprite.sprite.getCurrentFrame(gameTimeMs))
                       .pos(sprite.pos)
                       .scale(sprite.scale)
                       .flipHorizontal(sprite.flipHorizontal)
                       .color(sprite.tint)
                       .draw();
        }

        Color overlayColor = currentStep.getOverlayColor(gameTimeMs, currentStepStartTimeMs);
        spriteBatch.r(Textures.white).size(new Vec2f(mainRenderBuffer.Width, mainRenderBuffer.Height)).color(overlayColor).draw();

        // dialog box
        if (currentStep is DialogStep)
        {
            int menuWidthTiles = 20;
            int menuHeightTiles = 3;
            Vec2f _dialogPos = (new Vec2f((int)(Constants.levelWidth / 2.0 - menuWidthTiles / 2.0), Constants.levelHeight - menuHeightTiles - 0.5) * Constants.tileSize);
            Vec2i dialogPos = new Vec2i(_dialogPos.x, _dialogPos.y);

            DialogStep dialog = (DialogStep)currentStep;

            string speakerName = "";
            Texture2D portrait = null;
            Vec2i portraitPosition = new Vec2i();

            switch (dialog.speaker)
            {
                case DialogStep.Speaker.Drilbert:
                    speakerName = "Drilbert";
                    portrait = Textures.drilbertBig;
                    portraitPosition = new Vec2i(10, Constants.levelHeight * Constants.tileSize - portrait.Height - 10);
                    break;
                case DialogStep.Speaker.Drilfather:
                    speakerName = "Drilfather";
                    portrait = Textures.drilfatherBig;
                    portraitPosition = new Vec2i(Constants.levelWidth * Constants.tileSize - portrait.Width - 4, Constants.levelHeight * Constants.tileSize - portrait.Height + 10);
                    break;
            }

            if (dialog.overrideName != null)
                speakerName = dialog.overrideName;

            // portrait
            if (dialog.showPortrait && portrait != null)
                spriteBatch.r(portrait).pos(portraitPosition).draw();

            // Clear a chunk of screen that we will draw the menu on top of
            spriteBatch.r(Textures.white).size(new Vec2f(menuWidthTiles, menuHeightTiles) * Constants.tileSize).pos(dialogPos).color(Constants.levelBackgroundColor).draw();

            // Draw the border tileset
            {
                Render.renderTileAtPixel(spriteBatch, gameTimeMs, new RenderTileId(Constants.guiBorderTopLeftId), new Vec2i(0, 0) + dialogPos);
                Render.renderTileAtPixel(spriteBatch, gameTimeMs, new RenderTileId(Constants.guiBorderTopRightId), new Vec2i((menuWidthTiles - 1) * Constants.tileSize, 0) + dialogPos);
                Render.renderTileAtPixel(spriteBatch, gameTimeMs, new RenderTileId(Constants.guiBorderBottomLeftId), new Vec2i(0, (menuHeightTiles - 1) * Constants.tileSize) + dialogPos);
                Render.renderTileAtPixel(spriteBatch, gameTimeMs, new RenderTileId(Constants.guiBorderBottomRightId), new Vec2i((menuWidthTiles - 1) * Constants.tileSize, (menuHeightTiles - 1) * Constants.tileSize) + dialogPos);
                for (int x = 1; x < menuWidthTiles - 1; x++)
                {
                    Render.renderTileAtPixel(spriteBatch, gameTimeMs, new RenderTileId(Constants.guiBorderTopId), new Vec2i(x * Constants.tileSize, 0) + dialogPos);
                    Render.renderTileAtPixel(spriteBatch, gameTimeMs, new RenderTileId(Constants.guiBorderBottomId), new Vec2i(x * Constants.tileSize, (menuHeightTiles - 1) * Constants.tileSize) + dialogPos);
                }

                for (int y = 1; y < menuHeightTiles - 1; y++)
                {
                    Render.renderTileAtPixel(spriteBatch, gameTimeMs, new RenderTileId(Constants.guiBorderLeftId), new Vec2i(0, y * Constants.tileSize) + dialogPos);
                    Render.renderTileAtPixel(spriteBatch, gameTimeMs, new RenderTileId(Constants.guiBorderRightId), new Vec2i((menuWidthTiles - 1) * Constants.tileSize, y * Constants.tileSize) + dialogPos);
                }
            }

            Textures.drilbertFont.draw(speakerName, dialogPos + new Vec2i(Constants.tileSize / 2, Constants.tileSize * 0.4), spriteBatch, underlineColor: Constants.drilbertWhite);
            Vec2i textPos = new Vec2i(Constants.tileSize / 2, Constants.tileSize * 1.25);

            int charsToShowTotal = dialogForcedEnd ? Int32.MaxValue : dialog.charsToShow(gameTimeMs, currentStepStartTimeMs);
            int charsShown = 0;
            foreach (string line in dialog.lines)
            {
                int newCharsShown = charsShown + line.Length;
                if (newCharsShown > charsToShowTotal)
                    newCharsShown = charsToShowTotal;

                int charsToShowThisLine = newCharsShown - charsShown;
                if (charsToShowThisLine == 0)
                    break;

                Textures.drilbertFont.draw(line.Substring(0, charsToShowThisLine), dialogPos + textPos, spriteBatch);
                textPos.y += (int)(Textures.drilbertFont.lineHeight * 1.5);

                charsShown = newCharsShown;
            }
        }

        spriteBatch.End();

        Render.renderEdgeDither(spriteBatch, 0, new Vec2i(Constants.levelWidth, Constants.levelHeight));

        GraphicsDevice.SetRenderTarget(null);
        GraphicsDevice.Clear(Constants.outsideLevelBackgroundColor);
        spriteBatch.Begin(SpriteSortMode.Deferred, Render.alphaBlend, SamplerState.PointWrap);
        {
            float renderScale;
            {
                float xScale = ((float)Window.ClientBounds.Width) / (mainRenderBuffer.Width + Constants.tileSize * 1);
                float yScale = ((float)Window.ClientBounds.Height) / (mainRenderBuffer.Height + Constants.tileSize * 1);
                renderScale = (int)Math.Floor(Math.Min(xScale, yScale));
            }

            {
                Vec2f targetSize = mainRenderBuffer.Bounds.f().size * renderScale;

                // centre on screen
                Vec2f pos = new Vec2f(
                    Window.ClientBounds.f().w / 2 - targetSize.x / 2,
                    Window.ClientBounds.f().h / 2 - targetSize.y / 2
                );
                spriteBatch.r(mainRenderBuffer).pos(pos).size(targetSize).draw();
            }
        }
        spriteBatch.End();
    }
}