namespace Simulator.Engine.Geometry;

public interface IPolyhedron
{
	public int PointCount { get; }
	public int SideCount { get; }
	public int EdgeCount { get; }

	public Vector GetPoint(int index);

	public Vector GetSideNormal(int index);

	public float GetSideOffset(int index);

	public int GetSidePointStartIndex(int index);

	public int GetSidePointEndIndex(int index);

	public int GetSidePointPointIndex(int index);

	public Vector GetSidePoint(int index);

	public PolyhedronEdge GetEdge(int index);
}
