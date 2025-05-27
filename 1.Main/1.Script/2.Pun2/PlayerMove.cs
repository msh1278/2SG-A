using Photon.Pun;
using TMPro;
using Unity.VisualScripting;
using UnityEditor.PackageManager.Requests;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Threading.Tasks;
using UnityEngine.ResourceManagement.AsyncOperations;
using System;

public class PlayerMove : MonoBehaviour
{
    [SerializeField]
    CameraMove cm;
    [SerializeField]
    private PhotonView pv;
    [SerializeField]
    private TMP_Text nickNameText;
    [SerializeField]
    private GameObject camera;

    private Animator animator;
    //private PhotonAnimatorView photonAnimatorView;
    [SerializeField]
    private Transform animator_Target_P;


    [SerializeField]
    private Joystick joystick;
    [SerializeField]
    private Rigidbody rb;
    [SerializeField]
    private bool isGround;
    [SerializeField]
    float speed = 1,jumpP = 100,cameraSpeed = 5;
    [SerializeField]
    private Transform cameraCenter;
    private Vector3 v,rot;
    Vector3 cameraSpeedV;



    private bool custom = true;
    [SerializeField]
    private TextMeshProUGUI chatAr;
    [SerializeField]
    private TMP_InputField chatInput;
    // Start is called before the first frame update
    void Awake()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        // �г���
        nickNameText.text = pv.IsMine ? PhotonNetwork.NickName : pv.Owner.NickName;


