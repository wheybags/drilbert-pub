using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Drilbert
{
    public struct TextureSlice
    {
        public Texture2D texture;
        public Rect area;
    }

    public static class Render
    {
        private static GraphicsDevice GraphicsDevice => Game1.game.GraphicsDevice;

        private static readonly BlendState punchHolesInAlphaBlendState = new BlendState()
        {
            ColorBlendFunction = BlendFunction.Add,
            ColorSourceBlend = Blend.Zero,
            ColorDestinationBlend = Blend.One,

            AlphaBlendFunction = BlendFunction.Add,
            AlphaSourceBlend = Blend.Zero,
            AlphaDestinationBlend = Blend.InverseSourceAlpha,
        };


        public static readonly BlendState alphaBlend = new BlendState()
        {
            ColorBlendFunction = BlendFunction.Add,
            ColorSourceBlend = Blend.SourceAlpha,
            AlphaSourceBlend = Blend.SourceAlpha,

            AlphaBlendFunction = BlendFunction.Add,
            ColorDestinationBlend = Blend.InverseSourceAlpha,
            AlphaDestinationBlend = Blend.DestinationAlpha,
        };

        public static Rect renderMenu(MySpriteBatch spriteBatch, long gameTimeMs, Menu menu)
        {
            GraphicsDevice.Clear(Color.Red);

            spriteBatch.Begin(SpriteSortMode.Deferred, Render.alphaBlend);

            const float lineHeightMultiplier = 1.5f;
            const float borderMargin = 0.5f;

            int lineHeight(MenuLine line)
            {
                if (line.forceLineHeight != 0)
                    return line.forceLineHeight;

                if (line.animation != null)
                    return line.animation[0].Height;

                return (int) (Textures.drilbertFont.lineHeight * lineHeightMultiplier);
            }

            string[] messages = new string[menu.lines.Count];

            int menuHeightPixels = 0;
            int menuWidthPixels = 0;
            int scrollCounted = -1;
            for (int i = 0; i < menu.lines.Count; i++)
            {
                if (i == menu.scrollAreaStart)
                    scrollCounted = 0;
                if (i == menu.scrollAreaEnd)
                    scrollCounted = -1;

                var line = menu.lines[i];
                int thisWidth;

                if (line.animation != null)
                {
                    Util.ReleaseAssert(!line.isSelectable());
                    thisWidth = line.animation[0].Width;
                }
                else
                {
                    string text = line.message;

                    if (line.type == MenuLine.Type.Slider)
                    {
                        int percent = (int)Math.Round(line.getSliderVal() * 100);
                        text += $"{percent,3}%";
                    }

                    messages[i] = text;

                    int forceCharacterWidth = line.forceTextFixedWidth ? 7 : 0;

                    if (line.isSelectable())
                        thisWidth = Textures.drilbertFont.measureText("* " + text + " *", forceCharacterWidth);
                    else
                        thisWidth = Textures.drilbertFont.measureText(text, forceCharacterWidth);
                }

                menuWidthPixels = Math.Max(thisWidth, menuWidthPixels);

                if (scrollCounted != -1)
                {
                    if (scrollCounted >= menu.scrollShowMax || i < menu.scrollOffset)
                        continue;
                }

                menuHeightPixels += lineHeight(line);

                if (scrollCounted != -1)
                    scrollCounted++;
            }

            // margin around text
            int textAreaWidthPixels = menuWidthPixels + (int)(Constants.tileSize * borderMargin * 2.0f);
            int textAreaHeightPixels = menuHeightPixels + (int)(Constants.tileSize * borderMargin * 2.0f);

            int menuWidthTiles= (int)MathF.Round((float)(textAreaWidthPixels) / Constants.tileSize);
            int menuHeightTiles = (int)MathF.Round((float)(textAreaHeightPixels) / Constants.tileSize);

            // Clear a chunk of screen that we will draw the menu on top of
            spriteBatch.r(Textures.white).size(new Vec2f(menuWidthTiles, menuHeightTiles) * Constants.tileSize).color(Constants.levelBackgroundColor).draw();

            // Draw the border tileset
            {
                renderTileAtPixel(spriteBatch, gameTimeMs, new RenderTileId(Constants.guiBorderTopLeftId), new Vec2i(0, 0));
                renderTileAtPixel(spriteBatch, gameTimeMs, new RenderTileId(Constants.guiBorderTopRightId), new Vec2i((menuWidthTiles - 1) * Constants.tileSize, 0));
                renderTileAtPixel(spriteBatch, gameTimeMs, new RenderTileId(Constants.guiBorderBottomLeftId), new Vec2i(0, (menuHeightTiles - 1) * Constants.tileSize));
                renderTileAtPixel(spriteBatch, gameTimeMs, new RenderTileId(Constants.guiBorderBottomRightId), new Vec2i((menuWidthTiles - 1) * Constants.tileSize, (menuHeightTiles - 1) * Constants.tileSize));
                for (int x = 1; x < menuWidthTiles - 1; x++)
                {
                    renderTileAtPixel(spriteBatch, gameTimeMs, new RenderTileId(Constants.guiBorderTopId), new Vec2i(x * Constants.tileSize, 0));
                    renderTileAtPixel(spriteBatch, gameTimeMs, new RenderTileId(Constants.guiBorderBottomId), new Vec2i(x * Constants.tileSize, (menuHeightTiles - 1) * Constants.tileSize));
                }

                for (int y = 1; y < menuHeightTiles - 1; y++)
                {
                    renderTileAtPixel(spriteBatch, gameTimeMs, new RenderTileId(Constants.guiBorderLeftId), new Vec2i(0, y * Constants.tileSize));
                    renderTileAtPixel(spriteBatch, gameTimeMs, new RenderTileId(Constants.guiBorderRightId), new Vec2i((menuWidthTiles - 1) * Constants.tileSize, y * Constants.tileSize));
                }
            }

            int scrollRendered = -1;

            // Draw the lines of text for the menu
            {
                int y = 0 + (menuHeightTiles * Constants.tileSize) / 2 - menuHeightPixels / 2;

                for (int i = 0; i < menu.lines.Count; i++)
                {
                    if (i == menu.scrollAreaStart)
                        scrollRendered = 0;
                    if (i == menu.scrollAreaEnd)
                        scrollRendered = -1;

                    if (scrollRendered != -1)
                    {
                        if (scrollRendered >= menu.scrollShowMax || i < menu.scrollOffset)
                            continue;
                    }

                    if (menu.lines[i].animation != null)
                    {
                        int x = (int) MathF.Round((Constants.tileSize * menuWidthTiles) / 2.0f - menu.lines[i].animation[0].Width / 2.0f);

                        foreach (Animation animation in menu.lines[i].animation)
                            spriteBatch.r(animation.getCurrentFrame(gameTimeMs)).pos(new Vec2f(x, y)).draw();
                    }
                    else
                    {
                        int blinkInterval = (int) (1000 * 0.5f);
                        bool showSelectionMarker = menu.selectedIndex == i && ((gameTimeMs % (blinkInterval * 2) < blinkInterval) || gameTimeMs - menu.lastChangeMs < Constants.menuKeyRepeatMs * 2);
                        string text = showSelectionMarker ? ("* " + messages[i] + " *") : messages[i];

                        int forceCharacterWidth = menu.lines[i].forceTextFixedWidth? 7 : 0;
                        int textWidth = Textures.drilbertFont.measureText(text, forceCharacterWidth);
                        int x = (int) MathF.Round((Constants.tileSize * menuWidthTiles) / 2.0f - textWidth / 2.0f);

                        Textures.drilbertFont.draw(text, new Vec2i(x, y), spriteBatch, forceCharacterWidth, tintColor: menu.lines[i].textTint);
                    }
                    y += lineHeight(menu.lines[i]);

                    if (scrollRendered != -1)
                        scrollRendered++;
                }
            }

            spriteBatch.End();

            return new Rect(0, 0, menuWidthTiles*Constants.tileSize, menuHeightTiles*Constants.tileSize);
        }

        public static void renderEdgeDither(MySpriteBatch spriteBatch, long gameTimeMs, Vec2i sizeInTiles)
        {
            spriteBatch.Begin(SpriteSortMode.Deferred, punchHolesInAlphaBlendState);
            {
                for (int x = 1; x < sizeInTiles.x - 1; x++)
                {
                    renderTile(spriteBatch, gameTimeMs, new RenderTileId(Constants.ditherTopId), new Vec2i(x, 0));
                    renderTile(spriteBatch, gameTimeMs, new RenderTileId(Constants.ditherBottomId), new Vec2i(x, sizeInTiles.y - 1));
                }

                for (int y = 1; y < sizeInTiles.y - 1; y++)
                {
                    renderTile(spriteBatch, gameTimeMs, new RenderTileId(Constants.ditherLeftId), new Vec2i(0, y));
                    renderTile(spriteBatch, gameTimeMs, new RenderTileId(Constants.ditherRightId), new Vec2i(sizeInTiles.x - 1, y));
                }

                renderTile(spriteBatch, gameTimeMs, new RenderTileId(Constants.ditherTopLeftId), new Vec2i(0, 0));
                renderTile(spriteBatch, gameTimeMs, new RenderTileId(Constants.ditherTopRightId), new Vec2i(sizeInTiles.x - 1, 0));
                renderTile(spriteBatch, gameTimeMs, new RenderTileId(Constants.ditherBottomLeftId), new Vec2i(0, sizeInTiles.y - 1));
                renderTile(spriteBatch, gameTimeMs, new RenderTileId(Constants.ditherBottomRightId), new Vec2i(sizeInTiles.x - 1, sizeInTiles.y - 1));
            }
            spriteBatch.End();
        }

        public static Vec2i getCoordsInTileset(RenderTileId tileId)
        {
            int tilesetWidth = Textures.tileset.Width / Constants.tileSize;
            int tileY = tileId.val / tilesetWidth;
            int tileX = tileId.val - (tileY * tilesetWidth);
            return new Vec2i(tileX, tileY);
        }

        public static void renderTile(MySpriteBatch spriteBatch, long gameTimeMs, RenderTileId tileId, Vec2i tilePos, Color? color = null)
        {
            renderTileAtPixel(spriteBatch, gameTimeMs, tileId, tilePos.f() * Constants.tileSize, color);
        }

        public static void renderTile(MySpriteBatch spriteBatch, long gameTimeMs, RenderTileId tileId, Vec2f tilePos, Color? color = null)
        {
           renderTileAtPixel(spriteBatch, gameTimeMs, tileId, tilePos * Constants.tileSize, color);
        }

        public static void renderTileAtPixel(MySpriteBatch spriteBatch, long gameTimeMs, RenderTileId tileId, Vec2i pixelPos)
        {
            renderTileAtPixel(spriteBatch, gameTimeMs, tileId, pixelPos.f());
        }

        public static void renderTileAtPixel(MySpriteBatch spriteBatch, long gameTimeMs, RenderTileId tileId, Vec2f pixelPos, Color? color = null)
        {
            Vec2i tilesetCoords = getCoordsInTileset(tileId);

            Rect tileUv = new Rect(tilesetCoords.f() * Constants.tileSize, Constants.tileSize, Constants.tileSize);
            spriteBatch.r(Textures.tileset.getCurrentFrame(gameTimeMs))
                       .pos(pixelPos)
                       .size(tileUv.size)
                       .uv(tileUv)
                       .color(color ?? Color.White)
                       .draw();
        }
    }
}