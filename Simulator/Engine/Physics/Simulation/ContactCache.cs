namespace Simulator.Engine.Physics.Simulation;

public struct ContactCache
{
	public float NormalImpulse;
	public float TangentImpulse;

	public ContactCache(Contact contact)
	{
		NormalImpulse = contact.TotalNormalImpulse;
		TangentImpulse = contact.TotalTangentImpulse;
	}
}
