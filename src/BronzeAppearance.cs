using System;
using System.Collections.Generic;
using Jotunn.Entities;
using Jotunn.Managers;
using UnityEngine;
using UnityEngine.Rendering;

namespace BronzeBattleaxe
{
    /// <summary>
    /// Gives the cloned battleaxe its bronze look: the bundled model (BronzeModel) plus a rendered icon.
    /// If the model can't be loaded it falls back to re-colouring the iron model's diffuse atlas and icon at
    /// load time. Purely cosmetic - if everything here fails the item just keeps the iron look.
    /// </summary>
    internal static class BronzeAppearance
    {
        // Multiplied onto the luminance of grey (metal) pixels: iron grey -> warm bronze.
        private static readonly Color BronzeTone = new Color(1.35f, 0.95f, 0.50f);

        public static void TryApply(CustomItem item)
        {
            // Dedicated servers run without a graphics device and never render the item.
            if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null)
            {
                return;
            }

            var shared = item.ItemDrop.m_itemData.m_shared;

            var customModel = false;
            try
            {
                BronzeModel.Apply(item.ItemPrefab);
                customModel = true;
            }
            catch (Exception e)
            {
                BronzeBattleaxePlugin.Log.LogWarning($"Could not load the bronze model, using the recoloured iron one: {e}");
            }

            try
            {
                if (!customModel)
                {
                    TintModel(item.ItemPrefab);
                }

                // Recoloured iron icon: the final icon for the fallback look, a placeholder until the render below lands.
                TintIcons(shared);
            }
            catch (Exception e)
            {
                BronzeBattleaxePlugin.Log.LogWarning($"Could not re-colour the Bronze Battleaxe, keeping the iron look: {e}");
            }

            if (customModel)
            {
                RenderIcon(item.ItemPrefab, shared);
            }
        }

        /// <summary>
        /// The prefab's long axis is Z and the blade lies in its X-Z plane. Jötunn's camera looks down -Z with the
        /// prefab's rotation applied, so this turns the axe to face the camera with the head to the upper right.
        /// </summary>
        private static readonly Quaternion IconRotation = Quaternion.LookRotation(new Vector3(-1f, 1f, 0f), Vector3.forward);

        private static void RenderIcon(GameObject prefab, ItemDrop.ItemData.SharedData shared)
        {
            // Jötunn only renders objects that are active in the hierarchy, and the item clone lives under its
            // inactive prefab container, so render a short-lived copy of just the model (no network components).
            var proxy = new GameObject("BronzeBattleaxeIcon");
            try
            {
                var model = prefab.GetComponentInChildren<MeshFilter>(true).gameObject;
                UnityEngine.Object.Instantiate(model, proxy.transform, false);

                var sprite = RenderManager.Instance.Render(new RenderManager.RenderRequest(proxy)
                {
                    Width = 128,
                    Height = 128,
                    Rotation = IconRotation,
                });

                if (sprite != null)
                {
                    shared.m_icons = new[] { sprite };
                }
                else
                {
                    BronzeBattleaxePlugin.Log.LogWarning("Icon render returned nothing, keeping the placeholder icon");
                }
            }
            catch (Exception e)
            {
                BronzeBattleaxePlugin.Log.LogWarning($"Could not render the Bronze Battleaxe icon, keeping the placeholder: {e}");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(proxy);
            }
        }

        // --- Model -------------------------------------------------------------------------

        private static void TintModel(GameObject prefab)
        {
            var materials = new Dictionary<Material, Material>();
            var textures = new Dictionary<Texture, Texture>();

            foreach (var renderer in prefab.GetComponentsInChildren<MeshRenderer>(true))
            {
                var shared = renderer.sharedMaterials;
                for (var i = 0; i < shared.Length; i++)
                {
                    var original = shared[i];
                    if (original == null || !original.HasProperty("_MainTex"))
                    {
                        continue;
                    }

                    if (!materials.TryGetValue(original, out var bronze))
                    {
                        bronze = BronzeMaterial(original, textures);
                        materials[original] = bronze;
                    }

                    shared[i] = bronze;
                }

                // Assign a new array: sharedMaterials returns a copy, and the originals are vanilla assets.
                renderer.sharedMaterials = shared;
            }
        }

        private static Material BronzeMaterial(Material original, Dictionary<Texture, Texture> cache)
        {
            var bronze = new Material(original) { name = original.name + "_bronze" };

            var diffuse = original.GetTexture("_MainTex");
            if (diffuse == null)
            {
                return bronze;
            }

            if (!cache.TryGetValue(diffuse, out var tinted))
            {
                // The metallic map marks the metal parts, so the wooden haft keeps its colour.
                var mask = original.HasProperty("_MetallicGlossMap") ? original.GetTexture("_MetallicGlossMap") : null;
                tinted = TintByMask(diffuse, mask);
                cache[diffuse] = tinted;
            }

            bronze.SetTexture("_MainTex", tinted);
            return bronze;
        }

