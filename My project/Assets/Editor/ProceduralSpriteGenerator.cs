#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Unity Editor utility that procedurally synthesizes 14 crisp 2D sprite textures
/// (Player, 3 Bird archetypes, 3 Bullet variants, 3 Power-up icons, 2 FX particles, 2 Background textures)
/// and writes them directly into Assets/Sprites/ with appropriate TextureImporter settings.
/// </summary>
public static class ProceduralSpriteGenerator
{
    private const string TargetFolder = "Assets/Sprites";

    [MenuItem("Tools/Generate Game Sprites")]
    public static void GenerateAllSprites()
    {
        string fullDirPath = Path.Combine(Application.dataPath, "Sprites");
        if (!Directory.Exists(fullDirPath))
        {
            Directory.CreateDirectory(fullDirPath);
            AssetDatabase.Refresh();
        }

        Debug.Log("<color=cyan>[ProceduralSpriteGenerator]</color> Synthesizing 14 2D textures...");

        SaveAndImportSprite("player_ship", GeneratePlayerShip(), 100f, TextureWrapMode.Clamp);
        SaveAndImportSprite("bird_basic", GenerateBirdBasic(), 100f, TextureWrapMode.Clamp);
        SaveAndImportSprite("bird_fast", GenerateBirdFast(), 100f, TextureWrapMode.Clamp);
        SaveAndImportSprite("bird_boss", GenerateBirdBoss(), 100f, TextureWrapMode.Clamp);
        SaveAndImportSprite("bullet_standard", GenerateBulletStandard(), 100f, TextureWrapMode.Clamp);
        SaveAndImportSprite("bullet_spread", GenerateBulletSpread(), 100f, TextureWrapMode.Clamp);
        SaveAndImportSprite("bullet_boss", GenerateBulletBoss(), 100f, TextureWrapMode.Clamp);
        SaveAndImportSprite("powerup_spread", GeneratePowerupSpread(), 100f, TextureWrapMode.Clamp);
        SaveAndImportSprite("powerup_rapid", GeneratePowerupRapid(), 100f, TextureWrapMode.Clamp);
        SaveAndImportSprite("powerup_health", GeneratePowerupHealth(), 100f, TextureWrapMode.Clamp);
        SaveAndImportSprite("particle_feather", GenerateParticleFeather(), 100f, TextureWrapMode.Clamp);
        SaveAndImportSprite("particle_spark", GenerateParticleSpark(), 100f, TextureWrapMode.Clamp);
        SaveAndImportSprite("bg_sky_base", GenerateBgSkyBase(), 100f, TextureWrapMode.Repeat);
        SaveAndImportSprite("bg_stars", GenerateBgStars(), 100f, TextureWrapMode.Repeat);
        SaveAndImportSprite("bg_clouds", GenerateBgClouds(), 100f, TextureWrapMode.Repeat);
        SaveAndImportSprite("bg_mountains", GenerateBgMountains(), 100f, TextureWrapMode.Repeat);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("<color=green>[ProceduralSpriteGenerator]</color> Successfully generated and imported all 14 sprites into Assets/Sprites/!");
    }

