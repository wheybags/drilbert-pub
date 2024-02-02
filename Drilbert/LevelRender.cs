using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Drilbert
{
    using TransitionMap = Dictionary<string, RenderTileId>;

    public static unsafe class LevelRender
    {
        private static GraphicsDevice GraphicsDevice => Game1.game.GraphicsDevice;

        private static readonly Dictionary<int, TransitionMap> tileTransitions = new Dictionary<int, TransitionMap>()
        {
            { Constants.dirtTileId, createTransitionMap(new RenderTileId(Constants.dirtTileId)) },
            { Constants.rockGraphicalBaseTile, createTransitionMap(new RenderTileId(Constants.rockGraphicalBaseTile)) },
            { Constants.bedrockTileId, createTransitionMap(new RenderTileId(Constants.bedrockTileId))},
        };
        private static TransitionMap fireTransitions = createFireTransitionMap();

        public enum EdgeEffect
        {
            None,
            Dither,
        }


        public static void render(MySpriteBatch spriteBatch, long gameTimeMs, Tilemap previous, Tilemap state, float stateLerpAlpha, bool shakeLevel, bool doRenderPlayer, EdgeEffect edgeEffect)
        {
            GraphicsDevice.Clear(Constants.levelBackgroundColor);

            spriteBatch.Begin(SpriteSortMode.Deferred, Render.alphaBlend);
            {
                renderLevel(spriteBatch, state.dead ? 0 : gameTimeMs, previous, state, stateLerpAlpha, shakeLevel);
                if (doRenderPlayer && (!state.win || stateLerpAlpha < 1))
                    renderPlayer(spriteBatch, gameTimeMs, previous, state, stateLerpAlpha);

                // drawSegmentDebugOverlay(spriteBatch, state);
                // drawTileIdentityDebugOverlay(spriteBatch, state);
                // drawFixedSegmentsDebugOverlay(spriteBatch, state, new Vec2i(1, 0));
                // drawShakingDebugOverlay(spriteBatch, state);
            }
            spriteBatch.End();

            if (edgeEffect == EdgeEffect.Dither)
                Render.renderEdgeDither(spriteBatch, gameTimeMs, state.dimensions);
        }

        static void drawShakingDebugOverlay(MySpriteBatch spriteBatch, Tilemap state)
        {
            for (int y = 0; y < state.dimensions.y; y++)
            {
                for (int x = 0; x < state.dimensions.x; x++)
                {
                    if (state.tileTempState.get(x, y)->shaking)
                        spriteBatch.r(Textures.white).pos(new Vec2f(x, y) * Constants.tileSize).size(new Vec2f(10)).color(Color.Red).draw();
                }
            }
        }

        static void drawSegmentDebugOverlay(MySpriteBatch spriteBatch, Tilemap state)
        {
            foreach (var segment in GameLogic.calculateSegments(state))
            {
                int segmentId = state.get(segment.First())->segmentId;
                if (segmentId == 0)
                    continue;

                Random r = new Random(segmentId);
                Color c = new Color();
                c.R = (byte) (r.Next() & 0x000000FF);
                c.G = (byte) (r.Next() & 0x000000FF);
                c.B = (byte) (r.Next() & 0x000000FF);
                c.A = 255;

                foreach (Vec2i p in segment)
                {
                    spriteBatch.r(Textures.white).pos(p.f() * Constants.tileSize).size(new Vec2f(10)).color(c).draw();
                    Textures.debugFont.draw("" + segmentId, p * Constants.tileSize, spriteBatch);
                }
            }
        }

        static void drawTileIdentityDebugOverlay(MySpriteBatch spriteBatch, Tilemap state)
        {
            for (int y = 0; y < state.dimensions.y; y++)
            {
                for (int x = 0; x < state.dimensions.x; x++)
                {
                    int identity = state.get(x, y)->tileIdentity;

                    if (identity != 0)
                        Textures.debugFont.draw("" + identity, new Vec2i(x, y) * Constants.tileSize, spriteBatch);
                }
            }
        }

        static void drawFixedSegmentsDebugOverlay(MySpriteBatch spriteBatch, Tilemap state, Vec2i direction)
        {
            List<GameLogic.Segment> segments = GameLogic.calculateSegments(state);
            HashSet<int> fixedSegments = GameLogic.calculateFixedSegments(state, segments, direction);

            for (int i = 0; i < segments.Count; i++)
            {
                if (fixedSegments.Contains(i))
                    continue;

                foreach (Vec2i p in segments[i])
                    spriteBatch.r(Textures.white).pos(new Vec2f(p.x, p.y) * Constants.tileSize).size(new Vec2f(5)).color(new Color(1f,0f,0f,1f)).draw();
            }
        }

        static void renderLevel(MySpriteBatch spriteBatch, long gameTimeMs, Tilemap previous, Tilemap state, float stateLerpAlpha, bool shake)
        {
            for (int backgroundLayer = state.backgroundLayerCount - 1; backgroundLayer >= 0; backgroundLayer--)
            {
                for (int y = 0; y < state.dimensions.y; y++)
                {
                    for (int x = 0; x < state.dimensions.x; x++)
                    {
                        float a = 0.25f;
                        Render.renderTile(spriteBatch, gameTimeMs, getRenderTileId(state, x, y, backgroundLayer + 1), new Vec2i(x, y), new Color(1.0f, 1.0f, 1.0f, a));
                        Render.renderTile(spriteBatch, gameTimeMs, new RenderTileId(Constants.oneTileDither[backgroundLayer]), new Vec2i(x, y), Constants.levelBackgroundColor);
                    }
                }
            }

            var oldTilesByIdentity = new Dictionary<int, Vec2f>();
            for (int y = 0; y < previous.dimensions.y; y++)
            {
                for (int x = 0; x < previous.dimensions.x; x++)
                {
                    Tile* tile = previous.get(x, y);
                    if (tile->tileIdentity != 0)
                        oldTilesByIdentity[tile->tileIdentity] = new Vec2f(x, y);
                }
            }

            foreach (var pair in state.removedTilesAnimationPoints)
            {
                int identity = pair.Key;
                if (!oldTilesByIdentity.ContainsKey(identity))
                    continue;
                Vec2f oldPos = oldTilesByIdentity[identity];
                Vec2f newPos = pair.Value.f();

                Vec2f tilePos = oldPos * (1.0f - stateLerpAlpha) + newPos * stateLerpAlpha;
                Render.renderTile(spriteBatch, gameTimeMs, getRenderTileId(previous, (int)oldPos.x, (int)oldPos.y), tilePos);
            }

            for (int y = 0; y < state.dimensions.y; y++)
            {
                for (int x = 0; x < state.dimensions.x; x++)
                {
                    Vec2f tilePos = new Vec2f(x, y);
                    int identity = state.get(x, y)->tileIdentity;
                    if (identity != 0 && oldTilesByIdentity.ContainsKey(identity))
                    {
                        Vec2f oldPos = oldTilesByIdentity[identity];
                        tilePos = oldPos * (1.0f - stateLerpAlpha) + tilePos * stateLerpAlpha;
                    }

                    if (shake)
                    {
                        double t = gameTimeMs / 5.0;
                        tilePos += new Vec2f((float) Math.Sin(t), (float) Math.Cos(t / 2.0)) / 10f;
                    }
                    else if (state.tileTempState.get(x, y)->shaking)
                    {
                        double t = gameTimeMs / 20.0;
                        tilePos += new Vec2f((float) Math.Sin(t), (float) Math.Cos(t / 2.0)) / 100f;
                        tilePos *= Constants.tileSize;
                        tilePos.x = MathF.Floor(tilePos.x);
                        tilePos.y = MathF.Floor(tilePos.y);
                        tilePos /= Constants.tileSize;
                    }

                    RenderTileId renderTile = getRenderTileId(state, x, y);
                    if (!Constants.diamondIds.Contains(renderTile.val))
                    {
                        Render.renderTile(spriteBatch, gameTimeMs, renderTile, tilePos);
                    }
                    else if (renderTile.val == Constants.diamondIds[0])
                    {
                        Vec2f pixelPos = tilePos * Constants.tileSize;
                        spriteBatch.r(Textures.diamond.getCurrentFrame(gameTimeMs))
                                   .pos(pixelPos)
                                   .draw();
                    }

                    if (state.tileTempState.get(x, y)->fireDirection != FireDirection.NoFire)
                        Render.renderTile(spriteBatch, gameTimeMs * 5, getFireRenderTile(state, x, y), tilePos);
                }
            }
        }

        static void renderPlayer(MySpriteBatch spriteBatch, long gameTimeMs, Tilemap previous, Tilemap state, float stateLerpAlpha)
        {
            Vec2f oldPosition = new Vec2f(previous.playerPosition.x, previous.playerPosition.y);
            Vec2f newPosition = new Vec2f(state.playerPosition.x, state.playerPosition.y);
            Vec2f position = oldPosition * (1.0f - stateLerpAlpha) + newPosition * stateLerpAlpha;

            Tilemap animationSource = stateLerpAlpha > 0.5f ? state : previous;
            Animation animation = Textures.playerIdle;
            bool flip = false;
            Grip grip = GameLogic.getGrip(animationSource);
            float rotation = 0f;
            bool animate = true;

            if (state.dead)
            {
                animation = Textures.playerIdle;
                animate = false;

                long frameIntervalMs = (long)(1000 * 0.2f);
                long totalAnimationLength = frameIntervalMs * 4;
                int frame = (int)((gameTimeMs % totalAnimationLength) / frameIntervalMs);
                rotation = 90 * frame;
            }
            else if (state.digDirection != Direction.None && stateLerpAlpha < 1.0f)
            {
                animation = Textures.playerDrill;

                switch (state.digDirection)
                {
                    case Direction.Right:
                        rotation = 0;
                        break;
                    case Direction.Down:
                        rotation = 90;
                        break;
                    case Direction.Left:
                        rotation = 180;
                        break;
                    case Direction.Up:
                        rotation = 270;
                        break;
                }
            }
            else if (grip.left && grip.right)
            {
                animation = Textures.playerHangInPipe;
            }
            else
            {
                if (grip.onSolidGround)
                {
                    if (grip.beside)
                    {
                        if (grip.left)
                            flip = true;
                        animation = Textures.playerStandAndHang;
                    }
                }
                else
                {
                    if (grip.beside)
                    {
                        if (grip.left)
                            flip = true;
                        animation = Textures.playerHangBeside;
                    }
                    else if (grip.below)
                    {
                        if (grip.belowLeft && grip.belowRight)
                        {
                            animation = Textures.playerHangOverPipe;
                        }
                        else
                        {
                            if (grip.belowLeft)
                                flip = true;
                            animation = Textures.playerHangAbove;
                        }
                    }
                }
            }

            Vec2f drawPosition = (position + new Vec2f(-1f)) * Constants.tileSize;

            spriteBatch.r(animation.getCurrentFrame(animate ? gameTimeMs : 0))
                       .pos(drawPosition)
                       .size(animation.size().f())
                       .rotate(rotation)
                       .flipHorizontal(flip)
                       .draw();
        }

        static RenderTileId getFireRenderTile(Tilemap state, int x, int y)
        {
            string getBaseKey(int xx, int yy)
            {
                if (state.tileTempState.isPointValid(xx, yy) && state.tileTempState.get(xx, yy)->fireDirection != FireDirection.NoFire)
                    return "1";

                return "0";
            }

            static string getFireKey(FireDirection fireDirection)
            {
                switch (fireDirection)
                {
                    case FireDirection.Up:
                        return "u";
                    case FireDirection.Down:
                        return "d";
                    case FireDirection.Left:
                        return "l";
                    case FireDirection.Right:
                        return "r";
                    case FireDirection.NoDirection:
                        return "n";
                }

                Util.ReleaseAssert(false);
                return "";
            }

            string baseKey = getBaseKey(x, y - 1) + getBaseKey(x, y + 1) + getBaseKey(x - 1, y) + getBaseKey(x + 1, y);
            string fireKey = getFireKey(state.tileTempState.get(x, y)->fireDirection);

            if (fireTransitions.ContainsKey(baseKey + fireKey))
                return fireTransitions[baseKey + fireKey];

            return fireTransitions[baseKey];
        }

        static TransitionMap createFireTransitionMap()
        {
            RenderTileId getId(int x, int y)
            {
                int tilesetWidth = Textures.tileset.Width / Constants.tileSize;
                return new RenderTileId(y * tilesetWidth + x);
            }

            Vec2i b = Render.getCoordsInTileset(new RenderTileId(Constants.fireTileId));

            return new TransitionMap()
            {
                //UDLR
                {"0000", getId(b.x + 0, b.y + 0)},

                {"0001l", getId(b.x + 0, b.y + 2)},
                {"0001",  getId(b.x + 4, b.y + 0)},

                {"0010r", getId(b.x + 4, b.y + 2)},
                {"0010",  getId(b.x + 4, b.y + 1)},

                {"0011l", getId(b.x + 1, b.y + 2)},
                {"0011r", getId(b.x + 3, b.y + 2)},
                {"0011",  getId(b.x + 0, b.y + 3)},

                {"0100u", getId(b.x + 2, b.y + 0)},
                {"0100",  getId(b.x + 3, b.y + 0)},

                {"0101",  getId(b.x + 3, b.y + 3)},

                {"0110",  getId(b.x + 4, b.y + 3)},

                {"0111",  getId(b.x + 0, b.y + 0)},

                {"1000d", getId(b.x + 2, b.y + 4)},
                {"1000",  getId(b.x + 3, b.y + 1)},

                {"1001",  getId(b.x + 3, b.y + 4)},

                {"1010",  getId(b.x + 4, b.y + 4)},

                {"1011",  getId(b.x + 1, b.y + 0)},

                {"1100u", getId(b.x + 2, b.y + 1)},
                {"1100d", getId(b.x + 2, b.y + 3)},
                {"1100",  getId(b.x + 1, b.y + 3)},

                {"1101",  getId(b.x + 0, b.y + 1)},

                {"1110",  getId(b.x + 1, b.y + 1)},

                {"1111",  getId(b.x + 2, b.y + 2)},
            };
        }

        static TransitionMap createTransitionMap(RenderTileId baseId)
        {
            RenderTileId getId(int x, int y)
            {
                int tilesetWidth = Textures.tileset.Width / Constants.tileSize;
                return new RenderTileId(y * tilesetWidth + x);
            }

            Vec2i b = Render.getCoordsInTileset(baseId);

            // baseId is the "standalone" block in the tileset, this offset gives us the top left corner
            b.x -= 4;
            b.y -= 1;

            return new TransitionMap()
            {
                //UDLR
                {"0000", getId(b.x + 4, b.y + 1)},
                {"0001", getId(b.x + 4, b.y + 2)},
                {"0010", getId(b.x + 6, b.y + 2)},
                {"0011", getId(b.x + 5, b.y + 2)},
                {"0100", getId(b.x + 3, b.y + 0)},
                {"0101", getId(b.x + 0, b.y + 0)},
                {"0110", getId(b.x + 2, b.y + 0)},
                {"0111", getId(b.x + 1, b.y + 0)},
                {"1000", getId(b.x + 3, b.y + 2)},
                {"1001", getId(b.x + 0, b.y + 2)},
                {"1010", getId(b.x + 2, b.y + 2)},
                {"1011", getId(b.x + 1, b.y + 2)},
                {"1100", getId(b.x + 3, b.y + 1)},
                {"1101", getId(b.x + 0, b.y + 1)},
                {"1110", getId(b.x + 2, b.y + 1)},
                {"1111", getId(b.x + 1, b.y + 1)},
            };
        }

        public static bool randomiseDirt = false;
        static RenderTileId getRenderTileId(Tilemap state, int x, int y, int layer = 0)
        {
            Tile* original = state.get(x, y, layer);

            if (original->overrideRenderId != 0)
                return new RenderTileId(original->overrideRenderId);

            int lookup = original->tileId.val;
            if (Constants.rockTileIdSet.Contains(lookup))
                lookup = Constants.rockGraphicalBaseTile;

            var thisTileTransitions = tileTransitions.GetValueOrDefault(lookup);
            if (thisTileTransitions == null)
                return new RenderTileId(original->tileId.val);

            string get(int xx, int yy)
            {
                if (xx < 0 || xx >= state.dimensions.x || yy < 0 || yy >= state.dimensions.y)
                    return "1";

                if (GameLogic.compatible(original, state.get(xx, yy, layer)))
                    return "1";

                return "0";
            }

            string key = get(x, y - 1) + get(x, y + 1) + get(x - 1, y) + get(x + 1, y);

            // This is turned off for the game because it looks bad when dirt changes shape after being dug.
            // Looks good when it's still though
            if (randomiseDirt && original->tileId == Constants.dirtTileId && key == "1111")
            {
                int hash = original->tileIdentity.ToString().GetHashCode();
                if (hash % 2 == 0)
                    return new RenderTileId(30);
            }

            return thisTileTransitions[key];
        }
    }
}
