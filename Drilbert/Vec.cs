using System;

namespace Drilbert
{
    public struct Vec2f
    {
        public float x;
        public float y;

        public Vec2f(float x, float y)
        {
            this.x = x;
            this.y = y;
        }

        public Vec2f(double x, double y)
        {
            this.x = (float)x;
            this.y = (float)y;
        }

        public Vec2f(float both)
        {
            x = both;
            y = both;
        }

        public Vec2f(double both)
        {
            x = (float)both;
            y = (float)both;
        }

        public Vec2f(Vec2i vec) : this(vec.x, vec.y) {}

        public float magnitudeSquared()
        {
            return x*x + y*y;
        }

        public float magnitude()
        {
            return (float)Math.Sqrt(magnitudeSquared());
        }

        public void normalise()
        {
            float mag = magnitude();
            x = x / mag;
            y = y / mag;
        }

        public Vec2f normalised()
        {
            Vec2f retval = this;
            retval.normalise();
            return retval;
        }

        public float toAngleDegrees()
        {
            float angle = MathF.Atan2(y, x) * (180.0f / MathF.PI);
            if (angle < 0)
                angle = 360.0f + angle;
            return angle;
        }

        public Vec2f rounded()
        {
            return new Vec2f(MathF.Round(x), MathF.Round(y));
        }

        public static Vec2f operator+(Vec2f a, Vec2f b) => new Vec2f(a.x + b.x, a.y + b.y);
        public static Vec2f operator-(Vec2f a, Vec2f b) => new Vec2f(a.x - b.x, a.y - b.y);
        public static Vec2f operator*(Vec2f a, float b) => new Vec2f(a.x * b, a.y * b);
        public static Vec2f operator/(Vec2f a, float b) => new Vec2f(a.x / b, a.y / b);

        public static Vec2f operator+(Vec2f a, Vec2i b) => new Vec2f(a.x + b.x, a.y + b.y);
        public static Vec2f operator-(Vec2f a, Vec2i b) => new Vec2f(a.x + b.x, a.y + b.y);

        // ReSharper disable CompareOfFloatsByEqualityOperator
        public static bool operator==(Vec2f a, Vec2f b) => a.x == b.x && a.y == b.y;
        public static bool operator!=(Vec2f a, Vec2f b) => a.x != b.x || a.y != b.y;
        // ReSharper restore CompareOfFloatsByEqualityOperator

        public override bool Equals(object other)
        {
            if (!(other is Vec2f))
                return false;

            return Equals((Vec2f)other);
        }

        public bool Equals(Vec2f other) => this == other;

        public override int GetHashCode() => x.GetHashCode() * 397 ^ y.GetHashCode();
    }

    public struct Vec2i
    {
        public int x;
        public int y;

        public Vec2i(int x, int y)
        {
            this.x = x;
            this.y = y;
        }

        public Vec2i(int both)
        {
            x = both;
            y = both;
        }

        public Vec2i(double x, double y)
        {
            this.x = (int)x;
            this.y = (int)y;
        }

        public Vec2i(double both)
        {
            x = (int)both;
            y = (int)both;
        }

        public Vec2i(Vec2f vec): this(vec.x, vec.y) {}

        public Vec2f f() { return new Vec2f(this); }

        public static Vec2i operator+(Vec2i a, Vec2i b) => new Vec2i(a.x + b.x, a.y + b.y);
        public static Vec2i operator-(Vec2i a, Vec2i b) => new Vec2i(a.x - b.x, a.y - b.y);
        public static Vec2i operator*(Vec2i a, int b) => new Vec2i(a.x * b, a.y * b);
        public static Vec2i operator/(Vec2i a, int b) => new Vec2i(a.x / b, a.y / b);

        public static bool operator==(Vec2i a, Vec2i b) => a.x == b.x && a.y == b.y;
        public static bool operator!=(Vec2i a, Vec2i b) => a.x != b.x || a.y != b.y;

        public override bool Equals(object other)
        {
            if (!(other is Vec2i))
                return false;

            return Equals((Vec2i)other);
        }

        public bool Equals(Vec2i other) => this == other;

        public override int GetHashCode() => (17 * 23 + x.GetHashCode()) * 23 + y.GetHashCode();
    }

    public struct Rect
    {
        public float x, y, w, h;

        public Vec2f topLeft { get => new Vec2f(x, y); set { x = value.x; y = value.y; }}
        public Vec2f topRight => new Vec2f(x + w, y);
        public Vec2f bottomLeft => new Vec2f(x, y + h);
        public Vec2f bottomRight => new Vec2f(x + w, y + h);

        public Vec2f size => new Vec2f(w, h);

        public Rect(float x, float y, float w, float h) { this.x = x; this.y = y; this.w = w; this.h = h; }
        public Rect(Vec2f topLeft, float w, float h) : this(topLeft.x, topLeft.y, w, h) {}
    }

    public static class Extensions
    {
        public static Rect f(this Microsoft.Xna.Framework.Rectangle r) { return new Rect(r.X, r.Y, r.Width, r.Height); }
    }
}