namespace Simulator.Backend.Cpu;

using System.Diagnostics;
using System.Threading.Tasks;
using Simulator.Core;

public class Executor : IExecutor
{
	public ParallelOptions ParallelOptions;

	public Executor()
	{
		ParallelOptions = new ParallelOptions
		{
			MaxDegreeOfParallelism = 7
		};
	}

	public void Execute<TCommand>(TCommand command, int count)
		where TCommand : struct, ICommand
	{
		if (Debugger.IsAttached)
		{
			for (int index = 0; index < count; index++)
			{
				command.Execute(index);
			}
		}
		else
		{
			Parallel.For(0, count, ParallelOptions, command.Execute);
		}
	}
}
