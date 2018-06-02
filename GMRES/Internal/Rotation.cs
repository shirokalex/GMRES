using System;

namespace GMRES.Internal
{
    internal struct Rotation
    {
        public static Rotation CreateRotationToMakeYZero(double x, double y)
        {
            if (Math.Abs(x) < double.Epsilon)
                return new Rotation(0.0, 1.0);

            var r = Math.Sqrt(x * x + y * y);
            var cos = Math.Abs(x) / r;
            var sin = cos * y / x;
            return new Rotation(cos, sin);
        }

        public Rotation(double cos, double sin)
        {
            Cos = cos;
            Sin = sin;
        }

        public double Cos { get; }
        public double Sin { get; }
    }
}