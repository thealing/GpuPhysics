namespace Simulator.Engine.Core;

public interface IExecutor
{
	public void Execute<TCommand>(TCommand command, int count)
		where TCommand : struct, ICommand;
}
