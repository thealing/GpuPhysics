namespace Simulator.Engine.Geometry.Validation;

using System.Collections.Generic;
using System.Diagnostics;

public static partial class PolyhedronValidator
{
	public static Result Validate<TPolyhedron>(TPolyhedron polyhedron, float tolerance)
		where TPolyhedron : IPolyhedron
	{
		if (IsFinite(polyhedron) == false)
		{
			return Result.NotFinite;
		}
		HashSet<Vector> pointSet = new HashSet<Vector>();
		for (int pointIndex = 0; pointIndex < polyhedron.PointCount; pointIndex++)
		{
			Vector point = polyhedron.GetPoint(pointIndex);
			pointSet.Add(point);
		}
		for (int sideIndex = 0; sideIndex < polyhedron.SideCount; sideIndex++)
		{
			int sidePointStartIndex = polyhedron.GetSidePointStartIndex(sideIndex);
			int sidePointEndIndex = polyhedron.GetSidePointEndIndex(sideIndex);
			for (int sidePointIndex = sidePointStartIndex; sidePointIndex < sidePointEndIndex; sidePointIndex++)
			{
				Vector point = polyhedron.GetSidePoint(sidePointIndex);
				if (pointSet.Contains(point) == false)
				{
					Debugger.Break();
					return Result.SidePointNotFound;
				}
			}
		}
		for (int sideIndex = 0; sideIndex < polyhedron.SideCount; sideIndex++)
		{
			int sidePointStartIndex = polyhedron.GetSidePointStartIndex(sideIndex);
			int sidePointEndIndex = polyhedron.GetSidePointEndIndex(sideIndex);
			if (sideIndex == 0)
			{
				if (sidePointStartIndex != 0)
				{
					Debugger.Break();
					return Result.FirstSidePointStartIndexIsNotZero;
				}
			}
			else
			{
				int lastSidePointEndIndex = polyhedron.GetSidePointEndIndex(sideIndex - 1);
				if (sidePointStartIndex != lastSidePointEndIndex)
				{
					Debugger.Break();
					return Result.SidePointIndicesAreNotContiguous;
				}
			}
			if (sidePointEndIndex - sidePointStartIndex < 3)
			{
				Debugger.Break();
				return Result.SideHasTooFewPoints;
			}
			Vector sideNormal = polyhedron.GetSideNormal(sideIndex);
			if (RealMath.Abs(sideNormal.GetLength() - 1) > tolerance)
			{
				Debugger.Break();
				return Result.SideNormalLengthIsNotCorrect;
			}
			float sideOffset = polyhedron.GetSideOffset(sideIndex);
			for (int sidePointIndex = sidePointStartIndex; sidePointIndex < sidePointEndIndex; sidePointIndex++)
			{
				Vector point = polyhedron.GetSidePoint(sidePointIndex);
				float offset = Vector.Dot(sideNormal, point);
				if (RealMath.Abs(offset - sideOffset) > tolerance)
				{
					Debugger.Break();
					return Result.SideIsNotPlanar;
				}
			}
			Vector pointA = polyhedron.GetSidePoint(sidePointEndIndex - 2);
			Vector pointB = polyhedron.GetSidePoint(sidePointEndIndex - 1);
			for (int sidePointIndex = sidePointStartIndex; sidePointIndex < sidePointEndIndex; sidePointIndex++)
			{
				Vector pointC = polyhedron.GetSidePoint(sidePointIndex);
				float orientation = Vector.TripleProduct(sideNormal, pointB - pointA, pointC - pointA);
				if (orientation <= -tolerance)
				{
					Debugger.Break();
					return Result.SideWindingIsNotPositive;
				}
				pointA = pointB;
				pointB = pointC;
			}
		}
		ShapeProperties properties = PolyhedronExtension.GetProperties(polyhedron);
		if (properties.Volume <= -tolerance)
		{
			Debugger.Break();
			return Result.VolumeIsNotPositive;
		}
		Dictionary<EdgeMapKey, EdgeMapValue> edgeMap = new Dictionary<EdgeMapKey, EdgeMapValue>();
		for (int sideIndex = 0; sideIndex < polyhedron.SideCount; sideIndex++)
		{
			int sidePointStartIndex = polyhedron.GetSidePointStartIndex(sideIndex);
			int sidePointEndIndex = polyhedron.GetSidePointEndIndex(sideIndex);
			Vector pointA = polyhedron.GetSidePoint(sidePointEndIndex - 1);
			for (int sidePointIndex = sidePointStartIndex; sidePointIndex < sidePointEndIndex; sidePointIndex++)
			{
				Vector pointB = polyhedron.GetSidePoint(sidePointIndex);
				EdgeMapKey key = new EdgeMapKey(pointA, pointB);
				if (edgeMap.ContainsKey(key))
				{
					Debugger.Break();
					return Result.EdgeIsDuplicated;
				}
				edgeMap[key] = new EdgeMapValue(sideIndex);
				pointA = pointB;
			}
		}
		for (int edgeIndex = 0; edgeIndex < polyhedron.EdgeCount; edgeIndex++)
		{
			PolyhedronEdge edge = polyhedron.GetEdge(edgeIndex);
			Vector startPoint = polyhedron.GetPoint(edge.StartPointIndex);
			Vector endPoint = polyhedron.GetPoint(edge.EndPointIndex);
			EdgeMapKey leftKey = new EdgeMapKey(startPoint, endPoint);
			EdgeMapKey rightKey = new EdgeMapKey(endPoint, startPoint);
			if (edgeMap.TryGetValue(leftKey, out EdgeMapValue leftValue) == false)
			{
				Debugger.Break();
				return Result.EdgeLeftSideNotFound;
			}
			if (leftValue.SideIndex != edge.LeftSideIndex)
			{
				Debugger.Break();
				return Result.EdgeLeftSideIsNotCorrect;
			}
			if (edgeMap.TryGetValue(rightKey, out EdgeMapValue rightValue) == false)
			{
				Debugger.Break();
				return Result.EdgeRightSideNotFound;
			}
			if (rightValue.SideIndex != edge.RightSideIndex)
			{
				Debugger.Break();
				return Result.EdgeRightSideIsNotCorrect;
			}
		}
		return Result.Valid;
	}

	public static bool IsFinite<TPolyhedron>(TPolyhedron polyhedron)
		where TPolyhedron : IPolyhedron
	{
		for (int pointIndex = 0; pointIndex < polyhedron.PointCount; pointIndex++)
		{
			Vector point = polyhedron.GetPoint(pointIndex);
			if (VectorValidator.IsFinite(point) == false)
			{
				return false;
			}
		}
		return true;
	}
}
