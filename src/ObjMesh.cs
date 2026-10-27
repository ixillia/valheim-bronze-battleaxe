using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEngine;

namespace BronzeBattleaxe
{
    /// <summary>
    /// Minimal Wavefront OBJ reader (v / vt / vn / f). Unity's own importer negates X to go from OBJ's
    /// right-handed space to its left-handed one; this does the same, so the mesh lines up with
    /// Valheim's meshes (and winding is reversed to match).
    /// </summary>
    internal static class ObjMesh
    {
        public static Mesh Load(Stream stream, string name)
        {
            var positions = new List<Vector3>();
            var uvs = new List<Vector2>();
            var normals = new List<Vector3>();

            var outPositions = new List<Vector3>();
            var outUvs = new List<Vector2>();
            var outNormals = new List<Vector3>();
            var triangles = new List<int>();
            var lookup = new Dictionary<string, int>();

            using (var reader = new StreamReader(stream))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    var parts = line.Split(new[] { ' ' }, System.StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length == 0)
                    {
                        continue;
                    }

                    switch (parts[0])
                    {
                        case "v":
                            positions.Add(new Vector3(-Float(parts[1]), Float(parts[2]), Float(parts[3])));
                            break;
                        case "vt":
                            uvs.Add(new Vector2(Float(parts[1]), Float(parts[2])));
                            break;
                        case "vn":
                            normals.Add(new Vector3(-Float(parts[1]), Float(parts[2]), Float(parts[3])));
                            break;
                        case "f":
                            var corners = new int[parts.Length - 1];
                            for (var i = 1; i < parts.Length; i++)
                            {
                                if (!lookup.TryGetValue(parts[i], out var index))
                                {
                                    var ids = parts[i].Split('/');
                                    index = outPositions.Count;
                                    outPositions.Add(positions[int.Parse(ids[0]) - 1]);
                                    outUvs.Add(ids.Length > 1 && ids[1].Length > 0 ? uvs[int.Parse(ids[1]) - 1] : Vector2.zero);
                                    outNormals.Add(ids.Length > 2 && ids[2].Length > 0 ? normals[int.Parse(ids[2]) - 1] : Vector3.up);
                                    lookup[parts[i]] = index;
                                }

                                corners[i - 1] = index;
                            }

                            for (var i = 1; i < corners.Length - 1; i++)
                            {
                                // Reversed winding, see class summary.
                                triangles.Add(corners[0]);
                                triangles.Add(corners[i + 1]);
                                triangles.Add(corners[i]);
                            }

                            break;
                    }
                }
            }

            var mesh = new Mesh { name = name };
            mesh.SetVertices(outPositions);
            mesh.SetUVs(0, outUvs);
            mesh.SetNormals(outNormals);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateBounds();
            mesh.RecalculateTangents();
            return mesh;
        }

        private static float Float(string s)
        {
            return float.Parse(s, CultureInfo.InvariantCulture);
        }
    }
}
