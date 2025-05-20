#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System.Linq;

public class AddSceneMaterialsAndTexturesToAddressables
{
    [MenuItem("Tools/어드레서블/씬 메테리얼 + 텍스처 + 씬 등록")]
    public static void AddMaterialsAndTexturesToAddressables()
    {
        string sceneName = SceneManager.GetActiveScene().name;
        string scenePath = SceneManager.GetActiveScene().path;

        if (string.IsNullOrEmpty(sceneName) || string.IsNullOrEmpty(scenePath))
        {
            Debug.LogError("씬 이름 또는 경로를 가져올 수 없습니다.");
            return;
        }

        Renderer[] renderers = GameObject.FindObjectsOfType<Renderer>();
        HashSet<Material> materials = new HashSet<Material>();
        HashSet<Texture> textures = new HashSet<Texture>();

        foreach (Renderer renderer in renderers)
        {
            foreach (Material mat in renderer.sharedMaterials)
            {
                if (mat == null) continue;
                materials.Add(mat);

                Shader shader = mat.shader;
                int count = ShaderUtil.GetPropertyCount(shader);
                for (int i = 0; i < count; i++)
                {
                    if (ShaderUtil.GetPropertyType(shader, i) == ShaderUtil.ShaderPropertyType.TexEnv)
                    {
                        string propName = ShaderUtil.GetPropertyName(shader, i);
                        Texture tex = mat.GetTexture(propName);
                        if (tex != null)
                            textures.Add(tex);
                    }
                }
            }
        }

        AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
        if (settings == null)
        {
            Debug.LogError("Addressables 설정을 찾을 수 없습니다.");
            return;
        }

        // 씬 이름 레이블 등록
        if (!settings.GetLabels().Contains(sceneName))
            settings.AddLabel(sceneName);

        // AllMaterials 그룹 준비
        AddressableAssetGroup allGroup = settings.FindGroup("AllMaterials") ??
            settings.CreateGroup("AllMaterials", false, false, false, null, typeof(BundledAssetGroupSchema));

        // 메테리얼 추가
        foreach (Material mat in materials)
        {
            AddAssetToAddressables(mat, sceneName, settings, allGroup);
        }

        // 텍스처 추가
        foreach (Texture tex in textures)
        {
            AddAssetToAddressables(tex, sceneName, settings, allGroup);
        }

        // 현재 씬도 Addressables에 등록
        string sceneGuid = AssetDatabase.AssetPathToGUID(scenePath);
        if (!string.IsNullOrEmpty(sceneGuid))
        {
            AddressableAssetGroup sceneGroup = settings.FindGroup("Scene") ??
                settings.CreateGroup("Scene", false, false, false, null, typeof(BundledAssetGroupSchema));

            AddressableAssetEntry sceneEntry = settings.FindAssetEntry(sceneGuid);
            if (sceneEntry == null)
            {
                sceneEntry = settings.CreateOrMoveEntry(sceneGuid, sceneGroup);
                sceneEntry.address = $"Scenes/{sceneName}";
            }
            else if (sceneEntry.parentGroup != sceneGroup)
            {
                settings.MoveEntry(sceneEntry, sceneGroup);
            }

            // 레이블 추가 (씬 이름으로)
            sceneEntry.SetLabel(sceneName, true);
            sceneEntry.SetLabel("Scene", true);
        }

        AssetDatabase.SaveAssets();
        settings.SetDirty(AddressableAssetSettings.ModificationEvent.EntryMoved, null, true);

        Debug.Log($"씬 '{sceneName}'의 메테리얼 {materials.Count}개, 텍스처 {textures.Count}개, 씬 자체가 Addressables에 등록되었습니다.");
    }

    private static void AddAssetToAddressables(Object asset, string sceneName, AddressableAssetSettings settings, AddressableAssetGroup allGroup)
    {
        string path = AssetDatabase.GetAssetPath(asset);
        if (string.IsNullOrEmpty(path)) return;

        string guid = AssetDatabase.AssetPathToGUID(path);
        AddressableAssetEntry entry = settings.FindAssetEntry(guid);

        if (entry == null)
        {
            AddressableAssetGroup sceneGroup = settings.FindGroup(sceneName) ??
                settings.CreateGroup(sceneName, false, false, false, null, typeof(BundledAssetGroupSchema));

            entry = settings.CreateOrMoveEntry(guid, sceneGroup);
            entry.address = $"{sceneName}/{asset.name}";
        }
        else
        {
            if (entry.parentGroup != null && entry.parentGroup.Name != sceneName)
            {
                entry = settings.CreateOrMoveEntry(guid, allGroup);
            }
        }

        if (!entry.labels.Contains(sceneName))
        {
            entry.SetLabel(sceneName, true);
        }
    }
}
#endif