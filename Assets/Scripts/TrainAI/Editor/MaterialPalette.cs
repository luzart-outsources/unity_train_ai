using System.IO;
using UnityEditor;
using UnityEngine;

namespace TrainAI.Editor
{
    // Centralized colored materials so prefabs/scenes share one palette.
    // Picks URP/Lit when available, falls back to Standard, so the project renders
    // correctly whether or not the URP package is active.
    public static class MaterialPalette
    {
        public static void EnsureAll(string folder)
        {
            EnsureFolder(folder);
            Player(folder);
            PlayerFace(folder);
            Npc(folder);
            NpcHat(folder);
            Arrow(folder);
            Ground(folder);
            Area(folder);
            Door(folder);
            FreeArea(folder);
            MiniMapBG(folder);
        }

        public static Material Player(string folder)     => GetOrCreate(folder, "M_Player",     new Color(0.20f, 0.55f, 0.95f));
        public static Material PlayerFace(string folder) => GetOrCreate(folder, "M_PlayerFace", new Color(1.00f, 0.85f, 0.60f));
        public static Material Npc(string folder)        => GetOrCreate(folder, "M_NPC",        new Color(0.80f, 0.35f, 0.30f));
        public static Material NpcHat(string folder)     => GetOrCreate(folder, "M_NPCHat",     new Color(0.20f, 0.20f, 0.25f));
        public static Material Arrow(string folder)      => GetOrCreate(folder, "M_Arrow",      new Color(1.00f, 0.85f, 0.10f), emissive: true);
        public static Material Ground(string folder)     => GetOrCreate(folder, "M_Ground",     new Color(0.35f, 0.55f, 0.30f));
        public static Material Area(string folder)       => GetOrCreate(folder, "M_Area",       new Color(0.35f, 0.70f, 0.80f, 0.85f));
        public static Material Door(string folder)       => GetOrCreate(folder, "M_Door",       new Color(0.50f, 0.30f, 0.15f));
        public static Material FreeArea(string folder)   => GetOrCreate(folder, "M_FreeArea",   new Color(0.85f, 0.85f, 0.40f, 0.30f), transparent: true);
        public static Material MiniMapBG(string folder)  => GetOrCreate(folder, "M_MinimapBG",  new Color(0.10f, 0.18f, 0.10f));

        static Material GetOrCreate(string folder, string name, Color color, bool emissive = false, bool transparent = false)
        {
            string path = $"{folder}/{name}.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(PickShader());
                AssetDatabase.CreateAsset(mat, path);
            }
            ApplyColor(mat, color, emissive, transparent);
            EditorUtility.SetDirty(mat);
            return mat;
        }

        static Shader PickShader()
        {
            return Shader.Find("Universal Render Pipeline/Lit")
                ?? Shader.Find("Standard")
                ?? Shader.Find("Diffuse");
        }

        static void ApplyColor(Material mat, Color color, bool emissive, bool transparent)
        {
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
            if (mat.HasProperty("_Color"))     mat.SetColor("_Color", color);

            if (transparent)
            {
                // URP/Lit surface type: 1 = transparent
                if (mat.HasProperty("_Surface")) mat.SetFloat("_Surface", 1f);
                mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            }

            if (emissive)
            {
                mat.EnableKeyword("_EMISSION");
                Color emit = color * 1.5f;
                if (mat.HasProperty("_EmissionColor")) mat.SetColor("_EmissionColor", emit);
                mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            }
        }

        static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path).Replace('\\', '/');
            string leaf = Path.GetFileName(path);
            if (!AssetDatabase.IsValidFolder(parent))
                AssetDatabase.CreateFolder(Path.GetDirectoryName(parent).Replace('\\', '/'),
                                           Path.GetFileName(parent));
            AssetDatabase.CreateFolder(parent, leaf);
        }
    }
}
