namespace Simulator.Engine.Collisions;
public struct Collision
{
	public Vector Point;
	public Vector Normal;
	public float Depth;

	public Collision(Vector point, Vector normal, float depth)
	{
		Point = point;
		Normal = normal;
		Depth = depth;
	}

	public void Flip()
	{
		Normal = -Normal;
	}
}
