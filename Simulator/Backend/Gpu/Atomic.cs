namespace Simulator.Backend.Gpu;

using Simulator.Engine.Core;

public readonly struct Atomic : IAtomic
{
	public int CompareExchange(ref int location, int comparand, int value)
	{
		return ILGPU.Atomic.CompareExchange(ref location, comparand, value);
	}
}
