namespace Simulator.Backend.Cpu;

using System;
using System.Runtime.CompilerServices;

public unsafe readonly struct UnsafeSpan<T>
	where T : unmanaged
{
	public int Length => _length;

	public UnsafeSpan(ReadOnlySpan<T> span)
	{
		ref T tRef = ref Unsafe.AsRef(in span[0]);
		_items = (T*)Unsafe.AsPointer(ref tRef);
		_length = span.Length;
	}

	public ref T this[int index]
	{
		get
		{
			return ref _items[index];
		}
	}

	private readonly T* _items;
	private readonly int _length;
}
