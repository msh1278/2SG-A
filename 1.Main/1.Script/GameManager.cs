using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceLocations;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.SceneManagement;
using TMPro;
using Agora_RTC_Plugin.API_Example.Examples.Basic.JoinChannelAudio;
using Agora.Rtc;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine.Rendering;
using System.Runtime.CompilerServices;

[System.Serializable]
public class UserPos
{
    public string map_name;
    public float posPlayerX;
    public float posPlayerY;
    public float posPlayerZ;
}
public class GameManager : MonoBehaviourPunCallbacks
{
    public JoinChannelAudio voice {  get; set; }
    [SerializeField]
    private Transform listBtnP;

    [SerializeField]
    private GameObject listBtn;
    [SerializeField]
    private GameObject listPanel;
    public UserPos userPos { get; set; }


    public int voiceSize { get; set; } = 100;
    public bool mute { get; set; } = true;


    [SerializeField]
    private GameObject loading;
    [SerializeField]
    private Slider loadingSlider;
    [SerializeField]
    private TextMeshProUGUI loadingTextMeshProUGUI;

    private void Awake()
    {
        SoundSet();

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        //네트워크 속도
        PhotonNetwork.SendRate = 60;
        PhotonNetwork.SerializationRate = 30;

        Singleton();

        Connect(); //서버 연결 시작
    }
    private void SoundSet()
    {
        
        if (PlayerPrefs.HasKey("VoiceSize"))  // 음량 설정확인
        {
            voiceSize = PlayerPrefs.GetInt("VoiceSize"); // 음량 설정확인
        }
        else
        {
            PlayerPrefs.SetInt("VoiceSize", 100); // 음량 설정이 없으면 기본 값
        }

        if (PlayerPrefs.HasKey("Mute"))  // 음소거 설정확인
        {
            mute = PlayerPrefs.GetInt("Mute") == 1 ? true : false;
            voiceSize = PlayerPrefs.GetInt("VoiceSize"); // 음량 설정확인
        }
        else
        {
            PlayerPrefs.SetInt("Mute", 1);
        }
    }
    private static GameManager instance = null;
    private bool roomIn = false;
    public static GameManager Instance // 싱글톤 패턴
    {
        get
        {
            if (null == instance)
            {
                return null;
            }
            return instance;
        }
    }

    private void Singleton()
    {
        if (instance == null)
        {
            instance = this;

            DontDestroyOnLoad(this.gameObject);

        }
        else
        {
            if (instance == this)
            {
                Destroy(this.gameObject);
            }
        }
    }


    public void Connect()
    {
        PhotonNetwork.ConnectUsingSettings();

    }

    private bool connectC = false;
    public override void OnConnectedToMaster()
    {
        connectC = true;
        //여기서는 호출 하면 안됨
        //PhotonNetwork.LocalPlayer.NickName = DataBase.Instance.DataUser.stu_name;
        //PhotonNetwork.JoinOrCreateRoom(SceneManager.GetActiveScene().name, new RoomOptions { MaxPlayers = 20 }, null);
        //방 생성 시 (방이름 < 방 이름이어야 함)
    }

    public override void OnJoinedRoom()
    {
        roomIn = true;
        //방 참가 시
        Spawn();
    }

    public void Spawn()
    {
        PhotonNetwork.LocalPlayer.NickName = DataBase.Instance.DataUser.stu_name;
        if (GameManager.Instance.userPos.map_name == SceneManager.GetActiveScene().name)
        {
            PhotonNetwork.Instantiate("Player", new Vector3(GameManager.Instance.userPos.posPlayerX, GameManager.Instance.userPos.posPlayerY, GameManager.Instance.userPos.posPlayerZ), Quaternion.identity);
        }
        else
        {
            Transform set = GameObject.Find("Start_Point").transform;
            PhotonNetwork.Instantiate("Player", set.position, set.rotation);
        }
        //캐릭터 생성
    }
    public void LeaveRoom()
    {
        PhotonNetwork.LeaveRoom();
    }

    // 방 나가기 시 호출되는 부분
    public override void OnLeftRoom()
    {
        roomIn = false;
        Debug.Log("방을 나갔습니다.");
    }

    public void MoveMap(string sceneName)
    {
        if(roomIn)
            LeaveRoom();

        if (sceneName == "Custom" || sceneName == "SignUp")
            StartCoroutine(LoadSceneAsync(sceneName));
        else
            DownLoad(sceneName,true);
    }
    IEnumerator LoadSceneAsync(string sceneName)
    {
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName);


        // 씬이 완전히 로드될 때까지 대기
        while (!asyncLoad.isDone)
        {
            Debug.Log($"로드 중... {asyncLoad.progress * 100}%");
            yield return null;
        }

        Debug.Log("씬 로드 완료!");

