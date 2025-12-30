namespace Simulator.Engine.Geometry;

public partial class PolyhedronDefinition
{
	public Vector[] Points { get; }
	public Side[] Sides { get; }
	public PolyhedronEdge[] Edges { get; }

	public PolyhedronDefinition(Vector[] points, Side[] sides, PolyhedronEdge[] edges)
	{
		Points = points;
		Sides = sides;
		Edges = edges;
	}

	public void Transform(Transform transform)
	{
		for (int pointIndex = 0; pointIndex < Points.Length; pointIndex++)
		{
			Points[pointIndex] = transform * Points[pointIndex];
		}
	}

	public void Scale(Vector scale)
	{
		for (int pointIndex = 0; pointIndex < Points.Length; pointIndex++)
		{
			Points[pointIndex] *= scale;
		}
	}
}
