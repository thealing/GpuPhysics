namespace Simulator.Engine.Physics.Simulation;

public static class ContactHelper
{
	public static Vector GetEffectiveVelocity(Twist velocity, Vector lever)
	{
		Vector linear = velocity.Linear;
		Vector angular = Vector.Cross(velocity.Angular, lever);
		return linear + angular;
	}

	public static float GetEffectiveInverseMass(InverseMass inverseMass, Vector lever, Vector direction)
	{
		Vector tangent = Vector.Cross(lever, direction);
		float linear = inverseMass.Linear;
		float angular = Vector.Dot(tangent, inverseMass.Angular * tangent);
		return linear + angular;
	}

	public static Twist GetEffectiveImpulse(Vector lever, Vector impulse)
	{
		Vector linear = impulse;
		Vector angular = Vector.Cross(lever, impulse);
		return new Twist(linear, angular);
	}
}
