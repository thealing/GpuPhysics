namespace Simulator.Engine.Collisions.NarrowPhase;

using Simulator.Engine.Geometry;

public static class SpherePolyhedronCollision
{
	public static bool Check<TSphere, TPolyhedron>(TSphere sphere, TPolyhedron polyhedron, ref Collision collision)
		where TSphere : ISphere
		where TPolyhedron : IPolyhedron
	{
		if (PolyhedronSphereCollision.Check(polyhedron, sphere, ref collision) == false)
		{
			return false;
		}
		collision.Flip();
		return true;
	}
}
