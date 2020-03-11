using Autodesk.Max;
using Autodesk.Max.Plugins;
using System;


namespace ExtrudeModifier
{

	/// <summary>
	/// The Descriptor class for the Extrude modifier.
	/// </summary>
	public class ExtrudeModifierDescriptor : ClassDesc2
	{

		#region Fields

		private IGlobal global;
		private static IClass_ID classID;

		#endregion



		#region Properties

		internal IParamBlockDesc2 ParamBlockDesc;

		#endregion



		#region Constructor

		public ExtrudeModifierDescriptor(IGlobal global_)
		{
			this.global = global_;

			classID = global.Class_ID.Create(0x38414b6d, 0x470043f2);

			// Create Parameter block
			ParamBlockDesc = this.global.ParamBlockDesc2.Create(
				0,						  // The id of the parameter block
				"Parameters",			   // Internal name of the parameters
				IntPtr.Zero,
				this,					   // Reference to class descriptor
				ParamBlock2Flags.Version | ParamBlock2Flags.AutoConstruct | ParamBlock2Flags.AutoUi,
				new object[] { 1, 0 });	 // Must contain 2 integer values. First one is the version of parameter block (ie. 1), second one is the reference index of the parameter block (ex. Should be 0 if there are no other references used inside the plugin).

			ParamBlockDesc.AddParam(0, 
				new object[] { 
					"Amount",					   // Parameter name
					ParamType2.Float,			   // Parameter type
					ParamBlock2Flags.Animatable,	// Parameter block flags associated with parameter
					0,							  // Resource id of parameter name (int)- should be 0
					5.0f,						   // The default value
					0.0f,						   // If parameter is of type int or float this and next parameters provide a range
					100.0f						  // The max range for the previous parameter if it is needed
			   });
		}

		#endregion



		#region Properties 
		
		public override string Category
		{
			get { return "Max.Net Tutorials"; }
		}


		public override IClass_ID ClassID
		{
			get { return classID; }
		}


		public override string ClassName
		{
			get { return "Extrude.Net"; }
		}


		/// <summary>
		/// Specifies the visibility in the 3ds Max GUI.
		/// </summary>
		public override bool IsPublic
		{
			get { return true; }
		}


		/// <summary>
		/// Declares the kind of plugIn and thus where it appears in 3ds Max GUI.
		/// </summary>
		public override SClass_ID SuperClassID
		{
			get { return SClass_ID.Osm; }  // ObjectSpaceModifier
		}
		
		#endregion



		#region Methods		

		/// <summary>
		/// Returns a new instance of the Modifier.
		/// </summary>
		/// <param name="loading"></param>
		/// <returns></returns>
		public override object Create(bool loading)
		{
			return new ExtrudeModifier(global, this);
		}

		#endregion   

	}

}
