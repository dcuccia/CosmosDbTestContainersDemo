using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Azure.CosmosRepository;

namespace IntegrationTests.Features.Thing;

public class CosmosDbThing : Item { }

public class CosmosDbThingService
{
    private readonly IRepository<CosmosDbThing> _repository;

    public CosmosDbThingService(IRepository<CosmosDbThing> repository)
    {
        _repository = repository;
    }

    public async ValueTask<bool> CreateCosmosDbThingAsync(CosmosDbThing thing)
    {
        if (await _repository.ExistsAsync(thing.Id))
            return false;

        return await _repository.CreateAsync(thing) is { };
    }

    public async ValueTask<bool> UpdateAsync(CosmosDbThing thing)
    {
        if (await _repository.ExistsAsync(thing.Id) is false)
            return false;

        return await _repository.UpdateAsync(thing) is { };
    }

    public async ValueTask<bool> DeleteAsync(string id)
    {
        if (await _repository.ExistsAsync(id))
            return false;

        await _repository.DeleteAsync(id);
        return true;
    }

    public async ValueTask<CosmosDbThing?> GetByIdAsync(string id)
        => await _repository.GetAsync(id);
}