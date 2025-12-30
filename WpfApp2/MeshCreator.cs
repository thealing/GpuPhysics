namespace WpfApp2;

using System.Windows.Media.Media3D;
using Simulator.Engine;
using Simulator.Engine.Geometry;

public static class MeshCreator
{
	public static MeshGeometry3D CreatePolyhedronMesh<TPolyhedron>(TPolyhedron polyhedron)
		where TPolyhedron : IPolyhedron
	{
		MeshGeometry3D mesh = new MeshGeometry3D();
		for (int pointIndex = 0; pointIndex < polyhedron.PointCount; pointIndex++)
		{
			Vector point = polyhedron.GetPoint(pointIndex);
			Point3D point3D = point.ToPoint3D();
			mesh.Positions.Add(point3D);
		}
		for (int sideIndex = 0; sideIndex < polyhedron.SideCount; sideIndex++)
		{
			Vector sideNormal = polyhedron.GetSideNormal(sideIndex);
			Vector3D sideNormal3D = sideNormal.ToVector3D();
			int sidePointStartIndex = polyhedron.GetSidePointStartIndex(sideIndex);
			int sidePointEndIndex = polyhedron.GetSidePointEndIndex(sideIndex);
			int pointIndexA = polyhedron.GetSidePointPointIndex(sidePointStartIndex);
			int pointIndexB = polyhedron.GetSidePointPointIndex(sidePointStartIndex + 1);
			for (int sidePointIndex = sidePointStartIndex + 2; sidePointIndex < sidePointEndIndex; sidePointIndex++)
			{
				int pointIndexC = polyhedron.GetSidePointPointIndex(sidePointIndex);
				mesh.TriangleIndices.Add(pointIndexA);
				mesh.TriangleIndices.Add(pointIndexB);
				mesh.TriangleIndices.Add(pointIndexC);
				mesh.Normals.Add(sideNormal3D);
				pointIndexB = pointIndexC;
			}
		}
		return mesh;
	}

	public interface IMap<TValue>
	{
		public void Insert(TValue value);
	}

	public interface IBackend<TValue>
	{
		public void Insert(TValue value);
	}

	public class Map<TValue, TBackend> : IMap<TValue>
		where TBackend : IBackend<TValue>
	{
		public TBackend Backend;

		public void Insert(TValue value)
		{
			Backend.Insert(value);
		}
	}
}
