namespace Simulator.Engine.Geometry;

public static class Geometry
{
	public static float GetDistanceSquared(Vector pointA, Vector pointB)
	{
		Vector difference = pointB - pointA;
		return difference.GetLengthSquared();
	}

	public static void ProjectOntoSegment(Vector startPoint, Vector endPoint, ref Vector projectedPoint)
	{
		Vector segmentVector = endPoint - startPoint;
		float lengthSquared = segmentVector.GetLengthSquared();
		float factor = Vector.Dot(projectedPoint - startPoint, segmentVector) / lengthSquared;
		float clampedFactor = RealMath.Clamp(factor, 0, 1);
		projectedPoint = startPoint + segmentVector * clampedFactor;
	}

	public static void ProjectOntoPlane(Vector planeNormal, float planeOffset, ref Vector projectedPoint)
	{
		float pointOffset = Vector.Dot(planeNormal, projectedPoint);
		float distanceFromPlane = pointOffset - planeOffset;
		projectedPoint -= planeNormal * distanceFromPlane;
	}

	public static Vector GetMidpointBetweenLines(Vector firstPointA, Vector secondPointA, Vector firstPointB, Vector secondPointB)
	{
		Vector difference = firstPointB - firstPointA;
		Vector lineVectorA = secondPointA - firstPointA;
		Vector lineVectorB = secondPointB - firstPointB;
		float dA = Vector.Dot(difference, lineVectorA);
		float dB = Vector.Dot(difference, lineVectorB);
		float aA = Vector.Dot(lineVectorA, lineVectorA);
		float bB = Vector.Dot(lineVectorB, lineVectorB);
		float aB = Vector.Dot(lineVectorA, lineVectorB);
		float den = aA * bB - aB * aB;
		if (den == 0)
		{
			return (firstPointA + secondPointA + firstPointB + secondPointB) / 4;
		}
		float factorA = (dA * bB - dB * aB) / den;
		float factorB = (dA * aB - dB * aA) / den;
		Vector closestPointA = firstPointA + lineVectorA * factorA;
		Vector closestPointB = firstPointB + lineVectorB * factorB;
		return (closestPointA + closestPointB) / 2;
	}
}
