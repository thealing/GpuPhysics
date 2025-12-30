namespace Simulator.Engine.Collisions.BroadPhase;

using System;
using Simulator.Engine;

public readonly struct DynamicGridKey : IEquatable<DynamicGridKey>
{
	public static readonly int MaxSize = 32;

	public readonly int X;
	public readonly int Y;
	public readonly int Z;
	public readonly int Size;

	public DynamicGridKey(int x, int y, int z, int size)
	{
		X = x;
		Y = y;
		Z = z;
		Size = size;
	}

	public DynamicGridKey(Vector location, int size)
	{
		X = GetCellIndex(location.X, size);
		Y = GetCellIndex(location.Y, size);
		Z = GetCellIndex(location.Z, size);
		Size = size;
	}

	public bool Equals(DynamicGridKey other)
	{
		return X == other.X && Y == other.Y && Z == other.Z && Size == other.Size;
	}

	public override bool Equals(object? obj)
	{
		return obj is DynamicGridKey other && Equals(other);
	}

	public override int GetHashCode()
	{
		return X * 73856093 + Y * 19349669 + Z * 83492791 + Size * 49979539;
	}

	private static int GetCellIndex(float coordinate, int size)
	{
		float scaledValue = RealMath.ScaleB(coordinate, -size);
		return (int)RealMath.Floor(scaledValue);
	}
}
