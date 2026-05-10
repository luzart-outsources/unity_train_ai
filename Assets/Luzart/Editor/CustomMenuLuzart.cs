#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class CustomMenuLuzart : EditorWindow
{
    static bool IsSceneInBuildSettings(string sceneName)
    {
        for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
        {
            string scenePath = SceneUtility.GetScenePathByBuildIndex(i);
            string sceneFileName = System.IO.Path.GetFileNameWithoutExtension(scenePath);
            if (sceneFileName == sceneName)
            {
                return true;
            }
        }
        return false;
    }
    static int GetBuildIndex(string sceneName)
    {
        for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
        {
            string scenePath = SceneUtility.GetScenePathByBuildIndex(i);
            string sceneFileName = System.IO.Path.GetFileNameWithoutExtension(scenePath);
            if (sceneFileName == sceneName)
            {
                return i;
            }
        }
        return -1;
    }

    // Thêm scene vào Build Settings
    static void AddSceneToBuildSettings(string sceneName)
    {
        // Lấy đường dẫn của scene
        string scenePath = "Assets/_GameLuzart/Scenes/" + sceneName + ".unity"; // Đường dẫn của scene trong thư mục Assets

        // Tạo một danh sách mới với tất cả các scene hiện tại trong Build Settings
        List<EditorBuildSettingsScene> scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);

        // Tạo một scene mới và đặt nó là enabled
        EditorBuildSettingsScene newScene = new EditorBuildSettingsScene(scenePath, true);
        scenes.Insert(0, newScene);

        // Cập nhật Build Settings với danh sách mới
        EditorBuildSettings.scenes = scenes.ToArray();

        Debug.Log("Scene " + sceneName + " đã được thêm vào Build Settings.");
    }

    [MenuItem("Luzart/Play")]
    public static void Play()
    {
        string scenePath = SceneUtility.GetScenePathByBuildIndex(0);
        EditorSceneManager.OpenScene(scenePath);
        EditorApplication.isPlaying = true;
    }
}
public static class DynamicMenuGenerator
{
    private const string MenuScriptPath = "Assets/_GameLuzart/Script/Utility/Editor/GeneratedDynamicMenu.cs";

    [MenuItem("Luzart/LuzartTool/Generate Scene Menus")]
    public static void GenerateSceneMenus()
    {
        var scenes = EditorBuildSettings.scenes;

        // Kiểm tra nếu không có scene nào trong Build Settings
        if (scenes.Length == 0)
        {
            Debug.LogWarning("Không có scene nào trong Build Settings.");
            return;
        }

        // Bắt đầu tạo nội dung script
        string scriptContent = "using UnityEditor;\nusing UnityEditor.SceneManagement;\n\n";
        scriptContent += "namespace Luzart\n{\n";
        scriptContent += "    public static class GeneratedDynamicMenu\n    {\n";

        for (int i = 0; i < scenes.Length; i++)
        {
            var scene = scenes[i];
            if (!scene.enabled) continue; // Bỏ qua scene không được tick

            string sceneName = Path.GetFileNameWithoutExtension(scene.path);
            scriptContent += $@"
        [MenuItem(""Luzart/_Scenes/{i:D2}.{sceneName}"")]
        public static void OpenScene_{i}()
        {{
            EditorSceneManager.OpenScene(@""{scene.path.Replace("\\", "/")}"");
        }}
";
        }

        scriptContent += "    }\n}\n";

        if (File.Exists(MenuScriptPath))
        {
            // Đọc nội dung file cũ và kiểm tra xem có cần thêm hoặc cập nhật gì không
            string existingContent = File.ReadAllText(MenuScriptPath);
            if (existingContent != scriptContent)
            {
                // Ghi lại nội dung mới nếu có sự thay đổi
                File.WriteAllText(MenuScriptPath, scriptContent);
                Debug.Log("File script đã được cập nhật.");
            }
            else
            {
                Debug.Log("File script đã có nội dung giống nhau, không cần cập nhật.");
            }
        }
        else
        {
            // Nếu chưa có file thì tạo mới
            Directory.CreateDirectory(Path.GetDirectoryName(MenuScriptPath) ?? string.Empty);
            File.WriteAllText(MenuScriptPath, scriptContent);
            Debug.Log("File script đã được tạo mới.");
        }

        // Import lại file script vừa tạo hoặc cập nhật
        AssetDatabase.Refresh();
    }
}

    #region Helper Classes and Methods

