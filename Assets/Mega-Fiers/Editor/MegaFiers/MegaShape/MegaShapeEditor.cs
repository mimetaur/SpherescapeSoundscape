
using UnityEditor;
using UnityEngine;
using System.IO;
using System.Collections.Generic;

public class MegaSplineUndo
{
	public int			curve;
	public MegaSpline	spline;
}


[CustomEditor(typeof(MegaShape))]
public class MegaShapeEditor : Editor
{
	public bool		showcommon	= true;
	public float	outline		= 0.0f;
	int				selected	= -1;
	Vector3			pm			= new Vector3();
	Vector3			delta		= new Vector3();
	bool			showsplines	= false;
	bool			showknots	= false;
	bool			showlabels	= true;
	float			ImportScale	= 1.0f;
#if UNITY_5_5 || UNITY_5_6 || UNITY_2017 || UNITY_2018 || UNITY_2019
#else
	bool hidewire = false;
#endif

	Bounds			bounds;
	string			lastpath	= "";
	MegaKnotAnim	ma;

	public bool		showfuncs = false;
	public bool		export = false;
	public MegaAxis	xaxis = MegaAxis.X;
	public MegaAxis	yaxis = MegaAxis.Z;
	public float	strokewidth = 1.0f;
	public Color	strokecol = Color.black;

	static public Vector3	CursorPos		= Vector3.zero;
	static public Vector3	CursorSpline	= Vector3.zero;
	static public Vector3	CursorTangent	= Vector3.zero;
	static public int		CursorKnot		= 0;

	public delegate bool ParseBinCallbackType(BinaryReader br, string id);
	public delegate void ParseClassCallbackType(string classname, BinaryReader br);

	static public MegaShapeEditor editor;

	public int	maxUndo	= 20;
	public List<MegaSplineUndo>	undoHistory = new List<MegaSplineUndo>();

#if UNITY_2017_2 || UNITY_2017_3 || UNITY_2018 || UNITY_2019
	Dictionary<int, string> myControls = new Dictionary<int, string>();
	public static int cid = 0;
#endif

	void NewControlFrame()
	{
#if UNITY_2017_2 || UNITY_2017_3 || UNITY_2018 || UNITY_2019
		myControls.Clear();
		cid = 10661066;
#else
#endif
	}

	void SetControlName(string name)
	{
#if UNITY_2017_2 || UNITY_2017_3 || UNITY_2018 || UNITY_2019
		cid++;
		if ( myControls == null )
			myControls = new Dictionary<int, string>();

		//Debug.Log("add " + cid + " " + name);
		myControls.Add(cid, name);
#else
		GUI.SetNextControlName(name);
#endif
	}

	string GetControlName()
	{
#if UNITY_2017_2 || UNITY_2017_3 || UNITY_2018 || UNITY_2019
		if ( myControls != null )
		{
			int hc = GUIUtility.hotControl;
			//Debug.Log("hc " + hc);

			if ( myControls.ContainsKey(hc) )
			{
				//Debug.Log("name " + myControls[hc]);
				return myControls[hc];
			}
		}

		return "";
#else
		return GUI.GetNameOfFocusedControl();
#endif
	}

	public void PushSpline(MegaSpline s, int c)
	{
		MegaSplineUndo su = new MegaSplineUndo();
		su.curve = c;
		su.spline = new MegaSpline();
		su.spline.closed = s.closed;

		for ( int k = 0; k < s.knots.Count; k++ )
		{
			MegaKnot knot = new MegaKnot();
			knot.p = s.knots[k].p;
			knot.invec = s.knots[k].invec;
			knot.outvec = s.knots[k].outvec;
			knot.twist = s.knots[k].twist;
			knot.id = s.knots[k].id;
			knot.notlocked = s.knots[k].notlocked;

			su.spline.knots.Add(knot);
		}

		undoHistory.Add(su);
		if ( undoHistory.Count > maxUndo )
		{
			undoHistory.RemoveAt(0);
		}
	}

	public void PopSpline(MegaShape s)
	{
		if ( undoHistory.Count > 0 )
		{
			MegaSplineUndo su = undoHistory[undoHistory.Count - 1];

			s.splines[su.curve] = su.spline;

			undoHistory.RemoveAt(undoHistory.Count - 1);
		}
	}

	public virtual bool Params()
	{
		MegaShape shape = (MegaShape)target;

		bool rebuild = false;

		float radius = EditorGUILayout.FloatField("Radius", shape.defRadius);
		if ( radius != shape.defRadius )
		{
			if ( radius < 0.001f )
				radius = 0.001f;

			shape.defRadius = radius;
			rebuild = true;
		}

		return rebuild;
	}

