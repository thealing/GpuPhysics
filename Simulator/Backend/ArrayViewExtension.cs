namespace Simulator.Backend;

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using ILGPU;
using ILGPU.Runtime;

public static class ArrayViewExtension
{
	public static ArrayView<TValue> Slice<TValue>(this ArrayView<TValue> arrayView, int startIndex, int endIndex)
		where TValue : unmanaged
	{
		return arrayView.SubView(startIndex, endIndex - startIndex);
	}

	public static void SafeCopyFromCPU<TValue>(ref this ArrayView<TValue> arrayView, TValue[] array, int count)
		where TValue : unmanaged
	{
		if (arrayView.Length != array.Length)
		{
			Accelerator accelerator = arrayView.GetAccelerator();
			arrayView = accelerator.AllocateZeroedView<TValue>(array.Length);
		}
		arrayView.SubView(0, count).CopyFromCPU(array[..count]);
	}

	public static void SafeCopyFromCPU<TValue>(ref this ArrayView<TValue> arrayView, TValue[] array)
		where TValue : unmanaged
	{
		if (arrayView.Length != array.Length)
		{
			Accelerator accelerator = arrayView.GetAccelerator();
			arrayView = accelerator.AllocateZeroedView<TValue>(array.Length);
		}
		arrayView.CopyFromCPU(array);
	}

	public static void SafeCopyFromCPU<TValue>(ref this ArrayView<TValue> arrayView, List<TValue> list)
		where TValue : unmanaged
	{
		if (arrayView.Length != list.Capacity)
		{
			Accelerator accelerator = arrayView.GetAccelerator();
			arrayView = accelerator.AllocateZeroedView<TValue>(list.Capacity);
		}
		ReadOnlySpan<TValue> span = CollectionsMarshal.AsSpan(list);
		arrayView.SubView(0, list.Count).CopyFromCPU(span[..list.Count]);
	}

	public static void CopyToCPU<TValue>(ref this ArrayView<TValue> arrayView, TValue[] array, int count)
		where TValue : unmanaged
	{
		Span<TValue> span = array.AsSpan();
		arrayView.SubView(0, count).CopyToCPU(span[..count]);
	}

	public static void CopyToCPU<TValue>(ref this ArrayView<TValue> arrayView, List<TValue> list)
		where TValue : unmanaged
	{
		Span<TValue> span = CollectionsMarshal.AsSpan(list);
		arrayView.SubView(0, list.Count).CopyToCPU(span[..list.Count]);
	}
}
