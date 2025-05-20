using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Net;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using static PlayerMove;

[SerializeField]
public class CustomData
{
    public string modelName;
    public int customNum;
}
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
    private TextMeshProUGUI textModelName, textModelNumName,saveT,nameTag;

    private void Start()
    {
        nameTag.text = DataBase.Instance.DataUser.stu_name;
        LoadAssetsByLabel();
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

                    if (i != 0)
                    {
                        customModel[i].SetActive(false);
                    }

                    List<GameObject> listTemp = new List<GameObject>();
                    Transform parent = customModel[i].transform;

                    for (int i2 = 0; i2 < parent.childCount; i2++)
                    {
                        GameObject child = parent.GetChild(i2).gameObject;
                        if (child != null && child.name != "Root")
                        {
                            child.SetActive(i2 == 0);
                            listTemp.Add(child);
                        }
                    }

                    listModelCh.Add(i, listTemp);

                    if (textModelName != null && textModelNumName != null)
                    {
                        textModelName.text = customName[index].ToString();
                        textModelNumName.text = indexNum.ToString();
                    }

                    i++;
                    Debug.Log("로드된 키(Key): " + location.PrimaryKey);
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"Error loading asset {location.PrimaryKey}: {e.Message}");
                }
            }
        }
        else
        {
            Debug.LogError($"에셋 로드 실패: {handle.OperationException?.Message}");
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

    public void CustomEnd() =>
                        GameManager.Instance.MoveMap(GameManager.Instance.userPos.map_name); // 뒤로가기버튼

    public void CustomSet()
    {
        string jsonData = "{\"modelName\": \""+ customName[index].ToString() + 
                         "\",\"modelNum\": "+ indexNum.ToString() + 
                         ",\"email\": \"" + DataBase.Instance.Data.email + "\"}";
        DataBase.Instance.SendMessageApi(jsonData, "CustomChange", (Success, request) => {
            Debug.Log(request);
            if(request == "1")
            {
                //성공
                //성공 시 이전 씬으로 이동
                saveT.text = "<color=#00FF00>저장 완료</color>";
            }
            else
            {
                saveT.text = "<color=#FF0000>저장 실패</color>";
                //실패
            }
            StartCoroutine(SaveTextChange());
        });
    }
    IEnumerator SaveTextChange()
    {
        yield return new WaitForSeconds(2);
        saveT.text = "";
    }
}
