namespace Simulator.Backend.Cpu;

using Simulator.Engine.Core;

public class SequentialExecutor : IExecutor
{
	public void Execute<TCommand>(TCommand command, int count)
		where TCommand : struct, ICommand
	{
		for (int index = 0; index < count; index++)
		{
			command.Execute(index);
		}
	}
}
