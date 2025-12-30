namespace WpfApp2;

using System;
using System.Linq;
using Simulator.Engine.Geometry;
using Simulator.Engine;

public class WorldPolyhedron : IPolyhedron
{
	public int PointCount => _worldPoints.Length;
	public int SideCount => _worldSideNormals.Length;
	public int EdgeCount => _edges.Length;

	public WorldPolyhedron(PolyhedronDefinition definition)
	{
		int pointCount = definition.Points.Length;
		_localPoints = new Vector[pointCount];
		_worldPoints = new Vector[pointCount];
		Array.Copy(definition.Points, _localPoints, pointCount);
		Array.Copy(definition.Points, _worldPoints, pointCount);
		int sideCount = definition.Sides.Length;
		_localSideNormals = new Vector[sideCount];
		_worldSideNormals = new Vector[sideCount];
		int sidePointCount = definition.Sides.Sum(side => side.PointIndices.Length);
		_sidePointIndices = new int[sidePointCount];
		_sidePointIndexOffsets = new int[sideCount + 1];
		_sidePointIndexOffsets[sideCount] = sidePointCount;
		int sidePointIndexOffset = 0;
		for (int sideIndex = 0; sideIndex < sideCount; sideIndex++)
		{
			PolyhedronDefinition.Side side = definition.Sides[sideIndex];
			int pointIndexA = side.PointIndices[0];
			int pointIndexB = side.PointIndices[1];
			int pointIndexC = side.PointIndices[2];
			Vector pointA = definition.Points[pointIndexA];
			Vector pointB = definition.Points[pointIndexB];
			Vector pointC = definition.Points[pointIndexC];
			Vector direction = Vector.Cross(pointB - pointA, pointC - pointA);
			Vector normal = direction.Normalize();
			_localSideNormals[sideIndex] = normal;
			_worldSideNormals[sideIndex] = normal;
			int sidePointIndexCount = side.PointIndices.Length;
			Array.Copy(side.PointIndices, 0, _sidePointIndices, sidePointIndexOffset, sidePointIndexCount);
			_sidePointIndexOffsets[sideIndex] = sidePointIndexOffset;
			sidePointIndexOffset += sidePointIndexCount;
		}
		int edgeCount = definition.Edges.Length;
		_edges = new PolyhedronEdge[edgeCount];
		Array.Copy(definition.Edges, _edges, edgeCount);
	}

	public Vector GetPoint(int index)
	{
		return _worldPoints[index];
	}

	public Vector GetSideNormal(int index)
	{
		return _worldSideNormals[index];
	}

	public float GetSideOffset(int index)
	{
		int sidePointIndex = GetSidePointStartIndex(index);
		Vector sidePoint = GetSidePoint(sidePointIndex);
		Vector sideNormal = GetSideNormal(index);
		return Vector.Dot(sidePoint, sideNormal);
	}

	public int GetSidePointStartIndex(int index)
	{
		return _sidePointIndexOffsets[index];
	}

	public int GetSidePointEndIndex(int index)
	{
		return _sidePointIndexOffsets[index + 1];
	}

	public int GetSidePointPointIndex(int index)
	{
		return _sidePointIndices[index];
	}

	public Vector GetSidePoint(int index)
	{
		int pointIndex = GetSidePointPointIndex(index);
		return _worldPoints[pointIndex];
	}

	public PolyhedronEdge GetEdge(int index)
	{
		return _edges[index];
	}

	public void SetTransform(Transform transform)
	{
		for (int pointIndex = 0; pointIndex < _worldPoints.Length; pointIndex++)
		{
			_worldPoints[pointIndex] = transform * _localPoints[pointIndex];
		}
		for (int sideIndex = 0; sideIndex < _worldSideNormals.Length; sideIndex++)
		{
			_worldSideNormals[sideIndex] = transform.Rotation * _localSideNormals[sideIndex];
		}
	}

	public void ApplyTransform(Transform transform)
	{
		for (int pointIndex = 0; pointIndex < _worldPoints.Length; pointIndex++)
		{
			_worldPoints[pointIndex] = transform * _worldPoints[pointIndex];
		}
		for (int sideIndex = 0; sideIndex < _worldSideNormals.Length; sideIndex++)
		{
			_worldSideNormals[sideIndex] = transform.Rotation * _worldSideNormals[sideIndex];
		}
	}

	private readonly Vector[] _localPoints;
	private readonly Vector[] _localSideNormals;
	private readonly Vector[] _worldPoints;
	private readonly Vector[] _worldSideNormals;
	private readonly int[] _sidePointIndices;
	private readonly int[] _sidePointIndexOffsets;
	private readonly PolyhedronEdge[] _edges;
}
