namespace Simulator.Engine.Geometry;

using Simulator.Engine;

public struct Transform
{
	public static readonly Transform Identity = new Transform(Vector.Zero, Quaternion.Identity);

	public Vector Position;
	public Quaternion Rotation;

	public Transform(Vector position, Quaternion rotation)
	{
		Position = position;
		Rotation = rotation;
	}

	public static Vector operator *(Transform transform, Vector vector)
	{
		return transform.Position + transform.Rotation * vector;
	}
}
