using System;
using System.Diagnostics;
using System.Linq;
using CommandLine;
using MathNet.Numerics.Data.Text;

namespace GMRES.ConsoleApp
{
    public class Options
    {
        [Option('a', "input-a", Default = "a.mtx", HelpText = "Input file path of coefficient matrix (A). Matrix Market format (*.mtx)")]
        public string InputMatrixFileName { get; set; }

        [Option('b', "input-b", Default = "b.mtx", HelpText = "Input file path of free coefficients vector (b). Matrix Market format (*.mtx)")]
        public string InputVectorFileName { get; set; }

        [Option('o', "output", Default = "x.mtx", HelpText = "Output file path of solution vector. Matrix Market format (*.mtx)")]
        public string OutputFileName { get; set; }

        [Option("inner-iter", Default = null, HelpText = "Max inner iterations")]
        public int? MaxInnerIterations { get; set; }

        [Option("outer-iter", Default = null, HelpText = "Max outer iterations")]
        public int? MaxOuterIterations { get; set; }

        [Option('e', "epsilon", Default = GmresSolver.DefaultEpsilon, HelpText = "Convergence threshold")]
        public double Epsilon { get; set; }

        [Option('n', Default = GmresSolver.DefaultDegreeOfParallelism, HelpText = "Degree of parallelism (thread count)")]
        public int DegreeOfParallelism { get; set; }
    }

    public static class Program
    {
        private static void Main(string[] args)
        {
            try
            {
                Parser.Default.ParseArguments<Options>(args).WithParsed(Execute);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        public static void Execute(Options options)
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            Console.WriteLine($"Reading from {options.InputMatrixFileName}...");
            var a = MatrixMarketReader.ReadMatrix<double>(options.InputMatrixFileName);

            Console.WriteLine($"Reading from {options.InputVectorFileName}...");
            var b = MatrixMarketReader.ReadVector<double>(options.InputVectorFileName);

            Console.WriteLine("Start solving...");
            var result = GmresSolver.Solve(a, b, options.MaxInnerIterations, options.MaxOuterIterations, options.Epsilon, degreeOfParallelism: options.DegreeOfParallelism);

            Console.WriteLine($"Finished. IsConverged = {result.IsConverged}. Last error: {result.Errors.Last()}");
            Console.WriteLine($"Performed {result.InnerIterations} inner iterations and {result.OuterIterations} outer iterations");

            Console.WriteLine($"Writing solution to {options.OutputFileName}...");
            MatrixMarketWriter.WriteVector(options.OutputFileName, result.X);

            Console.WriteLine($"Total execution time, ms: {stopwatch.Elapsed.TotalMilliseconds}");
        }
    }
}