        if (pv.IsMine)
        {
            //ī�޶� �ѱ�
            camera.SetActive(true);
        }else
        {
            GetComponent<Rigidbody>().useGravity = false;
            transform.GetComponent<Collider>().enabled = false;
            this.enabled = false; // ��ũ��Ʈ ��������
        }
    }
    void Start()
    {
        if (pv.IsMine)
        {
            pv.RPC("Custom", RpcTarget.AllBuffered); // ����ȭ
            chatInput.onEndEdit.AddListener(SendMsgNext);
            PosSave();
        }
    }


    void SendMsgNext(string text)
    {
        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            SendChat();
        }
    }

    [PunRPC]
    public async void Custom()
    {
        if (animator != null) return;
        Debug.Log(pv.IsMine ? PhotonNetwork.NickName : pv.Owner.NickName);
        
        // customData가 없거나 modelName이 비어있으면 저장된 데이터 확인
        if (DataBase.Instance == null || DataBase.Instance.customData == null || string.IsNullOrEmpty(DataBase.Instance.customData.modelName))
        {
            Debug.LogWarning("Custom data not loaded yet, checking saved data");
            string savedCustomData = PlayerPrefs.GetString("CustomData", "");
            if (!string.IsNullOrEmpty(savedCustomData))
            {
                try
                {
                    var savedData = JsonUtility.FromJson<DataBase.CustomData>(savedCustomData);
                    Debug.Log($"Loaded saved custom data from PlayerPrefs: {savedCustomData}");
                    
                    if (DataBase.Instance != null)
                    {
                        DataBase.Instance.customData = savedData;
                        Debug.Log($"Updated DataBase.Instance.customData: {JsonUtility.ToJson(DataBase.Instance.customData)}");
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Error loading saved custom data: {ex.Message}");
                    if (DataBase.Instance != null)
                    {
                        DataBase.Instance.customData = new DataBase.CustomData
                        {
                            email = DataBase.Instance.Data.email,
                            modelName = "model_1",
                            customNum = 0
                        };
                    }
                }
            }
            else
            {
                Debug.LogWarning("No saved custom data found, using default model");
                if (DataBase.Instance != null && DataBase.Instance.customData == null)
                {
                    DataBase.Instance.customData = new DataBase.CustomData
                    {
                        email = DataBase.Instance.Data.email,
                        modelName = "model_1",
                        customNum = 0
                    };
                }
            }
        }

        // 최종 데이터 확인
        if (DataBase.Instance != null && DataBase.Instance.customData != null)
        {
            Debug.Log($"Final custom data to be applied: {JsonUtility.ToJson(DataBase.Instance.customData)}");
        }

        try
        {
            // Addressables 초기화 확인 및 대기
            if (!Addressables.InitializationOperation.IsDone)
            {
                Debug.Log("Waiting for Addressables initialization...");
                await Addressables.InitializationOperation.Task;
                Debug.Log("Addressables initialization completed");
            }

            // 모델 로드 전에 위치 확인
            if (animator_Target_P == null)
            {
                Debug.LogError("animator_Target_P is null!");
                return;
            }

            string modelName = DataBase.Instance.customData.modelName;
            Debug.Log($"Attempting to load model: {modelName}");

            // 모델 로드 시도
            try
            {
                var loadOperation = Addressables.LoadResourceLocationsAsync(modelName);
                await loadOperation.Task;

                if (loadOperation.Status != AsyncOperationStatus.Succeeded || loadOperation.Result == null || loadOperation.Result.Count == 0)
                {
                    Debug.LogError($"Failed to load model locations for: {modelName}, trying default model");
                    modelName = "model_1";
                    loadOperation = Addressables.LoadResourceLocationsAsync(modelName);
                    await loadOperation.Task;
                    
                    if (loadOperation.Status != AsyncOperationStatus.Succeeded || loadOperation.Result == null || loadOperation.Result.Count == 0)
                    {
                        Debug.LogError("Failed to load even default model!");
                        return;
                    }
                }

                // 기존 모델이 있다면 제거
                if (animator != null)
                {
                    var oldModel = animator.gameObject;
                    animator = null;
                    Addressables.ReleaseInstance(oldModel);
                }

                // 모델 인스턴스화
                var instantiateOperation = Addressables.InstantiateAsync(
                    modelName,
                    animator_Target_P.position,
                    Quaternion.identity,
                    animator_Target_P
                );

                await instantiateOperation.Task;

                if (instantiateOperation.Status != AsyncOperationStatus.Succeeded)
                {
                    Debug.LogError($"Failed to instantiate model: {instantiateOperation.OperationException?.Message}");
                    return;
                }

                GameObject modelInstance = instantiateOperation.Result;
                if (modelInstance != null)
                {
                    animator = modelInstance.GetComponent<Animator>();

                    if (animator == null)
                    {
                        Debug.LogError("Animator component is missing on the model!");
                        Addressables.ReleaseInstance(modelInstance);
                        return;
                    }

                    Transform parent = modelInstance.transform;
                    parent.localPosition = Vector3.zero;
                    
                    bool foundCustomModel = false;
                    for (int i2 = 0; i2 < parent.childCount; i2++)
                    {
                        GameObject child = parent.GetChild(i2).gameObject;
                        if (child.name != "Root")
                        {
                            bool shouldActivate = DataBase.Instance.customData.customNum == i2;
                            child.SetActive(shouldActivate);
                            if (shouldActivate) foundCustomModel = true;
                            Debug.Log($"Child model {i2} activated: {shouldActivate}");
                        }
                    }

                    if (!foundCustomModel)
                    {
                        Debug.LogWarning($"Custom model number {DataBase.Instance.customData.customNum} not found in children!");
                        // 첫 번째 모델 활성화
                        if (parent.childCount > 1)
                        {
                            parent.GetChild(1).gameObject.SetActive(true);
                            Debug.Log("Activated first available child model");
                        }
                    }

                    custom = false;
                    Debug.Log($"Model {modelName} loaded and set up successfully with custom number {DataBase.Instance.customData.customNum}");
                }
                else
                {
                    Debug.LogError("Failed to instantiate model from Addressables!");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error during model loading: {ex.Message}\n{ex.StackTrace}");
                throw;
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error in Custom(): {e.Message}\n{e.StackTrace}");
            // 에러 발생 시 기본 모델로 시도
            try
            {
                if (DataBase.Instance != null)
                {
                    DataBase.Instance.customData = new DataBase.CustomData
                    {
                        email = DataBase.Instance.Data.email,
                        modelName = "model_1",
                        customNum = 0
                    };
                    PlayerPrefs.SetString("CustomData", JsonUtility.ToJson(DataBase.Instance.customData));
                    PlayerPrefs.Save();
                    
                    // 잠시 대기 후 재시도
                    await Task.Delay(1000);
                    Custom();
                }
            }
            catch (System.Exception retryEx)
            {
                Debug.LogError($"Failed to retry with default model: {retryEx.Message}");
            }
        }
    }
    // Update is called once per frame
    void Update()
    {
        if (!EventSystem.current.currentSelectedGameObject == chatInput.gameObject)
        {
            if (animator != null) Move();
        }


    }
    void Move()
    {
        if (pv.IsMine)
        {
            /*
            if(Input.GetMouseButtonDown(0))
            {
                pv.RPC("Animation", RpcTarget.All, "MoveNum", 4);
                return;
            }
            */
            #if UNITY_ANDROID || UNITY_IOS//|| UNITY_EDITOR
                float x = joystick.direction.x;
                float z = joystick.direction.y;
            #else
                //�̵�
                float x = Input.GetAxisRaw("Horizontal");
                float z = Input.GetAxisRaw("Vertical");
            #endif
            Vector3 axis;

            if (Input.GetKey(KeyCode.LeftControl))
                axis = speed * transform.TransformDirection(new Vector3(x, 0, z).normalized);
            else if (Input.GetKey(KeyCode.LeftShift))
                axis = speed * 2f * transform.TransformDirection(new Vector3(x, 0, z).normalized);
            else
                axis = speed * 1.5f * transform.TransformDirection(new Vector3(x, 0, z).normalized);

            /*
            animator_Target_P.LookAt(animator_Target_P.position + axis);

            transform.LookAt(transform.position - new Vector3(cm.direction.normalized.x,0, cm.direction.normalized.z));
            */
            pv.RPC("RotateSet", RpcTarget.All, axis);

            rb.velocity = new Vector3(axis.x, rb.velocity.y, axis.z);

            if (axis != Vector3.zero)
            {
                if (Input.GetKey(KeyCode.LeftControl))
                    pv.RPC("Animation", RpcTarget.All,"MoveNum",2);
                else if (Input.GetKey(KeyCode.LeftShift))
                    pv.RPC("Animation", RpcTarget.All, "MoveNum", 1);
                else
                    pv.RPC("Animation", RpcTarget.All, "MoveNum", 3);
                //animator.SetInteger("MoveNum", 1);
            }
            else
                pv.RPC("Animation", RpcTarget.All,"MoveNum", 0);


            //����
            //isGround = Physics.Raycast(transform.position, Vector3.down, 1, LayerMask.GetMask("Ground"));
            isGround = Physics.Raycast(transform.position+new Vector3(0,0.1f,0), Vector3.down, 1);


            if (Input.GetKeyDown(KeyCode.Space) && isGround)
            {
                pv.RPC("JumpRPC", RpcTarget.All);
            }


            if (GameManager.Instance.respawnArea > transform.position.y)
            {
                transform.position = GameObject.Find("Start_Point").transform.position;
                GameManager.Instance.userPos.posPlayerX = transform.position.x;
                GameManager.Instance.userPos.posPlayerY = transform.position.y;
                GameManager.Instance.userPos.posPlayerZ = transform.position.z; 

                GameManager.Instance.MoveMap(GameManager.Instance.userPos.map_name);//다시 시작
            }

        }
    }
    [PunRPC]
    public void RotateSet(Vector3 axis)
    {
        if (animator == null) return;


        animator_Target_P.LookAt(animator_Target_P.position + axis);

        transform.LookAt(transform.position - new Vector3(cm.direction.normalized.x, 0, cm.direction.normalized.z));
    }
    [PunRPC]
    public void Animation(string name, int num)
    {
        if (animator == null) return;

        animator.SetInteger(name, num);
    }
    [PunRPC]
    public void JumpRPC()
    {
        if (animator == null) return;

        animator.SetTrigger("Jump");
        rb.velocity = Vector3.zero;
        rb.AddForce(Vector3.up * jumpP);
    }
    
    private void OnDestroy()
    {
        if (pv.IsMine)
        {
            //�÷��̾� ĳ���Ͱ� ������
            //PosSave();
            //���ͳ��� ���� ��Ȳ

            GameManager.Instance.userPos.map_name = SceneManager.GetActiveScene().name;

            GameManager.Instance.userPos.posPlayerX = transform.position.x;
            GameManager.Instance.userPos.posPlayerY = transform.position.y;
            GameManager.Instance.userPos.posPlayerZ = transform.position.z;
            
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }
    }
    private void OnApplicationQuit()
    {
        if (pv.IsMine)
        {
            //������ �����
            PosSave();
        }
    }

    private void OnSceneLoaded(Scene arg0,LoadSceneMode arg1)
    {
        if (pv.IsMine)
        {
            //���� �ٲ�
            //�α���,Ŀ���� ������ �Ѿ �� �� ����
            if (arg0.name == "Custom" || arg0.name == "SignUp")
                PosSave();

            SceneManager.sceneLoaded -= OnSceneLoaded;
        }
    }

    private void PosSave()
    {
        if (!pv.IsMine) return;

        GameManager.Instance.userPos.map_name = SceneManager.GetActiveScene().name;
        GameManager.Instance.userPos.posPlayerX = transform.position.x;
        GameManager.Instance.userPos.posPlayerY = transform.position.y;
        GameManager.Instance.userPos.posPlayerZ = transform.position.z;

        string jsonData = "{" +
            "\"email\": \"" + DataBase.Instance.Data.email + "\"," +
            "\"map_name\": \"" + SceneManager.GetActiveScene().name + "\"," +
            "\"posPlayerX\": " + transform.position.x.ToString() + "," +
            "\"posPlayerY\": " + transform.position.y.ToString() + "," +
            "\"posPlayerZ\": " + transform.position.z.ToString() +
        "}";

        DataBase.Instance.SendMessageApi(jsonData, "PosSet", (Success, request) =>
        {
            Debug.Log("위치 저장 완료");
        });
    }
    public void SendChat()
    {
        pv.RPC("ReceiveChat", RpcTarget.All, PhotonNetwork.NickName, chatInput.text);
        chatInput.text = "";
    }

    [PunRPC]
    void ReceiveChat(string sender, string message)
    {
        //Debug.Log($"{sender}: {message}");
        chatAr.text += $" \n{sender}: {message}";
        LayoutRebuilder.ForceRebuildLayoutImmediate(chatAr.transform as RectTransform);
    }
}
