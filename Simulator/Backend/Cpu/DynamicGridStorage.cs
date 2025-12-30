namespace Simulator.Backend.Cpu;

using System;
using Simulator.Core;
using Simulator.Engine.Collisions;
using Simulator.Engine.Collisions.BroadPhase;

public struct DynamicGridStorage : IDynamicGridStorage
{
	public int ObjectCount { get; set; }
	public Bound[] Bounds;
	public Node[] Nodes;
	public int[] SizeFlags;
	public Map<DynamicGridKey, DynamicGridValue, MapStorage<DynamicGridKey, DynamicGridValue>> Map;

	public DynamicGridStorage()
	{
		Bounds = Array.Empty<Bound>();
		Nodes = Array.Empty<Node>();
		SizeFlags = new int[DynamicGridKey.MaxSize];
		Map.Storage = new MapStorage<DynamicGridKey, DynamicGridValue>();
	}

	public void SetCapacity(int capacity)
	{
		if (capacity <= Bounds.Length)
		{
			return;
		}
		Array.Resize(ref Bounds, capacity);
		Array.Resize(ref Nodes, capacity);
		Map.Storage.SetCapacity(capacity * 2);
	}

	public Bound GetBound(int index)
	{
		return Bounds[index];
	}

	public void SetBound(int index, Bound bound)
	{
		Bounds[index] = bound;
	}

	public Node GetNode(int index)
	{
		return Nodes[index];
	}

	public void SetNode(int index, Node node)
	{
		Nodes[index] = node;
	}

	public void InsertNodeAtomic(int index, int headIndex)
	{
		Atomic atomic = new Atomic();
		Nodes[index] = Node.Insert(atomic, ref Nodes[headIndex], index);
	}

	public bool GetSizeFlag(int size)
	{
		return SizeFlags[size] != 0;
	}

	public void SetSizeFlag(int size, bool flag)
	{
		SizeFlags[size] = flag ? 1 : 0;
	}

	public void Insert(DynamicGridKey key, DynamicGridValue value)
	{
		Map.Insert(key, value);
	}

	public bool Get(DynamicGridKey key, ref DynamicGridValue value)
	{
		return Map.Get(key, ref value);
	}

	public void Clear(IExecutor executor)
	{
		Array.Fill(SizeFlags, 0);
		Map.Clear(executor);
	}
}
