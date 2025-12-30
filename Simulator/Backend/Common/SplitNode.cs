namespace Simulator.Backend.Common;

using Simulator.Core;
using Simulator.Engine.Physics;

public struct SplitContact
{
	public Node Node;
	public Twist Impulse;

	public SplitContact(Node node, Twist impulse)
	{
		Node = node;
		Impulse = impulse;
	}
}