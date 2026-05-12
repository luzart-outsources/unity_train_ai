using System.IO;
using TrainAI.Presentation;
using TrainAI.SO.Base;
using UnityEditor;
using UnityEngine;

namespace TrainAI.Editor
{
    public static class PrefabBuilder
    {
        const string PrefabFolder = "Assets/Prefabs/TrainAI";
        const string MaterialFolder = "Assets/_Data/Materials";

        [MenuItem("Tools/Build Game/4. Build Prefabs", false, 104)]
        public static void BuildAll()
        {
            EnsureFolder(PrefabFolder);
            EnsureFolder(MaterialFolder);
            MaterialPalette.EnsureAll(MaterialFolder);
            BuildPlayer();
            BuildNPC();
            BuildQuestArrow();
            BuildInteractable();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[PrefabBuilder] prefabs created at " + PrefabFolder);
        }

        static void BuildPlayer()
        {
            string path = $"{PrefabFolder}/Player.prefab";
            var go = new GameObject("Player");
            go.tag = "Player";
            var cc = go.AddComponent<CharacterController>();
            cc.height = 1.8f; cc.radius = 0.3f; cc.center = new Vector3(0, 0.9f, 0);

            // Visual capsule as child, centered on CC so feet sit at root Y=0 (not buried).
            var visual = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            visual.name = "Visual";
            visual.transform.SetParent(go.transform, false);
            visual.transform.localPosition = new Vector3(0, 0.9f, 0);
            visual.transform.localScale = new Vector3(0.6f, 0.9f, 0.6f);
            if (visual.TryGetComponent<Collider>(out var c)) Object.DestroyImmediate(c);
            ApplyMat(visual, MaterialPalette.Player(MaterialFolder));

            // Small face indicator (eyes/forehead) so player orientation is obvious.
            var face = GameObject.CreatePrimitive(PrimitiveType.Cube);
            face.name = "Face";
            face.transform.SetParent(visual.transform, false);
            face.transform.localPosition = new Vector3(0, 0.55f, 0.45f);
            face.transform.localScale = new Vector3(0.4f, 0.2f, 0.1f);
            if (face.TryGetComponent<Collider>(out var fc)) Object.DestroyImmediate(fc);
            ApplyMat(face, MaterialPalette.PlayerFace(MaterialFolder));

            go.AddComponent<PlayerController>();
            go.AddComponent<PlayerInteractor>();

            var triggerGo = new GameObject("InteractTrigger");
            triggerGo.transform.SetParent(go.transform, false);
            triggerGo.transform.localPosition = new Vector3(0, 0.9f, 0);
            var sc = triggerGo.AddComponent<SphereCollider>();
            sc.radius = 2f; sc.isTrigger = true;

            PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
        }

        static void BuildNPC()
        {
            string path = $"{PrefabFolder}/NPC.prefab";
            var go = new GameObject("NPC");
            go.tag = "NPC";

            // Visual capsule child, mesh-feet at root Y=0.
            var visual = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            visual.name = "Visual";
            visual.transform.SetParent(go.transform, false);
            visual.transform.localPosition = new Vector3(0, 0.9f, 0);
            visual.transform.localScale = new Vector3(0.6f, 0.9f, 0.6f);
            ApplyMat(visual, MaterialPalette.Npc(MaterialFolder));

            // Replace primitive collider with capsule on root sized to match.
            if (visual.TryGetComponent<Collider>(out var oldCol)) Object.DestroyImmediate(oldCol);
            var cap = go.AddComponent<CapsuleCollider>();
            cap.height = 1.8f; cap.radius = 0.3f; cap.center = new Vector3(0, 0.9f, 0);
            cap.isTrigger = false;

            // Hat to differentiate NPC silhouette.
            var hat = GameObject.CreatePrimitive(PrimitiveType.Cube);
            hat.name = "Hat";
            hat.transform.SetParent(visual.transform, false);
            hat.transform.localPosition = new Vector3(0, 0.65f, 0);
            hat.transform.localScale = new Vector3(1.1f, 0.25f, 1.1f);
            if (hat.TryGetComponent<Collider>(out var hc)) Object.DestroyImmediate(hc);
            ApplyMat(hat, MaterialPalette.NpcHat(MaterialFolder));

            go.AddComponent<NpcView>();
            PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
        }

        static void BuildQuestArrow()
        {
            string path = $"{PrefabFolder}/QuestArrow.prefab";
            var root = new GameObject("QuestArrow");

            // Arrow holder sits above player's head by default.
            var holder = new GameObject("ArrowAnchor");
            holder.transform.SetParent(root.transform, false);
            holder.transform.localPosition = new Vector3(0, 2.5f, 0);

            // Tall arrow shaped from cube + tip for visibility.
            var shaft = GameObject.CreatePrimitive(PrimitiveType.Cube);
            shaft.name = "Shaft";
            shaft.transform.SetParent(holder.transform, false);
            shaft.transform.localScale = new Vector3(0.25f, 0.15f, 0.9f);
            shaft.transform.localPosition = new Vector3(0, 0, 0);
            if (shaft.TryGetComponent<Collider>(out var sc)) Object.DestroyImmediate(sc);
            ApplyMat(shaft, MaterialPalette.Arrow(MaterialFolder));

            var tip = GameObject.CreatePrimitive(PrimitiveType.Cube);
            tip.name = "Tip";
            tip.transform.SetParent(holder.transform, false);
            tip.transform.localScale = new Vector3(0.55f, 0.3f, 0.4f);
            tip.transform.localPosition = new Vector3(0, 0, 0.65f);
            tip.transform.localRotation = Quaternion.Euler(0, 45, 0);
            if (tip.TryGetComponent<Collider>(out var tc)) Object.DestroyImmediate(tc);
            ApplyMat(tip, MaterialPalette.Arrow(MaterialFolder));

            var hud = root.AddComponent<QuestArrowHUD>();
            var so = new SerializedObject(hud);
            var arrowProp = so.FindProperty("arrow");
            if (arrowProp != null)
            {
                arrowProp.objectReferenceValue = holder.transform;
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
        }

        static void BuildInteractable()
        {
            string path = $"{PrefabFolder}/Interactable.prefab";
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = "Interactable";
            go.tag = "Interactable";
            var col = go.GetComponent<BoxCollider>();
            col.isTrigger = true;
            col.size = new Vector3(2f, 2f, 2f);
            go.AddComponent<InteractableMarker>();
            ApplyMat(go, MaterialPalette.Area(MaterialFolder));
            PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
        }

        static void ApplyMat(GameObject go, Material mat)
        {
            if (mat == null) return;
            var rend = go.GetComponent<Renderer>();
            if (rend != null) rend.sharedMaterial = mat;
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
