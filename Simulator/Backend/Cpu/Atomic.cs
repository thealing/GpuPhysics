namespace Simulator.Backend.Cpu;

using System.Threading;
using Simulator.Core;

public readonly struct Atomic : IAtomic
{
	public int CompareExchange(ref int location, int comparand, int value)
	{
		return Interlocked.CompareExchange(ref location, value, comparand);
	}
}
