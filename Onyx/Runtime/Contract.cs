namespace Onyx.Runtime;

public readonly struct Contract<T> where T : struct
{
    public required byte[] OfferingAgent { get; init; }
    public required byte[] ReceivingAgent { get; init; }
    public required object Tag { get; init; }
    public required T Parameters { get; init; }
}