using UnityEngine;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System;

public class DataBase : MonoBehaviour
{
    public int stu_local_code { get; set; } = 1;

    private string nodeURL = "ws://127.0.0.1:8080";

    private static DataBase instance = null;

    private ClientWebSocket ws = new ClientWebSocket();
    StateRe stateRe = StateRe.ready;

    private UserID_Pw data;

    private bool loginOn = false;// 로그인 중인가?
    private bool isReceiving = false; // 현재 수신 중인지 확인
    [System.Serializable]
    public class UserID_Pw
    {
        public string stu_number;
        public string stu_password;
        public int stu_local_code;
        public string time;
    }
    enum StateRe
    {
        trying,
        ready
    }
    //싱글톤
    private async void Awake()
    {
        Singleton();
        await ConnectWebSocketAsync();
    }

    // Connect WebSocket
    private async Task ConnectWebSocketAsync()
    {
        if(stateRe == StateRe.trying)
        {
            return;
        }
        stateRe = StateRe.trying;//시도중
        while (true)
        {
            try
            {
                /*
                if (ws.State == WebSocketState.Open)
                {
                    return; // 이미 연결이 되어있으면 추가 연결을 시도하지 않음
                }
                */
                ws = new ClientWebSocket();
                await ws.ConnectAsync(new Uri(nodeURL), CancellationToken.None);
                Debug.Log("WebSocket 연결 성공");
                stateRe = StateRe.ready;//다시 시도 가능

                return;
            }
            catch (Exception ex)
            {
                Debug.LogError($"WebSocket 연결 실패: {ex.Message}");

                await Task.Delay(1000); // 1초 후 다시 시도
            }
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
    public static DataBase Instance // 싱글톤 보안성
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

    //-------------------------------------------------------

    public async void SendMessageApi(string message, string path, Action<bool, string> requestMsg)
    {
        //await ws.ConnectAsync(new System.Uri("ws://127.0.0.1:8080"), CancellationToken.None);
        await SendMessage(message, path, requestMsg);
    }
    async Task SendMessage(string message, string path, Action<bool, string> requestMsg)
    {   while (true)
        {
            if (isReceiving)
            {
                Debug.Log("이미 실행 중");
                await Task.Delay(100);
            }
            else
            {
                Debug.Log("재생");
                break;
            }
        }
        // 이미 수신 중이면 실행하지 않음
        isReceiving = true;

        message = path + "{****}" + message;
        try
        {
            byte[] bytes = Encoding.UTF8.GetBytes(message);//메시지를 UTF-8 바이트 배열로 변환
            await ws.SendAsync(new System.ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, CancellationToken.None);
            //WebSocketMessageType.Text → 메시지 타입이 텍스트 데이터


            byte[] buffer = new byte[1024];
            WebSocketReceiveResult result = await ws.ReceiveAsync(new System.ArraySegment<byte>(buffer), CancellationToken.None);
            string receivedMsg = Encoding.UTF8.GetString(buffer, 0, result.Count);

            //Debug.Log(receivedMsg);//에러시 에러 메시지 받기
            requestMsg?.Invoke(true, receivedMsg);
        }
        catch (WebSocketException wsEx)
        {
            await ConnectWebSocketAsync();
            // WebSocket 재 시도



            Debug.LogError($"WebSocket 전송 실패: {wsEx.Message}");
            requestMsg?.Invoke(false, $"WebSocket 오류: {wsEx.Message}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"예기치 않은 오류 발생: {ex.Message}");
            requestMsg?.Invoke(false, $"오류 발생: {ex.Message}");
        }
        finally
        {
            Debug.Log("finally");
            isReceiving = false; // 수신 완료 후 다시 받을 수 있도록 변경
        }
    }
    private async void OnApplicationQuit()
    {
        if (ws.State == WebSocketState.Open)
        {
            await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "Application is quitting", CancellationToken.None);
            Debug.Log("WebSocket 연결 종료");
        }
    }
    public void Login(string _id, string _password)
    {
        if (loginOn) return;

        DataBase.Instance.stu_local_code = UnityEngine.Random.Range(1, 2147483646);
        // 보내는 데이터 (JSON 형식으로)
        data = new UserID_Pw
        {
            stu_number = _id,
            stu_password = _password,
            stu_local_code = DataBase.Instance.stu_local_code, //랜덤 수 ( 로그인 시 고유 다른 아이디랑 같아도 됨 )
            time = DateTime.Now.ToString(("mm")) //현재 분 저장
        };

        // JSON 형식으로 변환
        string jsonData = JsonUtility.ToJson(data);
        Debug.Log(jsonData);

        DataBase.Instance.SendMessageApi(jsonData, "Login", (Success, request) => {
            Debug.Log(request);
            if(request == "1")
            {
                //로그인 성공 시
                loginOn = true;
                //StartCoroutine(DataUpdate());
            }
        });


    }
    float time = 0;
    private void Update()
    {
        if (loginOn==false) return;

        time += Time.deltaTime;

        //Debug.Log(time);
        if (time > 40)
        {
            Debug.Log(time);
            DataBase.Instance.SendMessageApi(data.stu_number.ToString(), "UidCheck", (Success, request) => {

                Debug.Log(stu_local_code.ToString());

                if (request != stu_local_code.ToString())
                {
                    loginOn = false;
                    Debug.Log("계정이 다름");
                }
            });

            if(loginOn)
            {
                Debug.Log("데이터 시간 업데이트");
                data.time = DateTime.Now.ToString(("mm"));
                string jsonData = JsonUtility.ToJson(data);
                DataBase.Instance.SendMessageApi(jsonData, "Upate", (Success, request) => { });
            }

            //만약에 지금 uid 코드가 서버에 있는것과 다르면 로그아웃 할것 ( 로그인 창으로 다시 복귀)
            time = 0;
        }
    }
}