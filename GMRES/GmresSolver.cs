using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GMRES.Internal;
using MathNet.Numerics;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Double;

namespace GMRES
{
    public static class GmresSolver
    {
        public const double DefaultEpsilon = 1e-6;

        public static GmresResult Solve(Matrix<double> A, Vector<double> b, int? maxInnerIterations = null, int? maxOuterIterations = null, double epsilon = DefaultEpsilon, Vector<double> x0 = null, int? degreeOfParallelism = null)
        {
            Validate(A, b, ref maxInnerIterations, ref maxOuterIterations, ref degreeOfParallelism);
            x0 = x0 ?? new DenseVector(Enumerable.Repeat(0.0, b.Count).ToArray());

            Control.MaxDegreeOfParallelism = degreeOfParallelism.Value;

            var x = x0.Clone();
            var errors = new List<double>();
            for (var i = 0; i < maxOuterIterations; i++)
            {
                var iterationResult = MakeInnerIterations(A, b, x, maxInnerIterations.Value, epsilon, degreeOfParallelism.Value);
                x = iterationResult.x;
                errors.AddRange(iterationResult.Errors);

                if (iterationResult.IsConverged)
                    return new GmresResult(x, true, i, iterationResult.InnerIterations, errors.ToArray());
            }
            return new GmresResult(x, false, maxOuterIterations.Value, maxInnerIterations.Value, errors.ToArray());
        }

        private static void Validate(Matrix<double> A, Vector<double> b, ref int? maxInnerIterations, ref int? maxOuterIterations, ref int? degreeOfParallelism)
        {
            if (A.RowCount != A.ColumnCount)
                throw new ArgumentException("Coeffitient matrix must be square");
            if (A.RowCount != b.Count)
                throw new ArgumentException("Coeffitient matrix and vector must have same dimentions");

            maxInnerIterations = maxInnerIterations ?? Math.Min(b.Count, 10);
            if (maxInnerIterations < 1)
                maxInnerIterations = 1;
            if (maxInnerIterations > A.RowCount)
                maxInnerIterations = A.RowCount;

            maxOuterIterations = maxOuterIterations ?? Math.Min(b.Count / maxInnerIterations.Value, 10);
            if (maxInnerIterations < 1)
                maxOuterIterations = 1;

            degreeOfParallelism = degreeOfParallelism ?? Control.MaxDegreeOfParallelism;
        }

        private static (Vector<double> x, bool IsConverged, int InnerIterations, List<double> Errors) MakeInnerIterations(Matrix<double> A, Vector<double> b, Vector<double> x0, int maxInnerIterations, double epsilon, int degreeOfParallelism)
        {
            var n = A.RowCount;
            var m = maxInnerIterations;
            var r0 = b - A * x0;

            var bNorm = b.Norm(2);
            var error = r0.Norm(2) / bNorm;
            var errors = new List<double> {error};
            var rotations = new Rotation[m];

            if (error <= epsilon)
                return (x0.Clone(), true, 0, errors);

            var r0Norm = r0.Norm(2);
            var Q = new DenseMatrix(n, m + 1);
            Q.SetColumn(0, r0 / r0Norm);
            var beta = new DenseVector(n + 1) {[0] = r0Norm}; // Use n+1 instead of n - to hold beta[k+1]

            var H = new DenseMatrix(m + 1, m);

            for (var k = 0; k < m; k++)
            {
                var (hColumn, q) = MakeArnoldiIteration(A, Q, k + 1, degreeOfParallelism);
                Q.SetColumn(k + 1, 0, q.Count, q);

                var (rotatedHColumn, rotation) = ApplyGivenRotationsToHColumn(hColumn, rotations, k);
                H.SetColumn(k, 0, rotatedHColumn.Count, rotatedHColumn);
                rotations[k] = rotation;

                beta[k + 1] = -rotation.Sin * beta[k];
                beta[k] = rotation.Cos * beta[k];
                error = Math.Abs(beta[k + 1]) / bNorm;
                errors.Add(error);

                if (error <= epsilon)
                {
                    return (GetSolution(H, Q, x0, beta, k + 1), true, k + 1, errors);
                }
            }

            return (GetSolution(H, Q, x0, beta, m), false, m, errors);
        }

        private static Vector<double> GetSolution(Matrix<double> H, Matrix<double> Q, Vector<double> x0, Vector<double> beta, int k)
        {
            var y = H.SubMatrix(0, k, 0, k).Solve(beta.SubVector(0, k));
            var x = x0 + Q.SubMatrix(0, Q.RowCount, 0, k) * y;
            return x;
        }

        private static (Vector<double> h, Vector<double> q) MakeArnoldiIteration(Matrix<double> A, Matrix<double> Q, int k, int degreeOfParallelism)
        {
            var h = new DenseVector(k + 1);
            var q = A * Q.Column(k - 1);

            var parallelOptions = new ParallelOptions {MaxDegreeOfParallelism = degreeOfParallelism};
            Parallel.For(0, k, parallelOptions, i =>
            {
                var qi = Q.Column(i);
                h[i] = q * qi;
                q.Subtract(h[i] * qi, q);
            });

            var qNorm = q.Norm(2);
            h[k] = qNorm;
            q = q / qNorm;

            return (h, q);
        }

        private static (Vector<double> h, Rotation rotation) ApplyGivenRotationsToHColumn(Vector<double> h, IReadOnlyList<Rotation> rotations, int k)
        {
            for (var i = 0; i < k; i++)
            {
                var temp = rotations[i].Cos * h[i] + rotations[i].Sin * h[i + 1];
                h[i + 1] = -rotations[i].Sin * h[i] + rotations[i].Cos * h[i + 1];
                h[i] = temp;
            }

            var newRotation = Rotation.CreateRotationToMakeYZero(h[k], h[k + 1]);

            h[k] = newRotation.Cos * h[k] + newRotation.Sin * h[k + 1];
            h[k + 1] = 0.0;

            return (h, newRotation);
        }
    }
}