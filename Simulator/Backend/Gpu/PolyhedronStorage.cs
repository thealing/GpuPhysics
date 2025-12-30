namespace Simulator.Backend.Gpu;

using ILGPU;
using ILGPU.Runtime;
using Simulator.Backend.Common;
using Simulator.Engine;
using Simulator.Engine.Collisions;
using Simulator.Engine.Geometry;

public struct PolyhedronStorage
{
	public int Count;
	public ArrayView<PolyhedronOffset> Offsets;
	public ArrayView<Vector> LocalPoints;
	public ArrayView<Vector> LocalSideNormals;
	public ArrayView<Vector> WorldPoints;
	public ArrayView<Vector> WorldSideNormals;
	public ArrayView<int> SidePointIndices;
	public ArrayView<int> SidePointIndexOffsets;
	public ArrayView<PolyhedronEdge> Edges;

	public PolyhedronStorage(Accelerator accelerator)
	{
		Offsets = accelerator.AllocateZeroedView<PolyhedronOffset>(0);
		LocalPoints = accelerator.AllocateZeroedView<Vector>(0);
		LocalSideNormals = accelerator.AllocateZeroedView<Vector>(0);
		WorldPoints = accelerator.AllocateZeroedView<Vector>(0);
		WorldSideNormals = accelerator.AllocateZeroedView<Vector>(0);
		SidePointIndices = accelerator.AllocateZeroedView<int>(0);
		SidePointIndexOffsets = accelerator.AllocateZeroedView<int>(0);
		Edges = accelerator.AllocateZeroedView<PolyhedronEdge>(0);
	}

	public void CopyFromCPU(Cpu.PolyhedronStorage polyhedronStorage)
	{
		Count = polyhedronStorage.Count;
		Offsets.SafeCopyFromCPU(polyhedronStorage.Offsets);
		LocalPoints.SafeCopyFromCPU(polyhedronStorage.LocalPoints);
		LocalSideNormals.SafeCopyFromCPU(polyhedronStorage.LocalSideNormals);
		WorldPoints.SafeCopyFromCPU(polyhedronStorage.WorldPoints);
		WorldSideNormals.SafeCopyFromCPU(polyhedronStorage.WorldSideNormals);
		SidePointIndices.SafeCopyFromCPU(polyhedronStorage.SidePointIndices);
		SidePointIndexOffsets.SafeCopyFromCPU(polyhedronStorage.SidePointIndexOffsets);
		Edges.SafeCopyFromCPU(polyhedronStorage.Edges);
	}

	public void CopyToCPU(Cpu.PolyhedronStorage polyhedronStorage)
	{
		Offsets.CopyToCPU(polyhedronStorage.Offsets);
		LocalPoints.CopyToCPU(polyhedronStorage.LocalPoints);
		LocalSideNormals.CopyToCPU(polyhedronStorage.LocalSideNormals);
		WorldPoints.CopyToCPU(polyhedronStorage.WorldPoints);
		WorldSideNormals.CopyToCPU(polyhedronStorage.WorldSideNormals);
		SidePointIndices.CopyToCPU(polyhedronStorage.SidePointIndices);
		SidePointIndexOffsets.CopyToCPU(polyhedronStorage.SidePointIndexOffsets);
		Edges.CopyToCPU(polyhedronStorage.Edges);
	}

	public Polyhedron GetPolyhedron(int index)
	{
		PolyhedronOffset startOffset = Offsets[index];
		PolyhedronOffset endOffset = Offsets[index + 1];
		ArrayView<Vector> points = WorldPoints.Slice(startOffset.PointOffset, endOffset.PointOffset);
		ArrayView<Vector> sideNormals = WorldSideNormals.Slice(startOffset.SideNormalOffset, endOffset.SideNormalOffset);
		ArrayView<int> sidePointIndices = SidePointIndices.Slice(startOffset.SidePointIndiceOffset, endOffset.SidePointIndiceOffset);
		ArrayView<int> sidePointIndexOffsets = SidePointIndexOffsets.Slice(startOffset.SidePointIndexOffsetOffset, endOffset.SidePointIndexOffsetOffset);
		ArrayView<PolyhedronEdge> edges = Edges.Slice(startOffset.EdgeOffset, endOffset.EdgeOffset);
		return new Polyhedron(points, sideNormals, sidePointIndices, sidePointIndexOffsets, edges);
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
