namespace Simulator.Engine.Physics;

using Simulator.Engine;

public struct Material
{
	public float Restitution;
	public float StaticFriction;
	public float DynamicFriction;

	public Material(float restitution, float staticFriction, float dynamicFriction)
	{
		Restitution = restitution;
		StaticFriction = staticFriction;
		DynamicFriction = dynamicFriction;
	}

	public static Material Combine(Material a, Material b)
	{
		float restitution = RealMath.Max(a.Restitution, b.Restitution);
		float staticFriction = RealMath.Sqrt(a.StaticFriction * b.StaticFriction);
		float dynamicFriction = RealMath.Sqrt(a.DynamicFriction * b.DynamicFriction);
		return new Material(restitution, staticFriction, dynamicFriction);
	}
}
