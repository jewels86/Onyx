using System.Collections.Concurrent;
using System.Runtime.Serialization;
namespace Onyx.Defense;

public delegate void Agent(AgentContext ctx);

public class Runtime
{
    // We assume the Runtime is created in a safe environment; before any possible arbitrary code execution.
    // This means we can safely use properties and fields without worrying about malicious code altering them.
    
    public List<Agent> Agents { get; set; } = new();
    
    public Runtime() { }

    public void Start()
    {
        
    }
}