using MikroTikBackup.Core.Interfaces;
using MikroTikBackup.Core.Models;
using MikroTikBackup.Core.Services;

namespace Core.Tests;

public sealed class BackupRunnerTests
{
    [Fact]
    public async Task RunAsync_SingleRouter_ReturnsSuccessfulResult()
    {
        var router =
            CreateRouter("office-01");

        var expectedResult =
            CreateResult(
                router.Name,
                BackupStatus.Success);

        var credentialStore =
            new FakeCredentialStore();

        var factory =
            new FakeBackupServiceFactory(
                new Dictionary<string, RouterBackupResult>
                {
                    [router.Name] = expectedResult
                });

        var runner =
            new BackupRunner(
                factory,
                credentialStore);

        var config =
            new AppConfig();

        var result =
            await runner.RunAsync(
                [router],
                config,
                CancellationToken.None);

        Assert.Single(result.Results);
        Assert.Equal(
            BackupStatus.Success,
            result.Results[0].Status);
        Assert.Equal(
            router.Name,
            result.Results[0].Router);
        Assert.Equal(
            1,
            result.SuccessCount);
        Assert.Equal(
            0,
            result.WarningCount);
        Assert.Equal(
            0,
            result.FailedCount);
    }

    [Fact]
    public async Task RunAsync_MultipleRouters_ProcessesAllRouters()
    {
        var routers =
            new[]
            {
                CreateRouter("office-01"),
                CreateRouter("office-02"),
                CreateRouter("warehouse-01")
            };

        var results =
            routers.ToDictionary(
                router => router.Name,
                router => CreateResult(
                    router.Name,
                    BackupStatus.Success));

        var credentialStore =
            new FakeCredentialStore();

        var factory =
            new FakeBackupServiceFactory(
                results);

        var runner =
            new BackupRunner(
                factory,
                credentialStore);

        var result =
            await runner.RunAsync(
                routers,
                new AppConfig(),
                CancellationToken.None);

        Assert.Equal(
            3,
            result.Results.Count);

        Assert.Equal(
            3,
            result.SuccessCount);

        Assert.Equal(
            0,
            result.FailedCount);

        Assert.Equal(
            3,
            factory.CreatedServices.Count);
    }

    [Fact]
    public async Task RunAsync_PassesCorrectCredentialReference()
    {
        var router =
            CreateRouter(
                "office-01",
                "router-admin");

        var credentialStore =
            new FakeCredentialStore();

        var factory =
            new FakeBackupServiceFactory(
                new Dictionary<string, RouterBackupResult>
                {
                    [router.Name] =
                        CreateResult(
                            router.Name,
                            BackupStatus.Success)
                });

        var runner =
            new BackupRunner(
                factory,
                credentialStore);

        await runner.RunAsync(
            [router],
            new AppConfig(),
            CancellationToken.None);

        Assert.Single(
            credentialStore.RequestedReferences);

        Assert.Equal(
            "router-admin",
            credentialStore.RequestedReferences[0]);
    }

    [Fact]
    public async Task RunAsync_PassesCredentialToBackupService()
    {
        var router =
            CreateRouter("office-01");

        var credential =
            new Credential(
                "admin",
                "secret");

        var credentialStore =
            new FakeCredentialStore(
                credential);

        var factory =
            new FakeBackupServiceFactory(
                new Dictionary<string, RouterBackupResult>
                {
                    [router.Name] =
                        CreateResult(
                            router.Name,
                            BackupStatus.Success)
                });

        var runner =
            new BackupRunner(
                factory,
                credentialStore);

        await runner.RunAsync(
            [router],
            new AppConfig(),
            CancellationToken.None);

        var service =
            Assert.Single(
                factory.CreatedServices);

        Assert.Equal(
            credential,
            service.ReceivedCredentials[0]);
    }

