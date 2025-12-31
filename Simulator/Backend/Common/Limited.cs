namespace Simulator.Backend.Common;

using Simulator.Engine.Core;

public static class Limited
{
	public static int Increment<TAtomic>(TAtomic atomic, ref int location, int limit)
		where TAtomic : IAtomic
	{
		int index = location;
		while (true)
		{
			if (index >= limit)
			{
				return -1;
			}
			int value = index;
			index = atomic.CompareExchange(ref location, value, value + 1);
			if (index == value)
			{
				return index;
			}
		}
	}
}
