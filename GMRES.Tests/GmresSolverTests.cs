using System;
using System.Collections.Generic;
using System.IO;
using FluentAssertions;
using MathNet.Numerics.Data.Text;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Double;
using NUnit.Framework;

namespace GMRES.Tests
{
    [TestFixture]
    internal class GmresSolverTests
    {
        private const double Epsilon = 1e-10;
        private const int MaxInnerIterations = 100;
        private const int MaxOuterIterations = 100;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            Directory.SetCurrentDirectory(AppDomain.CurrentDomain.BaseDirectory);
        }

        [TestCase(2, 3)]
        [TestCase(3, 2)]
        public void Should_throw_argument_exception_when_matrix_is_not_square(int rowCount, int columnCount)
        {
            var a = new SparseMatrix(rowCount, columnCount);
            var b = new DenseVector(rowCount);
            Assert.Throws<ArgumentException>(() => GmresSolver.Solve(a, b));
        }

        [TestCaseSource(nameof(GetTestInputs))]
        public void Should_solve(Matrix<double> a, Vector<double> b)
        {
            var result = GmresSolver.Solve(a, b, MaxInnerIterations, MaxOuterIterations, Epsilon);

            result.IsConverged.Should().BeTrue();
            CheckSolution(a, b, result.X);
        }

        [Test]
        public void Should_solve_tridiagonal_matrix_system()
        {
            var a = MatrixMarketReader.ReadMatrix<double>("matrix.mtx");
            var b = MatrixMarketReader.ReadVector<double>("vector.mtx");

            var result = GmresSolver.Solve(a, b, 15, 1);
            result.IsConverged.Should().BeTrue();
            result.OuterIterations.Should().Be(0);
            result.InnerIterations.Should().Be(11);
            foreach (var xValue in result.X)
            {
                xValue.Should().BeApproximately(1.0, GmresSolver.DefaultEpsilon);
            }
        }

        private void CheckSolution(Matrix<double> a, Vector<double> b, Vector<double> x)
        {
            x.Should().HaveCount(b.Count);

            var z = a.Multiply(x);

            for (var i = 0; i < b.Count; i++)
            {
                z[i].Should().BeApproximately(b[i], 2 * Epsilon);
            }
        }

        private static IEnumerable<TestCaseData> GetTestInputs()
        {
            var unitMatrix = Matrix<double>.Build.SparseIdentity(100);
            var unitVector = Vector<double>.Build.Dense(unitMatrix.RowCount, 1.0);

            yield return new TestCaseData(unitMatrix, unitVector).SetName("Unit matrix");
            yield return new TestCaseData(unitMatrix.Multiply(Math.PI), unitVector).SetName("Scaled unit matrix");
            yield return new TestCaseData(GetPoissonMatrix(), unitVector).SetName("Poisson matrix");

            var orders = new[] {4, 8};
            foreach (var order in orders)
            {
                var a = Matrix<double>.Build.Random(order, order, 1);
                var b = Vector<double>.Build.Random(order, 1);
                yield return new TestCaseData(a, b).SetName($"Random matrix of order {order}");
            }
        }

        private static Matrix<double> GetPoissonMatrix()
        {
            var matrix = Matrix<double>.Build.Sparse(100, 100);

            const int gridSize = 10; // Assemble the matrix. We assume we're solving the Poisson equation on a rectangular 10 x 10 grid

            // The pattern is: 0 .... 0 -1 0 0 0 0 0 0 0 0 -1 4 -1 0 0 0 0 0 0 0 0 -1 0 0 ... 0
            for (var i = 0; i < matrix.RowCount; i++)
            {
                // Insert the first set of -1's
                if (i > gridSize - 1)
                    matrix[i, i - gridSize] = -1;

                // Insert the second set of -1's
                if (i > 0)
                    matrix[i, i - 1] = -1;

                // Insert the centerline values
                matrix[i, i] = 4;

                // Insert the first trailing set of -1's
                if (i < matrix.RowCount - 1)
                    matrix[i, i + 1] = -1;

                // Insert the second trailing set of -1's
                if (i < matrix.RowCount - gridSize)
                    matrix[i, i + gridSize] = -1;
            }

            return matrix;
        }
    }
}