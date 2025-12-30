namespace Simulator.Engine.Physics.Simulation;

using Simulator.Engine.Collisions;

public struct Contact
{
	public Vector Normal;
	public float Depth;
	public Material Material;
	public Vector LeverA;
	public Vector LeverB;
	public float TargetNormalVelocity;
	public float NormalMass;
	public Vector Tangent;
	public float TangentMass;
	public float TotalNormalImpulse;
	public float TotalTangentImpulse;

	public Contact(Collision collision, Material material, Vector leverA, Vector leverB)
	{
		Normal = collision.Normal;
		Depth = collision.Depth;
		Material = material;
		LeverA = leverA;
		LeverB = leverB;
	}

	public void AddNormalImpulse(ref float impulse)
	{
		float oldImpulse = TotalNormalImpulse;
		float newImpulse = RealMath.Max(TotalNormalImpulse + impulse, 0);
		TotalNormalImpulse = newImpulse;
		impulse = newImpulse - oldImpulse;
	}

	public void AddTangentImpulse(ref float impulse)
	{
		float oldImpulse = TotalTangentImpulse;
		float newImpulse = TotalTangentImpulse + impulse;
		float maxStaticImpulse = TotalNormalImpulse * Material.StaticFriction;
		if (RealMath.Abs(newImpulse) > maxStaticImpulse)
		{
			float maxDynamicImpulse = TotalNormalImpulse * Material.DynamicFriction;
			newImpulse = RealMath.Clamp(newImpulse, -maxDynamicImpulse, maxDynamicImpulse);
		}
		TotalTangentImpulse = newImpulse;
		impulse = newImpulse - oldImpulse;
	}

	public void Persist(ContactCache cache)
	{
		TotalNormalImpulse = cache.NormalImpulse;
		TotalTangentImpulse = cache.TangentImpulse;
	}
}