    private static void SaveAndImportSprite(string fileName, Texture2D texture, float ppu, TextureWrapMode wrapMode)
    {
        string relativePath = $"{TargetFolder}/{fileName}.png";
        string fullPath = Path.Combine(Application.dataPath, "Sprites", $"{fileName}.png");

        byte[] pngBytes = texture.EncodeToPNG();
        File.WriteAllBytes(fullPath, pngBytes);
        UnityEngine.Object.DestroyImmediate(texture);

        AssetDatabase.ImportAsset(relativePath, ImportAssetOptions.ForceUpdate);

        TextureImporter importer = AssetImporter.GetAtPath(relativePath) as TextureImporter;
        if (importer != null)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = ppu;
            importer.spritePivot = new Vector2(0.5f, 0.5f);
            importer.filterMode = FilterMode.Bilinear;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.wrapMode = wrapMode;
            importer.SaveAndReimport();
        }
    }

    #region Procedural Sprite Generation

    private static Texture2D GeneratePlayerShip()
    {
        int W = 128, H = 128;
        var tex = new Texture2D(W, H, TextureFormat.RGBA32, false);
        ClearTexture(tex);
        float cx = 64f;

        for (int y = 0; y < H; y++)
        {
            for (int x = 0; x < W; x++)
            {
                float dx = Mathf.Abs(x + 0.5f - cx);
                float py = y + 0.5f;

                // Thrusters
                if (py >= 6 && py <= 24 && dx >= 4 && dx <= 14)
                {
                    float flameT = (py - 6) / 18f;
                    float flameWidth = 5f * (1f - flameT * 0.4f);
                    float dFlame = Mathf.Abs(dx - 9f);
                    if (dFlame < flameWidth)
                    {
                        float alpha = (1f - dFlame / flameWidth) * (1f - (24 - py) / 24f * 0.3f);
                        Color flameCol = Color.Lerp(new Color(1f, 0.24f, 0f, 1f), new Color(1f, 0.93f, 0.35f, 1f), flameT);
                        BlendPixel(tex, x, y, flameCol, alpha);
                    }
                }

                // Wings
                float wingEdge = 16f + (py - 22) * 1.05f;
                if (py >= 22 && py <= 60 && dx <= 54 && dx >= 14)
                {
                    float trailingEdge = 22f + (dx - 14) * 0.25f;
                    if (py >= trailingEdge && py <= wingEdge)
                    {
                        float edgeDist = Mathf.Min(wingEdge - py, Mathf.Min(py - trailingEdge, 54 - dx));
                        float alpha = Mathf.Clamp01(edgeDist + 0.5f);
                        Color wingCol = dx > 48 ? new Color(0f, 0.47f, 0.76f, 1f) : new Color(0f, 0.90f, 1f, 1f);
                        BlendPixel(tex, x, y, wingCol, alpha);
                    }
                }

                // Wingtip Cannons
                if (dx >= 49 && dx <= 53 && py >= 34 && py <= 62)
                {
                    Color cannonCol = py > 56 ? Color.white : new Color(0.81f, 0.85f, 0.86f, 1f);
                    BlendPixel(tex, x, y, cannonCol, 1f);
                }

                // Fuselage
                float bodyWidth;
                if (py > 116) bodyWidth = 0;
                else if (py >= 85) bodyWidth = 2f + (116 - py) * 0.45f;
                else if (py >= 35) bodyWidth = 16f - (py - 35) * 0.05f;
                else bodyWidth = 16f - (35 - py) * 0.2f;

                if (dx <= bodyWidth + 0.5f)
                {
                    float dist = bodyWidth - dx;
                    float alpha = Mathf.Clamp01(dist + 0.5f);
                    Color bodyCol = new Color(0f, 0.90f, 1f, 1f);
                    if (dx < 3f && py < 85) bodyCol = new Color(0.70f, 0.92f, 0.95f, 1f);
                    else if (dx > bodyWidth - 2.5f) bodyCol = new Color(0f, 0.30f, 0.51f, 1f);

                    BlendPixel(tex, x, y, bodyCol, alpha);
                }

                // Canopy
                if (py >= 68 && py <= 96)
                {
                    float canopyW = 6.5f * (1f - Mathf.Abs(py - 82) / 16f);
                    if (dx <= canopyW)
                    {
                        float alpha = Mathf.Clamp01(canopyW - dx + 0.5f);
                        Color glassCol = (dx < 2.5f && py > 78) ? Color.white : new Color(0.88f, 0.97f, 0.98f, 1f);
                        BlendPixel(tex, x, y, glassCol, alpha);
                    }
                }
            }
        }
        tex.Apply();
        return tex;
    }

    private static Texture2D GenerateBirdBasic()
    {
        int W = 96, H = 96;
        var tex = new Texture2D(W, H, TextureFormat.RGBA32, false);
        ClearTexture(tex);
        float cx = 48f;

        for (int y = 0; y < H; y++)
        {
            for (int x = 0; x < W; x++)
            {
                float dx = Mathf.Abs(x + 0.5f - cx);
                float py = y + 0.5f;

                // Tail
                if (py >= 70 && py <= 88 && dx <= 14)
                {
                    float tailW = 6f + (py - 70) * 0.45f;
                    if (dx <= tailW)
                    {
                        float alpha = Mathf.Clamp01(tailW - dx + 0.5f);
                        BlendPixel(tex, x, y, new Color(0.01f, 0.47f, 0.74f, 1f), alpha);
                    }
                }

                // Wings
                if (dx >= 12 && dx <= 44 && py >= 38 && py <= 78)
                {
                    float wingTip = 38f + (dx - 12) * 1.15f;
                    float wingBase = 32f + (dx - 12) * 0.4f;
                    if (py >= wingBase && py <= wingTip)
                    {
                        float alpha = Mathf.Clamp01(Mathf.Min(wingTip - py, py - wingBase) + 0.5f);
                        Color wingCol = dx > 34 ? new Color(0.01f, 0.34f, 0.61f, 1f) : new Color(0.01f, 0.66f, 0.96f, 1f);
                        BlendPixel(tex, x, y, wingCol, alpha);
                    }
                }

                // Body
                float bDist = (dx * dx) / (16f * 16f) + ((py - 52) * (py - 52)) / (20f * 20f);
                if (bDist <= 1.05f)
                {
                    float alpha = Mathf.Clamp01((1f - bDist) * 15f);
                    Color bodyCol = (dx <= 9f && py >= 42 && py <= 64) ? new Color(0.93f, 0.94f, 0.95f, 1f) : new Color(0.01f, 0.66f, 0.96f, 1f);
                    BlendPixel(tex, x, y, bodyCol, alpha);
                }

                // Head
                float hDist = Mathf.Sqrt(dx * dx + (py - 30) * (py - 30));
                if (hDist <= 13f)
                {
                    float alpha = Mathf.Clamp01(13f - hDist + 0.5f);
                    BlendPixel(tex, x, y, new Color(0.01f, 0.53f, 0.82f, 1f), alpha);
                }

                // Beak
                if (py >= 12 && py <= 24)
                {
                    float beakW = (py - 12) * 0.55f;
                    if (dx <= beakW)
                    {
                        float alpha = Mathf.Clamp01(beakW - dx + 0.5f);
                        BlendPixel(tex, x, y, new Color(1f, 0.76f, 0.03f, 1f), alpha);
                    }
                }

                // Eyes
                if (dx >= 4.5f && dx <= 7.5f && py >= 26.5f && py <= 29.5f)
                {
                    BlendPixel(tex, x, y, new Color(0.13f, 0.13f, 0.13f, 1f), 1f);
                }
                if (dx >= 5f && dx <= 6f && py >= 27f && py <= 28f)
                {
                    BlendPixel(tex, x, y, Color.white, 1f);
                }
            }
        }
        tex.Apply();
        return tex;
    }

    private static Texture2D GenerateBirdFast()
    {
        int W = 96, H = 96;
        var tex = new Texture2D(W, H, TextureFormat.RGBA32, false);
        ClearTexture(tex);
        float cx = 48f;

        for (int y = 0; y < H; y++)
        {
            for (int x = 0; x < W; x++)
            {
                float dx = Mathf.Abs(x + 0.5f - cx);
                float py = y + 0.5f;

                // Wings
                if (dx >= 10 && dx <= 45 && py >= 30 && py <= 84)
                {
                    float wingLead = 30f + (dx - 10) * 1.55f;
                    float wingTrail = 24f + (dx - 10) * 0.9f;
                    if (py >= wingTrail && py <= wingLead)
                    {
                        float alpha = Mathf.Clamp01(Mathf.Min(wingLead - py, py - wingTrail) + 0.5f);
                        Color col = dx > 32 ? new Color(0.84f, 0f, 0f, 1f) : new Color(1f, 0.09f, 0.27f, 1f);
                        BlendPixel(tex, x, y, col, alpha);
                    }
                }

                // Body
                float bDist = (dx * dx) / (12f * 12f) + ((py - 48) * (py - 48)) / (24f * 24f);
                if (bDist <= 1.05f)
                {
                    float alpha = Mathf.Clamp01((1f - bDist) * 14f);
                    Color col = dx < 4f ? new Color(1f, 0.43f, 0f, 1f) : new Color(1f, 0.09f, 0.27f, 1f);
                    BlendPixel(tex, x, y, col, alpha);
                }

                // Beak
                if (py >= 10 && py <= 22)
                {
                    float beakW = (py - 10) * 0.45f;
                    if (dx <= beakW)
                    {
                        float alpha = Mathf.Clamp01(beakW - dx + 0.5f);
                        BlendPixel(tex, x, y, new Color(1f, 0.84f, 0f, 1f), alpha);
                    }
                }

                // Eyes
                if (dx >= 4f && dx <= 7f && py >= 24.5f && py <= 27.5f)
                {
                    BlendPixel(tex, x, y, new Color(1f, 0.92f, 0f, 1f), 1f);
                }
                if (dx >= 5f && dx <= 6f && py >= 25.5f && py <= 26.5f)
                {
                    BlendPixel(tex, x, y, Color.black, 1f);
                }
            }
        }
        tex.Apply();
        return tex;
    }

    private static Texture2D GenerateBirdBoss()
    {
        int W = 256, H = 256;
        var tex = new Texture2D(W, H, TextureFormat.RGBA32, false);
        ClearTexture(tex);
        float cx = 128f;

        for (int y = 0; y < H; y++)
        {
            for (int x = 0; x < W; x++)
            {
                float dx = Mathf.Abs(x + 0.5f - cx);
                float py = y + 0.5f;

                // Tail
                if (py >= 170 && py <= 248 && dx <= 55)
                {
                    float tailW = 20f + (py - 170) * 0.45f;
                    if (dx <= tailW)
                    {
                        float alpha = Mathf.Clamp01(tailW - dx + 0.5f);
                        Color tailCol = Color.Lerp(new Color(0.42f, 0.11f, 0.60f, 1f), new Color(1f, 0.57f, 0f, 1f), (py - 170) / 78f);
                        BlendPixel(tex, x, y, tailCol, alpha);
                    }
                }

                // Wings
                if (dx >= 28 && dx <= 122 && py >= 75 && py <= 220)
                {
                    float wingUpper = 75f + (dx - 28) * 1.55f;
                    float wingLower = 55f + (dx - 28) * 0.75f;
                    if (py >= wingLower && py <= wingUpper)
                    {
                        float alpha = Mathf.Clamp01(Mathf.Min(wingUpper - py, py - wingLower) + 0.5f);
                        Color wingCol = dx > 105 ? new Color(1f, 0.84f, 0f, 1f) :
                                        dx > 75 ? new Color(0.56f, 0.14f, 0.67f, 1f) :
                                                  new Color(0.42f, 0.11f, 0.60f, 1f);
                        BlendPixel(tex, x, y, wingCol, alpha);
                    }
                }

                // Torso
                float tDist = (dx * dx) / (38f * 38f) + ((py - 125) * (py - 125)) / (55f * 55f);
                if (tDist <= 1.05f)
                {
                    float alpha = Mathf.Clamp01((1f - tDist) * 18f);
                    BlendPixel(tex, x, y, new Color(0.42f, 0.11f, 0.60f, 1f), alpha);
                }

                // Chest Armor
                if (py >= 95 && py <= 145)
                {
                    float armorW = 24f * (1f - Mathf.Abs(py - 120) / 30f);
                    if (dx <= armorW)
                    {
                        float alpha = Mathf.Clamp01(armorW - dx + 0.5f);
                        Color armorCol = dx < 6f ? new Color(1f, 0.93f, 0.35f, 1f) : new Color(1f, 0.76f, 0.03f, 1f);
                        BlendPixel(tex, x, y, armorCol, alpha);
                    }
                }

                // Head
                float hDist = Mathf.Sqrt(dx * dx + (py - 65) * (py - 65));
                if (hDist <= 28f)
                {
                    float alpha = Mathf.Clamp01(28f - hDist + 0.5f);
                    BlendPixel(tex, x, y, new Color(0.56f, 0.14f, 0.67f, 1f), alpha);
                }

                // Beak
                if (py >= 22 && py <= 45)
                {
                    float beakW = (py - 22) * 0.6f;
                    if (dx <= beakW)
                    {
                        float alpha = Mathf.Clamp01(beakW - dx + 0.5f);
                        BlendPixel(tex, x, y, new Color(1f, 0.63f, 0f, 1f), alpha);
                    }
                }

                // Ruby Eyes
                if (dx >= 11f && dx <= 17f && py >= 55f && py <= 61f)
                {
                    BlendPixel(tex, x, y, new Color(1f, 0.09f, 0.27f, 1f), 1f);
                }
                if (dx >= 13f && dx <= 15f && py >= 57f && py <= 59f)
                {
                    BlendPixel(tex, x, y, Color.white, 1f);
                }
            }
        }
        tex.Apply();
        return tex;
    }

    private static Texture2D GenerateBulletStandard()
    {
        int W = 32, H = 64;
        var tex = new Texture2D(W, H, TextureFormat.RGBA32, false);
        ClearTexture(tex);
        float cx = 16f, cy = 32f;

        for (int y = 0; y < H; y++)
        {
            for (int x = 0; x < W; x++)
            {
                float dx = Mathf.Abs(x + 0.5f - cx);
                float dy = Mathf.Abs(y + 0.5f - cy);
                float capY = Mathf.Max(0f, dy - 18f);
                float dist = Mathf.Sqrt(dx * dx + capY * capY);

                if (dist <= 14f)
                {
                    float core = Mathf.Clamp01(3.5f - dist);
                    float glow = Mathf.Clamp01((14f - dist) / 14f);
                    Color col = Color.Lerp(new Color(0f, 0.90f, 1f, 1f), Color.white, core);
                    BlendPixel(tex, x, y, col, glow * glow);
                }
            }
        }
        tex.Apply();
        return tex;
    }

    private static Texture2D GenerateBulletSpread()
    {
        int W = 48, H = 48;
        var tex = new Texture2D(W, H, TextureFormat.RGBA32, false);
        ClearTexture(tex);
        float cx = 24f, cy = 24f;

        for (int y = 0; y < H; y++)
        {
            for (int x = 0; x < W; x++)
            {
                float dist = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(cx, cy));
                if (dist <= 22f)
                {
                    float t = dist / 22f;
                    float alpha = (1f - t) * (1f - t);
                    Color col;
                    if (dist <= 6f) col = Color.white;
                    else if (dist <= 12f) col = new Color(1f, 0.93f, 0.35f, 1f);
                    else col = new Color(1f, 0.43f, 0f, 1f);

                    BlendPixel(tex, x, y, col, alpha);
                }
            }
        }
        tex.Apply();
        return tex;
    }

    private static Texture2D GenerateBulletBoss()
    {
        int W = 32, H = 32;
        var tex = new Texture2D(W, H, TextureFormat.RGBA32, false);
        ClearTexture(tex);
        float cx = 16f, cy = 16f;

        for (int y = 0; y < H; y++)
        {
            for (int x = 0; x < W; x++)
            {
                float dist = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(cx, cy));
                if (dist <= 15f)
                {
                    float t = dist / 15f;
                    float alpha = (1f - t) * (1f - t);
                    Color col = dist <= 4f ? Color.white : new Color(1f, 0.09f, 0.27f, 1f);
                    BlendPixel(tex, x, y, col, alpha);
                }
            }
        }
        tex.Apply();
        return tex;
    }

    private static Texture2D GeneratePowerupSpread()
    {
        int W = 64, H = 64;
        var tex = new Texture2D(W, H, TextureFormat.RGBA32, false);
        ClearTexture(tex);
        float cx = 32f, cy = 32f;

        DrawHexagon(tex, cx, cy, 27f, new Color(0.01f, 0.53f, 0.82f, 1f), new Color(0.01f, 0.34f, 0.61f, 1f));
        DrawArrow(tex, cx, 18f, cx, 42f, 4.5f, new Color(1f, 0.84f, 0f, 1f));
        DrawArrow(tex, 22f, 22f, 28f, 40f, 3.5f, new Color(1f, 0.84f, 0f, 1f));
        DrawArrow(tex, 42f, 22f, 36f, 40f, 3.5f, new Color(1f, 0.84f, 0f, 1f));

        tex.Apply();
        return tex;
    }

    private static Texture2D GeneratePowerupRapid()
    {
        int W = 64, H = 64;
        var tex = new Texture2D(W, H, TextureFormat.RGBA32, false);
        ClearTexture(tex);
        float cx = 32f, cy = 32f;

        DrawHexagon(tex, cx, cy, 27f, new Color(0.90f, 0.32f, 0f, 1f), new Color(0.75f, 0.21f, 0.05f, 1f));

        for (int y = 14; y <= 50; y++)
        {
            for (int x = 18; x <= 46; x++)
            {
                bool isBolt = false;
                if (y >= 14 && y <= 32 && Mathf.Abs((x - 32) + (y - 23) * 0.6f) < 4f) isBolt = true;
                if (y >= 28 && y <= 34 && x >= 24 && x <= 40) isBolt = true;
                if (y >= 32 && y <= 50 && Mathf.Abs((x - 32) + (y - 41) * 0.6f) < 4f) isBolt = true;

                if (isBolt) BlendPixel(tex, x, y, new Color(1f, 0.92f, 0f, 1f), 1f);
            }
        }

        tex.Apply();
        return tex;
    }

    private static Texture2D GeneratePowerupHealth()
    {
        int W = 64, H = 64;
        var tex = new Texture2D(W, H, TextureFormat.RGBA32, false);
        ClearTexture(tex);
        float cx = 32f, cy = 32f;

        DrawCircleBadge(tex, cx, cy, 27f, new Color(0f, 0.78f, 0.33f, 1f), new Color(0.11f, 0.37f, 0.13f, 1f));

        for (int y = 18; y <= 46; y++)
        {
            for (int x = 18; x <= 46; x++)
            {
                bool isCross = (Mathf.Abs(x - 32) <= 5 && Mathf.Abs(y - 32) <= 14) ||
                               (Mathf.Abs(y - 32) <= 5 && Mathf.Abs(x - 32) <= 14);
                if (isCross) BlendPixel(tex, x, y, Color.white, 1f);
            }
        }

        tex.Apply();
        return tex;
    }

    private static Texture2D GenerateParticleFeather()
    {
        int W = 32, H = 32;
        var tex = new Texture2D(W, H, TextureFormat.RGBA32, false);
        ClearTexture(tex);

        for (int y = 0; y < H; y++)
        {
            for (int x = 0; x < W; x++)
            {
                float spineX = 6f + (y / 32f) * 20f;
                float dist = Mathf.Abs(x - spineX);
                float vaneW = 6f * Mathf.Sin((y / 32f) * Mathf.PI);

                if (dist <= vaneW)
                {
                    float alpha = Mathf.Clamp01((vaneW - dist) / 2f);
                    Color col = dist < 1.2f ? new Color(0.86f, 0.86f, 0.86f, 1f) : Color.white;
                    BlendPixel(tex, x, y, col, alpha);
                }
            }
        }
        tex.Apply();
        return tex;
    }

    private static Texture2D GenerateParticleSpark()
    {
        int W = 32, H = 32;
        var tex = new Texture2D(W, H, TextureFormat.RGBA32, false);
        ClearTexture(tex);
        float cx = 16f, cy = 16f;

        for (int y = 0; y < H; y++)
        {
            for (int x = 0; x < W; x++)
            {
                float dx = Mathf.Abs(x + 0.5f - cx);
                float dy = Mathf.Abs(y + 0.5f - cy);
                float star = 1f / (1f + (dx * dy * 0.4f) + (dx + dy) * 0.15f);
                if (dx <= 14 && dy <= 14 && star > 0.05f)
                {
                    Color col = (dx < 2f && dy < 2f) ? Color.white : new Color(1f, 0.92f, 0.23f, 1f);
                    BlendPixel(tex, x, y, col, star);
                }
            }
        }
        tex.Apply();
        return tex;
    }

    private static Texture2D GenerateBgSkyBase()
    {
        int W = 512, H = 1024;
        var tex = new Texture2D(W, H, TextureFormat.RGBA32, false);
        Color spaceNavy = new Color(0.04f, 0.07f, 0.16f, 1f);
        Color twilight = new Color(0.06f, 0.16f, 0.27f, 1f);
        Color azureHorizon = new Color(0.11f, 0.23f, 0.44f, 1f);

        for (int y = 0; y < H; y++)
        {
            float normY = (float)y / H;
            float t = Mathf.Sin(normY * Mathf.PI * 2f - Mathf.PI * 0.5f) * 0.5f + 0.5f;
            Color rowCol = t < 0.5f ? Color.Lerp(spaceNavy, twilight, t * 2f) : Color.Lerp(twilight, azureHorizon, (t - 0.5f) * 2f);
            for (int x = 0; x < W; x++)
            {
                tex.SetPixel(x, y, rowCol);
            }
        }

        var rng = new System.Random(42);
        for (int i = 0; i < 200; i++)
        {
            int sx = rng.Next(W);
            int sy = rng.Next(H);
            float starAlpha = rng.Next(60, 220) / 255f;
            BlendPixel(tex, sx, sy, Color.white, starAlpha);
        }

        tex.Apply();
        return tex;
    }

    private static Texture2D GenerateBgClouds()
    {
        int W = 512, H = 1024;
        var tex = new Texture2D(W, H, TextureFormat.RGBA32, false);

        for (int y = 0; y < H; y++)
        {
            for (int x = 0; x < W; x++)
            {
                double nx = (double)x / W;
                double ny = (double)y / H;
                double angleY = ny * Math.PI * 2.0;
                double cy1 = Math.Cos(angleY) * 1.5;
                double sy1 = Math.Sin(angleY) * 1.5;

                double noise = Math.Sin(nx * 12.0 + cy1) * 0.5 +
                               Math.Cos(nx * 20.0 + sy1) * 0.3 +
                               Math.Sin(nx * 32.0 + cy1 * 2.0) * 0.2;

                noise = (noise + 1.0) * 0.5;

                if (noise > 0.58)
                {
                    float cloudDensity = (float)((noise - 0.58) / 0.42);
                    float alpha = cloudDensity * cloudDensity * 0.45f;
                    tex.SetPixel(x, y, new Color(0.94f, 0.97f, 1f, alpha));
                }
                else
                {
                    tex.SetPixel(x, y, Color.clear);
                }
            }
        }
        tex.Apply();
        return tex;
    }

    public static Texture2D GenerateBgStars()
    {
        return GenerateBgSkyBase();
    }

    public static Texture2D GenerateBgMountains()
    {
        int W = 512, H = 1024;
        var tex = new Texture2D(W, H, TextureFormat.RGBA32, false);
        ClearTexture(tex);

        for (int x = 0; x < W; x++)
        {
            float t = (float)x / W * Mathf.PI * 2f;
            int h1 = (int)(280 + Mathf.Sin(t * 3f) * 60f + Mathf.Sin(t * 7f + 1.2f) * 30f);
            for (int y = h1; y < H; y++)
            {
                float alpha = 0.55f * ((float)(y - h1) / (H - h1) * 0.5f + 0.5f);
                BlendPixel(tex, x, y, new Color(0.11f, 0.16f, 0.29f, 1f), alpha);
            }

            int h2 = (int)(520 + Mathf.Sin(t * 2f + 0.8f) * 80f + Mathf.Cos(t * 5f + 2.1f) * 45f);
            for (int y = h2; y < H; y++)
            {
                float alpha = 0.72f * ((float)(y - h2) / (H - h2) * 0.4f + 0.6f);
                BlendPixel(tex, x, y, new Color(0.07f, 0.11f, 0.22f, 1f), alpha);
            }
        }
        tex.Apply();
        return tex;
    }

    #endregion

    #region Helpers

    private static void ClearTexture(Texture2D tex)
    {
        Color[] clear = new Color[tex.width * tex.height];
        for (int i = 0; i < clear.Length; i++) clear[i] = Color.clear;
        tex.SetPixels(clear);
    }

    private static void BlendPixel(Texture2D tex, int x, int y, Color color, float alpha)
    {
        if (x < 0 || x >= tex.width || y < 0 || y >= tex.height || alpha <= 0f) return;
        alpha = Mathf.Clamp01(alpha);

        Color dst = tex.GetPixel(x, y);
        float srcA = color.a * alpha;
        float dstA = dst.a;
        float outA = srcA + dstA * (1f - srcA);

        if (outA <= 0.001f) return;

        float r = (color.r * srcA + dst.r * dstA * (1f - srcA)) / outA;
        float g = (color.g * srcA + dst.g * dstA * (1f - srcA)) / outA;
        float b = (color.b * srcA + dst.b * dstA * (1f - srcA)) / outA;

        tex.SetPixel(x, y, new Color(r, g, b, outA));
    }

    private static void DrawHexagon(Texture2D tex, float cx, float cy, float radius, Color borderCol, Color fillCol)
    {
        for (int y = (int)(cy - radius - 2); y <= (int)(cy + radius + 2); y++)
        {
            for (int x = (int)(cx - radius - 2); x <= (int)(cx + radius + 2); x++)
            {
                float dx = Mathf.Abs(x + 0.5f - cx);
                float dy = Mathf.Abs(y + 0.5f - cy);
                float d = Mathf.Max(dx * 0.866025f + dy * 0.5f, dy);
                if (d <= radius)
                {
                    float edge = radius - d;
                    float alpha = Mathf.Clamp01(edge + 0.5f);
                    Color col = (edge <= 3f) ? borderCol : fillCol;
                    BlendPixel(tex, x, y, col, alpha);
                }
            }
        }
    }

    private static void DrawCircleBadge(Texture2D tex, float cx, float cy, float radius, Color borderCol, Color fillCol)
    {
        for (int y = (int)(cy - radius - 2); y <= (int)(cy + radius + 2); y++)
        {
            for (int x = (int)(cx - radius - 2); x <= (int)(cx + radius + 2); x++)
            {
                float dist = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(cx, cy));
                if (dist <= radius)
                {
                    float edge = radius - dist;
                    float alpha = Mathf.Clamp01(edge + 0.5f);
                    Color col = (edge <= 3.5f) ? borderCol : fillCol;
                    BlendPixel(tex, x, y, col, alpha);
                }
            }
        }
    }

    private static void DrawArrow(Texture2D tex, float tipX, float tipY, float tailX, float tailY, float width, Color col)
    {
        float dx = tipX - tailX;
        float dy = tipY - tailY;
        float len = Mathf.Sqrt(dx * dx + dy * dy);
        if (len < 1f) return;

        for (int step = 0; step <= (int)len; step++)
        {
            float t = (float)step / len;
            float px = tailX + dx * t;
            float py = tailY + dy * t;

            for (int ox = -1; ox <= 1; ox++)
            {
                for (int oy = -1; oy <= 1; oy++)
                {
                    BlendPixel(tex, (int)px + ox, (int)py + oy, col, 0.9f);
                }
            }
        }

        for (int i = 0; i <= (int)width; i++)
        {
            BlendPixel(tex, (int)tipX, (int)tipY, col, 1f);
            BlendPixel(tex, (int)tipX - i, (int)tipY + i, col, 1f);
            BlendPixel(tex, (int)tipX + i, (int)tipY + i, col, 1f);
        }
    }

    #endregion
}
#endif
