namespace Simulator.Backend.Gpu;

using ILGPU;
using ILGPU.Runtime;
using Simulator.Core;
using Simulator.Engine.Collisions;
using Simulator.Engine.Collisions.BroadPhase;

public struct DynamicGridStorage : IDynamicGridStorage
{
	public int ObjectCount { get; set; }
	public ArrayView<Bound> Bounds;
	public ArrayView<Node> Nodes;
	public ArrayView<int> SizeFlags;
	public Map<DynamicGridKey, DynamicGridValue, MapStorage<DynamicGridKey, DynamicGridValue>> Map;

	public DynamicGridStorage(Accelerator accelerator)
	{
		Bounds = accelerator.AllocateZeroedView<Bound>(0);
		Nodes = accelerator.AllocateZeroedView<Node>(0);
		SizeFlags = accelerator.AllocateZeroedView<int>(DynamicGridKey.MaxSize);
		Map.Storage = new MapStorage<DynamicGridKey, DynamicGridValue>(accelerator);
	}

	public void CopyFromCPU(Cpu.DynamicGridStorage dynamicGridStorage)
	{
		ObjectCount = dynamicGridStorage.ObjectCount;
		Bounds.SafeCopyFromCPU(dynamicGridStorage.Bounds);
		Nodes.SafeCopyFromCPU(dynamicGridStorage.Nodes);
		SizeFlags.SafeCopyFromCPU(dynamicGridStorage.SizeFlags);
		Map.Storage.CopyFromCPU(dynamicGridStorage.Map.Storage);
	}

	public void CopyToCPU(Cpu.DynamicGridStorage dynamicGridStorage)
	{
		Bounds.CopyToCPU(dynamicGridStorage.Bounds);
		Nodes.CopyToCPU(dynamicGridStorage.Nodes);
		SizeFlags.CopyToCPU(dynamicGridStorage.SizeFlags);
		Map.Storage.CopyToCPU(dynamicGridStorage.Map.Storage);
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

	public bool GetOrInsert(DynamicGridKey key, ref DynamicGridValue value)
	{
		return Map.GetOrInsert(key, ref value);
	}

	public void Clear(IExecutor executor)
	{
		SizeFlags.MemSetToZero();
		Map.Clear(executor);
	}
}
