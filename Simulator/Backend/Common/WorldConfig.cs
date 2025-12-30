namespace Simulator.Backend.Common;

using Simulator.Engine.Physics;

public struct WorldConfig
{
	public float DeltaTime;
	public int IterationCount;
	public Twist Gravity;
	public byte UseWarmStarting;
	public float CorrectionVelocityFactor;
	public float CorrectionVelocityLimit;

	public WorldConfig()
	{
	}
}
