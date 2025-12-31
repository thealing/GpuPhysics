namespace Simulator.Backend.Cpu;

using System;
using Simulator.Engine.Core;

public struct MapStorage<TKey, TValue> : IMapStorage<TKey, TValue>
	where TKey : unmanaged, IEquatable<TKey>
	where TValue : unmanaged
{
	public int Size => Keys.Length;
	public TKey[] Keys;
	public TValue[] Values;
	public int[] Flags;

	public MapStorage()
	{
		Keys = Array.Empty<TKey>();
		Values = Array.Empty<TValue>();
		Flags = Array.Empty<int>();
	}

	public void SetCapacity(int capacity)
	{
		if (capacity <= Size)
		{
			return;
		}
		Array.Resize(ref Keys, capacity);
		Array.Resize(ref Values, capacity);
		Array.Resize(ref Flags, capacity);
	}

	public TKey GetKey(int index)
	{
		return Keys[index];
	}

	public void SetKey(int index, TKey key)
	{
		Keys[index] = key;
	}

	public TValue GetValue(int index)
	{
		return Values[index];
	}

	public void SetValue(int index, TValue value)
	{
		Values[index] = value;
	}

	public int GetFlag(int index)
	{
		return Flags[index];
	}

	public void SetFlag(int index, int flag)
	{
		Flags[index] = flag;
	}

	public int CompareExchangeFlag(int index, int comparand, int value)
	{
		Atomic atomic = new Atomic();
		return atomic.CompareExchange(ref Flags[index], comparand, value);
	}
}
