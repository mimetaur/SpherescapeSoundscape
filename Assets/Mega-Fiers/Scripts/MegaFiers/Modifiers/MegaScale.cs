
using UnityEngine;

[AddComponentMenu("Modifiers/Scale")]
public class MegaScale : MegaModifier
{
	public Vector3 scale = Vector3.one;

	public override string ModName() { return "Scale"; }
	public override string GetHelpURL() { return "?page_id=317"; }

	public override Vector3 Map(int i, Vector3 p)
	{
		p.x *= scale.x;
		p.y *= scale.y;
		p.z *= scale.z;
		return p;
	}
}