    [Fact]
    public async Task RunAsync_PassesRouterAndConfigToBackupService()
    {
        var router =
            CreateRouter("office-01");

        var config =
            new AppConfig
            {
                Storage =
                    new StorageConfig
                    {
                        Path = @"D:\MikroTik\Backups"
                    }
            };

        var factory =
            new FakeBackupServiceFactory(
                new Dictionary<string, RouterBackupResult>
                {
                    [router.Name] =
                        CreateResult(
                            router.Name,
                            BackupStatus.Success)
                });

        var runner =
            new BackupRunner(
                factory,
                new FakeCredentialStore());

        await runner.RunAsync(
            [router],
            config,
            CancellationToken.None);

        var service =
            Assert.Single(
                factory.CreatedServices);

        Assert.Same(
            router,
            service.ReceivedRouters[0]);

        Assert.Same(
            config,
            service.ReceivedConfigs[0]);
    }

    [Fact]
    public async Task RunAsync_BackupServiceReturnsWarning_PreservesWarningResult()
    {
        var router =
            CreateRouter("office-01");

        var expectedResult =
            CreateResult(
                router.Name,
                BackupStatus.SuccessWithWarning);

        var factory =
            new FakeBackupServiceFactory(
                new Dictionary<string, RouterBackupResult>
                {
                    [router.Name] = expectedResult
                });

        var runner =
            new BackupRunner(
                factory,
                new FakeCredentialStore());

        var result =
            await runner.RunAsync(
                [router],
                new AppConfig(),
                CancellationToken.None);

        Assert.Single(result.Results);

        Assert.Equal(
            BackupStatus.SuccessWithWarning,
            result.Results[0].Status);

        Assert.Equal(
            1,
            result.WarningCount);

        Assert.True(
            result.IsSuccessful);
    }

    [Fact]
    public async Task RunAsync_BackupServiceThrows_ReturnsFailedResult()
    {
        var router =
            CreateRouter("office-01");

        var factory =
            new FakeBackupServiceFactory(
                new Dictionary<string, RouterBackupResult>(),
                new InvalidOperationException(
                    "Backup failed."));

        var runner =
            new BackupRunner(
                factory,
                new FakeCredentialStore());

        var result =
            await runner.RunAsync(
                [router],
                new AppConfig(),
                CancellationToken.None);

        Assert.Single(result.Results);

        Assert.Equal(
            BackupStatus.Failed,
            result.Results[0].Status);

        Assert.Equal(
            router.Name,
            result.Results[0].Router);

        Assert.Equal(
            "Backup failed.",
            result.Results[0].Error);

        Assert.Equal(
            1,
            result.FailedCount);

        Assert.False(
            result.IsSuccessful);
    }

    [Fact]
    public async Task RunAsync_CredentialStoreThrows_ReturnsFailedResult()
    {
        var router =
            CreateRouter("office-01");

        var credentialStore =
            new FakeCredentialStore(
                exception:
                    new InvalidDataException(
                        "Credential not found."));

        var factory =
            new FakeBackupServiceFactory(
                new Dictionary<string, RouterBackupResult>());

        var runner =
            new BackupRunner(
                factory,
                credentialStore);

        var result =
            await runner.RunAsync(
                [router],
                new AppConfig(),
                CancellationToken.None);

        Assert.Single(result.Results);

        Assert.Equal(
            BackupStatus.Failed,
            result.Results[0].Status);

        Assert.Equal(
            "Credential not found.",
            result.Results[0].Error);

        Assert.Empty(
            factory.CreatedServices);
    }

    [Fact]
    public async Task RunAsync_OneRouterFails_ContinuesWithNextRouter()
    {
        var routers =
            new[]
            {
                CreateRouter("office-01"),
                CreateRouter("office-02")
            };

        var factory =
            new FakeBackupServiceFactory(
                new Dictionary<string, RouterBackupResult>
                {
                    ["office-02"] =
                        CreateResult(
                            "office-02",
                            BackupStatus.Success)
                },
                exceptionByRouter:
                new Dictionary<string, Exception>
                {
                    ["office-01"] =
                        new InvalidOperationException(
                            "First router failed.")
                });

        var runner =
            new BackupRunner(
                factory,
                new FakeCredentialStore());

        var result =
            await runner.RunAsync(
                routers,
                new AppConfig(),
                CancellationToken.None);

        Assert.Equal(
            2,
            result.Results.Count);

        Assert.Equal(
            1,
            result.FailedCount);

        Assert.Equal(
            1,
            result.SuccessCount);

        Assert.Equal(
            "office-01",
            result.Results[0].Router);

        Assert.Equal(
            BackupStatus.Failed,
            result.Results[0].Status);

        Assert.Equal(
            "office-02",
            result.Results[1].Router);

        Assert.Equal(
            BackupStatus.Success,
            result.Results[1].Status);
    }