// Helper classes for the new features
public class FileChangeInfo
{
    public string FilePath { get; set; }
    public ChangeType ChangeType { get; set; }
    public string OldNamespace { get; set; }
    public string NewNamespace { get; set; }
    public string PreviewContent { get; set; }
}

public enum ChangeType
{
    Add,
    Remove,
    Replace
}

public class NamespaceTreeNode
{
    public string Name { get; set; }
    public NodeType Type { get; set; }
    public string FullPath { get; set; }
    public string Namespace { get; set; }
    public List<NamespaceTreeNode> Children { get; set; } = new List<NamespaceTreeNode>();

    public NamespaceTreeNode(string name, NodeType type)
    {
        Name = name;
        Type = type;
    }

    public string GetPath()
    {
        return FullPath ?? Name;
    }
}

public enum NodeType
{
    Folder,
    File
}
public class MissingScriptFinder : EditorWindow
{
    private List<string> missingScriptObjects = new List<string>();

    [MenuItem("Luzart/LuzartTool/Missing Script Finder")]
    public static void ShowWindow()
    {
        GetWindow<MissingScriptFinder>("Missing Script Finder");
    }

    private void OnGUI()
    {
        if (GUILayout.Button("Find Missing Scripts"))
        {
            FindMissingScripts();
        }
        if (GUILayout.Button("Find Missing Scripts In Current"))
        {
            FindMissingScriptInCurrent();
        }
        if (GUILayout.Button("Remove Missing Scripts In Current"))
        {
            RemoveMissingScriptInCurrent();
        }
        if (GUILayout.Button("🧹 Remove All Missing Scripts In Project"))
        {
            RemoveAllMissingScriptsInProject();
        }
        if (GUILayout.Button("Clean Nested Missing Prefabs In Project"))
        {
            CleanNestedMissingPrefabsInProject();
        }
        if(GUILayout.Button("Quick Clean"))
        {
            Remove();
        }

        GUILayout.Space(10);
        GUILayout.Label("GameObjects with Missing Scripts:", EditorStyles.boldLabel);
        foreach (var obj in missingScriptObjects)
        {
            GUILayout.Label(obj);
        }
    }

    // =====================================================================
    // FIND
    // =====================================================================
    private void FindMissingScripts()
    {
        missingScriptObjects.Clear();
        GameObject[] allObjects = Resources.FindObjectsOfTypeAll<GameObject>();
        FindMissing(allObjects);
        Debug.Log("Missing script search complete.");
    }

    private void FindMissingScriptInCurrent()
    {
        missingScriptObjects.Clear();
        var prefabStage = PrefabStageUtility.GetCurrentPrefabStage();
        if (prefabStage != null)
        {
            GameObject root = prefabStage.prefabContentsRoot;
            Transform[] allTransforms = root.GetComponentsInChildren<Transform>(true);
            var allObjects = allTransforms.Select(x => x.gameObject).ToArray();
            FindMissing(allObjects);
            Debug.Log($"🟢 Bạn đang làm việc trong Prefab: {prefabStage.name}");
        }
        else
        {
            var activeScene = EditorSceneManager.GetActiveScene();
            GameObject[] allObjects = activeScene.GetRootGameObjects();
            FindMissing(allObjects);
            Debug.Log($"🟢 Bạn đang làm việc trong Scene: {activeScene.name}");
        }
        Debug.Log("Missing script search complete.");
    }

    // =====================================================================
    // REMOVE MISSING IN CURRENT
    // =====================================================================
    private void Remove()
    {
        var objs = Resources.FindObjectsOfTypeAll<GameObject>();
        int count = objs.Sum(GameObjectUtility.RemoveMonoBehavioursWithMissingScript);
        foreach (var obj in objs)
        {
            EditorUtility.SetDirty(obj);
        }
        Debug.Log($"Removed {count} missing scripts");
    }

