namespace Simulator.Backend.Cpu;

using Simulator.Engine.Core;

public readonly partial struct BodyStorage
{
	internal readonly struct ResetBodyCommand : ICommand
	{
		public ResetBodyCommand(BodyStorage storage)
		{
			_storage = storage;
		}

		public void Execute(int index)
		{
			_storage.ContactCounts[index] = 0;
			_storage.SplitNodes[index] = new Node(-1);
		}

		private readonly BodyStorage _storage;
	}
}
