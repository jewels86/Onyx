using System.Collections.Concurrent;
using System.Runtime.Serialization;

namespace Onyx.Runtime;

public delegate void Agent(AgentContext ctx);

public class Core
{
    public Core() { }

    public async Task Start(Agent[] agents, CoreParameters parameters)
    {
        object[] tagTypeObjects = parameters.TagTypeObjects;
        
        Thread[] threads = new Thread[agents.Count()];
        HashSet<Type> tagTypes = tagTypeObjects.Select(o => o.GetType()).ToHashSet();
        ConcurrentBag<Contract> contracts = [];

        Func<object, bool> isTagType = tag => tagTypes.Any(type => type.IsInstanceOfType(tag));
        Action<Contract> extend = contract =>
        {
            if (isTagType(contract.Tag)) contracts.Add(contract);
        };

        for (int i = 0; i < agents.Count(); i++)
        {
            AgentContext ctx = new()
            {
                Extend = extend,
                Check = contract =>
                {
                    IEnumerable<Contract> matching = contracts.Where(c => c.ReceivingAgent == contract.ReceivingAgent);
                    if (matching.Count() != 1)
                    {
                        // Contacts need to be refactored to allow parameters
                    }
                }
            };
            Agent agent = agents[i];
            threads[i] = new Thread(() => agent(ctx));
        }

        foreach (Thread thread in threads) thread.Start();

        while (threads.Any(thread => thread.IsAlive))
        {
            
        }
    }
}

public readonly struct CoreParameters
{
    public Action<byte[], byte[], object> MultipleContractsError { get; init; }
    public object[] TagTypeObjects { get; init; }
}