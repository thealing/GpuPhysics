namespace Simulator.Backend;

using ILGPU;
using ILGPU.Runtime;

public static class AcceleratorExtension
{
	public static MemoryBuffer1D<TValue, Stride1D.Dense> AllocateZeroed<TValue>(this Accelerator accelerator, long size)
		where TValue : unmanaged
	{
		MemoryBuffer1D<TValue, Stride1D.Dense> buffer = accelerator.Allocate1D<TValue>(size);
		buffer.MemSetToZero();
		return buffer;
	}

	public static ArrayView<TValue> AllocateZeroedView<TValue>(this Accelerator accelerator, long size)
		where TValue : unmanaged
	{
		MemoryBuffer1D<TValue, Stride1D.Dense> buffer = accelerator.AllocateZeroed<TValue>(size);
		return buffer.View;
	}
}