	public override void OnInspectorGUI()
	{
		bool buildmesh = false;
		bool recalc = false;
		MegaShape shape = (MegaShape)target;
		editor = this;

		EditorGUILayout.BeginHorizontal();

		int curve = shape.selcurve;

		if ( GUILayout.Button("Add Knot") )
		{
			if ( shape.splines == null || shape.splines.Count == 0 )
			{
				MegaSpline spline = new MegaSpline();	// Have methods for these
				shape.splines.Add(spline);
			}

			PushSpline(shape.splines[shape.selcurve], shape.selcurve);

			MegaKnot knot = new MegaKnot();
			float per = shape.CursorPercent * 0.01f;

			CursorTangent = shape.splines[curve].Interpolate(per + 0.01f, true, ref CursorKnot);	//this.GetPositionOnSpline(i) - p;
			CursorPos = shape.splines[curve].Interpolate(per, true, ref CursorKnot);	//this.GetPositionOnSpline(i) - p;

			knot.p = CursorPos;
			knot.outvec = (CursorTangent - knot.p);
			knot.outvec.Normalize();
			knot.outvec *= shape.splines[curve].knots[CursorKnot].seglength * 0.25f;
			knot.invec = -knot.outvec;
			knot.invec += knot.p;
			knot.outvec += knot.p;
			knot.twist = shape.splines[curve].knots[CursorKnot].twist;
			knot.id = shape.splines[curve].knots[CursorKnot].id;

			shape.splines[curve].knots.Insert(CursorKnot + 1, knot);

			if ( shape.smoothonaddknot )
				shape.AutoCurve(shape.splines[shape.selcurve]);	//, knum, knum + 2);

			shape.CalcLength();	//10);
			EditorUtility.SetDirty(target);
			buildmesh = true;
		}

		if ( GUILayout.Button("Delete Knot") )
		{
			if ( selected != -1 )
			{
				PushSpline(shape.splines[shape.selcurve], shape.selcurve);

				shape.splines[curve].knots.RemoveAt(selected);
				selected--;
				shape.CalcLength();	//10);
				recalc = true;
			}
			EditorUtility.SetDirty(target);
			buildmesh = true;
		}
		
		EditorGUILayout.EndHorizontal();
		EditorGUILayout.BeginHorizontal();

		if ( GUILayout.Button("Match Handles") )
		{
			if ( selected != -1 )
			{
				PushSpline(shape.splines[shape.selcurve], shape.selcurve);

				Vector3 p = shape.splines[curve].knots[selected].p;
				Vector3 d = shape.splines[curve].knots[selected].outvec - p;
				shape.splines[curve].knots[selected].invec = p - d;
				shape.CalcLength();	//10);
				recalc = true;
			}
			EditorUtility.SetDirty(target);
			buildmesh = true;
		}

		if ( GUILayout.Button("Load") )
		{
			LoadShape(ImportScale);
			buildmesh = true;
		}

		if ( GUILayout.Button("Load SXL") )
		{
			LoadSXL(ImportScale);
			buildmesh = true;
		}

		if ( GUILayout.Button("Load KML") )
		{
			LoadKML(ImportScale);
			buildmesh = true;
		}

		EditorGUILayout.EndHorizontal();

		EditorGUILayout.BeginHorizontal();
		if ( GUILayout.Button("AutoCurve") )
		{
			PushSpline(shape.splines[shape.selcurve], shape.selcurve);

			shape.AutoCurve();
			EditorUtility.SetDirty(target);
			buildmesh = true;
		}

		if ( GUILayout.Button("Reverse") )
		{
			PushSpline(shape.splines[shape.selcurve], shape.selcurve);

			shape.Reverse(curve);
			EditorUtility.SetDirty(target);
			buildmesh = true;
			recalc = true;
		}

		EditorGUILayout.EndHorizontal();

		if ( GUILayout.Button("Centre Shape") )
		{
			PushSpline(shape.splines[shape.selcurve], shape.selcurve);

			shape.Centre(1.0f, Vector3.one);
			EditorUtility.SetDirty(target);
			buildmesh = true;
		}

		if ( GUILayout.Button("Apply Scaling") )
		{
			PushSpline(shape.splines[shape.selcurve], shape.selcurve);

			shape.Scale(shape.transform.localScale);
			EditorUtility.SetDirty(target);
			shape.transform.localScale = Vector3.one;
			buildmesh = true;
		}

		if ( GUILayout.Button("Import SVG") )
		{
			LoadSVG(ImportScale);
			buildmesh = true;
		}

		EditorGUILayout.BeginVertical("box");
		maxUndo = EditorGUILayout.IntField("Max Undo", maxUndo);
		maxUndo = Mathf.Clamp(maxUndo, 2, 40);

		if ( GUILayout.Button("Undo Edit Spline (" + undoHistory.Count + ")") )
		{
			PopSpline(shape);
			recalc = true;
		}

		if ( GUILayout.Button("Clear Undo History") )
		{
			undoHistory.Clear();
		}

		EditorGUILayout.EndVertical();

		showcommon = EditorGUILayout.Foldout(showcommon, "Common Params");

		bool rebuild = false;	//Params();

		if ( showcommon )
		{
			shape.CursorPercent = EditorGUILayout.FloatField("Cursor", shape.CursorPercent);
			shape.CursorPercent = Mathf.Repeat(shape.CursorPercent, 100.0f);

			ImportScale = EditorGUILayout.FloatField("Import Scale", ImportScale);

			MegaAxis av = (MegaAxis)EditorGUILayout.EnumPopup("Axis", shape.axis);
			if ( av != shape.axis )
			{
				shape.axis = av;
				rebuild = true;
			}

			if ( shape.splines.Count > 1 )
				shape.selcurve = EditorGUILayout.IntSlider("Curve", shape.selcurve, 0, shape.splines.Count - 1);

			if ( shape.selcurve < 0 )
				shape.selcurve = 0;

			if ( shape.selcurve > shape.splines.Count - 1 )
				shape.selcurve = shape.splines.Count - 1;

			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.LabelField("Colors");
			shape.col1 = EditorGUILayout.ColorField(shape.col1);
			shape.col2 = EditorGUILayout.ColorField(shape.col2);
			EditorGUILayout.EndHorizontal();

			shape.VecCol = EditorGUILayout.ColorField("Vec Col", shape.VecCol);

			shape.KnotSize = EditorGUILayout.FloatField("Knot Size", shape.KnotSize);
			shape.stepdist = EditorGUILayout.FloatField("Step Dist", shape.stepdist);

			MegaSpline spline = shape.splines[shape.selcurve];

			if ( shape.stepdist < 0.01f )
				shape.stepdist = 0.01f;

			shape.dolateupdate = EditorGUILayout.Toggle("Do Late Update", shape.dolateupdate);
			shape.normalizedInterp = EditorGUILayout.Toggle("Normalized Interp", shape.normalizedInterp);

			spline.constantSpeed = EditorGUILayout.Toggle("Constant Speed", spline.constantSpeed);
			int subdivs = EditorGUILayout.IntField("Calc Subdivs", spline.subdivs);

			if ( subdivs < 2 )
				subdivs = 2;
			if ( subdivs != spline.subdivs )
				spline.CalcLength(subdivs);

			shape.drawHandles = EditorGUILayout.Toggle("Draw Handles", shape.drawHandles);
			shape.drawKnots = EditorGUILayout.Toggle("Draw Knots", shape.drawKnots);
			shape.drawTwist = EditorGUILayout.Toggle("Draw Twist", shape.drawTwist);
			shape.drawspline = EditorGUILayout.Toggle("Draw Spline", shape.drawspline);
			shape.showorigin = EditorGUILayout.Toggle("Origin Handle", shape.showorigin);
			shape.lockhandles = EditorGUILayout.Toggle("Lock Handles", shape.lockhandles);
			shape.updateondrag = EditorGUILayout.Toggle("Update On Drag", shape.updateondrag);

			shape.usesnap = EditorGUILayout.BeginToggleGroup("Use Snap", shape.usesnap);
			shape.usesnaphandles = EditorGUILayout.Toggle("Snap Handles", shape.usesnaphandles);
			shape.snap = EditorGUILayout.Vector3Field("Snap", shape.snap);
			if ( shape.snap.x < 0.0f ) shape.snap.x = 0.0f;
			if ( shape.snap.y < 0.0f ) shape.snap.y = 0.0f;
			if ( shape.snap.z < 0.0f ) shape.snap.z = 0.0f;
			EditorGUILayout.EndToggleGroup();

			MegaShapeBezComputeMode smode = (MegaShapeBezComputeMode)EditorGUILayout.EnumPopup("Smooth Mode", shape.smoothMode);
			if ( smode != shape.smoothMode )
			{
				shape.smoothMode = smode;
				shape.AutoCurve();
				recalc = true;
			}

			if ( shape.smoothMode == MegaShapeBezComputeMode.Old )
			{
				float smoothness = EditorGUILayout.Slider("Smoothness", shape.smoothness, 0.0f, 1.5f);
				if ( smoothness != shape.smoothness )
				{
					shape.smoothness = smoothness;
					shape.AutoCurve();
					recalc = true;
				}
			}

			shape.smoothOnDrag = EditorGUILayout.Toggle("Smooth on Drag", shape.smoothOnDrag);
			shape.smoothonaddknot = EditorGUILayout.Toggle("Smooth on Add Knot", shape.smoothonaddknot);

			shape.handleType = (MegaHandleType)EditorGUILayout.EnumPopup("Handle Type", shape.handleType);

			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.LabelField("Freeze Movement", GUILayout.Width(144));
			EditorGUILayout.LabelField("x", GUILayout.Width(12));
			shape.freezeX = EditorGUILayout.Toggle("", shape.freezeX, GUILayout.Width(20));
			EditorGUILayout.LabelField("y", GUILayout.Width(12));
			shape.freezeY = EditorGUILayout.Toggle("", shape.freezeY, GUILayout.Width(20));
			EditorGUILayout.LabelField("z", GUILayout.Width(12));
			shape.freezeZ = EditorGUILayout.Toggle("", shape.freezeZ, GUILayout.Width(20));
			EditorGUILayout.EndHorizontal();

			showlabels = EditorGUILayout.Toggle("Labels", showlabels);

#if UNITY_5_5 || UNITY_5_6 || UNITY_2017 || UNITY_2018 || UNITY_2019
#else
			bool hidewire1 = EditorGUILayout.Toggle("Hide Wire", hidewire);

			if ( hidewire1 != hidewire )
			{
				hidewire = hidewire1;
				EditorUtility.SetSelectedWireframeHidden(shape.GetComponent<Renderer>(), hidewire);
			}
#endif

			shape.animate = EditorGUILayout.Toggle("Animate", shape.animate);
			if ( shape.animate )
			{
				shape.time = EditorGUILayout.FloatField("Time", shape.time);
				shape.MaxTime = EditorGUILayout.FloatField("Loop Time", shape.MaxTime);
				shape.speed = EditorGUILayout.FloatField("Speed", shape.speed);
				shape.LoopMode = (MegaRepeatMode)EditorGUILayout.EnumPopup("Loop Mode", shape.LoopMode);
			}

			AnimationKeyFrames(shape);

			if ( shape.splines.Count > 0 )
			{
				if ( spline.outlineSpline != -1 )
				{
					int outlineSpline = EditorGUILayout.IntSlider("Outline Spl", spline.outlineSpline, 0, shape.splines.Count - 1);
					float outline = EditorGUILayout.FloatField("Outline", spline.outline);

					if ( outline != spline.outline || outlineSpline != spline.outlineSpline )
					{
						spline.outlineSpline = outlineSpline;
						spline.outline = outline;
						if ( outlineSpline != shape.selcurve )
						{
							shape.OutlineSpline(shape.splines[spline.outlineSpline], spline, spline.outline, true);
							spline.CalcLength();	//10);
							EditorUtility.SetDirty(target);
							buildmesh = true;
						}
					}
				}
				else
				{
					outline = EditorGUILayout.FloatField("Outline", outline);

					if ( GUILayout.Button("Outline") )
					{
						shape.OutlineSpline(shape, shape.selcurve, outline, true);
						shape.splines[shape.splines.Count - 1].outline = outline;
						shape.splines[shape.splines.Count - 1].outlineSpline = shape.selcurve;
						shape.selcurve = shape.splines.Count - 1;
						EditorUtility.SetDirty(target);
						buildmesh = true;
					}
				}
			}

			// Mesher
			shape.makeMesh = EditorGUILayout.Toggle("Make Mesh", shape.makeMesh);

			if ( shape.makeMesh )
			{
				shape.meshType = (MeshShapeType)EditorGUILayout.EnumPopup("Mesh Type", shape.meshType);
				shape.Pivot = EditorGUILayout.Vector3Field("Pivot", shape.Pivot);

				shape.CalcTangents = EditorGUILayout.Toggle("Calc Tangents", shape.CalcTangents);
				shape.GenUV = EditorGUILayout.Toggle("Gen UV", shape.GenUV);

				if ( GUILayout.Button("Build LightMap") )
				{
					MegaShapeLightMapWindow.Init();
				}

				EditorGUILayout.BeginVertical("Box");
				switch ( shape.meshType )
				{
					case MeshShapeType.Fill:
						shape.DoubleSided = EditorGUILayout.Toggle("Double Sided", shape.DoubleSided);
						shape.Height = EditorGUILayout.FloatField("Height", shape.Height);
						shape.UseHeightCurve = EditorGUILayout.Toggle("Use Height Crv", shape.UseHeightCurve);
						if ( shape.UseHeightCurve )
						{
							shape.heightCrv = EditorGUILayout.CurveField("Height Curve", shape.heightCrv);
							shape.heightOff = EditorGUILayout.Slider("Height Off", shape.heightOff, -1.0f, 1.0f);
						}
						shape.mat1 = (Material)EditorGUILayout.ObjectField("Top Mat", shape.mat1, typeof(Material), true);
						shape.mat2 = (Material)EditorGUILayout.ObjectField("Bot Mat", shape.mat2, typeof(Material), true);
						shape.mat3 = (Material)EditorGUILayout.ObjectField("Side Mat", shape.mat3, typeof(Material), true);

						shape.PhysUV = EditorGUILayout.Toggle("Physical UV", shape.PhysUV);
						shape.UVOffset = EditorGUILayout.Vector2Field("UV Offset", shape.UVOffset);
						shape.UVRotate = EditorGUILayout.Vector2Field("UV Rotate", shape.UVRotate);
						shape.UVScale = EditorGUILayout.Vector2Field("UV Scale", shape.UVScale);
						shape.UVOffset1 = EditorGUILayout.Vector2Field("UV Offset1", shape.UVOffset1);
						shape.UVRotate1 = EditorGUILayout.Vector2Field("UV Rotate1", shape.UVRotate1);
						shape.UVScale1 = EditorGUILayout.Vector2Field("UV Scale1", shape.UVScale1);
						break;

					case MeshShapeType.Tube:
						shape.TubeStart = EditorGUILayout.Slider("Start", shape.TubeStart, -1.0f, 2.0f);
						shape.TubeLength = EditorGUILayout.Slider("Length", shape.TubeLength, 0.0f, 1.0f);
						shape.rotate = EditorGUILayout.FloatField("Rotate", shape.rotate);
						shape.tsides = EditorGUILayout.IntField("Sides", shape.tsides);
						shape.tradius = EditorGUILayout.FloatField("Radius", shape.tradius);
						shape.offset = EditorGUILayout.FloatField("Offset", shape.offset);

						shape.scaleX = EditorGUILayout.CurveField("Scale X", shape.scaleX);
						shape.unlinkScale = EditorGUILayout.BeginToggleGroup("unlink Scale", shape.unlinkScale);
						shape.scaleY = EditorGUILayout.CurveField("Scale Y", shape.scaleY);
						EditorGUILayout.EndToggleGroup();

						shape.strands = EditorGUILayout.IntField("Strands", shape.strands);
						if ( shape.strands > 1 )
						{
							shape.strandRadius = EditorGUILayout.FloatField("Strand Radius", shape.strandRadius);
							shape.TwistPerUnit = EditorGUILayout.FloatField("Twist", shape.TwistPerUnit);
							shape.startAng = EditorGUILayout.FloatField("Start Twist", shape.startAng);
						}
						shape.UVOffset = EditorGUILayout.Vector2Field("UV Offset", shape.UVOffset);
						shape.uvtilex = EditorGUILayout.FloatField("UV Tile X", shape.uvtilex);
						shape.uvtiley = EditorGUILayout.FloatField("UV Tile Y", shape.uvtiley);

						shape.cap = EditorGUILayout.Toggle("Cap", shape.cap);
						shape.RopeUp = (MegaAxis)EditorGUILayout.EnumPopup("Up", shape.RopeUp);
						shape.mat1 = (Material)EditorGUILayout.ObjectField("Mat", shape.mat1, typeof(Material), true);
						shape.flipNormals = EditorGUILayout.Toggle("Flip Normals", shape.flipNormals);
						break;

					case MeshShapeType.Ribbon:
						shape.TubeStart = EditorGUILayout.Slider("Start", shape.TubeStart, -1.0f, 2.0f);
						shape.TubeLength = EditorGUILayout.Slider("Length", shape.TubeLength, 0.0f, 1.0f);
						shape.boxwidth = EditorGUILayout.FloatField("Width", shape.boxwidth);
						shape.raxis = (MegaAxis)EditorGUILayout.EnumPopup("Axis", shape.raxis);
						shape.rotate = EditorGUILayout.FloatField("Rotate", shape.rotate);
						shape.ribsegs = EditorGUILayout.IntField("Segs", shape.ribsegs);
						if ( shape.ribsegs < 1 )
							shape.ribsegs = 1;
						shape.offset = EditorGUILayout.FloatField("Offset", shape.offset);

						shape.scaleX = EditorGUILayout.CurveField("Scale X", shape.scaleX);

						shape.strands = EditorGUILayout.IntField("Strands", shape.strands);
						if ( shape.strands > 1 )
						{
							shape.strandRadius = EditorGUILayout.FloatField("Strand Radius", shape.strandRadius);
							shape.TwistPerUnit = EditorGUILayout.FloatField("Twist", shape.TwistPerUnit);
							shape.startAng = EditorGUILayout.FloatField("Start Twist", shape.startAng);
						}

						shape.UVOffset = EditorGUILayout.Vector2Field("UV Offset", shape.UVOffset);
						shape.uvtilex = EditorGUILayout.FloatField("UV Tile X", shape.uvtilex);
						shape.uvtiley = EditorGUILayout.FloatField("UV Tile Y", shape.uvtiley);

						shape.RopeUp = (MegaAxis)EditorGUILayout.EnumPopup("Up", shape.RopeUp);
						shape.mat1 = (Material)EditorGUILayout.ObjectField("Mat", shape.mat1, typeof(Material), true);
						shape.flipNormals = EditorGUILayout.Toggle("Flip Normals", shape.flipNormals);
						break;

					case MeshShapeType.Box:
						shape.TubeStart = EditorGUILayout.Slider("Start", shape.TubeStart, -1.0f, 2.0f);
						shape.TubeLength = EditorGUILayout.Slider("Length", shape.TubeLength, 0.0f, 1.0f);
						shape.rotate = EditorGUILayout.FloatField("Rotate", shape.rotate);
						shape.boxwidth = EditorGUILayout.FloatField("Box Width", shape.boxwidth);
						shape.boxheight = EditorGUILayout.FloatField("Box Height", shape.boxheight);
						shape.offset = EditorGUILayout.FloatField("Offset", shape.offset);

						shape.scaleX = EditorGUILayout.CurveField("Scale X", shape.scaleX);
						shape.unlinkScale = EditorGUILayout.BeginToggleGroup("unlink Scale", shape.unlinkScale);
						shape.scaleY = EditorGUILayout.CurveField("Scale Y", shape.scaleY);
						EditorGUILayout.EndToggleGroup();

						shape.strands = EditorGUILayout.IntField("Strands", shape.strands);
						if ( shape.strands > 1 )
						{
							shape.tradius = EditorGUILayout.FloatField("Radius", shape.tradius);
							shape.TwistPerUnit = EditorGUILayout.FloatField("Twist", shape.TwistPerUnit);
							shape.startAng = EditorGUILayout.FloatField("Start Twist", shape.startAng);
						}

						shape.UVOffset = EditorGUILayout.Vector2Field("UV Offset", shape.UVOffset);
						shape.uvtilex = EditorGUILayout.FloatField("UV Tile X", shape.uvtilex);
						shape.uvtiley = EditorGUILayout.FloatField("UV Tile Y", shape.uvtiley);

						shape.cap = EditorGUILayout.Toggle("Cap", shape.cap);
						shape.RopeUp = (MegaAxis)EditorGUILayout.EnumPopup("Up", shape.RopeUp);
						shape.mat1 = (Material)EditorGUILayout.ObjectField("Mat", shape.mat1, typeof(Material), true);
						shape.flipNormals = EditorGUILayout.Toggle("Flip Normals", shape.flipNormals);
						break;
				}

				if ( shape.strands < 1 )
					shape.strands = 1;

				EditorGUILayout.EndVertical();

				// Conform
				shape.conform = EditorGUILayout.BeginToggleGroup("Conform", shape.conform);

				GameObject contarget = (GameObject)EditorGUILayout.ObjectField("Target", shape.target, typeof(GameObject), true);

				if ( contarget != shape.target )
					shape.SetTarget(contarget);
				shape.conformAmount = EditorGUILayout.Slider("Amount", shape.conformAmount, 0.0f, 1.0f);
				shape.raystartoff = EditorGUILayout.FloatField("Ray Start Off", shape.raystartoff);
				shape.conformOffset = EditorGUILayout.FloatField("Conform Offset", shape.conformOffset);
				shape.raydist = EditorGUILayout.FloatField("Ray Dist", shape.raydist);
				EditorGUILayout.EndToggleGroup();
			}
			else
			{
				shape.ClearMesh();
			}

			showsplines = EditorGUILayout.Foldout(showsplines, "Spline Data");

			if ( showsplines )
			{
				EditorGUILayout.BeginVertical("Box");
				if ( shape.splines != null && shape.splines.Count > 0 )
					DisplaySpline(shape, shape.splines[shape.selcurve]);
				EditorGUILayout.EndVertical();
			}

			EditorGUILayout.BeginHorizontal();

			Color col = GUI.backgroundColor;
			GUI.backgroundColor = Color.green;
			if ( GUILayout.Button("Add") )
			{
				// Create a new spline in the shape
				MegaSpline spl = MegaSpline.Copy(shape.splines[shape.selcurve]);

				shape.splines.Add(spl);
				shape.selcurve = shape.splines.Count - 1;
				EditorUtility.SetDirty(shape);
				buildmesh = true;
			}

			if ( shape.splines.Count > 1 )
			{
				GUI.backgroundColor = Color.red;
				if ( GUILayout.Button("Delete") )
				{
					// Delete current spline
					shape.splines.RemoveAt(shape.selcurve);


					for ( int i = 0; i < shape.splines.Count; i++ )
					{
						if ( shape.splines[i].outlineSpline == shape.selcurve )
							shape.splines[i].outlineSpline = -1;

						if ( shape.splines[i].outlineSpline > shape.selcurve )
							shape.splines[i].outlineSpline--;
					}

					shape.selcurve--;
					if ( shape.selcurve < 0 )
						shape.selcurve = 0;

					EditorUtility.SetDirty(shape);
					buildmesh = true;
				}
			}
			GUI.backgroundColor = col;
			EditorGUILayout.EndHorizontal();
		}

		if ( !shape.imported )
		{
			if ( Params() )
			{
				rebuild = true;
			}
		}

		showfuncs = EditorGUILayout.Foldout(showfuncs, "Extra Functions");

		if ( showfuncs )
		{
			if ( GUILayout.Button("Flatten") )
			{
				PushSpline(shape.splines[shape.selcurve], shape.selcurve);

				shape.SetHeight(shape.selcurve, 0.0f);
				shape.CalcLength();
				EditorUtility.SetDirty(target);
			}

			if ( GUILayout.Button("Remove Twist") )
			{
				PushSpline(shape.splines[shape.selcurve], shape.selcurve);

				shape.SetTwist(shape.selcurve, 0.0f);
				EditorUtility.SetDirty(target);
			}

			if ( GUILayout.Button("Copy IDS") )
			{
				PushSpline(shape.splines[shape.selcurve], shape.selcurve);

				shape.CopyIDS(shape.selcurve);
				EditorUtility.SetDirty(target);
			}
		}

		export = EditorGUILayout.Foldout(export, "Export Options");

		if ( export )
		{
			xaxis = (MegaAxis)EditorGUILayout.EnumPopup("X Axis", xaxis);
			yaxis = (MegaAxis)EditorGUILayout.EnumPopup("Y Axis", yaxis);

			strokewidth = EditorGUILayout.FloatField("Stroke Width", strokewidth);
			strokecol = EditorGUILayout.ColorField("Stroke Color", strokecol);

			if ( GUILayout.Button("Export") )
			{
				Export(shape);
			}
		}

		if ( recalc )
		{
			shape.CalcLength();	//10);

			shape.BuildMesh();

			//MegaLoftLayerSimple[] layers = (MegaLoftLayerSimple[])FindObjectsOfType(typeof(MegaLoftLayerSimple));

			//for ( int i = 0; i < layers.Length; i++ )
			//{
				//layers[i].Notify(shape.splines[shape.selcurve], 0);
			//}

			EditorUtility.SetDirty(shape);
		}

		if ( GUI.changed )
		{
			EditorUtility.SetDirty(target);
			buildmesh = true;
		}

		if ( rebuild )
		{
			shape.MakeShape();
			EditorUtility.SetDirty(target);
			buildmesh = true;
		}

		if ( buildmesh )
		{
			if ( shape.makeMesh )
			{
				shape.SetMats();
				shape.BuildMesh();
			}
		}
	}

