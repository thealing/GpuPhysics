namespace Simulator.Engine.Physics.Simulation;

using System.Diagnostics;

public class Timer
{
	public Timer()
	{
		_stopwatch = new Stopwatch();
		_stopwatch.Start();
	}

	public double Lap()
	{
		double time = _stopwatch.Elapsed.TotalSeconds;
		_stopwatch.Restart();
		return time;
	}

	private readonly Stopwatch _stopwatch;
}
