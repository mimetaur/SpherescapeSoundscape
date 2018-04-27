
using UnityEngine;
using UnityEditor;

[CanEditMultipleObjects, CustomEditor(typeof(MegaScale))]
public class MegaScaleEditor : MegaModifierEditor
{
	public override string GetHelpString() { return "Scale Modifier by Chris West"; }
	public override Texture LoadImage() { return (Texture)EditorGUIUtility.LoadRequired("MegaFiers\\skew_help.png"); }

	public override void OnInspectorGUI()
	{
		MegaScale mod = (MegaScale)target;

		// Basic mod stuff
		showmodparams = EditorGUILayout.Foldout(showmodparams, "Modifier Common Params");

		if ( showmodparams )
		{
			CommonModParamsBasic(mod);
		}

		mod.scale = EditorGUILayout.Vector3Field("Scale", mod.scale);

		if ( GUI.changed )
			EditorUtility.SetDirty(target);
	}

#if false
	public override bool Inspector()
	{
		MegaScale mod = (MegaScale)target;

#if !UNITY_5 && !UNITY_2017
		EditorGUIUtility.LookLikeControls();
#endif
		mod.scale = EditorGUILayout.Vector3Field("Scale", mod.scale);
		return false;
	}
#endif
}