namespace Simulator.Core;

public interface IAtomic
{
	public int CompareExchange(ref int location, int comparand, int value);
}
