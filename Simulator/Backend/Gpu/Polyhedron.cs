namespace Simulator.Backend.Gpu;

using ILGPU;
using Simulator.Engine;
using Simulator.Engine.Geometry;

public readonly struct Polyhedron : IPolyhedron
{
	public int PointCount => _points.IntLength;
	public int SideCount => _sideNormals.IntLength;
	public int EdgeCount => _edges.IntLength;

	public Polyhedron(ArrayView<Vector> points, ArrayView<Vector> sideNormals, ArrayView<int> sidePointIndices, ArrayView<int> sidePointIndexOffsets, ArrayView<PolyhedronEdge> edges)
	{
		_points = points;
		_sideNormals = sideNormals;
		_sidePointIndices = sidePointIndices;
		_sidePointIndexOffsets = sidePointIndexOffsets;
		_edges = edges;
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

	private readonly ArrayView<Vector> _points;
	private readonly ArrayView<Vector> _sideNormals;
	private readonly ArrayView<int> _sidePointIndices;
	private readonly ArrayView<int> _sidePointIndexOffsets;
	private readonly ArrayView<PolyhedronEdge> _edges;
}
