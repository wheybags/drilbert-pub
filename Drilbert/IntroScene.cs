using Microsoft.Xna.Framework;

namespace Drilbert;

public class IntroScene : CutScene
{
    public IntroScene()
    {
        sequence = new CutsceneStep[]
        {
            new PushSpriteStep()
            {
                sprite = new Animation(Textures.drilheim),
                pos = new Vec2f(0, 0),
            },
            new PushSpriteStep()
            {
                sprite = Textures.drilfatherIdle,
                pos = new Vec2f(190, 16),
                scale = new Vec2f(2,2),
            },
            new PushSpriteStep()
            {
                sprite = Textures.playerIdle,
                pos = new Vec2f(178, 44),
                scale = new Vec2f(2,2),
            },
            new PushSpriteStep()
            {
                sprite = new Animation(Textures.drilheimUpperLayer),
                pos = new Vec2f(0, 0),
            },
            new ColorFadeStep()
            {
                start = Constants.outsideLevelBackgroundColor,
                end = Constants.outsideLevelBackgroundColor,
                lengthMs = 1000,
            },
            new DialogStep()
            {
                speaker = DialogStep.Speaker.Drilfather,
                lines = new[] { "Drilbert!", "Wake up!" },
                overlayColor = Constants.outsideLevelBackgroundColor,
                showPortrait = false,
            },
            new ColorFadeStep()
            {
                start = Constants.outsideLevelBackgroundColor,
                end = Color.Transparent,
                lengthMs = 2000,
            },
            new DialogStep()
            {
                speaker = DialogStep.Speaker.Drilbert,
                lines = new[] { "?" },
            },
            new DialogStep()
            {
                speaker = DialogStep.Speaker.Drilfather,
                lines = new[] { "An evil villain has stolen our treasure and", "distributed it throughout a series of contrived..." },
            },
            new DialogStep()
            {
                speaker = DialogStep.Speaker.Drilfather,
                lines = new[] { "underground puzzle chambers!" },
            },
            new DialogStep()
            {
                speaker = DialogStep.Speaker.Drilbert,
                lines = new[] { "!!!" },
            },
            new DialogStep()
            {
                speaker = DialogStep.Speaker.Drilfather,
                lines = new[] { "Drilbert!" },
            },
            new DialogStep()
            {
                speaker = DialogStep.Speaker.Drilfather,
                lines = new[] { "The time for action is now!", "You must retrieve our treasure!" },
            },
            new DialogStep()
            {
                speaker = DialogStep.Speaker.Drilbert,
                lines = new[] { "•••◆!" },
            },
            new RemoveSpriteStep()
            {
              spriteIndex = 1,
            },
            new PushSpriteStep()
            {
              sprite = Textures.drilfatherGogogo,
              pos = new Vec2f(190, 16),
              scale = new Vec2f(2,2),
              flipHorizontal = true,
              insertIndex = 1,
            },
            new AnimateSpriteStep()
            {
                start = new Vec2f(178, 44),
                end = new Vec2f(320, 44),
                spriteIndex = 2,
                lengthMs = 3000,
            },
            new ColorFadeStep()
            {
                start = Color.Transparent,
                end = Constants.outsideLevelBackgroundColor,
                lengthMs = 1000,
            },
        };
    }

    protected override void onEnd()
    {
        Game1.game.setScene(Game1.game.levelSelectScene);
    }
}