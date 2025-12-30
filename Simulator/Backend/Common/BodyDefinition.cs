namespace Simulator.Backend.Common;

using Simulator.Engine;
using Simulator.Engine.Geometry;
using Simulator.Engine.Physics;

public class BodyDefinition
{
	public Transform Transform;
	public Twist Velocity;

	public BodyDefinition()
	{
		Transform = new Transform(Vector.Zero, Quaternion.Identity);
		Velocity = new Twist();
	}
}