	void DisplayKnot(MegaShape shape, MegaSpline spline, MegaKnot knot, int i)
	{
		bool recalc = false;

		Vector3 p = EditorGUILayout.Vector3Field("Knot [" + i + "] Pos", knot.p);
		if ( p != knot.p )
		{
			PushSpline(spline, shape.selcurve);
		}

		delta = p - knot.p;

		knot.invec += delta;
		knot.outvec += delta;

		if ( knot.p != p )
		{
			recalc = true;
			knot.p = p;
		}

		if ( recalc )
		{
			shape.CalcLength();	//10);
		}
		knot.twist = EditorGUILayout.FloatField("Twist", knot.twist);
		knot.id = EditorGUILayout.IntField("ID", knot.id);
	}

	void DisplaySpline(MegaShape shape, MegaSpline spline)
	{
		bool closed = EditorGUILayout.Toggle("Closed", spline.closed);

		if ( closed != spline.closed )
		{
			PushSpline(spline, shape.selcurve);
			spline.closed = closed;
			shape.CalcLength();	//10);
			EditorUtility.SetDirty(target);
		}

		bool reverse = EditorGUILayout.Toggle("Reverse", spline.reverse);

		if ( reverse != spline.reverse )
		{
			PushSpline(spline, shape.selcurve);
			spline.reverse = reverse;
		}

		EditorGUILayout.LabelField("Length ", spline.length.ToString("0.000"));
		spline.twistmode = (MegaShapeEase)EditorGUILayout.EnumPopup("Twist Mode", spline.twistmode);

		showknots = EditorGUILayout.Foldout(showknots, "Knots");

		if ( showknots )
		{
			for ( int i = 0; i < spline.knots.Count; i++ )
			{
				DisplayKnot(shape, spline, spline.knots[i], i);
			}
		}
	}

