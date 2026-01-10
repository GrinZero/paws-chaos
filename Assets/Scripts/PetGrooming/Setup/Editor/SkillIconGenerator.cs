using UnityEngine;
using UnityEditor;
using System.IO;

namespace PetGrooming.Setup.Editor
{
    /// <summary>
    /// Editor utility to generate skill icon sprites programmatically.
    /// Creates placeholder icons with distinct visual designs for each skill.
    /// Requirements: 3.5.1-3.5.6
    /// </summary>
    public static class SkillIconGenerator
    {
        private const int IconSize = 128;
        private const string IconsPath = "Assets/UI/MobileUI/Icons";

        // Theme colors from design document
        private static readonly Color CaptureNetColor = new Color(0.29f, 0.56f, 0.85f, 1f);    // #4A90D9 Blue
        private static readonly Color LeashColor = new Color(0.36f, 0.72f, 0.36f, 1f);         // #5CB85C Green
        private static readonly Color CalmingSprayColor = new Color(0.61f, 0.35f, 0.71f, 1f);  // #9B59B6 Purple
        private static readonly Color CaptureButtonColor = new Color(0.96f, 0.65f, 0.14f, 1f); // #F5A623 Gold
        private static readonly Color StruggleButtonColor = new Color(0.91f, 0.30f, 0.24f, 1f);// #E74C3C Orange-Red

        [MenuItem("PetGrooming/Generate Skill Icons")]
        public static void GenerateAllSkillIcons()
        {
            // Ensure directory exists
            if (!Directory.Exists(IconsPath))
            {
                Directory.CreateDirectory(IconsPath);
            }

            // Generate each skill icon
            GenerateCaptureNetIcon();
            GenerateLeashIcon();
            GenerateCalmingSprayIcon();
            GenerateCaptureButtonIcon();
            GenerateStruggleButtonIcon();

            AssetDatabase.Refresh();
            Debug.Log("Skill icons generated successfully at: " + IconsPath);
        }

        /// <summary>
        /// Generates the Capture Net icon - Blue net/mesh visual
        /// Requirement: 3.5.1
        /// </summary>
        private static void GenerateCaptureNetIcon()
        {
            Texture2D texture = new Texture2D(IconSize, IconSize, TextureFormat.RGBA32, false);
            Color[] pixels = new Color[IconSize * IconSize];

            // Fill with transparent
            for (int i = 0; i < pixels.Length; i++)
                pixels[i] = Color.clear;

            // Draw net pattern
            int center = IconSize / 2;
            int radius = IconSize / 2 - 8;

            // Draw circular background
            DrawFilledCircle(pixels, center, center, radius, new Color(CaptureNetColor.r, CaptureNetColor.g, CaptureNetColor.b, 0.3f));

            // Draw net grid lines
            int gridSpacing = 16;
            Color lineColor = CaptureNetColor;

            // Horizontal lines
            for (int y = center - radius + gridSpacing; y < center + radius; y += gridSpacing)
            {
                for (int x = 0; x < IconSize; x++)
                {
                    if (IsInsideCircle(x, y, center, center, radius))
                    {
                        SetPixel(pixels, x, y, lineColor);
                        SetPixel(pixels, x, y + 1, lineColor);
                    }
                }
            }

            // Vertical lines
            for (int x = center - radius + gridSpacing; x < center + radius; x += gridSpacing)
            {
                for (int y = 0; y < IconSize; y++)
                {
                    if (IsInsideCircle(x, y, center, center, radius))
                    {
                        SetPixel(pixels, x, y, lineColor);
                        SetPixel(pixels, x + 1, y, lineColor);
                    }
                }
            }

            // Draw circle outline
            DrawCircleOutline(pixels, center, center, radius, lineColor, 3);

            texture.SetPixels(pixels);
            texture.Apply();

            SaveTextureAsSprite(texture, "Icon_CaptureNet");
        }

        /// <summary>
        /// Generates the Leash icon - Green rope/hook visual
        /// Requirement: 3.5.2
        /// </summary>
        private static void GenerateLeashIcon()
        {
            Texture2D texture = new Texture2D(IconSize, IconSize, TextureFormat.RGBA32, false);
            Color[] pixels = new Color[IconSize * IconSize];

            for (int i = 0; i < pixels.Length; i++)
                pixels[i] = Color.clear;

            int center = IconSize / 2;
            Color ropeColor = LeashColor;
            Color darkRope = new Color(ropeColor.r * 0.7f, ropeColor.g * 0.7f, ropeColor.b * 0.7f, 1f);

            // Draw curved rope
            for (int i = 0; i < 100; i++)
            {
                float t = i / 100f;
                int x = (int)(20 + t * 88);
                int y = (int)(center + Mathf.Sin(t * Mathf.PI * 1.5f) * 30);
                DrawFilledCircle(pixels, x, y, 4, ropeColor);
                DrawFilledCircle(pixels, x, y, 2, darkRope);
            }

            // Draw hook at end
            int hookX = 108;
            int hookY = center;
            DrawFilledCircle(pixels, hookX, hookY, 8, new Color(0.8f, 0.7f, 0.3f, 1f)); // Gold hook
            DrawFilledCircle(pixels, hookX, hookY + 10, 6, new Color(0.8f, 0.7f, 0.3f, 1f));
            DrawFilledCircle(pixels, hookX - 5, hookY + 15, 5, new Color(0.8f, 0.7f, 0.3f, 1f));

            texture.SetPixels(pixels);
            texture.Apply();

            SaveTextureAsSprite(texture, "Icon_Leash");
        }

