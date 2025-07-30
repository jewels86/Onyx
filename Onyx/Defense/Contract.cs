namespace Onyx.Defense;

public readonly struct Contract<T> where T : Enum
{
    public required byte[] OfferingAgent { get; init; }
    public required byte[] ReceivingAgent { get; init; }
    public required T Tag { get; init; }
}

public readonly struct Event<T> where T : struct
{
    public required Contract<> Contract { get; init; }
    public required T Parameter { get; init; }
}