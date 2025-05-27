using Photon.Pun;
using TMPro;
using Unity.VisualScripting;
using UnityEditor.PackageManager.Requests;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

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
        if (DataBase.Instance == null || DataBase.Instance.customData == null)
        {
            Debug.LogError("DataBase.Instance or customData is null!");
            return;
        }

        if (string.IsNullOrEmpty(DataBase.Instance.customData.modelName))
        {
            Debug.LogError("Model name is not set in customData!");
            return;
        }

        try
        {
            GameObject modelInstance = await Addressables.InstantiateAsync(
                DataBase.Instance.customData.modelName, 
                animator_Target_P.TransformDirection(Vector3.zero), 
                Quaternion.identity, 
                animator_Target_P
            ).Task;

            if (modelInstance != null)
            {
                //var animatorView = modelInstance.GetComponent<PhotonAnimatorView>();
                //var transformView = modelInstance.GetComponent<PhotonTransformView>();
                /*
                if (animatorView == null || transformView == null)
                {
                    Debug.LogError("Required components (PhotonAnimatorView or PhotonTransformView) are missing on the model!");
                    Addressables.ReleaseInstance(modelInstance);
                    return;
                }

                animatorView.enabled = true;
                transformView.enabled = true;
                */
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
                    }
                }

                if (!foundCustomModel)
                {
                    Debug.LogWarning($"Custom model number {DataBase.Instance.customData.customNum} not found in children!");
                }

                custom = false;
            }
            else
            {
                Debug.LogError("Failed to instantiate model from Addressables!");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error in Custom(): {e.Message}\n{e.StackTrace}");
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
            if(Input.GetMouseButtonDown(0))
            {
                pv.RPC("Animation", RpcTarget.All, "MoveNum", 4);
                return;
            }
            #if UNITY_ANDROID || UNITY_IOS//|| UNITY_EDITOR
                float x = joystick.direction.x;
                float z = joystick.direction.y;
            #else
                //�̵�
                float x = Input.GetAxisRaw("Horizontal");
                float z = Input.GetAxisRaw("Vertical");
            #endif
            Vector3 axis;

            if (Input.GetKey(KeyCode.LeftShift))
            {
                axis = speed * transform.TransformDirection(new Vector3(x, 0, z).normalized);
            }
            else
            {
                axis = speed * 2 * transform.TransformDirection(new Vector3(x, 0, z).normalized);
            }

            /*
            animator_Target_P.LookAt(animator_Target_P.position + axis);

            transform.LookAt(transform.position - new Vector3(cm.direction.normalized.x,0, cm.direction.normalized.z));
            */
            pv.RPC("RotateSet", RpcTarget.All, axis);

            rb.velocity = new Vector3(axis.x, rb.velocity.y, axis.z);

            if (axis != Vector3.zero)
            {
                if (Input.GetKey(KeyCode.LeftShift))
                    pv.RPC("Animation", RpcTarget.All,"MoveNum",2);
                else
                    pv.RPC("Animation", RpcTarget.All, "MoveNum", 1);
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