        /// <summary>
        /// Generates the Calming Spray icon - Purple spray/mist visual
        /// Requirement: 3.5.3
        /// </summary>
        private static void GenerateCalmingSprayIcon()
        {
            Texture2D texture = new Texture2D(IconSize, IconSize, TextureFormat.RGBA32, false);
            Color[] pixels = new Color[IconSize * IconSize];

            for (int i = 0; i < pixels.Length; i++)
                pixels[i] = Color.clear;

            int center = IconSize / 2;
            Color sprayColor = CalmingSprayColor;

            // Draw spray bottle body
            DrawFilledRect(pixels, 30, 40, 35, 70, new Color(0.4f, 0.4f, 0.5f, 1f));
            DrawFilledRect(pixels, 35, 30, 25, 15, new Color(0.5f, 0.5f, 0.6f, 1f)); // Cap
            DrawFilledRect(pixels, 55, 35, 20, 8, new Color(0.6f, 0.6f, 0.7f, 1f)); // Nozzle

            // Draw spray mist particles
            System.Random rand = new System.Random(42); // Fixed seed for consistency
            for (int i = 0; i < 30; i++)
            {
                int px = 75 + rand.Next(40);
                int py = 25 + rand.Next(60);
                int size = 2 + rand.Next(4);
                float alpha = 0.3f + (float)rand.NextDouble() * 0.5f;
                DrawFilledCircle(pixels, px, py, size, new Color(sprayColor.r, sprayColor.g, sprayColor.b, alpha));
            }

            texture.SetPixels(pixels);
            texture.Apply();

            SaveTextureAsSprite(texture, "Icon_CalmingSpray");
        }


        /// <summary>
        /// Generates the Capture/Grab button icon - Gold/yellow hand visual
        /// Requirement: 3.5.4
        /// </summary>
        private static void GenerateCaptureButtonIcon()
        {
            Texture2D texture = new Texture2D(IconSize, IconSize, TextureFormat.RGBA32, false);
            Color[] pixels = new Color[IconSize * IconSize];

            for (int i = 0; i < pixels.Length; i++)
                pixels[i] = Color.clear;

            int center = IconSize / 2;
            Color handColor = CaptureButtonColor;
            Color darkHand = new Color(handColor.r * 0.8f, handColor.g * 0.8f, handColor.b * 0.8f, 1f);

            // Draw palm
            DrawFilledCircle(pixels, center, center + 15, 28, handColor);

            // Draw fingers (5 fingers spread out in grabbing pose)
            int[] fingerAngles = { -60, -30, 0, 30, 60 };
            int[] fingerLengths = { 25, 35, 38, 35, 25 };

            for (int i = 0; i < 5; i++)
            {
                float angle = (fingerAngles[i] - 90) * Mathf.Deg2Rad;
                int length = fingerLengths[i];

                for (int j = 0; j < length; j++)
                {
                    int fx = center + (int)(Mathf.Cos(angle) * (25 + j));
                    int fy = center + 15 + (int)(Mathf.Sin(angle) * (25 + j));
                    int fingerWidth = 6 - j / 10;
                    DrawFilledCircle(pixels, fx, fy, fingerWidth, handColor);
                }
            }

            // Add highlight
            DrawFilledCircle(pixels, center - 5, center + 10, 8, new Color(1f, 1f, 1f, 0.3f));

            texture.SetPixels(pixels);
            texture.Apply();

            SaveTextureAsSprite(texture, "Icon_Capture");
        }

