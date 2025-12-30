namespace Simulator.Engine.Collisions.BroadPhase;

public interface ICollisionCallback
{
	public void ProcessCollision(int indexA, int indexB);
}
