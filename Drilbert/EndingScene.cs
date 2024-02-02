using Microsoft.Xna.Framework;

namespace Drilbert;

public class EndingScene : CutScene
{
    public EndingScene()
    {
        Vec2f bounds = new Vec2f(Constants.levelWidth, Constants.levelHeight) * Constants.tileSize;

        sequence = new CutsceneStep[]
        {
            new PushSpriteStep()
            {
              sprite = new Animation(Textures.white),
              pos = new Vec2f(0, 0),
              scale = bounds,
              tint = Constants.levelBackgroundColor,
            },
            new PushSpriteStep()
            {
                sprite = Textures.diamond,
                pos = new Vec2f(bounds.x / 2.0f - Textures.diamond.size().f().x*2 / 2.0f, 30).rounded(),
                scale = new Vec2f(2,2),
            },
            new PushSpriteStep()
            {
                sprite = new Animation(Textures.playerHangInPipe.frames[1]),
                pos = new Vec2f(bounds.x / 2.0f - Textures.playerHangInPipe.size().f().x*2 / 2.0f, 70).rounded(),
                scale = new Vec2f(2,2),
            },
            new DialogStep()
            {
                speaker = DialogStep.Speaker.None,
                lines = new[] { "Congratulations!" },
            },
            new DialogStep()
            {
                speaker = DialogStep.Speaker.None,
                lines = new[] { "You have successfully recovered all the treasure!" },
            },
            new DialogStep()
            {
                speaker = DialogStep.Speaker.None,
                lines = new[] { "Drilfather will be so pleased" },
            },
            new DialogStep()
            {
                speaker = DialogStep.Speaker.Drilfather,
                lines = new[] { "I'm so pleased!" },
            },
            new DialogStep()
            {
                speaker = DialogStep.Speaker.Drilbert,
                lines = new[] { "...!!!" },
            },
            new ColorFadeStep()
            {
                start = Color.Transparent,
                end = Constants.outsideLevelBackgroundColor,
                lengthMs = 500,
            },
            new RemoveSpriteStep()
            {
                spriteIndex = 2,
            },
            new RemoveSpriteStep()
            {
                spriteIndex = 1,
            },
            new PushSpriteStep()
            {
                sprite = Textures.logo,
                pos = (bounds / 2.0f - Textures.logo.size().f()*2 / 2.0f).rounded(),
                scale = new Vec2f(2,2),
            },
            new ColorFadeStep()
            {
                start = Constants.outsideLevelBackgroundColor,
                end = Color.Transparent,
                lengthMs = 500,
            },
            new DialogStep()
            {
                speaker = DialogStep.Speaker.None,
                lines = new[] { "Thank you for playing" },
            },
            new DialogStep()
            {
                speaker = DialogStep.Speaker.None,
                lines = new[] { "Drilbert: a game by Tom Mason" },
            },
            new DialogStep()
            {
                speaker = DialogStep.Speaker.None,
                lines = new[] { "Music by Nicole Marie T" },
            },
            new DialogStep()
            {
                overrideName = "Special thanks",
                speaker = DialogStep.Speaker.None,
                lines = new[] { "Ben Buckton" },
            },
            new DialogStep()
            {
                overrideName = "Special thanks",
                speaker = DialogStep.Speaker.None,
                lines = new[] { "Frankfurt indies group" },
            },
            new DialogStep()
            {
                overrideName = "Special thanks",
                speaker = DialogStep.Speaker.None,
                lines = new[] { "My friends and colleagues at powder" },
            },
            new DialogStep()
            {
                overrideName = "Special thanks",
                speaker = DialogStep.Speaker.None,
                lines = new[] { "Tiled editor" },
            },
            new DialogStep()
            {
                overrideName = "Special thanks",
                speaker = DialogStep.Speaker.None,
                lines = new[] { "Aseprite" },
            },
            new ColorFadeStep()
            {
                start = Color.Transparent,
                end = Constants.outsideLevelBackgroundColor,
                lengthMs = 2000,
            },
        };
    }

    protected override void onEnd()
    {
        Game1.game.setScene(Game1.game.mainMenuScene);
    }
}