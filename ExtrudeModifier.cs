using Autodesk.Max;
using Autodesk.Max.Plugins;
using System;
using System.Windows.Forms;

namespace ExtrudeModifier
{
	/// <summary>
	/// The Extrude Modifier in Max.Net.
	/// </summary>
	class ExtrudeModifier : Modifier
	{
        
		#region Fields

		private IGlobal global;
		private IManager manager;
		private ExtrudeModifierDescriptor desc;
		private IIParamBlock2 paramBlock;
		private IIParamMap2 paramMap;

		#endregion

        
		#region Properties

		public override uint ChannelsUsed
		{
			get 
			{ 
				return (uint)(ChannelMask.GEOM_CHANNEL | ChannelMask.TOPO_CHANNEL);
			}
		}

		public override uint ChannelsChanged
		{
			get 
			{
				return (uint)(ChannelMask.ALL_CHANNELS);
			}
		}

		public override int NumRefs
		{
			get
			{
				return 1;
			}
		}

		public override int NumSubs
		{
			get
			{
				return 1;
			}
		}

		public override int NumParamBlocks
		{
			get
			{
				return 1;
			}
		}

		public override ICreateMouseCallBack  CreateMouseCallBack
		{
			get { return null; }
		}
        
		public IIParamBlock2 Pblock2
		{
			get
			{
				return this.paramBlock;
			}
			set
			{
				this.paramBlock = value;
			}
		}
        
		public override IClass_ID InputType
		{
			get
			{
				return global.SplineShapeClassID; 
			}
		}

		#endregion


		#region Constructor

		public ExtrudeModifier(IGlobal global_, ExtrudeModifierDescriptor desc_)
		{
			this.global = global_;
			this.manager = this.global.Manager;
			this.desc = desc_;
			this.desc.MakeAutoParamBlocks(this);  // Call the SetReference method to assign the ParamBlock
		}

		#endregion	


		#region Methods

		public override void ModifyObject(int t, IModContext mc, IObjectState os, IINode node)
		{
			//take spline (footprint) and extrude it 
			IShapeObject shape = (IShapeObject)os.Obj;

			float amount = paramBlock.GetFloat(0, t, 0);
            ITriObject triObj = this.global.CreateNewTriObject(); 
			this.Extrude(shape, triObj, amount);

			//set obj to new triObj
			os.Obj = triObj;

			//update the objects validity, with the validity interval of the applied modifier - essential for using animations
			os.Obj.UpdateValidity((int)ChannelMask.GEOM_CHANNEL, this.LocalValidity(t)); 
			
			return;
		}

        public override RefResult NotifyRefChanged(IInterval changeInt, IReferenceTarget hTarget, ref UIntPtr partID, RefMessage message, bool b)
        {
            return RefResult.Succeed;
        }

		public override void SetReference(int i, IReferenceTarget rtarg)
		{
			if (i == 0)
				paramBlock = (IIParamBlock2)rtarg;
		}

		public override IReferenceTarget GetReference(int i)
		{
			return i == 0 ? paramBlock : null;
		}
        
		public override IAnimatable SubAnim(int i)
		{
			return i == 0 ? paramBlock : null;
		}


		public override IIParamBlock2 GetParamBlock(int i)
		{
			return i == 0 ? paramBlock : null;
		}

		public override void BeginEditParams(IIObjParam ip, uint flags, IAnimatable prev)
		{
			base.BeginEditParams(ip, flags, prev);

			this.paramMap = desc.ParamBlockDesc.CreateParamMap2(ip, null, IntPtr.Zero, this.paramBlock);
		}

		public override void EndEditParams(IIObjParam ip, uint flags, IAnimatable next)
		{
			base.EndEditParams(ip, flags, next);

			if (this.paramMap != null)
			{
				this.global.DestroyCPParamMap2(this.paramMap);
				this.paramMap = null;
			}
		}

