namespace Simulator.Engine.Collisions.BroadPhase;

using Simulator.Core;

public interface ICollisionMap
{
	public void PrepareUpdate(IExecutor executor);

	public void UpdateBound(int index, Bound bound);

	public void DetectCollisions<TCollisionCallback>(IExecutor executor, TCollisionCallback callback)
		where TCollisionCallback : struct, ICollisionCallback;
}
