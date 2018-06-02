using MathNet.Numerics.LinearAlgebra;

namespace GMRES
{
    public class GmresResult
    {
        public GmresResult(Vector<double> x, bool isConverged, int outerIterations, int innerIterations, double[] errors)
        {
            X = x;
            IsConverged = isConverged;
            OuterIterations = outerIterations;
            InnerIterations = innerIterations;
            Errors = errors;
        }

        public Vector<double> X { get; }
        public bool IsConverged { get; }
        public int OuterIterations { get; }
        public int InnerIterations { get; }
        public double[] Errors { get; }
    }
}