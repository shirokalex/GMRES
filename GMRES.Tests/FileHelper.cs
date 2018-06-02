using System.Globalization;
using System.IO;
using System.Linq;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Double;

namespace GMRES.Tests
{
    public static class FileHelper
    {
        public static Matrix<double> LoadMatrixFromFile(string fileName)
        {
            return File.ReadAllLines(fileName)
                .Select(line => line.Split(' ')
                    .Where(str => !string.IsNullOrWhiteSpace(str))
                    .Select(str => double.Parse(str, CultureInfo.InvariantCulture))
                    .ToArray())
                .ToArray()
                .ToDenseMatrix();
        }

        public static Vector<double> LoadVectorFromFile(string fileName)
        {
            return File.ReadAllLines(fileName)
                .Select(line => line.Split(' ')
                    .Where(str => !string.IsNullOrWhiteSpace(str))
                    .Select(str => double.Parse(str, CultureInfo.InvariantCulture))
                    .First())
                .ToArray()
                .ToDenseVector();
        }

        private static Matrix<double> ToDenseMatrix(this double[][] jaggedArray)
        {
            var rows = jaggedArray.Length;
            var cols = jaggedArray.Max(subArray => subArray.Length);
            var matrix = new DenseMatrix(rows, cols);

            for (var i = 0; i < rows; i++)
            {
                cols = jaggedArray[i].Length;
                for (var j = 0; j < cols; j++)
                {
                    matrix[i, j] = jaggedArray[i][j];
                }
            }
            return matrix;
        }

        private static Vector<double> ToDenseVector(this double[] array)
        {
            return new DenseVector(array);
        }
    }
}