        /// <summary>
        /// Generates the Struggle button icon - Orange/red breaking chains visual
        /// Requirement: 3.5.5
        /// </summary>
        private static void GenerateStruggleButtonIcon()
        {
            Texture2D texture = new Texture2D(IconSize, IconSize, TextureFormat.RGBA32, false);
            Color[] pixels = new Color[IconSize * IconSize];

            for (int i = 0; i < pixels.Length; i++)
                pixels[i] = Color.clear;

            int center = IconSize / 2;
            Color chainColor = StruggleButtonColor;
            Color darkChain = new Color(chainColor.r * 0.6f, chainColor.g * 0.6f, chainColor.b * 0.6f, 1f);

            // Draw chain links (broken in the middle)
            // Left chain segment
            for (int i = 0; i < 3; i++)
            {
                int linkX = 20 + i * 20;
                int linkY = center + (i % 2 == 0 ? -5 : 5);
                DrawChainLink(pixels, linkX, linkY, chainColor, darkChain);
            }

            // Right chain segment (offset to show break)
            for (int i = 0; i < 3; i++)
            {
                int linkX = 70 + i * 20;
                int linkY = center + (i % 2 == 0 ? 5 : -5);
                DrawChainLink(pixels, linkX, linkY, chainColor, darkChain);
            }

            // Draw break effect (sparks/energy)
            Color sparkColor = new Color(1f, 0.9f, 0.3f, 1f);
            DrawFilledCircle(pixels, center, center, 8, sparkColor);
            
            // Draw radiating lines for break effect
            for (int angle = 0; angle < 360; angle += 45)
            {
                float rad = angle * Mathf.Deg2Rad;
                for (int r = 10; r < 20; r += 2)
                {
                    int px = center + (int)(Mathf.Cos(rad) * r);
                    int py = center + (int)(Mathf.Sin(rad) * r);
                    SetPixel(pixels, px, py, sparkColor);
                }
            }

            texture.SetPixels(pixels);
            texture.Apply();

            SaveTextureAsSprite(texture, "Icon_Struggle");
        }

        private static void DrawChainLink(Color[] pixels, int cx, int cy, Color mainColor, Color darkColor)
        {
            // Outer ring
            DrawCircleOutline(pixels, cx, cy, 10, mainColor, 3);
            // Inner highlight
            DrawCircleOutline(pixels, cx - 1, cy - 1, 8, new Color(1f, 1f, 1f, 0.3f), 1);
        }

        #region Drawing Helpers

        private static void SetPixel(Color[] pixels, int x, int y, Color color)
        {
            if (x >= 0 && x < IconSize && y >= 0 && y < IconSize)
            {
                int index = y * IconSize + x;
                // Alpha blend
                Color existing = pixels[index];
                float alpha = color.a;
                pixels[index] = new Color(
                    existing.r * (1 - alpha) + color.r * alpha,
                    existing.g * (1 - alpha) + color.g * alpha,
                    existing.b * (1 - alpha) + color.b * alpha,
                    Mathf.Max(existing.a, alpha)
                );
            }
        }

        private static bool IsInsideCircle(int x, int y, int cx, int cy, int radius)
        {
            int dx = x - cx;
            int dy = y - cy;
            return dx * dx + dy * dy <= radius * radius;
        }

        private static void DrawFilledCircle(Color[] pixels, int cx, int cy, int radius, Color color)
        {
            for (int y = cy - radius; y <= cy + radius; y++)
            {
                for (int x = cx - radius; x <= cx + radius; x++)
                {
                    if (IsInsideCircle(x, y, cx, cy, radius))
                    {
                        SetPixel(pixels, x, y, color);
                    }
                }
            }
        }

        private static void DrawCircleOutline(Color[] pixels, int cx, int cy, int radius, Color color, int thickness)
        {
            for (int y = cy - radius - thickness; y <= cy + radius + thickness; y++)
            {
                for (int x = cx - radius - thickness; x <= cx + radius + thickness; x++)
                {
                    int dx = x - cx;
                    int dy = y - cy;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);
                    if (dist >= radius - thickness / 2f && dist <= radius + thickness / 2f)
                    {
                        SetPixel(pixels, x, y, color);
                    }
                }
            }
        }

        private static void DrawFilledRect(Color[] pixels, int x, int y, int width, int height, Color color)
        {
            for (int py = y; py < y + height; py++)
            {
                for (int px = x; px < x + width; px++)
                {
                    SetPixel(pixels, px, py, color);
                }
            }
        }

        #endregion

        #region Save Helpers

        private static void SaveTextureAsSprite(Texture2D texture, string name)
        {
            string path = $"{IconsPath}/{name}.png";
            byte[] pngData = texture.EncodeToPNG();
            File.WriteAllBytes(path, pngData);
            Object.DestroyImmediate(texture);

            // Import as sprite
            AssetDatabase.ImportAsset(path);
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single; // 单精灵模式
                importer.spritePixelsPerUnit = 100;
                importer.filterMode = FilterMode.Bilinear;
                importer.mipmapEnabled = false;
                importer.alphaIsTransparency = true;
                importer.SaveAndReimport();
            }

            Debug.Log($"Generated skill icon: {path}");
        }

        #endregion
    }
}
