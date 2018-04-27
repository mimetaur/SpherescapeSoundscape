
using UnityEngine;

[AddComponentMenu("Modifiers/Hit")]
public class MegaHitDeform : MegaModifier
{
	public override string		ModName()		{ return "Hit Deform"; }
	public float				hitradius		= 1.0f;
	public float				Hardness		= 0.5f;
	public float				deformlimit		= 0.1f;
	public float				scaleforce		= 1000.0f;
	public float				maxForce		= 1.0f;
	Vector3[]					offsets;
	float						msize;

	public override void Modify(MegaModifiers mc)
	{
		if ( offsets != null )
		{
			for ( int i = 0; i < sverts.Length; i++ )
				sverts[i] = verts[i] + offsets[i];
		}
	}

	public void Deform(Vector3 point, Vector3 normal, float force)
	{
		force = Mathf.Min(maxForce, force);

		if ( force > 0.01f )
		{
			float df = force * (msize * (0.1f / Mathf.Max(0.1f, Hardness)));

			float max = deformlimit;
			float maxsqr = max * max;

			Vector3 p = transform.InverseTransformPoint(point);
			Vector3 nr = transform.InverseTransformDirection(normal);

			for ( int i = 0; i < verts.Length; i++ )
			{
				float d = ((verts[i] + offsets[i]) - p).sqrMagnitude;
				if ( d <= df )
				{
					Vector3 n = nr * (1.0f - (d / df)) * df;

					offsets[i] += n;

					if ( deformlimit > 0.0f )
					{
						n = offsets[i];
						d = n.sqrMagnitude;
						if ( d > maxsqr )
							offsets[i] = (n * (max / n.magnitude));
					}
				}
			}
		}
	}

	void Deform(Collision collision)
	{
#if UNITY_5 || UNITY_2017
		float cf = Mathf.Min(maxForce, collision.relativeVelocity.sqrMagnitude / scaleforce);
#else
		float cf = Mathf.Min(maxForce, collision.impactForceSum.sqrMagnitude / scaleforce);
#endif
     
		if ( cf > 0.01f )
		{
			ContactPoint[] contacts = collision.contacts;

			for ( int c = 0; c < contacts.Length; c++ )
				Deform(contacts[c].point, contacts[c].normal, cf);
		}
	}

	public override bool ModLateUpdate(MegaModContext mc)
	{
		return Prepare(mc);
	}

	public override bool Prepare(MegaModContext mc)
	{
		if ( offsets == null || offsets.Length != mc.mod.verts.Length )
			offsets = new Vector3[mc.mod.verts.Length];

		msize = MegaUtils.SmallestValue(mc.bbox.Size());
		return true;
	}

	public void Repair(float repair)
	{
		Repair(repair, Vector3.zero, 0.0f);
	}

	public void Repair(float repair, Vector3 point, float radius)
	{
		point = transform.InverseTransformPoint(point);

		float rsqr = radius * radius;
		for ( int i = 0; i < offsets.Length; i++ )
		{
			if ( radius > 0.0f )
			{
				Vector3 vector3 = point - verts[i];
				if ( vector3.sqrMagnitude >= rsqr )
					break;
			}
			offsets[i] *= repair;
		}
	}

	public void OnCollisionEnter(Collision collision)
	{
		Deform(collision);
	}

	public void OnCollisionStay(Collision collision)
	{
		Deform(collision);
	}
}