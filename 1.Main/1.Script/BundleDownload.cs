using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using TMPro;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.SceneManagement;
using UnityEngine.TextCore.Text;

public class BundleDownload : MonoBehaviour
{
    public string sceneToLoad; // Addressables에서 불러올 씬 이름
    public string label = "Scene"; // 다운로드할 에셋 레이블

    private AsyncOperationHandle<SceneInstance> handle;

    public void Down()
    {
        // 앱 시작 시 또는 버튼 눌러서 다운로드 시작
        StartCoroutine(DownloadAndPrepareScene());
    }

    private System.Collections.IEnumerator DownloadAndPrepareScene()
    {
        Debug.Log("씬 번들 다운로드 시작...");

        var downloadHandle = Addressables.DownloadDependenciesAsync(label);

        yield return downloadHandle;

        if (downloadHandle.Status == AsyncOperationStatus.Succeeded)
        {
            Debug.Log("다운로드 완료! 이제 StartLoading으로 씬 로드 가능.");
            //StartLoading();
        }
        else
        {
            Debug.LogError("에셋 다운로드 실패!");
        }
    }





    public void StartLoading()
    {
        StartCoroutine(LoadSceneAsync(sceneToLoad));
    }

    private System.Collections.IEnumerator LoadSceneAsync(string sceneName)
    {
        handle = Addressables.LoadSceneAsync(sceneName, LoadSceneMode.Single);

        while (!handle.IsDone)
        {
            float percent = handle.PercentComplete * 100f;
            //loadingText.text = $"Loading... {percent:F0}%";
            yield return null; // 다음 프레임까지 기다림
        }

        if (handle.Status == AsyncOperationStatus.Succeeded)
        {
            //loadingText.text = "Load Complete!";
            Debug.Log("씬 로드 완료!");
        }
        else
        {
            //loadingText.text = "Failed to load scene!";
            Debug.LogError("씬 로드 실패");
        }
    }
}
