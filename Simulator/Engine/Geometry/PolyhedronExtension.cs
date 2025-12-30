namespace Simulator.Engine.Geometry;

using Simulator.Engine.Collisions;

public static class PolyhedronExtension
{
	public static Bound GetBound<TPolyhedron>(this TPolyhedron polyhedron)
		where TPolyhedron : IPolyhedron
	{
		Vector lower = new Vector(float.PositiveInfinity);
		Vector upper = new Vector(float.NegativeInfinity);
		for (int pointIndex = 0; pointIndex < polyhedron.PointCount; pointIndex++)
		{
			Vector point = polyhedron.GetPoint(pointIndex);
			lower.X = RealMath.Min(lower.X, point.X);
			lower.Y = RealMath.Min(lower.Y, point.Y);
			lower.Z = RealMath.Min(lower.Z, point.Z);
			upper.X = RealMath.Max(upper.X, point.X);
			upper.Y = RealMath.Max(upper.Y, point.Y);
			upper.Z = RealMath.Max(upper.Z, point.Z);
		}
		return new Bound(lower, upper);
	}

	public static bool ContainsPoint<TPolyhedron>(this TPolyhedron polyhedron, Vector point)
		where TPolyhedron : IPolyhedron
	{
		for (int sideIndex = 0; sideIndex < polyhedron.SideCount; sideIndex++)
		{
			Vector sideNormal = polyhedron.GetSideNormal(sideIndex);
			float sideOffset = polyhedron.GetSideOffset(sideIndex);
			float pointOffset = Vector.Dot(sideNormal, point);
			if (pointOffset > sideOffset)
			{
				return false;
			}
		}
		return true;
	}

	public static ShapeProperties GetProperties<TPolyhedron>(this TPolyhedron polyhedron)
		where TPolyhedron : IPolyhedron
	{
		float volume = 0;
		Vector centroidTimesVolume = Vector.Zero;
		for (int sideIndex = 0; sideIndex < polyhedron.SideCount; sideIndex++)
		{
			int sidePointStartIndex = polyhedron.GetSidePointStartIndex(sideIndex);
			int sidePointEndIndex = polyhedron.GetSidePointEndIndex(sideIndex);
			Vector pointA = polyhedron.GetSidePoint(sidePointStartIndex);
			Vector pointB = polyhedron.GetSidePoint(sidePointStartIndex + 1);
			for (int sidePointIndex = sidePointStartIndex + 2; sidePointIndex < sidePointEndIndex; sidePointIndex++)
			{
				Vector pointC = polyhedron.GetSidePoint(sidePointIndex);
				float tetraVolume = GetTetrahedronVolume(pointA, pointB, pointC);
				Vector tetraCentroid = GetTetrahedronCentroid(pointA, pointB, pointC);
				volume += tetraVolume;
				centroidTimesVolume += tetraCentroid * tetraVolume;
				pointB = pointC;
			}
		}
		Vector centroid = centroidTimesVolume / volume;
		Matrix inertiaTensorTimesVolume = Matrix.Zero;
		for (int sideIndex = 0; sideIndex < polyhedron.SideCount; sideIndex++)
		{
			int sidePointStartIndex = polyhedron.GetSidePointStartIndex(sideIndex);
			int sidePointEndIndex = polyhedron.GetSidePointEndIndex(sideIndex);
			Vector pointA = polyhedron.GetRelativeSidePoint(sidePointStartIndex, centroid);
			Vector pointB = polyhedron.GetRelativeSidePoint(sidePointStartIndex + 1, centroid);
			for (int sidePointIndex = sidePointStartIndex + 2; sidePointIndex < sidePointEndIndex; sidePointIndex++)
			{
				Vector pointC = polyhedron.GetRelativeSidePoint(sidePointIndex, centroid);
				float tetraVolume = GetTetrahedronVolume(pointA, pointB, pointC);
				Matrix tetraTensor = GetTetrahedronInertiaTensor(pointA, pointB, pointC);
				inertiaTensorTimesVolume += tetraTensor * tetraVolume;
				pointB = pointC;
			}
		}
		Matrix inertiaTensor = inertiaTensorTimesVolume / volume;
		return new ShapeProperties(volume, centroid, inertiaTensor);
	}

	private static Vector GetRelativeSidePoint<TPolyhedron>(this TPolyhedron polyhedron, int sidePointIndex, Vector referencePoint)
		where TPolyhedron : IPolyhedron
	{
		Vector sidePoint = polyhedron.GetSidePoint(sidePointIndex);
		return sidePoint - referencePoint;
	}

	private static float GetTetrahedronVolume(Vector pointA, Vector pointB, Vector pointC)
	{
		float tripleProduct = Vector.TripleProduct(pointA, pointB, pointC);
		return tripleProduct / 6;
	}

	private static Vector GetTetrahedronCentroid(Vector pointA, Vector pointB, Vector pointC)
	{
		return (pointA + pointB + pointC) / 4;
	}

	private static Matrix GetTetrahedronInertiaTensor(Vector pointA, Vector pointB, Vector pointC)
	{
		Matrix outerMatrix = Matrix.Zero;
		outerMatrix += Vector.OuterProduct(pointA, pointA);
		outerMatrix += Vector.OuterProduct(pointB, pointB);
		outerMatrix += Vector.OuterProduct(pointC, pointC);
		outerMatrix *= 2;
		outerMatrix += Vector.OuterProduct(pointA, pointB);
		outerMatrix += Vector.OuterProduct(pointB, pointC);
		outerMatrix += Vector.OuterProduct(pointC, pointA);
		outerMatrix += Vector.OuterProduct(pointB, pointA);
		outerMatrix += Vector.OuterProduct(pointC, pointB);
		outerMatrix += Vector.OuterProduct(pointA, pointC);
		outerMatrix /= 20;
		float traceX = outerMatrix.Y.Y + outerMatrix.Z.Z;
		float traceY = outerMatrix.Z.Z + outerMatrix.X.X;
		float traceZ = outerMatrix.X.X + outerMatrix.Y.Y;
		Vector x = new Vector(traceX, -outerMatrix.X.Y, -outerMatrix.X.Z);
		Vector y = new Vector(-outerMatrix.Y.X, traceY, -outerMatrix.Y.Z);
		Vector z = new Vector(-outerMatrix.Z.X, -outerMatrix.Z.Y, traceZ);
		return new Matrix(x, y, z);
	}
}
