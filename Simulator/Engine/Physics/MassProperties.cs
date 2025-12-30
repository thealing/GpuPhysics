namespace Simulator.Engine.Physics;

using Simulator.Engine.Geometry;

public struct MassProperties
{
	public float LinearMass;
	public Matrix AngularMass;
	public Vector Centroid;

	public MassProperties(float linearMass, Matrix angularMass, Vector centroid)
	{
		LinearMass = linearMass;
		AngularMass = angularMass;
		Centroid = centroid;
	}

	public MassProperties(ShapeProperties shapeProperties, float density)
	{
		LinearMass = shapeProperties.Volume * density;
		AngularMass = shapeProperties.InertiaTensor * LinearMass;
		Centroid = shapeProperties.Centroid;
	}

	public static MassProperties Combine(MassProperties a, MassProperties b)
	{
		float linearMass = a.LinearMass + b.LinearMass;
		Vector centroid = (a.Centroid * a.LinearMass + b.Centroid * b.LinearMass) / linearMass;
		Vector displacementA = centroid - a.Centroid;
		Vector displacementB = centroid - b.Centroid;
		Matrix displacementTensorA = GetDisplacementTensor(displacementA, a.LinearMass);
		Matrix displacementTensorB = GetDisplacementTensor(displacementB, b.LinearMass);
		Matrix angularMass = a.AngularMass + b.AngularMass + displacementTensorA + displacementTensorB;
		return new MassProperties(linearMass, angularMass, centroid);
	}

	private static Matrix GetDisplacementTensor(Vector displacement, float scale)
	{
		Vector scaledDisplacement = displacement * scale;
		float matrixUnit = Vector.Dot(displacement, scaledDisplacement);
		Matrix matrix = Matrix.CreateUnit(matrixUnit);
		matrix.X -= displacement * scaledDisplacement.X;
		matrix.Y -= displacement * scaledDisplacement.Y;
		matrix.Z -= displacement * scaledDisplacement.Z;
		return matrix;
	}
}
