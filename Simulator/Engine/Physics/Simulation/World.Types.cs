namespace Simulator.Engine.Physics.Simulation;

using Simulator.Core;
using Simulator.Engine.Collisions.BroadPhase;

public partial struct World<TStorage, TCollisionMap>
	where TStorage : struct, IWorldStorage
	where TCollisionMap : struct, ICollisionMap
{
	internal readonly struct UpdateShapeTransformCommand : ICommand
	{
		public UpdateShapeTransformCommand(World<TStorage, TCollisionMap> world)
		{
			_world = world;
		}

		public void Execute(int index)
		{
			_world.UpdateShapeTransform(index);
		}

		private readonly World<TStorage, TCollisionMap> _world;
	}

	internal readonly struct CollisionCallback : ICollisionCallback
	{
		public CollisionCallback(World<TStorage, TCollisionMap> world)
		{
			_world = world;
		}

		public void ProcessCollision(int indexA, int indexB)
		{
			_world.CheckShapeCollision(indexA, indexB);
		}

		private readonly World<TStorage, TCollisionMap> _world;
	}

	internal readonly struct UpdateBodyTransformCommand : ICommand
	{
		public UpdateBodyTransformCommand(World<TStorage, TCollisionMap> world)
		{
			_world = world;
		}

		public void Execute(int index)
		{
			_world.UpdateBodyTransform(index);
		}

		private readonly World<TStorage, TCollisionMap> _world;
	}

	internal readonly struct PrepareContactCommand : ICommand
	{
		public PrepareContactCommand(World<TStorage, TCollisionMap> world)
		{
			_world = world;
		}

		public void Execute(int index)
		{
			_world.PrepareContact(index);
		}

		private readonly World<TStorage, TCollisionMap> _world;
	}

	internal readonly struct SolveContactCommand : ICommand
	{
		public SolveContactCommand(World<TStorage, TCollisionMap> world)
		{
			_world = world;
		}

		public void Execute(int index)
		{
			_world.SolveContact(index);
		}

		private readonly World<TStorage, TCollisionMap> _world;
	}

	internal readonly struct SaveContactCommand : ICommand
	{
		public SaveContactCommand(World<TStorage, TCollisionMap> world)
		{
			_world = world;
		}

		public void Execute(int index)
		{
			_world.SaveContactCache(index);
		}

		private readonly World<TStorage, TCollisionMap> _world;
	}

	internal readonly struct LoadContactCommand : ICommand
	{
		public LoadContactCommand(World<TStorage, TCollisionMap> world)
		{
			_world = world;
		}

		public void Execute(int index)
		{
			_world.LoadContactCache(index);
		}

		private readonly World<TStorage, TCollisionMap> _world;
	}

	internal readonly struct ApplyBodySplitImpulsesCommand : ICommand
	{
		public ApplyBodySplitImpulsesCommand(World<TStorage, TCollisionMap> world)
		{
			_world = world;
		}

		public void Execute(int index)
		{
			_world.ApplyBodySplitImpulses(index);
		}

		private readonly World<TStorage, TCollisionMap> _world;
	}

	internal readonly struct FinalizeBodyTransformCommand : ICommand
	{
		public FinalizeBodyTransformCommand(World<TStorage, TCollisionMap> world)
		{
			_world = world;
		}

		public void Execute(int index)
		{
			_world.FinalizeBodyTransform(index);
		}

		private readonly World<TStorage, TCollisionMap> _world;
	}

	internal readonly struct ApplyBodyGravityCommand : ICommand
	{
		public ApplyBodyGravityCommand(World<TStorage, TCollisionMap> world)
		{
			_world = world;
		}

		public void Execute(int index)
		{
			_world.ApplyBodyGravity(index);
		}

		private readonly World<TStorage, TCollisionMap> _world;
	}
}
