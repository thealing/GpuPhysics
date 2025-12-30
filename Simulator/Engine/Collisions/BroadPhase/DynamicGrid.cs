namespace Simulator.Engine.Collisions.BroadPhase;

using Simulator.Core;

public partial struct DynamicGrid<TStorage> : ICollisionMap
	where TStorage : IDynamicGridStorage
{
	public TStorage Storage;

	public DynamicGrid(TStorage storage)
	{
		Storage = storage;
	}

	public void PrepareUpdate(IExecutor executor)
	{
		Storage.Clear(executor);
	}

	public void UpdateBound(int index, Bound bound)
	{
		Storage.SetBound(index, bound);
		int size = GetSize(bound);
		Storage.SetSizeFlag(size, true);
		Node node = new Node(-1);
		Storage.SetNode(index, node);
		DynamicGridKey key = new DynamicGridKey(bound.Lower, size);
		DynamicGridValue value = new DynamicGridValue(index);
		if (Storage.GetOrInsert(key, ref value))
		{
			Storage.InsertNodeAtomic(index, value.NodeIndex);
		}
	}

	public void DetectCollisions<TCollisionCallback>(IExecutor executor, TCollisionCallback callback)
		where TCollisionCallback : struct, ICollisionCallback
	{
		CollisionCommand<TCollisionCallback> command = new CollisionCommand<TCollisionCallback>(this, callback);
		executor.Execute(command, Storage.ObjectCount);
	}

	private void DetectCollisions<TCollisionCallback>(int index, TCollisionCallback callback)
		where TCollisionCallback : struct, ICollisionCallback
	{
		Bound bound = Storage.GetBound(index);
		if (false)
		{
			for (int otherIndex = index + 1; otherIndex < Storage.ObjectCount; otherIndex++)
			{
				Bound otherBound = Storage.GetBound(otherIndex);
				if (Bound.Intersect(bound, otherBound))
				{
					callback.ProcessCollision(index, otherIndex);
				}
			}
			return;
		}
		int minSize = GetSize(bound);
		for (int size = minSize; size < DynamicGridKey.MaxSize; size++)
		{
			if (Storage.GetSizeFlag(size) == false)
			{
				continue;
			}
			DynamicGridKey lowerKey = new DynamicGridKey(bound.Lower, size);
			DynamicGridKey upperKey = new DynamicGridKey(bound.Upper, size);
			for (int x = lowerKey.X - 1; x <= upperKey.X; x++)
			{
				for (int y = lowerKey.Y - 1; y <= upperKey.Y; y++)
				{
					for (int z = lowerKey.Z - 1; z <= upperKey.Z; z++)
					{
						DynamicGridKey key = new DynamicGridKey(x, y, z, size);
						DynamicGridValue value = new DynamicGridValue();
						if (Storage.Get(key, ref value) == false)
						{
							continue;
						}
						int otherIndex = value.NodeIndex;
						do
						{
							if (size > minSize || otherIndex > index)
							{
								Bound otherBound = Storage.GetBound(otherIndex);
								if (Bound.Intersect(bound, otherBound))
								{
									callback.ProcessCollision(index, otherIndex);
								}
							}
							Node otherNode = Storage.GetNode(otherIndex);
							otherIndex = otherNode.NextIndex;
						}
						while (otherIndex != -1);
					}
				}
			}
		}
	}

	private static int GetSize(Bound bound)
	{
		Vector size = bound.Upper - bound.Lower;
		float maxSize = RealMath.Max(size.X, size.Y, size.Z);
		maxSize = RealMath.Max(maxSize, 0);
		return 1 + RealMath.ILogB(maxSize + 1);
	}
}
