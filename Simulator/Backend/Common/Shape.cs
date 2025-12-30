namespace Simulator.Backend.Common;

using Simulator.Engine.Physics;

public struct Shape
{
	public int SphereIndex;
	public int PolyhedronIndex;
	public int BodyIndex;
	public Material Material;

	public Shape(ShapeDefinition definition)
	{
		SphereIndex = -1;
		PolyhedronIndex = -1;
		BodyIndex = definition.BodyIndex;
		Material = definition.Material;
	}
}
