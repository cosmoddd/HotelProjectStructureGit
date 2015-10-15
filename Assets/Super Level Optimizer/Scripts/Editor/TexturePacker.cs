using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;

namespace NGS.SuperLevelOptimizer
{
    public class TexturePacker : MonoBehaviour
    {
        public delegate void Set_Readable_Flag(Texture texture);
        public static Set_Readable_Flag SetReadableFlag;

        public static void SplitMesh(Renderer source, ref List<Renderer> renderers)
        {
            if (source.gameObject.GetComponent<MeshFilter>().sharedMesh.subMeshCount == 1)
            {
                Mesh mesh = new Mesh();
                mesh.vertices = source.gameObject.GetComponent<MeshFilter>().sharedMesh.vertices;
                mesh.normals = source.gameObject.GetComponent<MeshFilter>().sharedMesh.normals;
                mesh.tangents = source.gameObject.GetComponent<MeshFilter>().sharedMesh.tangents;
                mesh.uv = source.gameObject.GetComponent<MeshFilter>().sharedMesh.uv;
                mesh.uv2 = source.gameObject.GetComponent<MeshFilter>().sharedMesh.uv2;
                mesh.uv3 = source.gameObject.GetComponent<MeshFilter>().sharedMesh.uv3;
                mesh.uv4 = source.gameObject.GetComponent<MeshFilter>().sharedMesh.uv4;
                mesh.colors = source.gameObject.GetComponent<MeshFilter>().sharedMesh.colors;
                mesh.subMeshCount = 1;
                mesh.SetTriangles(source.gameObject.GetComponent<MeshFilter>().sharedMesh.GetTriangles(0), 0);

                source.gameObject.GetComponent<MeshFilter>().mesh = mesh;
                source.gameObject.GetComponent<MeshRenderer>().material = source.sharedMaterial;
                return;
            }

            for (int i = 0; i < source.gameObject.GetComponent<MeshFilter>().sharedMesh.subMeshCount; i++)
            {
                Mesh mesh = new Mesh();

                mesh.vertices = source.gameObject.GetComponent<MeshFilter>().sharedMesh.vertices;
                mesh.normals = source.gameObject.GetComponent<MeshFilter>().sharedMesh.normals;
                mesh.tangents = source.gameObject.GetComponent<MeshFilter>().sharedMesh.tangents;
                mesh.uv = source.gameObject.GetComponent<MeshFilter>().sharedMesh.uv;
                mesh.uv2 = source.gameObject.GetComponent<MeshFilter>().sharedMesh.uv2;
                mesh.uv3 = source.gameObject.GetComponent<MeshFilter>().sharedMesh.uv3;
                mesh.uv4 = source.gameObject.GetComponent<MeshFilter>().sharedMesh.uv4;
                mesh.colors = source.gameObject.GetComponent<MeshFilter>().sharedMesh.colors;
                mesh.subMeshCount = 1;

                mesh.SetTriangles(source.gameObject.GetComponent<MeshFilter>().sharedMesh.GetTriangles(i), 0);

                GameObject go = new GameObject(source.gameObject.name);

                go.transform.position = source.transform.position;
                go.transform.localScale = source.transform.lossyScale;
                go.transform.rotation = source.transform.rotation;
                go.transform.parent = source.transform.parent;

                MeshRenderer rend = go.AddComponent<MeshRenderer>();
                MeshFilter filter = go.AddComponent<MeshFilter>();

                rend.material = source.sharedMaterials[i];
                filter.mesh = mesh;

                Component[] components = source.GetComponents<Component>();
                for (int c = 0; c < components.Length; c++)
                {
                    if (components[c] == null)
                        continue;

                    if (go.GetComponent(components[c].GetType()) == null)
                        go.AddComponent(components[c].GetType());
                }

                go.isStatic = true;

                renderers.Add(rend);
            }

            renderers.Remove(source);
            DestroyImmediate(source.gameObject);
        }

        public static void PackTextures(ShaderGroup shaderGroup, bool superOptimization)
        {
            if (shaderGroup.renderers.Count == 0)
                return;

            Material mat = null;

            for (int i = 0; i < shaderGroup.renderers.Count; i++)
            {
                if (shaderGroup.renderers[i].sharedMaterial != null)
                    mat = new Material(shaderGroup.renderers[i].sharedMaterial);
            }

            if (mat == null)
                return;

            int textureCount = 0;
            List<string> textureNames = new List<string>();

            for (int i = 0; i < ShaderUtilInterface.GetPropertyCount(mat); i++)
            {
                if (ShaderUtilInterface.GetPropertyType(mat, i) == 4)
                {
                    UnityEditor.MaterialProperty matProp = UnityEditor.MaterialEditor.GetMaterialProperty(new UnityEngine.Object[1] { mat },
                        ShaderUtilInterface.GetPropertyName(mat, i));

                    if (matProp.textureDimension == UnityEditor.MaterialProperty.TexDim.Tex2D)
                    {
                        textureCount++;
                        textureNames.Add(ShaderUtilInterface.GetPropertyName(mat, i));
                    }
                }  
            }

            List<Texture2D>[] textures = new List<Texture2D>[textureCount];

            for (int i = 0; i < textureCount; i++)
            {
                textures[i] = new List<Texture2D>();

                for (int c = 0; c < shaderGroup.renderers.Count; c++)
                {
                    textures[i].Add(shaderGroup.renderers[c].sharedMaterial.GetTexture(textureNames[i]) as Texture2D);
                    SetReadableFlag(shaderGroup.renderers[c].sharedMaterial.GetTexture(textureNames[i]));
                }
            }

            Rect[] rects = null;
            for (int i = 0; i < textureCount; i++)
            {
                Texture2D atlas = new Texture2D(32, 32, TextureFormat.RGBA32, true);

                if (rects == null)
                    rects = atlas.PackTextures(textures[i].ToArray(), 0, 8192, false);
                else
                    atlas.PackTextures(textures[i].ToArray(), 0, 8192, false);

                mat.SetTexture(textureNames[i], atlas);
                mat.SetTextureOffset(textureNames[i], new Vector2(0, 0));
                mat.SetTextureScale(textureNames[i], new Vector2(1, 1));
            }

            for (int i = 0; i < shaderGroup.renderers.Count; i++)
            {
                Vector2[] uv, uvs;

                uv = shaderGroup.renderers[i].GetComponent<MeshFilter>().sharedMesh.uv;
                uvs = uv;

                bool addMat = true;
                for (int c = 0; c < uvs.Length; c++)
                {
                    if (!superOptimization)
                    {
                        if (uvs[c].x > 1 || uvs[c].x < 0 || uvs[c].y > 1 || uvs[c].y < 0)
                        {
                            uvs = uv;
                            addMat = false;
                            break;
                        }
                    }
                    else
                        if (uvs[c].x > 1 || uvs[c].x < 0 || uvs[c].y > 1 || uvs[c].y < 0)
                            uvs[c] = uvs[c].normalized;

                    uvs[c] = new Vector2((float)(((float)uvs[c].x * (float)rects[i].width) + (float)rects[i].x), (float)(((float)uvs[c].y * (float)rects[i].height) + (float)rects[i].y));
                }

                if (addMat)
                {
                    shaderGroup.renderers[i].GetComponent<MeshFilter>().sharedMesh.uv = uvs;
                    shaderGroup.renderers[i].sharedMaterial = mat;
                }
            }
        }
    }
}
