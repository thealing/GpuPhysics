namespace Simulator.Engine.Geometry;

using System;
using Simulator.Engine.Collisions;

public static class SphereExtension
{
	public static Bound GetBound<TSphere>(this TSphere sphere)
		where TSphere : ISphere
	{
		return new Bound(sphere.Center - sphere.Radius, sphere.Center + sphere.Radius);
	}

	public static bool ContainsPoint<TSphere>(this TSphere sphere, Vector point)
		where TSphere : ISphere
	{
		Vector difference = point - sphere.Center;
		float distanceSquared = difference.GetLengthSquared();
		float radiusSquared = RealMath.Square(sphere.Radius);
		return distanceSquared <= radiusSquared;
	}

	public static ShapeProperties GetProperties<TSphere>(this TSphere sphere)
		where TSphere : ISphere
	{
		float volume = RealMath.Cube(sphere.Radius) * 4 / 3 * MathF.PI;
		Vector centroid = sphere.Center;
		float inertiaScalar = RealMath.Square(sphere.Radius) * 2 / 5;
		Matrix inertiaTensor = Matrix.CreateUnit(inertiaScalar);
		return new ShapeProperties(volume, centroid, inertiaTensor);
	}
}