	static bool editmode = true;
	//static string hackControlName = "hackery129578835432342";
 
 // call this function in OnGUI before calling TextField (only needs to be called once)
	//static void FocusHack() {
     // fake control used for unfocussing things, since we can't call
     // FocusControl with no arguments and an empty string doesn't work
     //GUI.SetNextControlName(hackControlName);
     //GUI.Button(new Rect(-10000,-10000, 0,0), GUIContent.none);
	//}

	string lastfocus = "";

	bool dragging = false;

	Vector3 CircleCap(int id, Vector3 pos, Quaternion rot, float size)
	{
#if UNITY_2017_2 || UNITY_2017_3 || UNITY_2018 || UNITY_2019
		return Handles.FreeMoveHandle(cid, pos, rot, size, Vector3.zero, Handles.CircleHandleCap);
#else
#if UNITY_5_6 || UNITY_2017_1
		return Handles.FreeMoveHandle(pos, rot, size, Vector3.zero, Handles.CircleHandleCap);
#else
		return Handles.FreeMoveHandle(pos, rot, size, Vector3.zero, Handles.CircleCap);
#endif
#endif
	}

	Vector3 SphereCap(int id, Vector3 pos, Quaternion rot, float size)
	{
#if UNITY_2017_2 || UNITY_2017_3 || UNITY_2018 || UNITY_2019
		return Handles.FreeMoveHandle(cid, pos, rot, size, Vector3.zero, Handles.SphereHandleCap);
#else
#if UNITY_5_6 || UNITY_2017_1
		return Handles.FreeMoveHandle(pos, rot, size, Vector3.zero, Handles.SphereHandleCap);
#else
		return Handles.FreeMoveHandle(pos, rot, size, Vector3.zero, Handles.SphereCap);
#endif
#endif
	}