    private void RemoveMissingScriptInCurrent()
    {
        var prefabStage = PrefabStageUtility.GetCurrentPrefabStage();
        if (prefabStage != null)
        {
            GameObject root = prefabStage.prefabContentsRoot;
            Transform[] allTransforms = root.GetComponentsInChildren<Transform>(true);
            var allObjects = allTransforms.Select(x => x.gameObject).ToArray();
            RemoveMissing(allObjects);
            PrefabUtility.SaveAsPrefabAsset(root, prefabStage.assetPath);
            Debug.Log($"💾 Saved prefab: {prefabStage.assetPath}");
            Debug.Log($"🟢 Removed Missing Scripts in Prefab: {prefabStage.name}");
        }
        else
        {
            var activeScene = EditorSceneManager.GetActiveScene();
            GameObject[] allObjects = activeScene.GetRootGameObjects();
            RemoveMissing(allObjects);
            EditorSceneManager.MarkSceneDirty(activeScene);
            Debug.Log($"🟢 Removed Missing Scripts in Scene: {activeScene.name}");
        }
    }

    // =====================================================================
    // REMOVE ALL MISSING IN PROJECT
    // =====================================================================
    public static void RemoveAllMissingScriptsInProject()
    {
        if (!EditorUtility.DisplayDialog(
                "Confirm Remove Missing Scripts",
                "This will scan ALL prefabs in the project and remove any missing MonoBehaviours.\n\nThis action cannot be undone.\nProceed?",
                "Yes, Remove All", "Cancel"))
            return;

        string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab");
        int totalPrefabs = prefabGuids.Length;
        int totalRemoved = 0;

        try
        {
            for (int i = 0; i < totalPrefabs; i++)
            {
                string guid = prefabGuids[i];
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var prefabAsset = AssetDatabase.LoadAssetAtPath<GameObject>(path);

                // Bỏ qua model prefab (FBX, OBJ...)
                if (PrefabUtility.IsPartOfModelPrefab(prefabAsset))
                    continue;

                EditorUtility.DisplayProgressBar(
                    "Removing Missing Scripts",
                    $"{i + 1}/{totalPrefabs}\n{path}",
                    i / (float)totalPrefabs);

                GameObject prefabRoot = PrefabUtility.LoadPrefabContents(path);
                int removedCount = 0;

                foreach (var go in prefabRoot.GetComponentsInChildren<Transform>(true))
                {
                    removedCount += GameObjectUtility.RemoveMonoBehavioursWithMissingScript(go.gameObject);
                    if (removedCount > 0)
                        EditorUtility.SetDirty(go.gameObject);
                }

                if (removedCount > 0)
                {
                    PrefabUtility.SaveAsPrefabAsset(prefabRoot, path);
                    Debug.Log($"🧹 Removed {removedCount} missing script(s) in prefab: {path}",
                        AssetDatabase.LoadAssetAtPath<Object>(path));
                    totalRemoved += removedCount;
                }

                PrefabUtility.UnloadPrefabContents(prefabRoot);
            }

            Debug.Log($"✅ Finished removing missing scripts from {totalPrefabs} prefabs. Total removed: {totalRemoved}.");
        }
        finally
        {
            EditorUtility.ClearProgressBar();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
    }

    // =====================================================================
    // CLEAN OR RESCUE BROKEN PREFABS
    // =====================================================================
    public static void CleanNestedMissingPrefabsInProject()
    {
        if (!EditorUtility.DisplayDialog(
            "Remove Nested Missing Prefabs",
            "This will scan ALL prefabs in the project.\n" +
            "If a prefab contains broken or missing nested prefab instances, they will be deleted.\n\nProceed?",
            "Yes, Proceed", "Cancel"))
            return;

        string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab");
        int total = prefabGuids.Length;
        int cleanedCount = 0;
        int totalNestedDeleted = 0;

        try
        {
            for (int i = 0; i < total; i++)
            {
                string guid = prefabGuids[i];
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);

                EditorUtility.DisplayProgressBar(
                    "Cleaning Nested Missing Prefabs",
                    $"{i + 1}/{total}\n{path}",
                    i / (float)total);

                if (prefab == null)
                    continue;

                // Bỏ qua model prefab
                if (PrefabUtility.IsPartOfModelPrefab(prefab))
                    continue;

                GameObject prefabRoot = PrefabUtility.LoadPrefabContents(path);

                // Tìm tất cả các Transform trong prefab
                var allTransforms = prefabRoot.GetComponentsInChildren<Transform>(true);
                int removedNested = 0;

                foreach (var t in allTransforms)
                {
                    if (t == null) continue;
                    // Nếu đây là 1 prefab instance con
                    if (PrefabUtility.IsPartOfPrefabInstance(t.gameObject))
                    {
                        var asset = PrefabUtility.GetCorrespondingObjectFromSource(t.gameObject);
                        if (asset == null)
                        {
                            // Prefab con bị mất gốc → xóa
                            Debug.LogWarning($"🟥 Nested prefab missing in: {path} → {t.name}");
                            Object.DestroyImmediate(t.gameObject);
                            removedNested++;
                        }
                    }
                }

                if (removedNested > 0)
                {
                    PrefabUtility.SaveAsPrefabAsset(prefabRoot, path);
                    Debug.Log($"🧹 Cleaned {removedNested} missing nested prefabs in: {path}");
                    cleanedCount++;
                    totalNestedDeleted += removedNested;
                }

                PrefabUtility.UnloadPrefabContents(prefabRoot);
            }

            Debug.Log($"✅ Finished cleaning nested missing prefabs.\n" +
                      $"🧩 Prefabs modified: {cleanedCount}\n" +
                      $"🗑 Nested missing prefabs removed: {totalNestedDeleted}");
        }
        finally
        {
            EditorUtility.ClearProgressBar();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
    }

    private static void TryRescuePrefab(GameObject prefabRoot, string path, ref int rescued, ref int deleted)
    {
        TryDeletePrefab(path);

        deleted++;
    }

    private static void TryDeletePrefab(string path)
    {
        if (File.Exists(path))
        {
            AssetDatabase.DeleteAsset(path);
            Debug.Log($"🗑 Deleted broken prefab: {path}");
        }
    }

    private static int RemoveAllMissingInObject(GameObject root)
    {
        int total = 0;
        foreach (var go in root.GetComponentsInChildren<Transform>(true))
        {
            total += GameObjectUtility.RemoveMonoBehavioursWithMissingScript(go.gameObject);
        }
        return total;
    }

    // =====================================================================
    // HELPERS
    // =====================================================================
    private static bool HasMissingScripts(GameObject obj)
    {
        Component[] components = obj.GetComponents<Component>();
        foreach (var c in components)
        {
            if (c == null)
                return true;
        }
        return false;
    }

    private string GetFullPath(GameObject obj)
    {
        string path = "/" + obj.name;
        while (obj.transform.parent != null)
        {
            obj = obj.transform.parent.gameObject;
            path = "/" + obj.name + path;
        }
        return path;
    }

    private void FindMissing(GameObject[] allObjects)
    {
        foreach (var obj in allObjects)
        {
            if (obj.hideFlags == HideFlags.NotEditable || obj.hideFlags == HideFlags.HideAndDontSave)
                continue;
            if (PrefabUtility.GetPrefabInstanceStatus(obj) == PrefabInstanceStatus.Connected)
                continue;
            if (HasMissingScripts(obj))
                missingScriptObjects.Add(GetFullPath(obj));
        }
    }

    private void RemoveMissing(GameObject[] allObjects)
    {
        int totalRemoved = 0;
        foreach (var obj in allObjects)
        {
            if (obj.hideFlags == HideFlags.NotEditable || obj.hideFlags == HideFlags.HideAndDontSave)
                continue;
            if (PrefabUtility.GetPrefabInstanceStatus(obj) == PrefabInstanceStatus.Connected)
                continue;

            int removed = GameObjectUtility.RemoveMonoBehavioursWithMissingScript(obj);
            if (removed > 0)
            {
                totalRemoved += removed;
                EditorUtility.SetDirty(obj);
                Debug.Log($"🧹 Removed {removed} missing script(s) from {GetFullPath(obj)}", obj);
            }
        }
        Debug.Log($"✅ Total removed in current context: {totalRemoved}");
    }
}

#endregion
#endif