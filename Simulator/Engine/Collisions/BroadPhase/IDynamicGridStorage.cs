namespace Simulator.Engine.Collisions.BroadPhase;

using Simulator.Core;

public interface IDynamicGridStorage : IMap<DynamicGridKey, DynamicGridValue>
{
	public int ObjectCount { get; }

	public Bound GetBound(int index);

	public void SetBound(int index, Bound bound);

	public Node GetNode(int index);

	public void SetNode(int index, Node node);

	public void InsertNodeAtomic(int index, int headIndex);

	public bool GetSizeFlag(int size);

	public void SetSizeFlag(int size, bool flag);
}