	public void OnSceneGUI()
	{
		MegaShape shape = (MegaShape)target;

		bool mouseup = false;
		bool recalc = false;

#if UNITY_2017 || UNITY_2018 || UNITY_2019
		if ( Event.current.type == EventType.MouseUp )
#else
		if ( Event.current.type == EventType.mouseUp )
#endif
		{
			mouseup = true;
			recalc = true;

			if ( dragging )
				PushSpline(shape.splines[shape.selcurve], shape.selcurve);

			dragging = false;
		}

#if UNITY_2017 || UNITY_2018 || UNITY_2019
		if ( Event.current.type == EventType.MouseDown )
#else
		if ( Event.current.type == EventType.mouseDown )
#endif
		{

		}

#if UNITY_2017 || UNITY_2018 || UNITY_2019
		if ( Event.current.type == EventType.MouseDrag && Event.current.button == 0 )
#else
		if ( Event.current.type == EventType.mouseDrag && Event.current.button == 0 )
#endif
		{
			dragging = true;
		}

		Handles.matrix = shape.transform.localToWorldMatrix;

		if ( shape.selcurve > shape.splines.Count - 1 )
			shape.selcurve = 0;

		Vector3 dragplane = Vector3.one;

		Color nocol = new Color(0, 0, 0, 0);

		bounds.size = Vector3.zero;

		Color twistcol = new Color(0.5f, 0.5f, 1.0f, 0.25f);

		//string hn = GUI.GetNameOfFocusedControl();
		string hn = GetControlName();
		//Debug.Log("hn " + hn);

		NewControlFrame();

		if ( hn != lastfocus )
		{
			if ( hn.Length > 0 )
			{
				if ( hn[0] == 'k' )
				{
					GUI.FocusControl("");
					string hn1 = hn.Replace('k', ' ');
					selected = int.Parse(hn1);
				}

				if ( hn[0] == 'a' )
				{
					PushSpline(shape.splines[shape.selcurve], shape.selcurve);
					GUI.FocusControl("");
					// Add knot
					string hn1 = hn.Replace('a', ' ');
					int ak = int.Parse(hn1);

					Vector3 mp = Vector3.zero;
					
					if ( shape.splines[shape.selcurve].closed )
					{
						mp = shape.splines[shape.selcurve].knots[ak].InterpolateCS(0.5f, shape.splines[shape.selcurve].knots[0]);
					}
					else
					{
						if ( ak == shape.splines[shape.selcurve].knots.Count - 1 )
						{
							mp = shape.InterpCurve3D(shape.selcurve, 1.02f, true);	//.splines[s].knots[p].InterpolateCS(0.5f, shape.splines[s].knots[p + 1]);
						}
						else
							mp = shape.splines[shape.selcurve].knots[ak].InterpolateCS(0.5f, shape.splines[shape.selcurve].knots[ak + 1]);
					}

					MegaKnot knot = new MegaKnot();

					knot.p = mp;
					knot.id = shape.splines[shape.selcurve].knots[ak].id;
					knot.twist = shape.splines[shape.selcurve].knots[ak].twist;

					shape.splines[shape.selcurve].knots.Insert(ak + 1, knot);

					if ( shape.smoothonaddknot )
						shape.AutoCurve(shape.splines[shape.selcurve]);	//, knum, knum + 2);
					shape.CalcLength();	//10);
					EditorUtility.SetDirty(target);
					if ( shape.makeMesh )
					{
						shape.SetMats();
						shape.BuildMesh();
					}
				}
			}

			lastfocus = hn;
		}

		// Draw nearest point (use for adding knot)
		Vector3 wcp = CursorPos;
		Vector3 newCursorPos = PosHandles(shape, wcp, Quaternion.identity);
		
		if ( newCursorPos != wcp )
		{
			Vector3 cd = newCursorPos - wcp;

			CursorPos += cd;

			float calpha = 0.0f;
			CursorPos = shape.FindNearestPoint(CursorPos, 5, ref CursorKnot, ref CursorTangent, ref calpha);
			shape.CursorPercent = calpha * 100.0f;
		}

		//GUI.SetNextControlName("Cursor");
		SetControlName("Cursor");
		Handles.Label(CursorPos, "Cursor " + shape.CursorPercent.ToString("0.00") + "% - " + CursorPos);

		// Move whole spline handle
		if ( shape.showorigin )
		{
			//GUI.SetNextControlName("Origin");
			SetControlName("Origin");

			Handles.Label(bounds.min, "Origin");
			Vector3 spos = Handles.PositionHandle(bounds.min, Quaternion.identity);
			if ( spos != bounds.min )
			{
				if ( shape.usesnap )
				{
					if ( shape.snap.x != 0.0f )
						spos.x = (int)(spos.x / shape.snap.x) * shape.snap.x;

					if ( shape.snap.y != 0.0f )
						spos.y = (int)(spos.y / shape.snap.y) * shape.snap.y;

					if ( shape.snap.z != 0.0f )
						spos.z = (int)(spos.z / shape.snap.z) * shape.snap.z;
				}

				// Move the spline
				shape.MoveSpline(spos - bounds.min, shape.selcurve, false);
				recalc = true;
			}
		}

		Vector3 np = Vector3.zero;
		Quaternion fwd = Quaternion.identity;
		Vector3 rg = Vector3.zero;

		for ( int s = 0; s < shape.splines.Count; s++ )
		{
			for ( int p = 0; p < shape.splines[s].knots.Count; p++ )
			{
				if ( p == selected )
				{
					if ( p == 0 || p < shape.splines[s].knots.Count - 2 )
					{
						np = shape.splines[s].knots[p].Interpolate(0.002f, shape.splines[s].knots[p + 1]);
						np = np - shape.splines[s].knots[p].p;
					}
					else
					{
						if ( shape.splines[s].closed )
						{
							np = shape.splines[s].knots[p].Interpolate(0.002f, shape.splines[s].knots[0]);
							np = np - shape.splines[s].knots[p].p;
						}
						else
						{
							np = shape.splines[s].knots[p - 1].Interpolate(0.998f, shape.splines[s].knots[p]);
							np = shape.splines[s].knots[p].p - np;
						}
					}

					if ( np == Vector3.zero )
						np = Vector3.forward;

					np = np.normalized;
					// np holds the tangent so we can align the arc
					fwd = Quaternion.LookRotation(np);

					rg = Vector3.Cross(np, Vector3.up);
				}

				if ( s == shape.selcurve )
				{
					bounds.Encapsulate(shape.splines[s].knots[p].p);
				}

				if ( shape.drawKnots && s == shape.selcurve )
				{
					pm = shape.splines[s].knots[p].p;

					if ( showlabels )
					{
						if ( p == selected && s == shape.selcurve )
						{
							Color col = Color.white;
							col.a = 1.0f;
							Handles.color = col;
							Handles.Label(pm, " Selected\n" + pm.ToString("0.000"));
						}
						else
						{
							Handles.color = shape.KnotCol;
							Handles.Label(pm, " " + p);
						}
					}

					Handles.color = nocol;
					Vector3 newp = Vector3.zero;
			
					//GUI.SetNextControlName("k" + p.ToString());
					SetControlName("k" + p.ToString());
					if ( p == selected )
					{
						if ( shape.usesnap )
							newp = PosHandlesSnap(shape, pm, fwd);	//Quaternion.identity);
						else
							newp = PosHandles(shape, pm, fwd);	//Quaternion.identity);

						if ( newp != pm )
						{
							if ( shape.freezeX ) newp.x = pm.x;
							if ( shape.freezeY ) newp.y = pm.y;
							if ( shape.freezeZ ) newp.z = pm.z;

							MegaUndo.SetSnapshotTarget(shape, "Knot Move");
						}

						Vector3 dl = Vector3.Scale(newp - pm, dragplane);

						shape.splines[s].knots[p].p += dl;	//Vector3.Scale(newp - pm, dragplane);

						shape.splines[s].knots[p].invec += dl;	//delta;
						shape.splines[s].knots[p].outvec += dl;	//delta;

						if ( newp != pm )
						{
							if ( shape.smoothOnDrag )
								shape.AutoCurve();
							recalc = true;
						}
					}
					else
					{
						//Handles.FreeMoveHandle(pm, Quaternion.identity, shape.KnotSize * 0.01f, Vector3.zero, Handles.CircleCap);
						CircleCap(0, pm, Quaternion.identity, shape.KnotSize * 0.01f);
					}

					//GUI.SetNextControlName("");
					//SetControlName("");
				}

				if ( shape.drawHandles && s == shape.selcurve )
				{
					if ( p == selected )
					{
						Handles.color = shape.VecCol;
						pm = shape.splines[s].knots[p].p;

						Vector3 ip = shape.splines[s].knots[p].invec;
						Vector3 op = shape.splines[s].knots[p].outvec;
						//GUI.SetNextControlName("hli" + p.ToString());
						SetControlName("hli" + p.ToString());

						Handles.DrawLine(pm, ip);
						//GUI.SetNextControlName("hlo" + p.ToString());
						SetControlName("hlo" + p.ToString());
						Handles.DrawLine(pm, op);

						Handles.color = shape.HandleCol;

						Vector3 invec = shape.splines[s].knots[p].invec;
						Handles.color = nocol;
						Vector3 newinvec = Vector3.zero;

						//GUI.SetNextControlName("hi" + p.ToString());
						SetControlName("hi" + p.ToString());

						if ( shape.usesnaphandles )
							newinvec = PosHandlesSnap(shape, invec, Quaternion.identity);
						else
							newinvec = PosHandles(shape, invec, Quaternion.identity);

						if ( newinvec != invec )	//shape.splines[s].knots[p].invec )
						{
							MegaUndo.SetSnapshotTarget(shape, "Handle Move");
						}
						Vector3 dl = Vector3.Scale(newinvec - invec, dragplane);
						shape.splines[s].knots[p].invec += dl;	//Vector3.Scale(newinvec - invec, dragplane);
						if ( invec != newinvec )	//shape.splines[s].knots[p].invec )
						{
							if ( shape.lockhandles )
								shape.splines[s].knots[p].outvec -= dl;

							selected = p;
							recalc = true;
						}
						Vector3 outvec = shape.splines[s].knots[p].outvec;

						//GUI.SetNextControlName("ho" + p.ToString());
						SetControlName("ho" + p.ToString());

						Vector3 newoutvec = Vector3.zero;
						if ( shape.usesnaphandles )
							newoutvec = PosHandlesSnap(shape, outvec, Quaternion.identity);
						else
							newoutvec = PosHandles(shape, outvec, Quaternion.identity);

						if ( newoutvec != outvec )	//shape.splines[s].knots[p].outvec )
						{
							MegaUndo.SetSnapshotTarget(shape, "Handle Move");
						}
						dl = Vector3.Scale(newoutvec - outvec, dragplane);
						shape.splines[s].knots[p].outvec += dl;
						if ( outvec != newoutvec )	//shape.splines[s].knots[p].outvec )
						{
							if ( shape.lockhandles )
								shape.splines[s].knots[p].invec -= dl;

							selected = p;
							recalc = true;
						}
						Vector3 hp = shape.splines[s].knots[p].invec;
						if ( selected == p )
							Handles.Label(hp, " " + p);

						hp = shape.splines[s].knots[p].outvec;

						if ( selected == p )
							Handles.Label(hp, " " + p);
					}
				}

				// Twist handles
				if ( shape.drawTwist && s == shape.selcurve && p == selected )
				{
					Handles.color = twistcol;
					float twist = shape.splines[s].knots[p].twist;

					Handles.DrawSolidArc(shape.splines[s].knots[p].p, np, rg, twist, shape.KnotSize * 0.1f);

					Vector3 tang = new Vector3(0.0f, 0.0f, shape.splines[s].knots[p].twist);
					Quaternion inrot = fwd * Quaternion.Euler(tang);
					//Quaternion rot = Handles.RotationHandle(inrot, shape.splines[s].knots[p].p);
					Handles.color = Color.white;
					Quaternion rot = Handles.Disc(inrot, shape.splines[s].knots[p].p, np, shape.KnotSize * 0.1f, false, 0.0f);
					
					if ( rot != inrot )
					{
						tang = rot.eulerAngles;
						float diff = (tang.z - shape.splines[s].knots[p].twist);
						if ( Mathf.Abs(diff) > 0.0001f  )
						{
							while ( diff > 180.0f )
								diff -= 360.0f;

							while ( diff < -180.0f )
								diff += 360.0f;

							shape.splines[s].knots[p].twist += diff;
							recalc = true;
						}
					}
				}

				// Midpoint add knot code
				if ( s == shape.selcurve )
				{
					if ( p < shape.splines[s].knots.Count - 1 )
					{
						Handles.color = Color.white;

						Vector3 mp = shape.splines[s].knots[p].InterpolateCS(0.5f, shape.splines[s].knots[p + 1]);

						//GUI.SetNextControlName("a" + p.ToString());
						SetControlName("a" + p.ToString());

						//Handles.FreeMoveHandle(mp, Quaternion.identity, shape.KnotSize * 0.01f, Vector3.zero, Handles.CircleCap);
						CircleCap(0, mp, Quaternion.identity, shape.KnotSize * 0.01f);

						//FocusHack();
					}
					else
					{
						if ( shape.splines[s].closed )
						{
							Handles.color = Color.white;

							Vector3 mp = shape.splines[s].knots[p].InterpolateCS(0.5f, shape.splines[s].knots[0]);

							//GUI.SetNextControlName("a" + p.ToString());
							SetControlName("a" + p.ToString());

							//Handles.FreeMoveHandle(mp, Quaternion.identity, shape.KnotSize * 0.01f, Vector3.zero, Handles.CircleCap);
							CircleCap(0, mp, Quaternion.identity, shape.KnotSize * 0.01f);
						}
						else
						{
							Handles.color = Color.white;

							Vector3 mp = shape.InterpCurve3D(s, 1.02f, true);	//.splines[s].knots[p].InterpolateCS(0.5f, shape.splines[s].knots[p + 1]);

							//GUI.SetNextControlName("a" + p.ToString());
							SetControlName("a" + p.ToString());

							//Handles.FreeMoveHandle(mp, Quaternion.identity, shape.KnotSize * 0.01f, Vector3.zero, Handles.CircleCap);
							CircleCap(0, mp, Quaternion.identity, shape.KnotSize * 0.01f);
						}
					}
				}
			}
		}

		//GUI.SetNextControlName("");
		//SetControlName("");

		if ( recalc )
		{
			shape.CalcLength();	//10);

			if ( shape.updateondrag || (!shape.updateondrag && mouseup) )
			{
				shape.BuildMesh();

				//MegaLoftLayerSimple[] layers = (MegaLoftLayerSimple[])FindObjectsOfType(typeof(MegaLoftLayerSimple));

				//for ( int i = 0; i < layers.Length; i++ )
					//layers[i].Notify(shape.splines[shape.selcurve], 0);

				EditorUtility.SetDirty(shape);
			}
		}

		Handles.matrix = Matrix4x4.identity;

		// This is wrong gui not changing here
		if ( GUI.changed )
		{
			MegaUndo.CreateSnapshot();
			MegaUndo.RegisterSnapshot();
		}

		MegaUndo.ClearSnapshotTarget();
	}

