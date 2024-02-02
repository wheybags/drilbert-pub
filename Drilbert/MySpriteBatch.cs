using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Drilbert
{
    public class MySpriteBatch
    {
        private SpriteBatch spriteBatch;
        public MySpriteBatch(GraphicsDevice graphicsDevice)
        {
            spriteBatch = new SpriteBatch(graphicsDevice);
        }

        public void Begin(SpriteSortMode sortMode = SpriteSortMode.Deferred,
                          BlendState blendState = null,
                          SamplerState samplerState = null,
                          DepthStencilState depthStencilState = null,
                          RasterizerState rasterizerState = null,
                          Effect effect = null,
                          Matrix? transformMatrix = null)
        {
            spriteBatch.Begin(sortMode, blendState, samplerState, depthStencilState, rasterizerState, effect, transformMatrix);
        }

        public void End()
        {
            spriteBatch.End();
        }

        public DrawBuilder r(Texture2D tex) { return new DrawBuilder(this, tex); }

        public struct DrawBuilder
        {
            private MySpriteBatch spriteBatch;
            private Texture2D tex;
            private Rect destination;
            private Rect sourceUvs;
            private Color _color;
            private Vec2f origin;
            private float rotateDegrees;

            public DrawBuilder(MySpriteBatch spriteBatch, Texture2D tex)
            {
                this.spriteBatch = spriteBatch;
                this.tex = tex;
                destination = new Rect(0, 0, tex.Width, tex.Height);
                sourceUvs = new Rect(0, 0, tex.Width, tex.Height);
                _color = Color.White;
                origin = new Vec2f();
                rotateDegrees = 0;
            }

            public DrawBuilder pos(Vec2f pos)
            {
                destination.topLeft += pos;
                return this;
            }

            public DrawBuilder pos(Vec2i pos)
            {
                return this.pos(pos.f());
            }

            public DrawBuilder pos(float x, float y)
            {
                return pos(new Vec2f(x, y));
            }

            public DrawBuilder scale(Vec2f scale)
            {
                destination.w *= scale.x;
                destination.h *= scale.y;
                return this;
            }

            public DrawBuilder scale(float scale)
            {
                destination.w *= scale;
                destination.h *= scale;
                return this;
            }

            public DrawBuilder size(Vec2f dims)
            {
                destination.w = dims.x;
                destination.h = dims.y;
                return this;
            }

            public DrawBuilder uv(Rect uv)
            {
                sourceUvs = uv;
                return this;
            }

            public DrawBuilder offsetUv(Vec2f offset)
            {
                sourceUvs.topLeft += offset;
                return this;
            }

            public DrawBuilder color(Color color)
            {
                _color = color;
                return this;
            }

            public DrawBuilder rotate(float degrees, Vec2f origin)
            {
                rotateDegrees = degrees;
                this.origin = origin;
                destination.topLeft += origin;
                return this;
            }

            public DrawBuilder rotate(float degrees)
            {
                rotate(degrees, destination.size / 2);
                return this;
            }

            public DrawBuilder flipHorizontal(bool active)
            {
                if (!active)
                    return this;
                sourceUvs = new Rect(sourceUvs.topRight, -sourceUvs.w, sourceUvs.h);
                return this;
            }

            public void draw()
            {
                // undo the weird scaling that SpriteBatch does to origin
                Vector2 fixedOrigin = new Vector2(origin.x, origin.y);
                if(sourceUvs.w != 0)
                    fixedOrigin.X = fixedOrigin.X / (float)destination.w * (float)sourceUvs.w;
                else
                    fixedOrigin.X = fixedOrigin.X / (float)destination.w / tex.TexelWidth;
                if(sourceUvs.h != 0)
                    fixedOrigin.Y = fixedOrigin.Y / (float)destination.h * (float)sourceUvs.h;
                else
                    fixedOrigin.Y = fixedOrigin.Y / (float)destination.h / tex.TexelHeight;


                spriteBatch.spriteBatch.Draw(tex,
                                             new Vector2(destination.x, destination.y), new Vector2(destination.w, destination.h),
                                             new Vector2(sourceUvs.x, sourceUvs.y), new Vector2(sourceUvs.w, sourceUvs.h),
                                             _color,
                                             MathHelper.ToRadians(rotateDegrees),
                                             fixedOrigin,
                                             SpriteEffects.None,
                                             0);
            }
        }
    }
}