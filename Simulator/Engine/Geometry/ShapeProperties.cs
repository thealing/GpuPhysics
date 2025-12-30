namespace Simulator.Engine.Geometry;

public readonly struct ShapeProperties
{
	public readonly float Volume;
	public readonly Vector Centroid;
	public readonly Matrix InertiaTensor;

	public ShapeProperties(float volume, Vector centroid, Matrix inertiaTensor)
	{
		Volume = volume;
		Centroid = centroid;
		InertiaTensor = inertiaTensor;
	}
}
