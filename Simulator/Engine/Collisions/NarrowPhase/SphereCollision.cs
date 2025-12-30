namespace Simulator.Engine.Collisions.NarrowPhase;

using Simulator.Engine.Geometry;

public static class SphereCollision
{
	public static bool Check<TSphereA, TSphereB>(TSphereA sphereA, TSphereB sphereB, ref Collision collision)
		where TSphereA : ISphere
		where TSphereB : ISphere
	{
		Vector centerDifference = sphereB.Center - sphereA.Center;
		float distanceSquared = centerDifference.GetLengthSquared();
		float radiusSum = sphereA.Radius + sphereB.Radius;
		float radiusSumSquared = RealMath.Square(radiusSum);
		if (distanceSquared > radiusSumSquared)
		{
			return false;
		}
		if (distanceSquared == 0)
		{
			Vector defaultNormal = new Vector(0, 1, 0);
			collision = new Collision(sphereA.Center, defaultNormal, radiusSum);
			return true;
		}
		float distance = RealMath.Sqrt(distanceSquared);
		Vector normal = centerDifference / distance;
		float penetration = radiusSum - distance;
		if (penetration > sphereA.Radius)
		{
			collision = new Collision(sphereA.Center, normal, penetration);
			return true;
		}
		if (penetration > sphereB.Radius)
		{
			collision = new Collision(sphereB.Center, normal, penetration);
			return true;
		}
		Vector collisionPoint = sphereA.Center + normal * (sphereA.Radius - penetration / 2);
		collision = new Collision(collisionPoint, normal, penetration);
		return true;
	}
}
