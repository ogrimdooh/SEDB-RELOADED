using System;
using VRage.Utils;
using VRageMath;

namespace SEDiscordBridge.Extensions
{
    public static class VectorUtils
    {
        const double _eps = 1E-4;
        public const double PiOver3 = Math.PI / 3;
        public const double PiOver6 = Math.PI / 6;

        public static double GetAngleBetween(Vector3D a, Vector3D b)
        {
            if (Vector3D.IsZero(a, _eps) || Vector3D.IsZero(b, _eps))
                return 0;

            if (IsUnitVector(a) && IsUnitVector(b))
                return Math.Acos(MathHelperD.Clamp(a.Dot(b), -1, 1));

            return Math.Acos(MathHelperD.Clamp(a.Dot(b) / Math.Sqrt(a.LengthSquared() * b.LengthSquared()), -1, 1));
        }

        public static Vector3D Project(Vector3D a, Vector3D b)
        {
            if (Vector3D.IsZero(a, _eps) || Vector3D.IsZero(b))
                return Vector3D.Zero;

            if (IsUnitVector(b))
                return a.Dot(b) * b;

            return a.Dot(b) / b.LengthSquared() * b;
        }

        public static bool IsUnitVector(Vector3D v)
        {
            double num = 1.0 - v.LengthSquared();
            return Math.Abs(num) < _eps;
        }

        public static Vector3D RandomPerpendicular(Vector3D referenceDir)
        {

            var refDir = Vector3D.Normalize(referenceDir);
            return Vector3D.Normalize(MyUtils.GetRandomPerpendicularVector(ref refDir));

        }

        public static Vector2 GetMultiplier(this Vector2 self, float multiplier)
        {
            return new Vector2(self.X * multiplier, self.Y * multiplier);
        }

        public static float GetRandom(this Vector2 self)
        {
            return MyUtils.GetRandomFloat(self.X, self.Y);
        }

        public static Vector2I GetMultiplier(this Vector2I self, float multiplier)
        {
            return new Vector2I((int)(self.X * multiplier), (int)(self.Y * multiplier));
        }

        public static float GetRandom(this Vector2I self)
        {
            return MyUtils.GetRandomFloat(self.X, self.Y);
        }

        public static int GetRandomInt(this Vector2I self)
        {
            return MyUtils.GetRandomInt(self.X, self.Y);
        }

    }

}