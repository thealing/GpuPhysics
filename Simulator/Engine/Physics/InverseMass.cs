namespace Simulator.Engine.Physics;

public struct InverseMass
{
	public float Linear;
	public Matrix Angular;

	public InverseMass(float linear, Matrix angular)
	{
		Linear = linear;
		Angular = angular;
	}

	public static Twist operator *(InverseMass inverseMass, Twist impulse)
	{
		Vector linear = impulse.Linear * inverseMass.Linear;
		Vector angular = inverseMass.Angular * impulse.Angular;
		return new Twist(linear, angular);
	}
}