    [Fact]
    public async Task RunAsync_CreatesSeparateBackupServiceForEachRouter()
    {
        var routers =
            new[]
            {
                CreateRouter("office-01"),
                CreateRouter("office-02"),
                CreateRouter("office-03")
            };

        var factory =
            new FakeBackupServiceFactory(
                routers.ToDictionary(
                    router => router.Name,
                    router => CreateResult(
                        router.Name,
                        BackupStatus.Success)));

        var runner =
            new BackupRunner(
                factory,
                new FakeCredentialStore());

        await runner.RunAsync(
            routers,
            new AppConfig(),
            CancellationToken.None);

        Assert.Equal(
            3,
            factory.CreatedServices.Count);

        Assert.NotSame(
            factory.CreatedServices[0],
            factory.CreatedServices[1]);

        Assert.NotSame(
            factory.CreatedServices[1],
            factory.CreatedServices[2]);

        Assert.NotSame(
            factory.CreatedServices[0],
            factory.CreatedServices[2]);
    }

    [Fact]
    public async Task RunAsync_EmptyRouterList_ReturnsEmptyResult()
    {
        var factory =
            new FakeBackupServiceFactory(
                new Dictionary<string, RouterBackupResult>());

        var runner =
            new BackupRunner(
                factory,
                new FakeCredentialStore());

        var result =
            await runner.RunAsync(
                [],
                new AppConfig(),
                CancellationToken.None);

        Assert.Empty(
            result.Results);

        Assert.Equal(
            0,
            result.Total);

        Assert.Equal(
            0,
            result.SuccessCount);

        Assert.Equal(
            0,
            result.WarningCount);

        Assert.Equal(
            0,
            result.FailedCount);

        Assert.True(
            result.IsSuccessful);

        Assert.Empty(
            factory.CreatedServices);
    }

    [Fact]
    public async Task RunAsync_CancellationBeforeStart_ThrowsOperationCanceledException()
    {
        var cancellationTokenSource =
            new CancellationTokenSource();

        cancellationTokenSource.Cancel();

        var router =
            CreateRouter("office-01");

        var runner =
            new BackupRunner(
                new FakeBackupServiceFactory(
                    new Dictionary<string, RouterBackupResult>()),
                new FakeCredentialStore());

        await Assert.ThrowsAsync<OperationCanceledException>(
            () =>
                runner.RunAsync(
                    [router],
                    new AppConfig(),
                    cancellationTokenSource.Token));
    }

    [Fact]
    public async Task RunAsync_CancellationBetweenRouters_StopsProcessing()
    {
        var cancellationTokenSource =
            new CancellationTokenSource();

        var routers =
            new[]
            {
                CreateRouter("office-01"),
                CreateRouter("office-02")
            };

        var credentialStore =
            new CancellingCredentialStore(
                cancellationTokenSource);

        var factory =
            new FakeBackupServiceFactory(
                new Dictionary<string, RouterBackupResult>
                {
                    ["office-01"] =
                        CreateResult(
                            "office-01",
                            BackupStatus.Success)
                });

        var runner =
            new BackupRunner(
                factory,
                credentialStore);

        await Assert.ThrowsAsync<OperationCanceledException>(
            () =>
                runner.RunAsync(
                    routers,
                    new AppConfig(),
                    cancellationTokenSource.Token));

        Assert.Single(
            factory.CreatedServices);
    }

