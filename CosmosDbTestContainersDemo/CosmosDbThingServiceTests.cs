using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Azure.CosmosRepository;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Configurations;
using DotNet.Testcontainers.Containers;

namespace IntegrationTests.Features.Thing;

public class CosmosDbThingServiceTests : CosmosDbFixture
{
    private readonly List<string> _createdThingIds = new();
    private CosmosDbThingService? _cosmosDbThingService;

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();

        if (this.Container?.ConnectionString is null)
            throw new Exception("Connection string cannot be null.");

        var cosmosThingRepository =
            new ServiceCollection()
                .AddSingleton<IConfiguration>(new ConfigurationBuilder().Build())
                .AddOptions()
                .AddCosmosRepository(
                    options =>
                    {
                        options.CosmosConnectionString = this.Container.ConnectionString;
                        options.ContainerId = "thing-store";
                        options.DatabaseId = "main";
                        options.ContainerPerItemType = true;
                        options.ContainerBuilder.Configure<CosmosDbThing>(containerOptions => containerOptions
                            .WithContainer("things")
                            .WithPartitionKey("/id")
                            .WithSyncableContainerProperties()
                        );
                    })
                .BuildServiceProvider()
                .GetRequiredService<IRepository<CosmosDbThing>>();

        _cosmosDbThingService = new CosmosDbThingService(cosmosThingRepository);
    }

    public override async Task DisposeAsync()
    {
        await base.DisposeAsync();

        foreach (var createdThingId in _createdThingIds)
        {
            await _cosmosDbThingService!.DeleteAsync(createdThingId);
        }
    }

    [Fact]
    public async Task CreateCosmosDbThingAsync_CreatesCosmosDbThing()
    {
        // Arrange
        var thing = new CosmosDbThing { Id = Guid.NewGuid().ToString() };

        // Act
        var success = await _cosmosDbThingService!.CreateCosmosDbThingAsync(thing);
        _createdThingIds.Add(thing.Id);

        // Assert
        Assert.True(success);
    }
}

public class CosmosDbFixture : DatabaseFixture<CosmosDbTestcontainer>
{
    private readonly CosmosDbTestcontainerConfiguration configuration = new CosmosDbTestcontainerConfiguration();

    private readonly CancellationTokenSource cts = new CancellationTokenSource(TimeSpan.FromMinutes(5));

    public CosmosDbFixture()
    {
        this.Container = new TestcontainersBuilder<CosmosDbTestcontainer>()
          .WithDatabase(this.configuration)
          .WithWaitStrategy(Wait.ForUnixContainer())
          .Build();
    }

    public override Task InitializeAsync()
    {
        return this.Container.StartAsync(this.cts.Token);
    }

    public override Task DisposeAsync()
    {
        return this.Container.DisposeAsync().AsTask();
    }

    public override void Dispose()
    {
        this.configuration.Dispose();
    }
}

public abstract class DatabaseFixture<TDockerContainer> : IAsyncLifetime, IDisposable
  where TDockerContainer : ITestcontainersContainer
{
    public TDockerContainer Container { get; protected set; }

    public abstract Task InitializeAsync();

    public abstract Task DisposeAsync();

    public abstract void Dispose();
}