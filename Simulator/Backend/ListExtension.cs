namespace Simulator.Backend;

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

public static class ListExtension
{
	public static Span<T> SubView<T>(this List<T> list, int startIndex, int endIndex)
	{
		Span<T> span = CollectionsMarshal.AsSpan(list);
		return span[startIndex..endIndex];
	}

	public static ref T GetMutableItem<T>(this List<T> list, int index)
	{
		Span<T> span = CollectionsMarshal.AsSpan(list);
		return ref span[index];
	}
}
