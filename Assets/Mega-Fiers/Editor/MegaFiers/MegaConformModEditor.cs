
using UnityEngine;
using UnityEditor;

[CanEditMultipleObjects, CustomEditor(typeof(MegaConformMod))]
public class MegaConformModEditor : MegaModifierEditor
{
	public override string GetHelpString() { return "Conform Modifier by Chris West"; }
	public override Texture LoadImage() { return (Texture)EditorGUIUtility.LoadRequired("MegaFiers\\bend_help.png"); }

	public override bool DisplayCommon()
	{
		return false;
	}


	public override bool Inspector()
	{
		MegaConformMod mod = (MegaConformMod)target;

#if !UNITY_5 && !UNITY_2017
		EditorGUIUtility.LookLikeControls();
#endif
		CommonModParamsBasic(mod);

		mod.target = (GameObject)EditorGUILayout.ObjectField("Target", mod.target, typeof(GameObject), true);
		mod.conformAmount = EditorGUILayout.Slider("Conform Amount", mod.conformAmount, 0.0f, 1.0f);
		mod.raystartoff = EditorGUILayout.FloatField("Ray Start Off", mod.raystartoff);
		mod.raydist = EditorGUILayout.FloatField("Ray Dist", mod.raydist);
		mod.offset = EditorGUILayout.FloatField("Offset", mod.offset);
		MegaAxis axis = (MegaAxis)EditorGUILayout.EnumPopup("Axis", mod.axis);
		if ( axis != mod.axis )
		{
			mod.axis = axis;
			mod.ChangeAxis();
		}

		mod.useLocalDown = EditorGUILayout.BeginToggleGroup("Use Local Down", mod.useLocalDown);
		mod.flipDown = EditorGUILayout.Toggle("Flip Down", mod.flipDown);
		mod.downAxis = (MegaAxis)EditorGUILayout.EnumPopup("Down Axis", mod.downAxis);
		EditorGUILayout.EndToggleGroup();
		return false;
	}
}