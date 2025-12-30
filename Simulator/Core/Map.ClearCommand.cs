namespace Simulator.Core;

using System;

public partial struct Map<TKey, TValue, TStorage> where TKey : struct, IEquatable<TKey>
	where TValue : struct
	where TStorage : struct, IMapStorage<TKey, TValue>
{
	internal readonly struct ClearCommand : ICommand
	{
		public ClearCommand(Map<TKey, TValue, TStorage> instance)
		{
			_instance = instance;
		}

		public void Execute(int index)
		{
			_instance.Clear(index);
		}

		private readonly Map<TKey, TValue, TStorage> _instance;
	}
}
