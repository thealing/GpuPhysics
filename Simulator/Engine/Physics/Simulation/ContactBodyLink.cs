namespace Simulator.Engine.Physics.Simulation;

public struct ContactBodyLink
{
	public int BodyIndexA;
	public int BodyIndexB;

	public ContactBodyLink(int bodyIndexA, int bodyIndexB)
	{
		BodyIndexA = bodyIndexA;
		BodyIndexB = bodyIndexB;
	}
}