        private static Texture TintByMask(Texture diffuse, Texture mask)
        {
            var width = diffuse.width;
            var height = diffuse.height;

            var pixels = ReadPixels(diffuse, width, height, srgb: true);
            var metal = mask != null ? ReadPixels(mask, width, height, srgb: false) : null;

            for (var i = 0; i < pixels.Length; i++)
            {
                var strength = metal != null ? metal[i].r / 255f : 1f;
                pixels[i] = Recolour(pixels[i], strength);
            }

            var result = new Texture2D(width, height, TextureFormat.RGBA32, true, false)
            {
                name = diffuse.name + "_bronze",
                filterMode = diffuse.filterMode,
                wrapMode = diffuse.wrapMode,
                anisoLevel = diffuse.anisoLevel,
            };
            result.SetPixels32(pixels);
            result.Apply(true, true);
            return result;
        }

        // --- Icon --------------------------------------------------------------------------

        private static void TintIcons(ItemDrop.ItemData.SharedData shared)
        {
            var icons = shared.m_icons;
            if (icons == null)
            {
                return;
            }

            var tinted = new Sprite[icons.Length];
            for (var i = 0; i < icons.Length; i++)
            {
                tinted[i] = TintSprite(icons[i]);
            }

            shared.m_icons = tinted;
        }

        private static Sprite TintSprite(Sprite sprite)
        {
            if (sprite == null)
            {
                return null;
            }

            var rect = sprite.textureRect;
            var pixels = ReadPixels(sprite.texture, (int)rect.width, (int)rect.height, srgb: true,
                new Vector2(rect.width / sprite.texture.width, rect.height / sprite.texture.height),
                new Vector2(rect.x / sprite.texture.width, rect.y / sprite.texture.height));

            for (var i = 0; i < pixels.Length; i++)
            {
                var c = pixels[i];
                if (c.a == 0)
                {
                    continue;
                }

                // No mask for icons: treat unsaturated (grey) pixels as metal, brown wood and leather stay.
                Color.RGBToHSV(c, out _, out var saturation, out _);
                var strength = 1f - Mathf.SmoothStep(0.12f, 0.35f, saturation);
                pixels[i] = Recolour(c, strength);
            }

            var texture = new Texture2D((int)rect.width, (int)rect.height, TextureFormat.RGBA32, false, false)
            {
                name = sprite.name + "_bronze",
                filterMode = sprite.texture.filterMode,
            };
            texture.SetPixels32(pixels);
            texture.Apply(false, true);

            var result = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f), sprite.pixelsPerUnit);
            result.name = sprite.name + "_bronze";
            return result;
        }

        // --- Helpers -----------------------------------------------------------------------

        private static Color32 Recolour(Color32 pixel, float strength)
        {
            var luminance = (0.299f * pixel.r + 0.587f * pixel.g + 0.114f * pixel.b) / 255f;
            var r = Mathf.Lerp(pixel.r / 255f, Mathf.Min(1f, luminance * BronzeTone.r), strength);
            var g = Mathf.Lerp(pixel.g / 255f, Mathf.Min(1f, luminance * BronzeTone.g), strength);
            var b = Mathf.Lerp(pixel.b / 255f, Mathf.Min(1f, luminance * BronzeTone.b), strength);
            return new Color(r, g, b, pixel.a / 255f);
        }

        /// <summary>
        /// Reads a texture's pixels even if it was imported as non-readable, by blitting it
        /// through a temporary render target. Optionally only a sub-rectangle (scale/offset in UV space).
        /// </summary>
        private static Color32[] ReadPixels(Texture source, int width, int height, bool srgb,
            Vector2? scale = null, Vector2? offset = null)
        {
            var target = RenderTexture.GetTemporary(width, height, 0, RenderTextureFormat.ARGB32,
                srgb ? RenderTextureReadWrite.sRGB : RenderTextureReadWrite.Linear);
            var previous = RenderTexture.active;
            try
            {
                if (scale.HasValue)
                {
                    Graphics.Blit(source, target, scale.Value, offset ?? Vector2.zero);
                }
                else
                {
                    Graphics.Blit(source, target);
                }

                RenderTexture.active = target;
                var readable = new Texture2D(width, height, TextureFormat.RGBA32, false, !srgb);
                readable.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                var pixels = readable.GetPixels32();
                UnityEngine.Object.Destroy(readable);
                return pixels;
            }
            finally
            {
                RenderTexture.active = previous;
                RenderTexture.ReleaseTemporary(target);
            }
        }
    }
}