	Vector3 PosHandlesSnap(MegaShape shape, Vector3 pos, Quaternion q)
	{
		switch ( shape.handleType )
		{
			case MegaHandleType.Position:
				pos = PositionHandle(pos, q, 1.0f, 0.75f);
				break;

			case MegaHandleType.Free:
				//pos = Handles.FreeMoveHandle(pos, q, shape.KnotSize * 0.01f, Vector3.zero, Handles.SphereCap);	//CircleCap);
				pos = SphereCap(0, pos, q, shape.KnotSize * 0.01f);
				break;
		}

		if ( shape.usesnap )
		{
			if ( shape.snap.x != 0.0f )
				pos.x = (int)(pos.x / shape.snap.x) * shape.snap.x;

			if ( shape.snap.y != 0.0f )
				pos.y = (int)(pos.y / shape.snap.y) * shape.snap.y;

			if ( shape.snap.z != 0.0f )
				pos.z = (int)(pos.z / shape.snap.z) * shape.snap.z;
		}

		return pos;
	}

	Vector3 PosHandles(MegaShape shape, Vector3 pos, Quaternion q)
	{
		switch ( shape.handleType )
		{
			case MegaHandleType.Position:
				pos = PositionHandle(pos, q, 1.0f, 0.75f);
				break;

			case MegaHandleType.Free:
				//pos = Handles.FreeMoveHandle(pos, q, shape.KnotSize * 0.01f, Vector3.zero, Handles.CircleCap);
				pos = CircleCap(0, pos, q, shape.KnotSize * 0.01f);
				break;
		}

		return pos;
	}

#if UNITY_2017_2 || UNITY_2017_3 || UNITY_2018 || UNITY_2019
	public static Vector3 PositionHandle(Vector3 position, Quaternion rotation, float size, float alpha)
	{
		return Handles.PositionHandle(position, rotation);
#if false
		float handlesize = HandleUtility.GetHandleSize(position) * size;
		Color color = Handles.color;
		Color col = Color.red;
		col.a = alpha;
		Handles.color = col;    //Color.red;	//Handles..xAxisColor;
								//position = Handles.Slider(position, rotation * Vector3.right, handlesize, new Handles.DrawCapFunction(Handles.ArrowHandleCap), 0.0f); //SnapSettings.move.x);
		position = Handles.Slider(cid, position, rotation * Vector3.right, handlesize, Handles.ArrowHandleCap, 0.0f);    // new Handles.DrawCapFunction(Handles.ArrowHandleCap), 0.0f); //SnapSettings.move.x);
		col = Color.green;
		col.a = alpha;
		Handles.color = col;    //Color.green;	//Handles.yAxisColor;
		position = Handles.Slider(cid, position, rotation * Vector3.up, handlesize, Handles.ArrowHandleCap, 0.0f);    //SnapSettings.move.y);

		col = Color.blue;
		col.a = alpha;

		Handles.color = col;    //Color.blue;	//Handles.zAxisColor;
		position = Handles.Slider(cid, position, rotation * Vector3.forward, handlesize, Handles.ArrowHandleCap, 0.0f);   //SnapSettings.move.z);

		col = Color.yellow;
		col.a = alpha;

		Handles.color = col;    //Color.yellow;	//Handles.centerColor;
		position = Handles.FreeMoveHandle(cid, position, rotation, handlesize * 0.15f, Vector3.zero, Handles.RectangleHandleCap);
		Handles.color = color;
		return position;
#endif
	}
#else
#if UNITY_5_6 || UNITY_2017_1
	public static Vector3 PositionHandle(Vector3 position, Quaternion rotation, float size, float alpha)
	{
		float handlesize = HandleUtility.GetHandleSize(position) * size;
		Color color = Handles.color;
		Color col = Color.red;
		col.a = alpha;
		Handles.color = col;    //Color.red;	//Handles..xAxisColor;
								//position = Handles.Slider(position, rotation * Vector3.right, handlesize, new Handles.DrawCapFunction(Handles.ArrowHandleCap), 0.0f); //SnapSettings.move.x);
		position = Handles.Slider(position, rotation * Vector3.right, handlesize, Handles.ArrowHandleCap, 0.0f);    // new Handles.DrawCapFunction(Handles.ArrowHandleCap), 0.0f); //SnapSettings.move.x);
		col = Color.green;
		col.a = alpha;
		Handles.color = col;    //Color.green;	//Handles.yAxisColor;
		position = Handles.Slider(position, rotation * Vector3.up, handlesize, Handles.ArrowHandleCap, 0.0f);    //SnapSettings.move.y);

		col = Color.blue;
		col.a = alpha;

		Handles.color = col;    //Color.blue;	//Handles.zAxisColor;
		position = Handles.Slider(position, rotation * Vector3.forward, handlesize, Handles.ArrowHandleCap, 0.0f);   //SnapSettings.move.z);

		col = Color.yellow;
		col.a = alpha;

		Handles.color = col;    //Color.yellow;	//Handles.centerColor;
		position = Handles.FreeMoveHandle(position, rotation, handlesize * 0.15f, Vector3.zero, Handles.RectangleHandleCap);
		Handles.color = color;
		return position;
	}
#else
	public static Vector3 PositionHandle(Vector3 position, Quaternion rotation, float size, float alpha)
	{
		float handlesize = HandleUtility.GetHandleSize(position) * size;
		Color color = Handles.color;
		Color col = Color.red;
		col.a = alpha;
		Handles.color = col;    //Color.red;	//Handles..xAxisColor;
		position = Handles.Slider(position, rotation * Vector3.right, handlesize, new Handles.DrawCapFunction(Handles.ArrowCap), 0.0f); //SnapSettings.move.x);
		col = Color.green;
		col.a = alpha;
		Handles.color = col;    //Color.green;	//Handles.yAxisColor;
		position = Handles.Slider(position, rotation * Vector3.up, handlesize, new Handles.DrawCapFunction(Handles.ArrowCap), 0.0f);    //SnapSettings.move.y);

		col = Color.blue;
		col.a = alpha;

		Handles.color = col;    //Color.blue;	//Handles.zAxisColor;
		position = Handles.Slider(position, rotation * Vector3.forward, handlesize, new Handles.DrawCapFunction(Handles.ArrowCap), 0.0f);   //SnapSettings.move.z);

		col = Color.yellow;
		col.a = alpha;

		Handles.color = col;    //Color.yellow;	//Handles.centerColor;
		position = Handles.FreeMoveHandle(position, rotation, handlesize * 0.15f, Vector3.zero, new Handles.DrawCapFunction(Handles.RectangleCap));
		Handles.color = color;
		return position;
	}
#endif
#endif

#if UNITY_5_1 || UNITY_5_2 || UNITY_5_3 || UNITY_5_4 || UNITY_5_5 || UNITY_5_6 || UNITY_2017 || UNITY_2018 || UNITY_2019
	[DrawGizmo(GizmoType.NotInSelectionHierarchy | GizmoType.Pickable | GizmoType.InSelectionHierarchy)]
#else
	[DrawGizmo(GizmoType.NotSelected | GizmoType.Pickable | GizmoType.SelectedOrChild)]
#endif
	static void RenderGizmo(MegaShape shape, GizmoType gizmoType)
	{
		if ( (gizmoType & GizmoType.Active) != 0 && Selection.activeObject == shape.gameObject )
		{
			if ( shape.splines == null || shape.splines.Count == 0 )
				return;

			DrawGizmos(shape, new Color(1.0f, 1.0f, 1.0f, 1.0f));

			if ( shape.splines[shape.selcurve].knots.Count > 1 )
			{
				Color col = Color.yellow;
				col.a = 0.5f;
				Gizmos.color = col;	//Color.yellow;
				CursorPos = shape.InterpCurve3D(shape.selcurve, shape.CursorPercent * 0.01f, true);
				Gizmos.DrawSphere(shape.transform.TransformPoint(CursorPos), shape.KnotSize * 0.01f);
				Handles.color = Color.white;

				if ( MegaShapeEditor.editor != null && editmode )	////if ( shape.handleType == MegaHandleType.Free && editmode )
				{
					int s = shape.selcurve;
					{
						for ( int p = 0; p < shape.splines[s].knots.Count; p++ )
						{
							if ( p == MegaShapeEditor.editor.selected )
							{
								if ( shape.handleType == MegaHandleType.Free )
								{
									if ( shape.drawKnots )	//&& s == shape.selcurve )
									{
										Gizmos.color = Color.green;
										Gizmos.DrawSphere(shape.transform.TransformPoint(shape.splines[s].knots[p].p), shape.KnotSize * 0.01f);
									}

									if ( shape.drawHandles )
									{
										Gizmos.color = Color.red;
										Gizmos.DrawSphere(shape.transform.TransformPoint(shape.splines[s].knots[p].invec), shape.KnotSize * 0.01f);
										Gizmos.DrawSphere(shape.transform.TransformPoint(shape.splines[s].knots[p].outvec), shape.KnotSize * 0.01f);
									}
								}
							}
							else
							{
								Gizmos.color = Color.green;
								Gizmos.DrawSphere(shape.transform.TransformPoint(shape.splines[s].knots[p].p), shape.KnotSize * 0.01f);
							}
						}
					}
				}
			}
		}
		else
			DrawGizmos(shape, new Color(1.0f, 1.0f, 1.0f, 0.25f));

		if ( Camera.current )
		{
			Vector3 vis = Camera.current.WorldToScreenPoint(shape.transform.position);

			if ( vis.z > 0.0f )
			{
				Gizmos.DrawIcon(shape.transform.position, "MegaSpherify icon.png", false);
				Handles.Label(shape.transform.position, " " + shape.name);
			}
		}
	}

