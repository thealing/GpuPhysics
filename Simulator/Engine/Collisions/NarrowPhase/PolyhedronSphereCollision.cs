namespace Simulator.Engine.Collisions;

using Simulator.Engine.Geometry;

public static partial class PolyhedronSphereCollision
{
	public static bool Check<TPolyhedron, TSphere>(TPolyhedron polyhedron, TSphere sphere, ref Collision collision)
		where TPolyhedron : IPolyhedron
		where TSphere : ISphere
	{
		float distanceMax = float.NegativeInfinity;
		int closestSideIndex = -1;
		bool onClosestSide = false;
		for (int sideIndex = 0; sideIndex < polyhedron.SideCount; sideIndex++)
		{
			Vector sideNormal = polyhedron.GetSideNormal(sideIndex);
			float sideOffset = polyhedron.GetSideOffset(sideIndex);
			float centerOffset = Vector.Dot(sideNormal, sphere.Center);
			float distance = centerOffset - sideOffset;
			if (distance > distanceMax)
			{
				if (distance > sphere.Radius)
				{
					return false;
				}
				bool onSide = true;
				int sidePointStartIndex = polyhedron.GetSidePointStartIndex(sideIndex);
				int sidePointEndIndex = polyhedron.GetSidePointEndIndex(sideIndex);
				Vector pointA = polyhedron.GetSidePoint(sidePointEndIndex - 1);
				for (int sidePointIndex = sidePointStartIndex; sidePointIndex < sidePointEndIndex; sidePointIndex++)
				{
					Vector pointB = polyhedron.GetSidePoint(sidePointIndex);
					float orientation = Vector.TripleProduct(sideNormal, pointB - pointA, sphere.Center - pointA);
					if (orientation < 0)
					{
						onSide = false;
						break;
					}
					pointA = pointB;
				}
				distanceMax = distance;
				closestSideIndex = sideIndex;
				onClosestSide = onSide;
			}
		}
		if (onClosestSide)
		{
			float depth = sphere.Radius - distanceMax;
			Vector normal = polyhedron.GetSideNormal(closestSideIndex);
			Vector point = sphere.Center;
			if (distanceMax > 0)
			{
				point -= normal * distanceMax;
			}
			collision = new Collision(point, normal, depth);
			return true;
		}
		float distanceSquaredMin = float.PositiveInfinity;
		Vector closestPoint = Vector.Zero;
		for (int edgeIndex = 0; edgeIndex < polyhedron.EdgeCount; edgeIndex++)
		{
			Vector projectedPoint = sphere.Center;
			ProjectOntoEdge(polyhedron, edgeIndex, ref projectedPoint);
			float distanceSquared = Geometry.GetDistanceSquared(sphere.Center, projectedPoint);
			if (distanceSquared < distanceSquaredMin)
			{
				distanceSquaredMin = distanceSquared;
				closestPoint = projectedPoint;
			}
		}
		float radiusSquared = RealMath.Square(sphere.Radius);
		if (distanceSquaredMin <= radiusSquared)
		{
			Vector direction = sphere.Center - closestPoint;
			Vector normal = direction.Normalize();
			float distanceMin = RealMath.Sqrt(distanceSquaredMin);
			float depth = sphere.Radius - distanceMin;
			if (depth >= 0)
			{
				collision = new Collision(closestPoint, normal, depth);
				return true;
			}
		}
		return false;
	}

	private static void ProjectOntoEdge<TPolyhedron>(TPolyhedron polyhedron, int edgeIndex, ref Vector projectedPoint)
		where TPolyhedron : IPolyhedron
	{
		PolyhedronEdge edge = polyhedron.GetEdge(edgeIndex);
		Vector startPoint = polyhedron.GetPoint(edge.StartPointIndex);
		Vector endPoint = polyhedron.GetPoint(edge.EndPointIndex);
		Geometry.ProjectOntoSegment(startPoint, endPoint, ref projectedPoint);
	}
}
