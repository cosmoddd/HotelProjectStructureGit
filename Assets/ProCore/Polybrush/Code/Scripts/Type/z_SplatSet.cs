using UnityEngine;
using System.Collections.Generic;

namespace Polybrush
{
	[System.Serializable]
	public class z_SplatSet
	{
		private static readonly Color32 Color32_Black = new Color32(0,0,0,0);

		const int COMPONENTS_PER_ATTRIBUTE = 4;

		// The mesh texture blend weights.
		public z_SplatWeight[] weights;

		// Assigns where each 4 component block of weights is stored.  Coincides with how the byte array is applied to a mesh.
		public z_MeshChannel[] channelMap;

		public int ComponentCount { get { return channelMap.Length * 4; } }
		public int ChannelCount { get { return channelMap.Length; } }
		public int Length { get { return weights.Length; } }

		public z_SplatWeight this[int i]
		{
			get { return weights[i]; }
			set { weights[i] = value; }
		}

		public z_SplatSet(int vertexCount, z_MeshChannel[] channelMap)
		{
			this.weights = z_Util.Fill<z_SplatWeight>((index) => { return new z_SplatWeight(channelMap.Length * 4); }, vertexCount);
			this.channelMap = channelMap;
		}

		public z_SplatSet(Mesh mesh, z_MeshChannel[] channelMap)
		{
			List<Vector4> 	uv0 = new List<Vector4>(),
							uv2 = new List<Vector4>(),
							uv3 = new List<Vector4>(),
							uv4 = new List<Vector4>();

			mesh.GetUVs(0, uv0);
			mesh.GetUVs(1, uv2);
			mesh.GetUVs(2, uv3);
			mesh.GetUVs(3, uv4);

			Color32[] color = mesh.colors32;
			Vector4[] tangent = mesh.tangents;

			int vertexCount = mesh.vertexCount;
			int channelCount = channelMap.Length;
			
			this.channelMap = channelMap;
			this.weights = z_Util.Fill<z_SplatWeight>((index) => { return new z_SplatWeight(channelCount * 4); }, vertexCount);	

			for(int n = 0; n < channelCount; n++)
			{
				int index = n * COMPONENTS_PER_ATTRIBUTE;

				switch(channelMap[n])
				{
					case z_MeshChannel.UV0:
					{
						if(uv0 == null || uv0.Count != vertexCount)
							goto case z_MeshChannel.NULL;

						for(int i = 0; i < vertexCount; i++)
							this.weights[i].Set(index, uv0[i]);

						break;
					}

					case z_MeshChannel.UV2:
					{
						if(uv2 == null || uv2.Count != vertexCount)
							goto case z_MeshChannel.NULL;

						for(int i = 0; i < vertexCount; i++)
							this.weights[i].Set(index, uv2[i]);

						break;
					}

					case z_MeshChannel.UV3:
					{
						if(uv3 == null || uv3.Count != vertexCount)
							goto case z_MeshChannel.NULL;

						for(int i = 0; i < vertexCount; i++)
							this.weights[i].Set(index, uv3[i]);

						break;
					}

					case z_MeshChannel.UV4:
					{
						if(uv4 == null || uv4.Count != vertexCount)
							goto case z_MeshChannel.NULL;

						for(int i = 0; i < vertexCount; i++)
							this.weights[i].Set(index, uv4[i]);

						break;
					}

					case z_MeshChannel.COLOR:
					{
						if(color == null || color.Length != vertexCount)
							goto case z_MeshChannel.NULL;


						for(int i = 0; i < vertexCount; i++)
							this.weights[i].Set(index, color[i]);

						break;
					}

					case z_MeshChannel.TANGENT:
					{
						if(tangent == null || tangent.Length != vertexCount)
							goto case z_MeshChannel.NULL;


						for(int i = 0; i < vertexCount; i++)
							this.weights[i].Set(index, tangent[i]);

						break;
					}

					case z_MeshChannel.NULL:
					{
						for(int i = 0; i < vertexCount; i++)
							this.weights[i].Set(index, Color32_Black);
						break;
					}
				}
			}

			for(int i = 0; i < vertexCount; i++)
				this.weights[i].MakeNonZero();
		}

		public void CopyTo(z_SplatSet other)
		{
			if(other.Length != Length)
			{
				Debug.LogWarning("Copying splat weights to mismatched container length.");
				other.weights = new z_SplatWeight[Length];
			}

			System.Array.Copy(weights, other.weights, Length);
		}

		public void Apply(Mesh mesh)
		{
			if(mesh == null || mesh.vertexCount != Length)
			{
				Debug.LogError("Assigning texture blend weights with mismatched array length!");
				return;
			}

			for(int n = 0; n < channelMap.Length; n++)
			{
				int index = n * COMPONENTS_PER_ATTRIBUTE;

				switch(channelMap[n])
				{
					case z_MeshChannel.UV0:
						mesh.SetUVs(0, GetList(index));
						break;

					case z_MeshChannel.UV2:
						mesh.SetUVs(1, GetList(index));
						break;

					case z_MeshChannel.UV3:
						mesh.SetUVs(2, GetList(index));
						break;

					case z_MeshChannel.UV4:
						mesh.SetUVs(3, GetList(index));
						break;

					case z_MeshChannel.COLOR:
						mesh.colors32 = GetByteArray(index);
						break;
	
					case z_MeshChannel.TANGENT:
						mesh.tangents = GetArray(index);
						break;
				}
			}
		}

		public List<Vector4> GetList(int index)
		{
			List<Vector4> list = new List<Vector4>(Length);
			for(int i = 0; i < Length; i++)
				list.Add( weights[i].GetVector4(index) );
			return list;
		}

		public Vector4[] GetArray(int index)
		{
			Vector4[] list = new Vector4[Length];
			for(int i = 0; i < Length; i++)
				list[i] = weights[i].GetVector4(index);
			return list;
		}

		public Color32[] GetByteArray(int index)
		{
			Color32[] bytes = new Color32[Length];
			for(int i = 0; i < Length; i++)
				bytes[i] = weights[i].GetColor32(index);
			return bytes;
		}
	}
}
