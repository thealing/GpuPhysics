namespace Simulator.Engine.Core;

public struct Node
{
	public int NextIndex;

	public Node(int nextIndex)
	{
		NextIndex = nextIndex;
	}

	public static Node Insert<TAtomic>(TAtomic atomic, ref Node headNode, int newFirstIndex)
		where TAtomic : IAtomic
	{
		ref int firstIndex = ref headNode.NextIndex;
		while (true)
		{
			int oldFirstIndex = firstIndex;
			if (atomic.CompareExchange(ref firstIndex, oldFirstIndex, newFirstIndex) == oldFirstIndex)
			{
				return new Node(oldFirstIndex);
			}
		}
	}
}