        //Connect();
    }

    public void DownLoad(string label,bool nextScene)
    {
        // 씬 로드나 버튼 클릭시 다운로드 시작
        StartCoroutine(CheckDownloadSizeAndDownload(label, nextScene));
    }

    private System.Collections.IEnumerator CheckDownloadSizeAndDownload(string label, bool nextScene)
    {
        Debug.Log("다운로드 크기 확인 중...");

        var sizeHandle = Addressables.GetDownloadSizeAsync(label);
        yield return sizeHandle;

        if (sizeHandle.Status == AsyncOperationStatus.Succeeded)
        {
            long downloadSize = sizeHandle.Result;

            if (downloadSize > 0)
            {
                Debug.Log($"추가 다운로드 필요. 크기: {downloadSize} bytes");
                yield return StartCoroutine(DownloadAndPrepareScene(label, nextScene));
            }
            else
            {
                Debug.Log("이미 다운로드되어 있습니다. 바로 씬 로드 시작.");
                if(nextScene)
                    StartLoading(label);
            }
        }
        else
        {
            Debug.LogError("추가 다운로드 크기 확인 실패!");
        }
    }

    private System.Collections.IEnumerator DownloadAndPrepareScene(string label, bool nextScene)
    {
        Debug.Log("Addressables 초기화 중...");
        var initHandle = Addressables.InitializeAsync();
        yield return initHandle;

        if (initHandle.Status != AsyncOperationStatus.Succeeded)
        {
            Debug.LogError("Addressables 초기화 실패!");
            yield break;
        }

        Debug.Log("씬 데이터 다운로드 시작...");

        loading.SetActive(true);

        var downloadHandle = Addressables.DownloadDependenciesAsync(label);

        while (!downloadHandle.IsDone)
        {
            //Debug.Log($"다운로드 중... {downloadHandle.PercentComplete * 100f}%");
            loadingSlider.value = downloadHandle.PercentComplete * 100f;//�߰�
            loadingTextMeshProUGUI.text = (downloadHandle.PercentComplete * 100f) + "%";//�߰�
            yield return null;
        }
        loading.SetActive(false);

        if (downloadHandle.Status == AsyncOperationStatus.Succeeded)
        {
            Debug.Log("다운로드 완료! 이제 StartLoading으로 씬 로드 시작.");
            if (nextScene)
                StartLoading(label); // 씬 이름이 label로 들어옴
        }
        else
        {
            Debug.LogError("추가 다운로드 실패!");
        }
    }

    public void StartLoading(string sceneName)
    {
        StartCoroutine(LoadSceneAsync2(sceneName));
    }

    private AsyncOperationHandle<SceneInstance> handle;

    private System.Collections.IEnumerator LoadSceneAsync2(string sceneName)
    {

        loading.SetActive(true);

        handle = Addressables.LoadSceneAsync(sceneName, LoadSceneMode.Single);

        while (!handle.IsDone)
        {
            float percent = handle.PercentComplete * 100f;
            // loadingText.text = $"Loading... {percent:F0}%";
            loadingSlider.value = handle.PercentComplete * 100f;//�߰�
            loadingTextMeshProUGUI.text = (handle.PercentComplete * 100f) + "%";//�߰�

            yield return null; // ���� �����ӱ��� ��ٸ�
        }

        if (handle.Status == AsyncOperationStatus.Succeeded)
        {
            // loadingText.text = "Load Complete!";
            Debug.Log("씬 로드 완료!");
            PhotonNetwork.JoinOrCreateRoom(SceneManager.GetActiveScene().name, new RoomOptions { MaxPlayers = 20 }, null);
        }
        else
        {
            // loadingText.text = "Failed to load scene!";
            Debug.LogError("씬 로드 실패");
        }
        loading.SetActive(false);
    }
    public override void OnDisconnected(DisconnectCause cause)
    {
        StartCoroutine(ReLoad());
    }
    public IEnumerator ReLoad()
    {
        if(connectC)
        {
            connectC = false;
            while (!connectC)
            {
                Connect();
                yield return new WaitForSeconds(5);
            }
            MoveMap(SceneManager.GetActiveScene().name);
        }
       
    }

    //===============================================================================

    private string targetLabel = "Scene";

    async void Start()
    {
        List<string> sceneNames = await GetSceneNamesWithLabel(targetLabel);
        
        foreach (string name in sceneNames)
        {
            GameObject listBtnTemp = GameObject.Instantiate(listBtn, listBtnP);
            listBtnTemp.SetActive(true);
            listBtnTemp.GetComponentInChildren<TextMeshProUGUI>().text = name;
            listBtnTemp.GetComponent<Button>().onClick.AddListener(() =>
            {
                MoveMap(name);
                listPanel.SetActive(false);
            });
        }
        
    }
    public void MapListOn() => listPanel.SetActive(true);

    public async Task<List<string>> GetSceneNamesWithLabel(string label)
    {
        List<string> sceneNames = new List<string>();

        AsyncOperationHandle<IList<IResourceLocation>> handle = Addressables.LoadResourceLocationsAsync(label);
        await handle.Task;

        if (handle.Status == UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationStatus.Succeeded)
        {
            foreach (var location in handle.Result)
            {
                // location.ResourceType == typeof(SceneInstance) Ȯ���ص� ��
                sceneNames.Add(location.PrimaryKey); // �Ǵ� location.InternalId
            }
        }
        else
        {
            Debug.LogError($"Failed to load locations with label: {label}");
        }

        return sceneNames;
    }

    private void OnApplicationQuit()
    {
        string jsonData = "{\"map_name\": \"" + SceneManager.GetActiveScene().name + 
                         "\",\"posPlayerX\": " + GameManager.Instance.userPos.posPlayerX + 
                         ",\"posPlayerZ\": " + GameManager.Instance.userPos.posPlayerZ + 
                         ",\"posPlayerY\": " + GameManager.Instance.userPos.posPlayerY + 
                         ",\"email\": \"" + DataBase.Instance.Data.email + "\"}";
        DataBase.Instance.SendMessageApi(jsonData, "PosSet", (Success, request) => {
            Debug.Log(request);
        });
    }
    //---------------------------

    public void Mute(bool muteSet)
    {
        voice.Mute(muteSet);


        PlayerPrefs.SetInt("Mute", muteSet == true ? 1 : 0);
    }
     
    public void OtherSound(int volume)
    {
        voice.OtherSound(volume);

        PlayerPrefs.SetInt("VoiceSize", volume);
    }
}
