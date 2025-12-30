namespace Simulator.Engine.Physics.Simulation;

public struct ContactSplitLink
{
	public int SplitIndexA;
	public int SplitIndexB;

	public ContactSplitLink(int splitIndexA, int splitIndexB)
	{
		SplitIndexA = splitIndexA;
		SplitIndexB = splitIndexB;
	}

	public static ContactSplitLink FromDensePairIndex(int contactIndex)
	{
		return new ContactSplitLink(contactIndex * 2, contactIndex * 2 + 1);
	}
}
