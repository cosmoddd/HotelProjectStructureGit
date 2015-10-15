using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NGS.SuperLevelOptimizer;

namespace NGS.SuperLevelOptimizer
{
    public class MeshCombiner
    {
        public static Mesh CombineMeshes(MaterialGroup materialGroup)
        {
            int vertexCount = materialGroup.vertexCount;

            Vector3[] vertices = new Vector3[materialGroup.vertexCount];
            Vector3[] normals = new Vector3[materialGroup.vertexCount];
            Vector4[] tangents = new Vector4[materialGroup.vertexCount];
            List<Vector2> uv = new List<Vector2>();
            List<Vector2> uv2 = new List<Vector2>();
            List<Vector2> uv3 = new List<Vector2>();
            List<Vector2> uv4 = new List<Vector2>();
            List<Color> colors = new List<Color>();

            int offset = 0;

            #region vertices
            for (int i = 0; i < materialGroup.meshes.Count; i++)
            {
                GetVertices(materialGroup.meshes[i].vertexCount, materialGroup.meshes[i].vertices, vertices, ref offset, materialGroup.renderers[i].transform.localToWorldMatrix);
            }
            #endregion

            #region normals
            offset = 0;
            for (int i = 0; i < materialGroup.meshes.Count; i++)
            {
                GetNormal(materialGroup.meshes[i].vertexCount, materialGroup.meshes[i].normals, normals, ref offset, materialGroup.renderers[i].transform.localToWorldMatrix);
            }
            #endregion

            #region tangents
            offset = 0;
            for (int i = 0; i < materialGroup.meshes.Count; i++)
            {
                GetTangents(materialGroup.meshes[i].vertexCount, materialGroup.meshes[i].tangents, tangents, ref offset, materialGroup.renderers[i].transform.localToWorldMatrix);
            }
            #endregion

            #region triangles

            List<int[]> triangles = new List<int[]>();

            for (int i = 0; i < materialGroup.meshes[0].subMeshCount; i++)
            {
                int curTrianglesCount = 0;
                foreach (var mesh in materialGroup.meshes)
                {
                    curTrianglesCount = curTrianglesCount + mesh.GetTriangles(i).Length;
                }

                int[] curTriangles = new int[curTrianglesCount];

                int triangleOffset = 0;
                int vertexOffset = 0;
                foreach (var renderer in materialGroup.renderers)
                {
                    int[] inputtriangles = materialGroup.meshes[materialGroup.renderers.IndexOf(renderer)].GetTriangles(i);
                    for (int c = 0; c < inputtriangles.Length; c += 3)
                    {
                        if (renderer.transform.lossyScale.x < 0 || renderer.transform.lossyScale.y < 0 || renderer.transform.lossyScale.z < 0)
                        {
                            curTriangles[c + triangleOffset] = inputtriangles[c + 2] + vertexOffset;
                            curTriangles[c + 1 + triangleOffset] = inputtriangles[c + 1] + vertexOffset;
                            curTriangles[c + 2 + triangleOffset] = inputtriangles[c] + vertexOffset;
                        }
                        else
                        {
                            curTriangles[c + triangleOffset] = inputtriangles[c] + vertexOffset;
                            curTriangles[c + 1 + triangleOffset] = inputtriangles[c + 1] + vertexOffset;
                            curTriangles[c + 2 + triangleOffset] = inputtriangles[c + 2] + vertexOffset;
                        }
                    }

                    triangleOffset += inputtriangles.Length;
                    vertexOffset += renderer.GetComponent<MeshFilter>().sharedMesh.vertexCount;
                }

                triangles.Add(curTriangles);
            }

            #endregion

            #region other

            foreach (var renderer in materialGroup.renderers)
            {
                uv.AddRange(renderer.GetComponent<MeshFilter>().sharedMesh.uv);
                uv2.AddRange(renderer.GetComponent<MeshFilter>().sharedMesh.uv2);
                uv3.AddRange(renderer.GetComponent<MeshFilter>().sharedMesh.uv3);
                uv4.AddRange(renderer.GetComponent<MeshFilter>().sharedMesh.uv4);
                colors.AddRange(renderer.GetComponent<MeshFilter>().sharedMesh.colors);
            }

            #endregion

            Mesh _mesh = new Mesh();
            _mesh.name = "CombineMesh";
            _mesh.vertices = vertices;
            _mesh.normals = normals;
            _mesh.tangents = tangents;

            if (uv.Count == materialGroup.vertexCount) _mesh.uv = uv.ToArray();
            if (uv2.Count == materialGroup.vertexCount) _mesh.uv2 = uv2.ToArray();
            if (uv3.Count == materialGroup.vertexCount) _mesh.uv3 = uv3.ToArray();
            if (uv4.Count == materialGroup.vertexCount) _mesh.uv4 = uv4.ToArray();
            if (colors.Count == materialGroup.vertexCount) _mesh.colors = colors.ToArray();

            _mesh.subMeshCount = materialGroup.meshes[0].subMeshCount;
            for (int i = 0; i < _mesh.subMeshCount; i++)
                _mesh.SetTriangles(triangles[i], i);

            _mesh.Optimize();

            return _mesh;
        }

        private static void GetVertices(int vertexcount, Vector3[] sources, Vector3[] main, ref int offset, Matrix4x4 transform)
        {
            for (int i = 0; i < sources.Length; i++)
                main[i + offset] = transform.MultiplyPoint(sources[i]);
            offset += vertexcount;
        }

        private static void GetNormal(int vertexcount, Vector3[] sources, Vector3[] main, ref int offset, Matrix4x4 transform)
        {
            for (int i = 0; i < sources.Length; i++)
                main[i + offset] = transform.MultiplyVector(sources[i]).normalized;
            offset += vertexcount;
        }

        private static void GetTangents(int vertexcount, Vector4[] sources, Vector4[] main, ref int offset, Matrix4x4 transform)
        {
            for (int i = 0; i < sources.Length; i++)
            {
                Vector4 p4 = sources[i];
                Vector3 p = new Vector3(p4.x, p4.y, p4.z);
                p = transform.MultiplyVector(p).normalized;
                main[i + offset] = new Vector4(p.x, p.y, p.z, p4.w);
            }

            offset += vertexcount;
        }
    }
}
