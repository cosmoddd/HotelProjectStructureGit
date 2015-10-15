using UnityEngine;
using System;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace NGS.SuperLevelOptimizer
{
    public class MaterialsSorter
    {
        public static ShaderGroup[] SortByShaders(Renderer[] renderers)
        {
            List<ShaderGroup> groups = new List<ShaderGroup>();

            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] == null)
                    continue;

                ShaderGroup group = null;

                for (int c = 0; c < groups.Count; c++)
                    if (groups[c].shader == renderers[i].sharedMaterial.shader)
                    {
                        if (ShaderGroup.GetName(groups[c].renderers[0].sharedMaterial) == ShaderGroup.GetName(renderers[i].sharedMaterial))
                            group = groups[c];
                    }

                if (group == null)
                {
                    group = new ShaderGroup();
                    group.shader = renderers[i].sharedMaterial.shader;
                    groups.Add(group);
                }

                group.renderers.Add(renderers[i]);
            }

            return groups.ToArray();
        }

        public static MaterialGroup[] SortByMaterials(Renderer[] renderers)
        {
            List<MaterialGroup> groups = new List<MaterialGroup>();

            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] == null)
                    continue;

                MaterialGroup group = null;

                for (int c = 0; c < groups.Count; c++)
                {
                    if (MaterialGroup.IsMatch(groups[c].materials.ToArray(), renderers[i].sharedMaterials))
                    {
                        int vertexCount = groups[c].vertexCount + renderers[i].GetComponent<MeshFilter>().sharedMesh.vertexCount;

                        if (vertexCount < 65536)
                            group = groups[c];
                    }
                }

                if (group == null)
                {
                    group = new MaterialGroup();
                    group.materials = renderers[i].sharedMaterials;
                    groups.Add(group);
                }

                group.renderers.Add(renderers[i]);
                group.meshes.Add(renderers[i].GetComponent<MeshFilter>().sharedMesh);
                group.vertexCount += renderers[i].GetComponent<MeshFilter>().sharedMesh.vertexCount;
            }

            return groups.ToArray();
        }
    }


    public class ShaderGroup
    {
        public List<Renderer> renderers = new List<Renderer>();
        public Shader shader;

        public static string GetName(Material mat)
        {
            string name = "";

            for (int c = 0; c < ShaderUtilInterface.GetPropertyCount(mat); c++)
            {
                if (ShaderUtilInterface.GetPropertyType(mat, c) == 0)
                    name = name + mat.GetColor(ShaderUtilInterface.GetPropertyName(mat, c));

                if (ShaderUtilInterface.GetPropertyType(mat, c) == 1)
                    name = name + mat.GetVector(ShaderUtilInterface.GetPropertyName(mat, c));

                if (ShaderUtilInterface.GetPropertyType(mat, c) == 3)
                    name = name + mat.GetFloat(ShaderUtilInterface.GetPropertyName(mat, c));

                if (ShaderUtilInterface.GetPropertyType(mat, c) == 4)
                    if (mat.GetTexture(ShaderUtilInterface.GetPropertyName(mat, c)) == null)
                        name = name + "Null";
                    else
                        name = name + "NoNull";
            }

            return name;
        }
    }

    public class MaterialGroup
    {
        public List<Renderer> renderers = new List<Renderer>();
        public List<Mesh> meshes = new List<Mesh>();

        public Material[] materials;
        public int vertexCount = 0;

        public static bool IsMatch(Material[] materials1, Material[] materials2)
        {
            if(materials1.Length !=materials2.Length)
                return false;

            for (int i = 0; i < materials1.Length; i++)
            {
                if (ShaderUtilInterface.GetPropertyCount(materials1[i]) != ShaderUtilInterface.GetPropertyCount(materials2[i]))
                    return false;

                for (int c = 0; c < ShaderUtilInterface.GetPropertyCount(materials1[i]); c++)
                {
                    if (ShaderUtilInterface.GetPropertyType(materials1[i], c) == ShaderUtilInterface.GetPropertyType(materials2[i], c))
                    {
                        if (ShaderUtilInterface.GetPropertyType(materials1[i], c) == 0)
                        {
                            if (materials1[i].GetColor(ShaderUtilInterface.GetPropertyName(materials1[i], c)) != materials2[i].GetColor(ShaderUtilInterface.GetPropertyName(materials2[i], c)))
                                return false;
                        }

                        if (ShaderUtilInterface.GetPropertyType(materials1[i], c) == 1)
                        {
                            if (materials1[i].GetVector(ShaderUtilInterface.GetPropertyName(materials1[i], c)) != materials2[i].GetVector(ShaderUtilInterface.GetPropertyName(materials2[i], c)))
                                return false;
                        }

                        if (ShaderUtilInterface.GetPropertyType(materials1[i], c) == 3)
                        {
                            if (materials1[i].GetFloat(ShaderUtilInterface.GetPropertyName(materials1[i], c)) != materials2[i].GetFloat(ShaderUtilInterface.GetPropertyName(materials2[i], c)))
                                return false;
                        }

                        if (ShaderUtilInterface.GetPropertyType(materials1[i], c) == 4)
                        {
                            if (materials1[i].GetTexture(ShaderUtilInterface.GetPropertyName(materials1[i], c)) != materials2[i].GetTexture(ShaderUtilInterface.GetPropertyName(materials2[i], c)))
                                return false;
                        }
                    }
                    else return false;
                }
            }

            return true;
        }
    }


    public static class ShaderUtilInterface
    {
        public static Dictionary<string, MethodInfo> methods = new Dictionary<string, MethodInfo>();

        static ShaderUtilInterface()
        {
            var asm = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(a => a.GetTypes().Any(t => t.Name == "ShaderUtil"));
            if (asm != null)
            {
                var tp = asm.GetTypes().FirstOrDefault(t => t.Name == "ShaderUtil");
                foreach (var method in tp.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static))
                {
                    methods[method.Name] = method;
                }
            }
        }

        public static List<Texture> GetTextures(Material shader, ref List<string> names)
        {
            var list = new List<Texture>();
            var count = GetPropertyCount(shader);
            for (var i = 0; i < count; i++)
            {
                if (GetPropertyType(shader, i) == 4)
                {
                    list.Add((Texture)GetProperty(shader, i));
                    names.Add(GetPropertyName(shader, i));
                }
            }
            return list;
        }

        public static List<Texture> GetTextures(Material shader)
        {
            var list = new List<Texture>();
            var count = GetPropertyCount(shader);
            for (var i = 0; i < count; i++)
            {
                if (GetPropertyType(shader, i) == 4)
                {
                    list.Add((Texture)GetProperty(shader, i));
                }
            }
            return list;
        }

        public static int GetPropertyCount(Material shader)
        {
            return Call<int>("GetPropertyCount", shader.shader);
        }

        public static int GetPropertyType(Material shader, int index)
        {
            return Call<int>("GetPropertyType", shader.shader, index);
        }

        public static string GetPropertyName(Material shader, int index)
        {
            return Call<string>("GetPropertyName", shader.shader, index);
        }

        public static object GetProperty(Material material, int index)
        {
            var name = GetPropertyName(material, index);
            var type = GetPropertyType(material, index);
            switch (type)
            {
                case 0:
                    return material.GetColor(name);

                case 1:
                    return material.GetVector(name);


                case 2:
                case 3:
                    return material.GetFloat(name);

                case 4:
                    return material.GetTexture(name);

            }
            return null;
        }

        public static T Call<T>(string name, params object[] parameters)
        {
            return (T)methods[name].Invoke(null, parameters);
        }
    }
}
