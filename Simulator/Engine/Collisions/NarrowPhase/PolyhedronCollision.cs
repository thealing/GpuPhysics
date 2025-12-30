namespace Simulator.Engine.Collisions;

using Simulator.Engine.Geometry;

public static partial class PolyhedronCollision
{
	public static bool Check<TPolyhedronA, TPolyhedronB>(TPolyhedronA polyhedronA, TPolyhedronB polyhedronB, ref Collision collision)
		where TPolyhedronA : IPolyhedron
		where TPolyhedronB : IPolyhedron
	{
		SideCheckOutput sideCheckOutputA = new SideCheckOutput();
		if (CheckSides(polyhedronA, polyhedronB, ref sideCheckOutputA) == false)
		{
			return false;
		}
		SideCheckOutput sideCheckOutputB = new SideCheckOutput();
		if (CheckSides(polyhedronB, polyhedronA, ref sideCheckOutputB) == false)
		{
			return false;
		}
		EdgeCheckOutput edgeCheckOutput = new EdgeCheckOutput();
		if (CheckEdges(polyhedronA, polyhedronB, ref edgeCheckOutput) == false)
		{
			return false;
		}
		float depth = RealMath.Min(sideCheckOutputA.Depth, sideCheckOutputB.Depth, edgeCheckOutput.Depth);
		if (sideCheckOutputA.Depth == depth)
		{
			collision = CreateSideCollision(polyhedronA, polyhedronB, sideCheckOutputA);
			return true;
		}
		if (sideCheckOutputB.Depth == depth)
		{
			collision = CreateSideCollision(polyhedronB, polyhedronA, sideCheckOutputB);
			collision.Flip();
			return true;
		}
		collision = CreateEdgeCollision(polyhedronA, polyhedronB, edgeCheckOutput);
		return true;
	}

	private static bool CheckSides<TPolyhedronA, TPolyhedronB>(TPolyhedronA polyhedronA, TPolyhedronB polyhedronB, ref SideCheckOutput output)
		where TPolyhedronA : IPolyhedron
		where TPolyhedronB : IPolyhedron
	{
		float depthMin = float.PositiveInfinity;
		int outputSideIndex = -1;
		int outputPointIndex = -1;
		for (int sideIndex = 0; sideIndex < polyhedronA.SideCount; sideIndex++)
		{
			Vector normal = polyhedronA.GetSideNormal(sideIndex);
			FindClosestPoint(polyhedronB, normal, out float closestPointOffset, out int closestPointIndex);
			float sideOffset = polyhedronA.GetSideOffset(sideIndex);
			float depth = sideOffset - closestPointOffset;
			if (depth < depthMin)
			{
				if (depth < 0)
				{
					return false;
				}
				depthMin = depth;
				outputSideIndex = sideIndex;
				outputPointIndex = closestPointIndex;
			}
		}
		output = new SideCheckOutput(depthMin, outputSideIndex, outputPointIndex);
		return true;
	}

	private static bool CheckEdges<TPolyhedronA, TPolyhedronB>(TPolyhedronA polyhedronA, TPolyhedronB polyhedronB, ref EdgeCheckOutput output)
		where TPolyhedronA : IPolyhedron
		where TPolyhedronB : IPolyhedron
	{
		float depthMin = float.PositiveInfinity;
		Vector outputNormal = Vector.Zero;
		int outputEdgeIndexA = -1;
		int outputEdgeIndexB = -1;
		for (int edgeIndexA = 0; edgeIndexA < polyhedronA.EdgeCount; edgeIndexA++)
		{
			Edge edgeA = GetEdge(polyhedronA, edgeIndexA);
			for (int edgeIndexB = 0; edgeIndexB < polyhedronB.EdgeCount; edgeIndexB++)
			{
				Edge edgeB = GetEdge(polyhedronB, edgeIndexB);
				if (IntersectEdges(edgeA, edgeB) == false)
				{
					continue;
				}
				Vector axis = Vector.Cross(edgeA.Direction, edgeB.Direction);
				if (axis == Vector.Zero)
				{
					continue;
				}
				Vector normal = axis.Normalize();
				if (Vector.Dot(normal, edgeA.LeftSideNormal + edgeA.RightSideNormal) < 0)
				{
					normal = -normal;
				}
				float offsetA = Vector.Dot(normal, edgeA.StartPoint);
				float offsetB = Vector.Dot(normal, edgeB.StartPoint);
				float depth = offsetA - offsetB;
				if (depth < depthMin)
				{
					if (depth < 0)
					{
						return false;
					}
					depthMin = depth;
					outputNormal = normal;
					outputEdgeIndexA = edgeIndexA;
					outputEdgeIndexB = edgeIndexB;
				}
			}
		}
		output = new EdgeCheckOutput(depthMin, outputNormal, outputEdgeIndexA, outputEdgeIndexB);
		return true;
	}

