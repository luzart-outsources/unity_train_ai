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

        [MenuItem("Tools/Build Game/4. Build Prefabs", false, 104)]
        public static void BuildAll()
        {
            EnsureFolder(PrefabFolder);
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
            var go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            go.name = "Player";
            go.tag = "Player";
            if (go.TryGetComponent<Collider>(out var c)) Object.DestroyImmediate(c);
            var cc = go.AddComponent<CharacterController>();
            cc.height = 1.8f; cc.radius = 0.3f; cc.center = new Vector3(0, 0.9f, 0);
            go.AddComponent<PlayerController>();
            go.AddComponent<PlayerInteractor>();
            var triggerGo = new GameObject("InteractTrigger");
            triggerGo.transform.SetParent(go.transform, false);
            var sc = triggerGo.AddComponent<SphereCollider>();
            sc.radius = 2f; sc.isTrigger = true;
            PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
        }

        static void BuildNPC()
        {
            string path = $"{PrefabFolder}/NPC.prefab";
            var go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            go.name = "NPC";
            go.tag = "NPC";
            go.AddComponent<NpcView>();
            PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
        }

        static void BuildQuestArrow()
        {
            string path = $"{PrefabFolder}/QuestArrow.prefab";
            var root = new GameObject("QuestArrow");
            var visual = GameObject.CreatePrimitive(PrimitiveType.Cube);
            visual.name = "ArrowMesh";
            visual.transform.SetParent(root.transform, false);
            visual.transform.localScale = new Vector3(0.2f, 0.05f, 0.5f);
            visual.transform.localPosition = new Vector3(0, 0.05f, 0.4f);
            if (visual.TryGetComponent<Collider>(out var col)) Object.DestroyImmediate(col);
            var hud = root.AddComponent<QuestArrowHUD>();
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
            PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
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
