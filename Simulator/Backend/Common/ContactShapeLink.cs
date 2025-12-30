namespace Simulator.Backend.Common;

using System;

public struct ContactShapeLink : IEquatable<ContactShapeLink>
{
	public int ShapeIndexA;
	public int ShapeIndexB;

	public ContactShapeLink(int shapeIndexA, int shapeIndexB)
	{
		ShapeIndexA = shapeIndexA;
		ShapeIndexB = shapeIndexB;
	}

	public readonly bool Equals(ContactShapeLink other)
	{
		return ShapeIndexA == other.ShapeIndexA && ShapeIndexB == other.ShapeIndexB;
	}

	public readonly override bool Equals(object? obj)
	{
		return obj is ContactShapeLink other && Equals(other);
	}

	public readonly override int GetHashCode()
	{
		return ShapeIndexA * 73856093 + ShapeIndexB * 19349669;
	}
}
