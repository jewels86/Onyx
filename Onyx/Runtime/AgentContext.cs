namespace Onyx.Runtime;

public class AgentContext
{
    public required Action<Contract> Extend { get; init; }
    public required Func<Contract, bool> Check { get; init; }
    
    public AgentContext Clone()
    {
        return new AgentContext()
        {
            Extend = Extend,
            Check = Check
        };
    }
}