namespace Simulator.Backend.Cpu;

using System;
using Simulator.Engine;
using Simulator.Engine.Geometry;

public readonly struct UnsafePolyhedron : IPolyhedron
{
	public int PointCount => _points.Length;
	public int SideCount => _sideNormals.Length;
	public int EdgeCount => _edges.Length;

	public UnsafePolyhedron(ReadOnlySpan<Vector> points, ReadOnlySpan<Vector> sideNormals, ReadOnlySpan<int> sidePointIndices, ReadOnlySpan<int> sidePointIndexOffsets, ReadOnlySpan<PolyhedronEdge> edges)
	{
		_points = new UnsafeSpan<Vector>(points);
		_sideNormals = new UnsafeSpan<Vector>(sideNormals);
		_sidePointIndices = new UnsafeSpan<int>(sidePointIndices);
		_sidePointIndexOffsets = new UnsafeSpan<int>(sidePointIndexOffsets);
		_edges = new UnsafeSpan<PolyhedronEdge>(edges);
	}

	public Vector GetPoint(int index)
	{
		return _points[index];
	}

	public Vector GetSideNormal(int index)
	{
		return _sideNormals[index];
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
		return _points[pointIndex];
	}

	public PolyhedronEdge GetEdge(int index)
	{
		return _edges[index];
	}

	private readonly UnsafeSpan<Vector> _points;
	private readonly UnsafeSpan<Vector> _sideNormals;
	private readonly UnsafeSpan<int> _sidePointIndices;
	private readonly UnsafeSpan<int> _sidePointIndexOffsets;
	private readonly UnsafeSpan<PolyhedronEdge> _edges;
}
