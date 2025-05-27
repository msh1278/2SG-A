using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Net;
using System.Threading.Tasks;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using static PlayerMove;

public class CustomManager : MonoBehaviour
{
    [SerializeField]
    private string[] customName;
    [SerializeField]
    private GameObject[] customModel;
    [SerializeField]
    private Transform p;
    private int index = 0;
    private int indexNum = 0;
    private Dictionary<int, List<GameObject>> listModelCh = new Dictionary<int, List<GameObject>>();

    [SerializeField]
    private TextMeshProUGUI textModelName, textModelNumName, saveT, nameTag;

    private void Start()
    {
        nameTag.text = DataBase.Instance.DataUser.stu_name;
        LoadSavedCustomData();
        LoadAssetsByLabel();
    }

    private void LoadSavedCustomData()
    {
        if (DataBase.Instance != null && DataBase.Instance.customData != null)
        {
            Debug.Log($"Loading saved custom data: {JsonUtility.ToJson(DataBase.Instance.customData)}");
            // 저장된 모델 이름으로 인덱스 찾기
            for (int i = 0; i < customName?.Length; i++)
            {
                if (customName[i] == DataBase.Instance.customData.modelName)
                {
                    index = i;
                    indexNum = DataBase.Instance.customData.customNum;
                    Debug.Log($"Found saved model at index {index} with custom number {indexNum}");
                    break;
                }
            }
        }
        else
        {
            // PlayerPrefs에서 저장된 데이터 확인
            string savedCustomData = PlayerPrefs.GetString("CustomData", "");
            if (!string.IsNullOrEmpty(savedCustomData))
            {
                try
                {
                    var savedData = JsonUtility.FromJson<DataBase.CustomData>(savedCustomData);
                    Debug.Log($"Loaded custom data from PlayerPrefs: {savedCustomData}");
                    
                    // 저장된 모델 이름으로 인덱스 찾기
                    for (int i = 0; i < customName?.Length; i++)
                    {
                        if (customName[i] == savedData.modelName)
                        {
                            index = i;
                            indexNum = savedData.customNum;
                            Debug.Log($"Found saved model at index {index} with custom number {indexNum}");
                            break;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Error loading saved custom data: {ex.Message}");
                }
            }
        }
    }

    void LoadAssetsByLabel()
    {
        Addressables.LoadResourceLocationsAsync("PlayerModel").Completed += OnLocationsLoaded;
    }

    async void OnLocationsLoaded(AsyncOperationHandle<IList<UnityEngine.ResourceManagement.ResourceLocations.IResourceLocation>> handle)
    {
        if (handle.Status == AsyncOperationStatus.Succeeded)
        {
            var locations = handle.Result;
            Debug.Log($"로드된 에셋의 총 개수: {locations.Count}");

            customName = new string[locations.Count];
            customModel = new GameObject[locations.Count];
            int i = 0;
            foreach (var location in locations)
            {
                try
                {
                    customName[i] = location.PrimaryKey;
                    var loadOperation = Addressables.InstantiateAsync(customName[i]);
                    await loadOperation.Task;
                    
                    if (loadOperation.Status != AsyncOperationStatus.Succeeded)
                    {
                        Debug.LogError($"Failed to load asset: {customName[i]}, Error: {loadOperation.OperationException?.Message}");
                        continue;
                    }

                    customModel[i] = loadOperation.Result;
                    if (customModel[i] == null)
                    {
                        Debug.LogError($"Instantiated object is null for asset: {customName[i]}");
                        continue;
                    }

                    if (p == null)
                    {
                        Debug.LogError("Parent transform 'p' is not assigned!");
                        continue;
                    }

                    customModel[i].transform.parent = p;
                    customModel[i].transform.localPosition = Vector3.zero;
                    customModel[i].transform.localRotation = Quaternion.identity;

                    // 현재 인덱스가 저장된 모델과 일치하는 경우에만 활성화
                    customModel[i].SetActive(i == index);

                    List<GameObject> listTemp = new List<GameObject>();
                    Transform parent = customModel[i].transform;

                    for (int i2 = 0; i2 < parent.childCount; i2++)
                    {
                        GameObject child = parent.GetChild(i2).gameObject;
                        if (child != null && child.name != "Root")
                        {
                            // 현재 인덱스가 저장된 모델과 일치하는 경우에만 커스텀 번호 적용
                            bool shouldActivate = (i == index && i2 == indexNum);
                            child.SetActive(shouldActivate);
                            listTemp.Add(child);
                        }
                    }

                    listModelCh.Add(i, listTemp);

                    if (textModelName != null && textModelNumName != null && i == index)
                    {
                        textModelName.text = customName[index].ToString();
                        textModelNumName.text = indexNum.ToString();
                    }

                    i++;
                    Debug.Log($"로드된 키(Key): {location.PrimaryKey}");
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"Error loading asset {location.PrimaryKey}: {e.Message}");
                }
            }

            // 모든 모델 로드 후 저장된 데이터 적용
            ApplySavedCustomData();
        }
        else
        {
            Debug.LogError($"에셋 로드 실패: {handle.OperationException?.Message}");
        }
    }

    private void ApplySavedCustomData()
    {
        if (DataBase.Instance != null && DataBase.Instance.customData != null)
        {
            // 저장된 모델 이름으로 인덱스 찾기
            for (int i = 0; i < customName?.Length; i++)
            {
                if (customName[i] == DataBase.Instance.customData.modelName)
                {
                    // 이전 모델 비활성화
                    if (customModel[index] != null)
                    {
                        customModel[index].SetActive(false);
                    }

                    // 새 모델 활성화
                    index = i;
                    indexNum = DataBase.Instance.customData.customNum;
                    
                    if (customModel[index] != null)
                    {
                        customModel[index].SetActive(true);
                        
                        // 커스텀 모델 번호 적용
                        for (int j = 0; j < listModelCh[index].Count; j++)
                        {
                            listModelCh[index][j].SetActive(j == indexNum);
                        }
                    }

                    if (textModelName != null && textModelNumName != null)
                    {
                        textModelName.text = customName[index].ToString();
                        textModelNumName.text = indexNum.ToString();
                    }

                    Debug.Log($"Applied saved custom data - Model: {customName[index]}, Custom Number: {indexNum}");
                    break;
                }
            }
        }
    }

    public void SaveCustomData()
    {
        if (DataBase.Instance != null)
        {
            DataBase.Instance.customData = new DataBase.CustomData
            {
                email = DataBase.Instance.Data.email,
                modelName = customName[index],
                customNum = indexNum
            };

            // PlayerPrefs에 저장
            string jsonData = JsonUtility.ToJson(DataBase.Instance.customData);
            PlayerPrefs.SetString("CustomData", jsonData);
            PlayerPrefs.Save();

            // 서버에 저장
            DataBase.Instance.SendMessageApi(jsonData, "CustomSet", (Success, request) =>
            {
                if (Success)
                {
                    Debug.Log("Custom data saved successfully");
                    saveT.text = "저장 완료!";
                }
                else
                {
                    Debug.LogError($"Failed to save custom data: {request}");
                    saveT.text = "저장 실패!";
                }
            });
        }
    }

    public void ArrowNum(int i)
    {
        listModelCh[index][indexNum].SetActive(false);
        indexNum += i; 
        if(indexNum < 0)
        {
            indexNum = listModelCh[index].Count-1;// 초기화
        }

        if (indexNum >= listModelCh[index].Count)
        {
            indexNum = 0;// 초기화
        }
        textModelNumName.text = indexNum.ToString();

        listModelCh[index][indexNum].SetActive(true);
    }
    public void Arrow(int i)
    {
        indexNum = 0;
        ArrowNum(0);// 초기화

        for (int t = 1;t < listModelCh[index].Count; t++)
        {
            listModelCh[index][t].SetActive(false);
        }
        customModel[index].SetActive(false);

        index += i;
        if (index < 0)
        {
            index = customModel.Length - 1;// 초기화
        }

        if (index >= customModel.Length)
        {
            index = 0;// 초기화
        }

        textModelName.text = customName[index].ToString();

        customModel[index].SetActive(true);
    }

    public void CustomEnd()
    {
        // 저장된 데이터 확인
        string savedData = PlayerPrefs.GetString("CustomData", "");
        if (!string.IsNullOrEmpty(savedData))
        {
            Debug.Log($"Current saved custom data: {savedData}");
        }
        else
        {
            Debug.LogWarning("No saved custom data found!");
        }

        // 이전 씬으로 돌아가기
        if (GameManager.Instance != null && GameManager.Instance.userPos != null)
        {
            string previousScene = GameManager.Instance.userPos.map_name;
            if (!string.IsNullOrEmpty(previousScene))
            {
                Debug.Log($"Returning to previous scene: {previousScene}");
                GameManager.Instance.MoveMap(previousScene);
            }
            else
            {
                Debug.LogWarning("Previous scene name is empty, using default scene");
                GameManager.Instance.MoveMap("Main");
            }
        }
        else
        {
            Debug.LogError("GameManager.Instance or userPos is null!");
            GameManager.Instance.MoveMap("Main");
        }
    }

    public void CustomSet()
    {
        if (DataBase.Instance == null)
        {
            Debug.LogError("DataBase.Instance is null!");
            return;
        }

        // 현재 선택된 커스텀 데이터 생성
        var customData = new DataBase.CustomData
        {
            email = DataBase.Instance.Data.email,
            modelName = customName[index],
            customNum = indexNum
        };

        // 먼저 로컬에 저장
        string jsonData = JsonUtility.ToJson(customData);
        PlayerPrefs.SetString("CustomData", jsonData);
        PlayerPrefs.Save();
        Debug.Log($"Saved custom data to PlayerPrefs: {jsonData}");

        // DataBase.Instance에도 즉시 업데이트
        DataBase.Instance.customData = customData;
        Debug.Log($"Updated DataBase.Instance.customData: {jsonData}");

        // 서버에 저장
        DataBase.Instance.SendMessageApi(jsonData, "CustomChange", (Success, request) => {
            Debug.Log($"Custom save response: {request}");
            try {
                var response = JsonUtility.FromJson<DataBase.CustomResponse>(request);
                if (response != null && response.success)
                {
                    // 서버 응답이 성공이면 현재 데이터 유지
                    if (response.data != null && !string.IsNullOrEmpty(response.data.modelName))
                    {
                        // 서버에서 받은 데이터가 유효한 경우에만 업데이트
                        DataBase.Instance.customData = response.data;
                        string savedData = JsonUtility.ToJson(response.data);
                        PlayerPrefs.SetString("CustomData", savedData);
                        PlayerPrefs.Save();
                        Debug.Log($"Updated custom data from server response: {savedData}");
                    }
                    else
                    {
                        // 서버 응답이 유효하지 않으면 현재 데이터 유지
                        Debug.LogWarning("Server response data is invalid, keeping current data");
                    }
                    saveT.text = "<color=#00FF00>저장 완료</color>";

                    // 저장 후 바로 씬 전환
                    StartCoroutine(SaveAndReturn());
                }
                else
                {
                    Debug.LogError($"Server response indicates failure: {request}");
                    saveT.text = "<color=#FF0000>저장 실패</color>";
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Error processing custom save response: {ex.Message}");
                // 에러 발생 시에도 현재 데이터 유지
                Debug.Log("Keeping current custom data due to error");
                saveT.text = "<color=#FF0000>저장 실패</color>";
            }
        });
    }

    private IEnumerator SaveAndReturn()
    {
        // 저장된 데이터 확인
        string savedData = PlayerPrefs.GetString("CustomData", "");
        if (!string.IsNullOrEmpty(savedData))
        {
            Debug.Log($"Current saved data before scene transition: {savedData}");
        }
        else
        {
            Debug.LogWarning("No saved data found before scene transition!");
        }

        // 저장 완료 메시지를 잠시 표시
        yield return new WaitForSeconds(1f);
        
        // 이전 씬으로 돌아가기
        if (GameManager.Instance != null && GameManager.Instance.userPos != null)
        {
            string previousScene = GameManager.Instance.userPos.map_name;
            if (!string.IsNullOrEmpty(previousScene))
            {
                Debug.Log($"Returning to previous scene: {previousScene}");
                GameManager.Instance.MoveMap(previousScene);
            }
            else
            {
                Debug.LogWarning("Previous scene name is empty, using default scene");
                GameManager.Instance.MoveMap("Main");
            }
        }
        else
        {
            Debug.LogError("GameManager.Instance or userPos is null!");
            GameManager.Instance.MoveMap("Main");
        }
    }
}