	// Dont want this in here, want in editor
	// If we go over a knot then should draw to the knot
	static void DrawGizmos(MegaShape shape, Color modcol1)
	{
		if ( ((1 << shape.gameObject.layer) & Camera.current.cullingMask) == 0 )
			return;

		if ( !shape.drawspline )
			return;

		Matrix4x4 tm = shape.transform.localToWorldMatrix;

		for ( int s = 0; s < shape.splines.Count; s++ )
		{
			if ( shape.splines[s].knots.Count > 1 )
			{
				float ldist = shape.stepdist * 0.1f;
				if ( ldist < 0.01f )
					ldist = 0.01f;

				Color modcol = modcol1;

				if ( s != shape.selcurve && modcol1.a == 1.0f )
					modcol.a *= 0.5f;

				if ( shape.splines[s].length / ldist > 500.0f )
					ldist = shape.splines[s].length / 500.0f;

				float ds = shape.splines[s].length / (shape.splines[s].length / ldist);

				if ( ds > shape.splines[s].length )
					ds = shape.splines[s].length;

				int c	= 0;
				int k	= -1;
				int lk	= -1;

				Vector3 first = shape.splines[s].Interpolate(0.0f, shape.normalizedInterp, ref lk);

				for ( float dist = ds; dist < shape.splines[s].length; dist += ds )
				{
					float alpha = dist / shape.splines[s].length;
					Vector3 pos = shape.splines[s].Interpolate(alpha, shape.normalizedInterp, ref k);

					if ( (c & 1) == 1 )
						Gizmos.color = shape.col1 * modcol;
					else
						Gizmos.color = shape.col2 * modcol;

					if ( k != lk )
					{
						for ( lk = lk + 1; lk <= k; lk++ )
						{
							Gizmos.DrawLine(tm.MultiplyPoint(first), tm.MultiplyPoint(shape.splines[s].knots[lk].p));
							first = shape.splines[s].knots[lk].p;
						}
					}

					lk = k;

					Gizmos.DrawLine(tm.MultiplyPoint(first), tm.MultiplyPoint(pos));

					c++;

					first = pos;
				}

				if ( (c & 1) == 1 )
					Gizmos.color = shape.col1 * modcol;
				else
					Gizmos.color = shape.col2 * modcol;

				Vector3 lastpos;
				if ( shape.splines[s].closed )
					lastpos = shape.splines[s].Interpolate(0.0f, shape.normalizedInterp, ref k);
				else
					lastpos = shape.splines[s].Interpolate(1.0f, shape.normalizedInterp, ref k);

				Gizmos.DrawLine(tm.MultiplyPoint(first), tm.MultiplyPoint(lastpos));
			}
		}
	}

	void LoadSVG(float scale)
	{
		MegaShape ms = (MegaShape)target;

		string filename = EditorUtility.OpenFilePanel("SVG File", lastpath, "svg");

		if ( filename == null || filename.Length < 1 )
			return;

		lastpath = filename;

		bool opt = true;
		if ( ms.splines != null && ms.splines.Count > 0 )
			opt = EditorUtility.DisplayDialog("Spline Import Option", "Splines already present, do you want to 'Add' or 'Replace' splines with this file?", "Add", "Replace");

		int startspline = 0;
		if ( opt )
			startspline = ms.splines.Count;

		StreamReader streamReader = new StreamReader(filename);
		string text = streamReader.ReadToEnd();
		streamReader.Close();
		MegaShapeSVG svg = new MegaShapeSVG();
		svg.importData(text, ms, scale, opt, startspline);	//.splines[0]);
		ms.imported = true;
	}

	void LoadSXL(float scale)
	{
		MegaShape ms = (MegaShape)target;

		string filename = EditorUtility.OpenFilePanel("SXL File", lastpath, "sxl");

		if ( filename == null || filename.Length < 1 )
			return;

		lastpath = filename;

		bool opt = true;
		if ( ms.splines != null && ms.splines.Count > 0 )
			opt = EditorUtility.DisplayDialog("Spline Import Option", "Splines already present, do you want to 'Add' or 'Replace' splines with this file?", "Add", "Replace");

		int startspline = 0;
		if ( opt )
			startspline = ms.splines.Count;

		StreamReader streamReader = new StreamReader(filename);
		string text = streamReader.ReadToEnd();
		streamReader.Close();
		MegaShapeSXL sxl = new MegaShapeSXL();
		sxl.importData(text, ms, scale, opt, startspline);	//.splines[0]);
		ms.imported = true;
	}

	void LoadKML(float scale)
	{
		MegaShape ms = (MegaShape)target;

		string filename = EditorUtility.OpenFilePanel("KML File", lastpath, "kml");

		if ( filename == null || filename.Length < 1 )
			return;

		lastpath = filename;

		MegaKML kml = new MegaKML();
		kml.KMLDecode(filename);
		Vector3[] points = kml.GetPoints(ImportScale);

		ms.BuildSpline(ms.selcurve, points, true);
	}

	void LoadShape(float scale)
	{
		MegaShape ms = (MegaShape)target;

		string filename = EditorUtility.OpenFilePanel("Shape File", lastpath, "spl");

		if ( filename == null || filename.Length < 1 )
			return;

		lastpath = filename;

		// Clear what we have
		bool opt = true;
		if ( ms.splines != null && ms.splines.Count > 0 )
			opt = EditorUtility.DisplayDialog("Spline Import Option", "Splines already present, do you want to 'Add' or 'Replace' splines with this file?", "Add", "Replace");

		int startspline = 0;
		if ( opt )
			startspline = ms.splines.Count;
		else
			ms.splines.Clear();

		ParseFile(filename, ShapeCallback);

		ms.Scale(scale, startspline);

		ms.MaxTime = 0.0f;

		for ( int s = 0; s < ms.splines.Count; s++ )
		{
			if ( ms.splines[s].animations != null )
			{
				for ( int a = 0; a < ms.splines[s].animations.Count; a++ )
				{
					MegaControl con = ms.splines[s].animations[a].con;
					if ( con != null )
					{
						float t = con.Times[con.Times.Length - 1];
						if ( t > ms.MaxTime )
							ms.MaxTime = t;
					}
				}
			}
		}
		ms.imported = true;
	}

	public void ShapeCallback(string classname, BinaryReader br)
	{
		switch ( classname )
		{
			case "Shape": LoadShape(br); break;
		}
	}

	public void LoadShape(BinaryReader br)
	{
		MegaParse.Parse(br, ParseShape);
	}

