namespace Onyx.Runtime;

public readonly struct Contract
{
    public required byte[] OfferingAgent { get; init; }
    public required byte[] ReceivingAgent { get; init; }
    public required object Tag { get; init; }
}