namespace Simulator.Backend.Cpu;

using System;
using System.Collections.Generic;
using Simulator.Backend.Common;
using Simulator.Engine;
using Simulator.Engine.Collisions;
using Simulator.Engine.Geometry;

public readonly struct PolyhedronStorage
{
	public int Count => Offsets.Count - 1;
	public List<PolyhedronOffset> Offsets { get; }
	public List<Vector> LocalPoints { get; }
	public List<Vector> LocalSideNormals { get; }
	public List<Vector> WorldPoints { get; }
	public List<Vector> WorldSideNormals { get; }
	public List<int> SidePointIndices { get; }
	public List<int> SidePointIndexOffsets { get; }
	public List<PolyhedronEdge> Edges { get; }

	public PolyhedronStorage()
	{
		Offsets = new List<PolyhedronOffset>();
		LocalPoints = new List<Vector>();
		LocalSideNormals = new List<Vector>();
		WorldPoints = new List<Vector>();
		WorldSideNormals = new List<Vector>();
		SidePointIndices = new List<int>();
		SidePointIndexOffsets = new List<int>();
		Edges = new List<PolyhedronEdge>();
		AddEndOffset();
	}

	public void AddPolyhedron(PolyhedronDefinition definition)
	{
		LocalPoints.AddRange(definition.Points);
		WorldPoints.AddRange(definition.Points);
		int sideCount = definition.Sides.Length;
		int sidePointIndexOffset = 0;
		for (int sideIndex = 0; sideIndex < sideCount; sideIndex++)
		{
			PolyhedronDefinition.Side side = definition.Sides[sideIndex];
			Vector direction = Vector.Zero;
			Vector pointA = definition.Points[side.PointIndices[^2]];
			Vector pointB = definition.Points[side.PointIndices[^1]];
			for (int sidePointIndex = 0; sidePointIndex < side.PointIndices.Length; sidePointIndex++)
			{
				Vector pointC = definition.Points[side.PointIndices[sidePointIndex]];
				direction += Vector.Cross(pointB - pointA, pointC - pointA);
				pointA = pointB;
				pointB = pointC;
			}
			Vector normal = direction.Normalize();
			LocalSideNormals.Add(normal);
			WorldSideNormals.Add(normal);
			SidePointIndices.AddRange(side.PointIndices);
			SidePointIndexOffsets.Add(sidePointIndexOffset);
			sidePointIndexOffset += side.PointIndices.Length;
		}
		SidePointIndexOffsets.Add(sidePointIndexOffset);
		Edges.AddRange(definition.Edges);
		AddEndOffset();
	}

	public void AddEndOffset()
	{
		PolyhedronOffset offset = new PolyhedronOffset(LocalPoints.Count, LocalSideNormals.Count, SidePointIndices.Count, SidePointIndexOffsets.Count, Edges.Count);
		Offsets.Add(offset);
	}

	public Polyhedron GetSafePolyhedron(int index)
	{
		PolyhedronOffset startOffset = Offsets[index];
		PolyhedronOffset endOffset = Offsets[index + 1];
		Span<Vector> points = WorldPoints.SubView(startOffset.PointOffset, endOffset.PointOffset);
		Span<Vector> sideNormals = WorldSideNormals.SubView(startOffset.SideNormalOffset, endOffset.SideNormalOffset);
		Span<int> sidePointIndices = SidePointIndices.SubView(startOffset.SidePointIndiceOffset, endOffset.SidePointIndiceOffset);
		Span<int> sidePointIndexOffsets = SidePointIndexOffsets.SubView(startOffset.SidePointIndexOffsetOffset, endOffset.SidePointIndexOffsetOffset);
		Span<PolyhedronEdge> edges = Edges.SubView(startOffset.EdgeOffset, endOffset.EdgeOffset);
		return new Polyhedron(points, sideNormals, sidePointIndices, sidePointIndexOffsets, edges);
	}

	public UnsafePolyhedron GetPolyhedron(int index)
	{
		PolyhedronOffset startOffset = Offsets[index];
		PolyhedronOffset endOffset = Offsets[index + 1];
		Span<Vector> points = WorldPoints.SubView(startOffset.PointOffset, endOffset.PointOffset);
		Span<Vector> sideNormals = WorldSideNormals.SubView(startOffset.SideNormalOffset, endOffset.SideNormalOffset);
		Span<int> sidePointIndices = SidePointIndices.SubView(startOffset.SidePointIndiceOffset, endOffset.SidePointIndiceOffset);
		Span<int> sidePointIndexOffsets = SidePointIndexOffsets.SubView(startOffset.SidePointIndexOffsetOffset, endOffset.SidePointIndexOffsetOffset);
		Span<PolyhedronEdge> edges = Edges.SubView(startOffset.EdgeOffset, endOffset.EdgeOffset);
		return new UnsafePolyhedron(points, sideNormals, sidePointIndices, sidePointIndexOffsets, edges);
	}

	public Bound UpdateTransform(int index, Transform transform)
	{
		PolyhedronOffset startOffset = Offsets[index];
		PolyhedronOffset endOffset = Offsets[index + 1];
		for (int pointIndex = startOffset.PointOffset; pointIndex < endOffset.PointOffset; pointIndex++)
		{
			WorldPoints[pointIndex] = transform * LocalPoints[pointIndex];
		}
		for (int sideNormalIndex = startOffset.SideNormalOffset; sideNormalIndex < endOffset.SideNormalOffset; sideNormalIndex++)
		{
			WorldSideNormals[sideNormalIndex] = transform.Rotation * LocalSideNormals[sideNormalIndex];
		}
		Vector lower = new Vector(float.PositiveInfinity);
		Vector upper = new Vector(float.NegativeInfinity);
		for (int pointIndex = startOffset.PointOffset; pointIndex < endOffset.PointOffset; pointIndex++)
		{
			Vector point = WorldPoints[pointIndex];
			lower.X = RealMath.Min(lower.X, point.X);
			lower.Y = RealMath.Min(lower.Y, point.Y);
			lower.Z = RealMath.Min(lower.Z, point.Z);
			upper.X = RealMath.Max(upper.X, point.X);
			upper.Y = RealMath.Max(upper.Y, point.Y);
			upper.Z = RealMath.Max(upper.Z, point.Z);
		}
		return new Bound(lower, upper);
	}
}
