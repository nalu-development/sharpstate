namespace Nalu.SharpState.Benchmarks;

internal static class BenchmarkServiceProviders
{
    private sealed class EmptyServiceProvider : IServiceProvider
    {
        public static readonly EmptyServiceProvider Instance = new();

        private EmptyServiceProvider()
        {
        }

        public object? GetService(Type serviceType) => null;
    }

    private sealed class EmptyServiceProviderResolver : IStateMachineServiceProviderResolver<IServiceProvider>
    {
        public static readonly EmptyServiceProviderResolver Instance = new();

        private EmptyServiceProviderResolver()
        {
        }

        public IServiceProvider GetServiceProvider() => EmptyServiceProvider.Instance;

        public IDisposable CreateScopedServiceProvider(out IServiceProvider serviceProvider)
        {
            serviceProvider = EmptyServiceProvider.Instance;
            return NoopDisposable.Instance;
        }

        private sealed class NoopDisposable : IDisposable
        {
            public static readonly NoopDisposable Instance = new();

            private NoopDisposable()
            {
            }

            public void Dispose()
            {
            }
        }
    }

    public static readonly IStateMachineServiceProviderResolver<IServiceProvider> EmptyResolver =
        EmptyServiceProviderResolver.Instance;
}