		public void Extrude(IShapeObject shape, ITriObject triObj, float amount)
		{
			IMesh mesh = triObj.Mesh;
            
			//convert to PolyLine for mesh creation
            IPolyShape polyFootPrint = global.PolyShape.Create();
			shape.MakePolyShape(0, polyFootPrint, -1, true);

			int numPolys = polyFootPrint.NumLines;
			int numPoints;
			IPolyLine poly;
			int numVertices = 0;

			//determine number of vertices in shape object
			for (int i = 0; i < numPolys; i++)
			{
				poly = (IPolyLine)(polyFootPrint.Lines[i]);
				numPoints = poly.NumPts;
				numVertices += numPoints;
			}

			if (numVertices == 0)
				return;

			mesh.SetNumVerts(2 * numVertices, false, false); //twice the number of vertices in footprint are needed for extrusion
			mesh.SetNumFaces(2 * numVertices, false, false); //closed splines have as many segments as vertices

			//Umlaufrichtung der Vertices auf der Shape muss beachtet werden für Flächen - sonst Normalen falsch

			//iterate through vertices, copy them along extrusion path and set faces and visible edges
			int vertIndex = 0;
			IPoint3 point;
			for (int i = 0; i < numPolys; i++)
			{
				poly = polyFootPrint.Lines[i];

				//iterate through all points of poly
				numPoints = poly.NumPts;
				for (int j = 0; j < numPoints; j++)
				{
					//creates vertices for footprint and its extrusion
					point = poly.Pts[j].P;

					mesh.SetVert(vertIndex, point); vertIndex++;
					mesh.SetVert(vertIndex, global.Point3.Create(point.X, point.Y, point.Z + amount)); vertIndex++;

				}

				//create faces from vertices
				MakeStripFromVertices(vertIndex - 2 * numPoints, vertIndex - 2 * numPoints, 2 * numPoints, triObj.Mesh);

			}

			mesh.InvalidateGeomCache();
			mesh.InvalidateTopologyCache();
            
			return;
		}

		// method takes number of vertices given by extrusion of a contour, the order is important, 
		public void MakeStripFromVertices(int startVerts, int startFaces, int numVerts, IMesh mesh)
		{

			//3 vertices are minimum for a triangle definition
			if (numVerts < 3)
				return;

			if (startVerts < 0 || startFaces < 0 || (startVerts + numVerts) > mesh.NumVerts)
				return;

			int v1, v2, v3;
			v1 = startVerts;
			v2 = startVerts + 1;

			for (int i = 0; i < numVerts; i++)
			{

				v3 = (startVerts + (i + 2) % numVerts);

				if (i % 2 == 0)	//account for normal orientation, every second face is flipped due to vertex order
				{
					mesh.Faces[startFaces + i].SetVerts(v1, v3, v2);
					mesh.Faces[startFaces + i].SetEdgeVisFlags(EdgeVisibility.Vis, EdgeVisibility.Invis, EdgeVisibility.Vis);
				}
				else
				{
					mesh.Faces[startFaces + i].SetVerts(v1, v2, v3);
					mesh.Faces[startFaces + i].SetEdgeVisFlags(EdgeVisibility.Invis, EdgeVisibility.Vis, EdgeVisibility.Vis);
				}

				v1 = v2;
				v2 = v3;
			}

		}

		//method determining validity of modifier (only modifier! not validity of underlying object or further modifiers)
		public override IInterval LocalValidity(int t)
		{
			IInterval valid = global.Interval.Create();
			valid.SetInfinite();

			//intersect valid with all animated parameters to get final valid interval
			float f = 0.0f;
			this.Pblock2.GetValue(0, t, ref f, valid, 0);

			return valid;
		}

		#endregion

	}

    public static class AssemblyFunctions
    {
        public static void AssemblyMain()
        {
            var g = Autodesk.Max.GlobalInterface.Instance;
            var i = g.COREInterface13;
            i.AddClass(new ExtrudeModifierDescriptor(g));

            MessageBox.Show("BSpline Extrude");
        }

        public static void AssemblyShutdown()
        {
        }
    }
}



