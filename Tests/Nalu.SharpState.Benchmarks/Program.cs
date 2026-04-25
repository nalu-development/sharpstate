using BenchmarkDotNet.Running;

namespace Nalu.SharpState.Benchmarks;

public static class Program
{
    public static void Main(string[] args) => BenchmarkRunner.Run(typeof(StateMachineBenchmark).Assembly, null, args);
}