	private static void FindClosestPoint<TPolyhedron>(TPolyhedron polyhedron, Vector direction, out float closestPointOffset, out int closestPointIndex)
		where TPolyhedron : IPolyhedron
	{
		closestPointOffset = float.PositiveInfinity;
		closestPointIndex = -1;
		for (int pointIndex = 0; pointIndex < polyhedron.PointCount; pointIndex++)
		{
			Vector point = polyhedron.GetPoint(pointIndex);
			float offset = Vector.Dot(point, direction);
			if (offset < closestPointOffset)
			{
				closestPointOffset = offset;
				closestPointIndex = pointIndex;
			}
		}
	}

	private static Edge GetEdge<TPolyhedron>(TPolyhedron polyhedron, int index)
		where TPolyhedron : IPolyhedron
	{
		PolyhedronEdge edge = polyhedron.GetEdge(index);
		Vector startPoint = polyhedron.GetPoint(edge.StartPointIndex);
		Vector endPoint = polyhedron.GetPoint(edge.EndPointIndex);
		Vector leftSideNormal = polyhedron.GetSideNormal(edge.LeftSideIndex);
		Vector rightSideNormal = polyhedron.GetSideNormal(edge.RightSideIndex);
		Vector direction = endPoint - startPoint;
		return new Edge(startPoint, endPoint, leftSideNormal, rightSideNormal, direction);
	}

	private static bool IntersectEdges(Edge edgeA, Edge edgeB)
	{
		float lA = Vector.Dot(edgeB.LeftSideNormal, edgeA.Direction);
		float rA = Vector.Dot(edgeB.RightSideNormal, edgeA.Direction);
		float lB = Vector.Dot(edgeA.LeftSideNormal, edgeB.Direction);
		float rB = Vector.Dot(edgeA.RightSideNormal, edgeB.Direction);
		return lA * rA < 0 && lB * rB < 0 && lA * rB < 0;
	}

	private static Collision CreateSideCollision<TPolyhedronA, TPolyhedronB>(TPolyhedronA polyhedronA, TPolyhedronB polyhedronB, SideCheckOutput checkOutput)
		where TPolyhedronA : IPolyhedron
		where TPolyhedronB : IPolyhedron
	{
		Vector sideNormal = polyhedronA.GetSideNormal(checkOutput.SideIndex);
		int sidePointStartIndex = polyhedronA.GetSidePointStartIndex(checkOutput.SideIndex);
		int sidePointEndIndex = polyhedronA.GetSidePointEndIndex(checkOutput.SideIndex);
		Vector resultPoint = polyhedronB.GetPoint(checkOutput.PointIndex);
		Vector pointA = polyhedronA.GetSidePoint(sidePointEndIndex - 1);
		for (int sidePointIndex = sidePointStartIndex; sidePointIndex < sidePointEndIndex; sidePointIndex++)
		{
			Vector pointB = polyhedronA.GetSidePoint(sidePointIndex);
			Vector edgeVector = pointB - pointA;
			Vector direction = Vector.Cross(sideNormal, edgeVector);
			float edgeOffset = Vector.Dot(direction, pointA);
			float pointOffset = Vector.Dot(direction, resultPoint);
			float distance = edgeOffset - pointOffset;
			if (distance > 0)
			{
				float lengthSquared = direction.GetLengthSquared();
				resultPoint += direction * distance / lengthSquared;
			}
			pointA = pointB;
		}
		return new Collision(resultPoint, sideNormal, checkOutput.Depth);
	}

	private static Collision CreateEdgeCollision<TPolyhedronA, TPolyhedronB>(TPolyhedronA polyhedronA, TPolyhedronB polyhedronB, EdgeCheckOutput checkOutput)
		where TPolyhedronA : IPolyhedron
		where TPolyhedronB : IPolyhedron
	{
		GetEdgePoints(polyhedronA, checkOutput.EdgeIndexA, out Vector startPointA, out Vector endPointA);
		GetEdgePoints(polyhedronB, checkOutput.EdgeIndexB, out Vector startPointB, out Vector endPointB);
		Vector midPoint = Geometry.GetMidpointBetweenLines(startPointA, endPointA, startPointB, endPointB);
		return new Collision(midPoint, checkOutput.Normal, checkOutput.Depth);
	}

	private static void GetEdgePoints<TPolyhedron>(TPolyhedron polyhedron, int index, out Vector startPoint, out Vector endPoint)
		where TPolyhedron : IPolyhedron
	{
		PolyhedronEdge edge = polyhedron.GetEdge(index);
		startPoint = polyhedron.GetPoint(edge.StartPointIndex);
		endPoint = polyhedron.GetPoint(edge.EndPointIndex);
	}
}
