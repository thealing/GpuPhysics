namespace Simulator.Engine.Physics;

public struct Twist
{
	public static readonly Twist Zero = new Twist(Vector.Zero, Vector.Zero);

	public Vector Linear;
	public Vector Angular;

	public Twist(Vector linear, Vector angular)
	{
		Linear = linear;
		Angular = angular;
	}

	public static Twist operator +(Twist a, Twist b)
	{
		Vector linear = a.Linear + b.Linear;
		Vector angular = a.Angular + b.Angular;
		return new Twist(linear, angular);
	}

	public static Twist operator *(Twist t, float s)
	{
		Vector linear = t.Linear * s;
		Vector angular = t.Angular * s;
		return new Twist(linear, angular);
	}
}
