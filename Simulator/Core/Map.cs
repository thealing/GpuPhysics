namespace Simulator.Core;

using System;

public partial struct Map<TKey, TValue, TStorage> : IMap<TKey, TValue>
	where TKey : struct, IEquatable<TKey>
	where TValue : struct
	where TStorage : struct, IMapStorage<TKey, TValue>
{
	private const int Empty = 0;
	private const int Pending = 1;
	private const int Occupied = 2;

	public TStorage Storage;

	public Map()
	{
	}

	public void Insert(TKey key, TValue value)
	{
		int hash = Hash(key);
		while (true)
		{
			int index = GetIndex(hash, Storage.Size);
			if (Storage.GetFlag(index) == Empty)
			{
				if (Storage.CompareExchangeFlag(index, Empty, Occupied) == Empty)
				{
					Storage.SetKey(index, key);
					Storage.SetValue(index, value);
					break;
				}
			}
			Step(ref hash);
		}
	}

	public bool Get(TKey key, ref TValue value)
	{
		int hash = Hash(key);
		while (true)
		{
			int index = GetIndex(hash, Storage.Size);
			if (Storage.GetFlag(index) == Empty)
			{
				return false;
			}
			if (Storage.GetKey(index).Equals(key))
			{
				value = Storage.GetValue(index);
				return true;
			}
			Step(ref hash);
		}
	}

	public bool GetOrInsert(TKey key, ref TValue value)
	{
		int hash = Hash(key);
		while (true)
		{
			int index = GetIndex(hash, Storage.Size);
			int flag = Storage.GetFlag(index);
			if (flag == Empty)
			{
				flag = Storage.CompareExchangeFlag(index, Empty, Pending);
				if (flag == Empty)
				{
					Storage.SetKey(index, key);
					Storage.SetValue(index, value);
					Storage.SetFlag(index, Occupied);
					return false;
				}
			}
			while (flag == Pending)
			{
				flag = Storage.GetFlag(index);
			}
			if (Storage.GetKey(index).Equals(key))
			{
				value = Storage.GetValue(index);
				return true;
			}
			Step(ref hash);
		}
	}

	public void Clear(IExecutor executor)
	{
		ClearCommand command = new ClearCommand(this);
		executor.Execute(command, Storage.Size);
	}

	private void Clear(int index)
	{
		Storage.SetFlag(index, Empty);
	}

	private static int Hash(TKey key)
	{
		int hashCode = key.GetHashCode();
		return hashCode | 1;
	}

	private static int GetIndex(int hash, int size)
	{
		return (int)((uint)hash % (uint)size);
	}

	private static void Step(ref int hash)
	{
		hash ^= hash << 13;
		hash ^= hash >> 17;
		hash ^= hash << 5;
	}
}
