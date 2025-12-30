namespace Simulator.Backend.Gpu;

using System;
using ILGPU;
using ILGPU.Runtime;
using Simulator.Core;

public struct MapStorage<TKey, TValue> : IMapStorage<TKey, TValue>
	where TKey : unmanaged, IEquatable<TKey>
	where TValue : unmanaged
{
	public int Size { get; set; }
	public ArrayView<TKey> Keys;
	public ArrayView<TValue> Values;
	public ArrayView<int> Flags;

	public MapStorage(Accelerator accelerator)
	{
		Keys = accelerator.AllocateZeroedView<TKey>(0);
		Values = accelerator.AllocateZeroedView<TValue>(0);
		Flags = accelerator.AllocateZeroedView<int>(0);
	}

	public void CopyFromCPU(Cpu.MapStorage<TKey, TValue> mapStorage)
	{
		Size = mapStorage.Size;
		Keys.SafeCopyFromCPU(mapStorage.Keys);
		Values.SafeCopyFromCPU(mapStorage.Values);
		Flags.SafeCopyFromCPU(mapStorage.Flags);
	}

	public void CopyToCPU(Cpu.MapStorage<TKey, TValue> mapStorage)
	{
		Keys.CopyToCPU(mapStorage.Keys);
		Values.CopyToCPU(mapStorage.Values);
		Flags.CopyToCPU(mapStorage.Flags);
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