	public void ParseFile(string assetpath, ParseClassCallbackType cb)
	{
		FileStream fs = new FileStream(assetpath, FileMode.Open, FileAccess.Read, System.IO.FileShare.Read);

		BinaryReader br = new BinaryReader(fs);

		bool processing = true;

		while ( processing )
		{
			string classname = MegaParse.ReadString(br);

			if ( classname == "Done" )
				break;

			int	chunkoff = br.ReadInt32();
			long fpos = fs.Position;

			cb(classname, br);

			fs.Position = fpos + chunkoff;
		}

		br.Close();
	}

	static public Vector3 ReadP3(BinaryReader br)
	{
		Vector3 p = Vector3.zero;

		p.x = br.ReadSingle();
		p.y = br.ReadSingle();
		p.z = br.ReadSingle();

		return p;
	}

	bool SplineParse(BinaryReader br, string cid)
	{
		MegaShape ms = (MegaShape)target;
		MegaSpline ps = ms.splines[ms.splines.Count - 1];

		switch ( cid )
		{
			case "Transform":
				Vector3 pos = ReadP3(br);
				Vector3 rot = ReadP3(br);
				Vector3 scl = ReadP3(br);
				rot.y = -rot.y;
				ms.transform.position = pos;
				ms.transform.rotation = Quaternion.Euler(rot * Mathf.Rad2Deg);
				ms.transform.localScale = scl;
				break;

			case "Flags":
				int count = br.ReadInt32();
				ps.closed = (br.ReadInt32() == 1);
				count = br.ReadInt32();
				ps.knots = new List<MegaKnot>(count);
				ps.length = 0.0f;
				break;

			case "Knots":
				for ( int i = 0; i < ps.knots.Capacity; i++ )
				{
					MegaKnot pk = new MegaKnot();

					pk.p = ReadP3(br);
					pk.invec = ReadP3(br);
					pk.outvec = ReadP3(br);
					pk.seglength = br.ReadSingle();

					ps.length += pk.seglength;
					pk.length = ps.length;
					ps.knots.Add(pk);
				}
				break;
		}
		return true;
	}

	bool AnimParse(BinaryReader br, string cid)
	{
		MegaShape ms = (MegaShape)target;

		switch ( cid )
		{
			case "V":
				int v = br.ReadInt32();
				ma = new MegaKnotAnim();
				int s = ms.GetSpline(v, ref ma);	//.s, ref ma.p, ref ma.t);

				if ( ms.splines[s].animations == null )
					ms.splines[s].animations = new List<MegaKnotAnim>();

				ms.splines[s].animations.Add(ma);
				break;

			case "Anim":
				ma.con = MegaParseBezVector3Control.LoadBezVector3KeyControl(br);
				break;
		}
		return true;
	}

	bool ParseShape(BinaryReader br, string cid)
	{
		MegaShape ms = (MegaShape)target;

		switch ( cid )
		{
			case "Num":
				int count = br.ReadInt32();
				ms.splines = new List<MegaSpline>(count);
				break;

			case "Spline":
				MegaSpline spl = new MegaSpline();
				ms.splines.Add(spl);
				MegaParse.Parse(br, SplineParse);
				break;

			case "Anim":
				MegaParse.Parse(br, AnimParse);
				break;
		}

		return true;
	}

	[MenuItem("GameObject/Create MegaShape Prefab")]
	static void DoCreateSimplePrefabNew()
	{
		if ( Selection.activeGameObject != null )
		{
			if ( !Directory.Exists("Assets/MegaPrefabs") )
			{
				AssetDatabase.CreateFolder("Assets", "MegaPrefabs");
			}

			GameObject obj = Selection.activeGameObject;

			GameObject prefab = PrefabUtility.CreatePrefab("Assets/MegaPrefabs/" + Selection.activeGameObject.name + ".prefab", Selection.activeGameObject);
			MeshFilter mf = obj.GetComponent<MeshFilter>();

			if ( mf )
			{
				MeshFilter newmf = prefab.GetComponent<MeshFilter>();

				Mesh mesh = CloneMesh(mf.sharedMesh);

				mesh.name = obj.name + " copy";
				AssetDatabase.AddObjectToAsset(mesh, prefab);
				newmf.sharedMesh = mesh;

				MeshCollider mc = prefab.GetComponent<MeshCollider>();
				if ( mc )
				{
					mc.sharedMesh = null;
					mc.sharedMesh = mesh;
				}
			}
			//MegaShapeLoft oldloft = obj.GetComponent<MegaShapeLoft>();
			//MegaShapeLoft loft = prefab.GetComponent<MegaShapeLoft>();

			//if ( loft )
			//{
				//for ( int i = 0; i < loft.Layers.Length; i++ )
					//loft.Layers[i].CopyLayer(oldloft.Layers[i]);
			//}
		}
	}

	static Mesh CloneMesh(Mesh mesh)
	{
		Mesh clonemesh = new Mesh();
		clonemesh.vertices = mesh.vertices;
#if UNITY_5_0 || UNITY_5_1 || UNITY_5_2 || UNITY_5 || UNITY_2017 || UNITY_2018 || UNITY_2019
		clonemesh.uv2 = mesh.uv2;
#else
		clonemesh.uv1 = mesh.uv1;
		clonemesh.uv2 = mesh.uv2;
#endif
		clonemesh.uv = mesh.uv;
		clonemesh.normals = mesh.normals;
		clonemesh.tangents = mesh.tangents;
		clonemesh.colors = mesh.colors;

		clonemesh.subMeshCount = mesh.subMeshCount;

		for ( int s = 0; s < mesh.subMeshCount; s++ )
			clonemesh.SetTriangles(mesh.GetTriangles(s), s);

		clonemesh.boneWeights = mesh.boneWeights;
		clonemesh.bindposes = mesh.bindposes;
		clonemesh.name = mesh.name + "_copy";
		clonemesh.RecalculateBounds();

		return clonemesh;
	}

	// Animation keyframe stuff
	// Need system to grab state of curve
	void AnimationKeyFrames(MegaShape shape)
	{
		MegaSpline spline = shape.splines[shape.selcurve];

		shape.showanimations = EditorGUILayout.Foldout(shape.showanimations, "Animations");

		if ( shape.showanimations )
		{
			EditorGUILayout.BeginVertical("Box");
			shape.keytime = EditorGUILayout.FloatField("Key Time", shape.keytime);
			if ( shape.keytime < 0.0f )
				shape.keytime = 0.0f;

			spline.splineanim.Enabled = EditorGUILayout.BeginToggleGroup("Enabled", spline.splineanim.Enabled);
			EditorGUILayout.BeginHorizontal();
			if ( GUILayout.Button("Add Key") )
				AddKeyFrame(shape, shape.keytime);

			if ( GUILayout.Button("Clear") )
				ClearAnim(shape);

			EditorGUILayout.EndHorizontal();

			// Need to show each keyframe
			if ( spline.splineanim != null )
			{
				int nk = spline.splineanim.NumKeys();
				float mt = 0.0f;
				for ( int i = 0; i < nk; i++ )
				{
					EditorGUILayout.BeginHorizontal();

					mt = spline.splineanim.GetKeyTime(i);

					EditorGUILayout.LabelField("" + i, GUILayout.MaxWidth(20));	//" + " Time: " + mt);
					float t = EditorGUILayout.FloatField("", mt, GUILayout.MaxWidth(100));

					if ( t != mt )
						spline.splineanim.SetKeyTime(spline, i, t);

					if ( GUILayout.Button("Delete", GUILayout.MaxWidth(50)) )
						spline.splineanim.RemoveKey(i);

					if ( GUILayout.Button("Update", GUILayout.MaxWidth(50)) )
						spline.splineanim.UpdateKey(spline, i);

					if ( GUILayout.Button("Get", GUILayout.MaxWidth(50)) )
					{
						spline.splineanim.GetKey(spline, i);
						EditorUtility.SetDirty(target);
					}

					EditorGUILayout.EndHorizontal();
				}

				if ( spline.splineanim.NumKeys() > 1 )
				{
					shape.MaxTime = mt;

					float at = EditorGUILayout.Slider("T", shape.testtime, 0.0f, mt);
					if ( at != shape.testtime )
					{
						shape.testtime = at;
						if ( !shape.animate )
						{
							for ( int s = 0; s < shape.splines.Count; s++ )
							{
								if ( shape.splines[s].splineanim != null && shape.splines[s].splineanim.Enabled )
								{
									shape.splines[s].splineanim.GetState1(shape.splines[s], at);
									shape.splines[s].CalcLength();	//(10);	// could use less here
								}
							}
						}
					}
				}
			}

			EditorGUILayout.EndToggleGroup();
			EditorGUILayout.EndVertical();
		}
	}
	void ClearAnim(MegaShape shape)
	{
		MegaSpline spline = shape.splines[shape.selcurve];

		if ( spline.splineanim != null )
			spline.splineanim.Init(spline);
	}

	void AddKeyFrame(MegaShape shape, float t)
	{
		MegaSpline spline = shape.splines[shape.selcurve];
		spline.splineanim.AddState(spline, t);
	}

	void Export(MegaShape shape)
	{
		string filename = EditorUtility.SaveFilePanel("Export Shape to SVG", "", shape.name, ".svg");

		if ( filename.Length > 0 )
		{
			string data = MegaShapeSVG.Export(shape, (int)xaxis, (int)yaxis, strokewidth, strokecol);
			System.IO.File.WriteAllText(filename, data);
		}
	}
}