    private static RouterConfig CreateRouter(
        string name,
        string credential = "default")
    {
        return new RouterConfig
        {
            Name = name,
            Address = "192.168.88.1",
            Protocol = "api",
            Port = 8728,
            TransferPort = 22,
            Credential = credential
        };
    }

    private static RouterBackupResult CreateResult(
        string router,
        BackupStatus status)
    {
        var now =
            DateTime.UtcNow;

        return new RouterBackupResult
        {
            Router = router,
            Status = status,
            StartedAt = now,
            CompletedAt = now
        };
    }

    private sealed class FakeCredentialStore : ICredentialStore
    {
        private readonly Credential _credential;
        private readonly Exception? _exception;

        public List<string> RequestedReferences { get; } = [];

        public FakeCredentialStore(
            Credential? credential = null,
            Exception? exception = null)
        {
            _credential =
                credential ??
                new Credential(
                    "admin",
                    "password");

            _exception = exception;
        }

        public Task<Credential> GetAsync(
            string reference,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            RequestedReferences.Add(
                reference);

            if (_exception != null)
            {
                throw _exception;
            }

            return Task.FromResult(
                _credential);
        }
    }

    private sealed class CancellingCredentialStore : ICredentialStore
    {
        private readonly CancellationTokenSource _source;
        private int _callCount;

        public CancellingCredentialStore(
            CancellationTokenSource source)
        {
            _source = source;
        }

        public Task<Credential> GetAsync(
            string reference,
            CancellationToken cancellationToken)
        {
            if (_callCount++ == 0)
            {
                return Task.FromResult(
                    new Credential(
                        "admin",
                        "password"));
            }

            _source.Cancel();

            cancellationToken.ThrowIfCancellationRequested();

            throw new InvalidOperationException(
                "Cancellation was expected.");
        }
    }

    private sealed class FakeBackupServiceFactory
        : IBackupServiceFactory
    {
        private readonly Dictionary<string, RouterBackupResult>
            _results;

        private readonly Exception? _globalException;

        private readonly Dictionary<string, Exception>
            _exceptionByRouter;

        public List<FakeBackupService> CreatedServices { get; } = [];

        public FakeBackupServiceFactory(
            Dictionary<string, RouterBackupResult> results,
            Exception? exception = null,
            Dictionary<string, Exception>? exceptionByRouter = null)
        {
            _results = results;
            _globalException = exception;
            _exceptionByRouter =
                exceptionByRouter ??
                new Dictionary<string, Exception>();
        }

        public IBackupService Create()
        {
            var service =
                new FakeBackupService(
                    _results,
                    _globalException,
                    _exceptionByRouter);

            CreatedServices.Add(
                service);

            return service;
        }
    }

    private sealed class FakeBackupService : IBackupService
    {
        private readonly Dictionary<string, RouterBackupResult>
            _results;

        private readonly Exception? _globalException;

        private readonly Dictionary<string, Exception>
            _exceptionByRouter;

        public List<RouterConfig> ReceivedRouters { get; } = [];

        public List<Credential> ReceivedCredentials { get; } = [];

        public List<AppConfig> ReceivedConfigs { get; } = [];

        public FakeBackupService(
            Dictionary<string, RouterBackupResult> results,
            Exception? globalException,
            Dictionary<string, Exception> exceptionByRouter)
        {
            _results = results;
            _globalException = globalException;
            _exceptionByRouter = exceptionByRouter;
        }

        public Task<RouterBackupResult> BackupRouterAsync(
            RouterConfig router,
            Credential credential,
            AppConfig config,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            ReceivedRouters.Add(router);
            ReceivedCredentials.Add(credential);
            ReceivedConfigs.Add(config);

            if (_globalException != null)
            {
                throw _globalException;
            }

            if (_exceptionByRouter.TryGetValue(
                    router.Name,
                    out var routerException))
            {
                throw routerException;
            }

            if (!_results.TryGetValue(
                    router.Name,
                    out var result))
            {
                throw new InvalidOperationException(
                    $"No fake result configured for router '{router.Name}'.");
            }

            return Task.FromResult(
                result);
        }
    }
}
