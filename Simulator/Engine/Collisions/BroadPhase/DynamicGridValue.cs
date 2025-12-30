namespace Simulator.Engine.Collisions.BroadPhase;

public readonly struct DynamicGridValue
{
	public readonly int NodeIndex;

	public DynamicGridValue(int nodeIndex)
	{
		NodeIndex = nodeIndex;
	}
}
