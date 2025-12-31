namespace Simulator.Engine.Collisions.BroadPhase;

using Simulator.Engine.Core;

public partial struct DynamicGrid<TStorage> where TStorage : IDynamicGridStorage
{
	internal readonly struct UpdateNodeCommand : ICommand
	{
		public UpdateNodeCommand(DynamicGrid<TStorage> instance)
		{
			_instance = instance;
		}

		public void Execute(int index)
		{
			_instance.UpdateNode(index);
		}

		private readonly DynamicGrid<TStorage> _instance;
	}

	internal readonly struct CollisionCommand<TCollisionCallback> : ICommand
		where TCollisionCallback : struct, ICollisionCallback
	{
		public CollisionCommand(DynamicGrid<TStorage> instance, TCollisionCallback callback)
		{
			_instance = instance;
			_callback = callback;
		}

		public void Execute(int index)
		{
			_instance.DetectCollisions(index, _callback);
		}

		private readonly DynamicGrid<TStorage> _instance;
		private readonly TCollisionCallback _callback;
	}
}
