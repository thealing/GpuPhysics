namespace WpfApp2;

using Simulator.Engine;
using Simulator.Engine.Geometry;

public class WorldSphere : ISphere
{
	public Vector Center => _worldCenter;
	public float Radius => _radius;

	public WorldSphere(Vector center, float radius)
	{
		_localCenter = center;
		_worldCenter = center;
		_radius = radius;
	}

	public void SetTransform(Transform transform)
	{
		_worldCenter = transform * _localCenter;
	}

	public void ApplyTransform(Transform transform)
	{
		_worldCenter = transform * _worldCenter;
	}

	private readonly Vector _localCenter;
	private readonly float _radius;
	private Vector _worldCenter;
}
