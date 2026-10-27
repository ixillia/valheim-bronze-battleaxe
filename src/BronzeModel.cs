using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using UnityEngine;

namespace BronzeBattleaxe
{
    /// <summary>
    /// Swaps the cloned battleaxe's mesh and textures for the bundled bronze model (embedded resources,
    /// see assets/ and scripts/fit_model.py). The model is fitted to the iron Battleaxe's mesh space, so the
    /// prefab's own transforms, the hand position and the attack animations keep working unchanged.
    /// </summary>
    internal static class BronzeModel
    {
        private const string ResourcePrefix = "BronzeBattleaxe.assets.";

        // Looked up by signature: referencing ImageConversion.LoadImage directly makes the compiler want the game's
        // ReadOnlySpan overload, which a net48 build can't resolve.
        private static readonly MethodInfo LoadImage = typeof(ImageConversion).GetMethod(
            "LoadImage", new[] { typeof(Texture2D), typeof(byte[]), typeof(bool) });

        /// <summary>Replaces mesh, textures and trail anchors. Throws if anything is missing.</summary>
        public static void Apply(GameObject prefab)
        {
            var filter = FindAxeMeshFilter(prefab);
            var renderer = filter.GetComponent<MeshRenderer>();

            // Load everything first so a failure leaves the prefab untouched.
            Mesh mesh;
            using (var stream = Open("bronze_battleaxe.obj"))
            {
                mesh = ObjMesh.Load(stream, "bronze_battleaxe");
            }

            var albedo = LoadTexture("bronze_battleaxe_albedo.png", linear: false);
            var normal = LoadTexture("bronze_battleaxe_normal.png", linear: true);
            var metallic = LoadTexture("bronze_battleaxe_metallic.png", linear: true);
            var trail = ReadTrail();

            // The vanilla mesh has a second submesh/material slot for upgrade effects; ours has one.
            var bronze = new Material(renderer.sharedMaterials[0]) { name = "bronze_battleaxe" };
            bronze.SetTexture("_MainTex", albedo);
            bronze.SetTexture("_BumpMap", normal);
            bronze.SetTexture("_MetallicGlossMap", metallic);

            filter.sharedMesh = mesh;
            renderer.sharedMaterials = new[] { bronze };
            MoveTrail(prefab, trail);
        }

        private static MeshFilter FindAxeMeshFilter(GameObject prefab)
        {
            foreach (var filter in prefab.GetComponentsInChildren<MeshFilter>(true))
            {
                if (filter.GetComponent<MeshRenderer>() != null)
                {
                    return filter;
                }
            }

            throw new InvalidOperationException("no mesh found on " + prefab.name);
        }

        /// <summary>Puts the swing-trail anchors along the new blade instead of the iron one's.</summary>
        private static void MoveTrail(GameObject prefab, Dictionary<string, Vector3> points)
        {
            foreach (var name in new[] { "base", "tip" })
            {
                var anchor = prefab.transform.Find("attach/equiped/trail/" + name);
                if (anchor != null && points.TryGetValue(name, out var position))
                {
                    anchor.localPosition = position;
                }
            }
        }

        private static Dictionary<string, Vector3> ReadTrail()
        {
            var points = new Dictionary<string, Vector3>();
            using (var reader = new StreamReader(Open("bronze_battleaxe.trail")))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    var p = line.Split(' ');
                    if (p.Length == 4)
                    {
                        points[p[0]] = new Vector3(Float(p[1]), Float(p[2]), Float(p[3]));
                    }
                }
            }

            return points;
        }

        private static Texture2D LoadTexture(string file, bool linear)
        {
            byte[] bytes;
            using (var stream = Open(file))
            using (var memory = new MemoryStream())
            {
                stream.CopyTo(memory);
                bytes = memory.ToArray();
            }

            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, true, linear)
            {
                name = Path.GetFileNameWithoutExtension(file),
                wrapMode = TextureWrapMode.Repeat,
                filterMode = FilterMode.Trilinear,
                anisoLevel = 4,
            };

            if (!(bool)LoadImage.Invoke(null, new object[] { texture, bytes, false }))
            {
                throw new InvalidOperationException("could not decode " + file);
            }

            return texture;
        }

        private static Stream Open(string file)
        {
            var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(ResourcePrefix + file);
            if (stream == null)
            {
                throw new FileNotFoundException("embedded resource missing: " + file);
            }

            return stream;
        }

        private static float Float(string s)
        {
            return float.Parse(s, CultureInfo.InvariantCulture);
        }
    }
}
