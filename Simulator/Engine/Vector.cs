namespace Simulator.Engine;

using System;

public struct Vector : IEquatable<Vector>
{
	public static readonly Vector Zero = new Vector(0, 0, 0);

	public float X;
	public float Y;
	public float Z;

	public Vector(float x, float y, float z)
	{
		X = x;
		Y = y;
		Z = z;
	}

	public Vector(float s)
	{
		X = s;
		Y = s;
		Z = s;
	}

	public readonly Vector Normalize()
	{
		return this / GetLength();
	}

	public readonly float GetLength()
	{
		float lengthSquared = GetLengthSquared();
		return RealMath.Sqrt(lengthSquared);
	}

	public readonly float GetLengthSquared()
	{
		return RealMath.Square(X) + RealMath.Square(Y) + RealMath.Square(Z);
	}

	public readonly bool Equals(Vector other)
	{
		return this == other;
	}

	public readonly override bool Equals(object? obj)
	{
		return obj is Vector other && Equals(other);
	}

	public readonly override int GetHashCode()
	{
		int xHash = X.GetHashCode();
		int yHash = Y.GetHashCode();
		int zHash = Z.GetHashCode();
		return xHash * 73856093 + yHash * 19349669 + zHash * 83492791;
	}

	public readonly override string ToString()
	{
		return $"({X}, {Y}, {Z})";
	}

	public static float Dot(Vector v, Vector w)
	{
		return v.X * w.X + v.Y * w.Y + v.Z * w.Z;
	}

	public static Vector Cross(Vector v, Vector w)
	{
		float x = v.Y * w.Z - v.Z * w.Y;
		float y = v.Z * w.X - v.X * w.Z;
		float z = v.X * w.Y - v.Y * w.X;
		return new Vector(x, y, z);
	}

	public static float TripleProduct(Vector v, Vector w, Vector u)
	{
		Vector c = Cross(w, u);
		return Dot(v, c);
	}

	public static Matrix OuterProduct(Vector v, Vector u)
	{
		return new Matrix(u * v.X, u * v.Y, u * v.Z);
	}

	public static Vector operator +(Vector v, Vector w)
	{
		return new Vector(v.X + w.X, v.Y + w.Y, v.Z + w.Z);
	}

	public static Vector operator -(Vector v, Vector w)
	{
		return new Vector(v.X - w.X, v.Y - w.Y, v.Z - w.Z);
	}

	public static Vector operator *(Vector v, Vector w)
	{
		return new Vector(v.X * w.X, v.Y * w.Y, v.Z * w.Z);
	}

	public static Vector operator /(Vector v, Vector w)
	{
		return new Vector(v.X / w.X, v.Y / w.Y, v.Z / w.Z);
	}

	public static Vector operator +(Vector v, float s)
	{
		return new Vector(v.X + s, v.Y + s, v.Z + s);
	}

	public static Vector operator -(Vector v, float s)
	{
		return new Vector(v.X - s, v.Y - s, v.Z - s);
	}

	public static Vector operator *(Vector v, float s)
	{
		return new Vector(v.X * s, v.Y * s, v.Z * s);
	}

	public static Vector operator /(Vector v, float s)
	{
		return new Vector(v.X / s, v.Y / s, v.Z / s);
	}

	public static Vector operator -(Vector v)
	{
		return new Vector(-v.X, -v.Y, -v.Z);
	}

	public static bool operator ==(Vector v, Vector w)
	{
		return v.X == w.X && v.Y == w.Y && v.Z == w.Z;
	}

	public static bool operator !=(Vector v, Vector w)
	{
		return v.X != w.X || v.Y != w.Y || v.Z != w.Z;
	}
}
