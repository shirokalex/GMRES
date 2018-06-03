using System;
using CommandLine;
using MathNet.Numerics.Data.Text;
using MathNet.Numerics.LinearAlgebra;

namespace MatrixMarketGenerator
{
    public class Options
    {
        [Option('s', "size", Required = true, HelpText = "Size of matrix or vector")]
        public int Size { get; set; }

        [Option('o', "output", Default = "x.mtx", HelpText = "Output file name. Matrix Market format (*.mtx)")]
        public string OutputFileName { get; set; }
    }

    [Verb("matrix", HelpText = "Generate random tridiagonal matrix")]
    public class MatrixOptions : Options
    {
    }

    [Verb("vector", HelpText = "Generate random vector")]
    public class VectorOptions : Options
    {
    }

    public static class Program
    {
        private static void Main(string[] args)
        {
            try
            {
                Parser.Default.ParseArguments<MatrixOptions, VectorOptions>(args)
                    .MapResult(
                        (MatrixOptions options) =>
                        {
                            ExecuteGenerateMatrix(options);
                            return 0;
                        },
                        (VectorOptions options) =>
                        {
                            ExecuteGenerateVector(options);
                            return 0;
                        },
                        errors => 1);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        private static void ExecuteGenerateMatrix(Options options)
        {
            var matrix = GenerateRandomTridiagonalMatrix(options.Size);
            MatrixMarketWriter.WriteMatrix(options.OutputFileName, matrix);
        }

        private static void ExecuteGenerateVector(Options options)
        {
            var vector = GenerateRandomVector(options.Size);
            MatrixMarketWriter.WriteVector(options.OutputFileName, vector);
        }

        private static Matrix<double> GenerateRandomTridiagonalMatrix(int size)
        {
            var mainDiagonal = GenerateRandomVector(size);
            var upperDiagonal = GenerateRandomVector(size - 1);
            var lowerDiagonal = GenerateRandomVector(size - 1);
            var matrix = Matrix<double>.Build.SparseOfDiagonalVector(mainDiagonal);

            for (var i = 0; i < size - 1; i++)
            {
                matrix[i, i + 1] = upperDiagonal[i];
                matrix[i + 1, i] = lowerDiagonal[i];
            }

            return matrix;
        }

        private static Vector<double> GenerateRandomVector(int size)
        {
            return Vector<double>.Build.Random(size);
        }
    }
}