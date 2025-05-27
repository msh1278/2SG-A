using UnityEngine;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System;
using System.Data;
using Photon.Pun;
using UnityEditor.SearchService;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public class DataBase : MonoBehaviour
{
    public CustomData customData { get; set; }
    public int stu_local_code { get; set; } = 1;

    private string nodeURL = "ws://106.248.231.106:9000";

    private static DataBase instance = null;

    private ClientWebSocket ws = new ClientWebSocket();
    StateRe stateRe = StateRe.ready;

    private UserID_Pw data;
    public UserID_Pw Data
    {
        get { return data; }
    }

    public PostDataSet dataUser;
    public PostDataSet DataUser
    {
        get { return dataUser; }
    }

    private bool loginOn = false;
    private bool isReceiving = false;

    private readonly object _wsLock = new object();
    private bool _isSending = false;
    private Queue<(string message, string path, Action<bool, string> callback)> _messageQueue = new Queue<(string, string, Action<bool, string>)>();
    private bool _isProcessingQueue = false;

    [System.Serializable]
    public class UserID_Pw
    {
        public string email;
        public string password;
        public int stu_local_code;
        public bool online;
        public string time;
        public string map_name;
        public float posPlayerX;
        public float posPlayerY;
        public float posPlayerZ;
        public string modelName;
        public int modelNum;
    }

    [System.Serializable]
    public class PostDataSet
    {
        public string stu_name;
        public string stu_password;
        public string stu_number;
    }

    enum StateRe
    {
        trying,
        ready
    }

    [System.Serializable]
    public class CustomData
    {
        public string email;
        public string modelName;
        public int customNum;
        // Add other custom data fields here
    }

    [System.Serializable]
    public class CustomResponse
    {
        public bool success;
        public CustomData data;
    }

    private async void Awake()
    {
        // Initialize the main thread dispatcher
        UnityMainThreadDispatcher.Initialize();
        
        Singleton();
        await ConnectWebSocketAsync();
    }

    private async Task ConnectWebSocketAsync()
    {
        if(stateRe == StateRe.trying)
        {
            return;
        }
        stateRe = StateRe.trying;
        
        int retryCount = 0;
        const int maxRetries = 3;
        
        while (retryCount < maxRetries)
        {
            try
            {
                if (ws.State == WebSocketState.Open)
                {
                    Debug.Log("WebSocket이 이미 연결되어 있습니다.");
                    stateRe = StateRe.ready;
                    return;
                }

                if (ws.State != WebSocketState.Closed)
                {
                    try {
                        await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "Reconnecting", CancellationToken.None);
                    } catch (Exception) {
                        // Ignore close errors
                    }
                }

                ws = new ClientWebSocket();
                ws.Options.KeepAliveInterval = TimeSpan.FromSeconds(30);
                
                using (var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5)))
                {
                    await ws.ConnectAsync(new Uri(nodeURL), cts.Token);
                }
                
                Debug.Log("WebSocket 연결 성공");
                stateRe = StateRe.ready;
                return;
            }
            catch (WebSocketException wsEx)
            {
                Debug.LogError($"WebSocket 연결 실패 (시도 {retryCount + 1}/{maxRetries}): {wsEx.Message}");
                retryCount++;
                if (retryCount < maxRetries)
                {
                    await Task.Delay(1000 * retryCount);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"연결 중 예외 발생 (시도 {retryCount + 1}/{maxRetries}): {ex.Message}");
                retryCount++;
                if (retryCount < maxRetries)
                {
                    await Task.Delay(1000 * retryCount);
                }
            }
        }
        
        Debug.LogError("WebSocket 연결 최대 시도 횟수 초과");
        stateRe = StateRe.ready;
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

    public static DataBase Instance
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

    private async Task ProcessMessageQueue()
    {
        if (_isProcessingQueue) return;
        _isProcessingQueue = true;

        try
        {
            while (_messageQueue.Count > 0)
            {
                var (message, path, callback) = _messageQueue.Dequeue();
                await SendMessageInternal(message, path, callback);
            }
        }
        finally
        {
            _isProcessingQueue = false;
        }
    }

    private async Task SendMessageInternal(string message, string path, Action<bool, string> requestMsg)
    {
        int maxRetries = 3;
        int retryCount = 0;

        while (retryCount < maxRetries)
        {
            try
            {
                if (ws.State != WebSocketState.Open)
                {
                    await ConnectWebSocketAsync();
                }

                string formattedMessage = path + "{****}" + message;
                byte[] bytes = Encoding.UTF8.GetBytes(formattedMessage);

                using (var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10)))
                {
                    await ws.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, cts.Token);
                    
                    byte[] buffer = new byte[4096];
                    WebSocketReceiveResult result = await ws.ReceiveAsync(new ArraySegment<byte>(buffer), cts.Token);
                    string receivedMsg = Encoding.UTF8.GetString(buffer, 0, result.Count);

                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        throw new WebSocketException("서버가 연결을 종료했습니다.");
                    }

                    requestMsg?.Invoke(true, receivedMsg);
                    return;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"메시지 전송 중 오류 발생 (시도 {retryCount + 1}/{maxRetries}): {ex.Message}");
                retryCount++;
                
                if (retryCount < maxRetries)
                {
                    await Task.Delay(1000 * retryCount);
                    try
                    {
                        await ConnectWebSocketAsync();
                    }
                    catch
                    {
                        // 연결 재시도 실패는 다음 반복에서 처리
                    }
                }
            }
        }

        requestMsg?.Invoke(false, "메시지 전송 실패: 최대 재시도 횟수 초과");
    }

    public async void SendMessageApi(string message, string path, Action<bool, string> requestMsg)
    {
        lock (_wsLock)
        {
            _messageQueue.Enqueue((message, path, requestMsg));
        }

        await ProcessMessageQueue();
    }

    private async Task SendMessageWithNewConnection(ClientWebSocket ws, string message, string path, Action<bool, string> requestMsg)
    {
        try
        {
            string formattedMessage = path + "{****}" + message;
            byte[] bytes = Encoding.UTF8.GetBytes(formattedMessage);
            
            using (var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10)))
            {
                await ws.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, cts.Token);
                
                byte[] buffer = new byte[4096];
                WebSocketReceiveResult result = await ws.ReceiveAsync(new ArraySegment<byte>(buffer), cts.Token);
                string receivedMsg = Encoding.UTF8.GetString(buffer, 0, result.Count);

                if (result.MessageType == WebSocketMessageType.Close)
                {
                    throw new WebSocketException("서버가 연결을 종료했습니다.");
                }

                requestMsg?.Invoke(true, receivedMsg);
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"새 연결로 메시지 전송 중 오류 발생: {ex.Message}");
            requestMsg?.Invoke(false, $"오류 발생: {ex.Message}");
        }
    }

    [System.Serializable]
    public class LoginResponse
    {
        public string token;
        public User user;
    }

    [System.Serializable]
    public class User
    {
        public int id;
        public string username;
        public string email;
        public string userType;
        public int universityId;
        public string universityName;
        public string studentId;
        public string grade;
    }

    float time = 0;
    private void Update()
    {
        return; // 잠시 온라인 상황 입력 정지

        if (loginOn == false)
        {
            if(time != 0)
                time = 0;
            return;
        }

        time += Time.deltaTime;

        if (time > 40)
        {
            Debug.Log(time);
            DataBase.Instance.SendMessageApi(data.email.ToString(), "UidCheck", (Success, request) => {
                Debug.Log(stu_local_code.ToString());

                if (request != stu_local_code.ToString())
                {
                    loginOn = false;
                    SceneManager.LoadScene("1.EnterScene");
                    Destroy(Instance.gameObject);
                    Destroy(GameManager.Instance.gameObject);
                }
            });

            if(loginOn)
            {
                Debug.Log("시간 업데이트");
                data.time = DateTime.Now.ToString("mm");
                string jsonData = JsonUtility.ToJson(data);
                DataBase.Instance.SendMessageApi(jsonData, "Upate", (Success, request) => { });
            }

            time = 0;
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

    public void Login(string _email, string _password)
    {
        if (loginOn) return;

        try
        {
            DataBase.Instance.stu_local_code = UnityEngine.Random.Range(1, 2147483646);
            
            data = new UserID_Pw
            {
                email = _email,
                password = _password,
                stu_local_code = DataBase.Instance.stu_local_code,
                time = DateTime.Now.ToString("mm"),
                online = true,
                map_name = "",
                posPlayerX = 0,
                posPlayerY = 0,
                posPlayerZ = 0,
                modelName = "",
                modelNum = 0
            };

            string jsonData = JsonUtility.ToJson(data);
            Debug.Log($"로그인 시도: {jsonData}");

            SendMessageApi(jsonData, "Login", (Success, request) => {
                Debug.Log($"로그인 응답: {request}");
                if(Success && !request.Contains("error"))
                {
                    try
                    {
                        var response = JsonUtility.FromJson<LoginResponse>(request);
                        if (response != null && response.user != null && !string.IsNullOrEmpty(response.user.email))
                        {
                            dataUser = new PostDataSet
                            {
                                stu_name = response.user.username,
                                stu_password = _password,
                                stu_number = response.user.email
                            };
                            
                            loginOn = true;
                            PhotonNetwork.NickName = dataUser.stu_name;

                            PlayerPrefs.SetString("email", data.email);
                            //PlayerPrefs.SetString("password", data.password);
                            // 로그인 성공 후 추가 요청
                            Task.Run(async () => {
                                try {
                                    using (var customWs = new ClientWebSocket()) {
                                        await customWs.ConnectAsync(new Uri(nodeURL), CancellationToken.None);
                                        
                                        // CustomGet 요청
                                        string customDataJson = "{\"email\": \"" + response.user.email + "\"}";
                                        await SendMessageWithNewConnection(customWs, customDataJson, "CustomGet", (Success, request) => {
                                            if (Success) {
                                                UnityMainThreadDispatcher.Instance().Enqueue(() => {
                                                    try {
                                                        Debug.Log($"CustomGet raw response: {request}"); // 실제 응답 로깅
                                                        
                                                        // 먼저 CustomResponse로 시도
                                                        var customResponse = JsonUtility.FromJson<CustomResponse>(request);
                                                        if (customResponse != null && customResponse.success && customResponse.data != null) {
                                                            customData = customResponse.data;
                                                            Debug.Log($"Custom data loaded successfully from response: {JsonUtility.ToJson(customData)}");
                                                            
                                                            // 데이터 유효성 검사
                                                            if (string.IsNullOrEmpty(customData.modelName)) {
                                                                Debug.LogWarning("Model name is empty in response, setting default");
                                                                customData.modelName = "model_1";
                                                            }
                                                            if (customData.customNum < 0) {
                                                                Debug.LogWarning("Invalid custom number in response, setting to 0");
                                                                customData.customNum = 0;
                                                            }
                                                        } else {
                                                            // CustomResponse 형식이 아니면 CustomData로 직접 시도
                                                            var directCustomData = JsonUtility.FromJson<CustomData>(request);
                                                            if (directCustomData != null && !string.IsNullOrEmpty(directCustomData.modelName)) {
                                                                customData = directCustomData;
                                                                Debug.Log($"Custom data loaded directly: {JsonUtility.ToJson(customData)}");
                                                            } else {
                                                                Debug.LogWarning("Custom data not found or invalid, using default values");
                                                                customData = new CustomData {
                                                                    email = response.user.email,
                                                                    modelName = "model_1",
                                                                    customNum = 0
                                                                };
                                                            }
                                                        }

                                                        // 최종 검증 및 저장
                                                        PlayerPrefs.SetString("CustomData", JsonUtility.ToJson(customData));
                                                        PlayerPrefs.Save();
                                                        Debug.Log($"Final custom data saved: {JsonUtility.ToJson(customData)}");
                                                    } catch (Exception ex) {
                                                        Debug.LogError($"Error processing custom data: {ex.Message}\nResponse: {request}");
                                                        customData = new CustomData {
                                                            email = response.user.email,
                                                            modelName = "model_1",
                                                            customNum = 0
                                                        };
                                                        PlayerPrefs.SetString("CustomData", JsonUtility.ToJson(customData));
                                                        PlayerPrefs.Save();
                                                    }
                                                });
                                            } else {
                                                Debug.LogError($"CustomGet request failed. Response: {request}");
                                                UnityMainThreadDispatcher.Instance().Enqueue(() => {
                                                    // 저장된 커스텀 데이터가 있는지 확인
                                                    string savedCustomData = PlayerPrefs.GetString("CustomData", "");
                                                    if (!string.IsNullOrEmpty(savedCustomData)) {
                                                        try {
                                                            customData = JsonUtility.FromJson<CustomData>(savedCustomData);
                                                            Debug.Log($"Loaded saved custom data: {JsonUtility.ToJson(customData)}");
                                                        } catch {
                                                            customData = new CustomData {
                                                                email = response.user.email,
                                                                modelName = "model_1",
                                                                customNum = 0
                                                            };
                                                        }
                                                    } else {
                                                        customData = new CustomData {
                                                            email = response.user.email,
                                                            modelName = "model_1",
                                                            customNum = 0
                                                        };
                                                    }
                                                    PlayerPrefs.SetString("CustomData", JsonUtility.ToJson(customData));
                                                    PlayerPrefs.Save();
                                                });
                                            }
                                        });

                                        // PosGet 요청
                                        await SendMessageWithNewConnection(customWs, customDataJson, "PosGet", (Success, request) => {
                                            if (Success) {
                                                UnityMainThreadDispatcher.Instance().Enqueue(() => {
                                                    try {
                                                        UserPos userPos = JsonUtility.FromJson<global::UserPos>(request);
                                                        if (userPos != null)
                                                        {
                                                            GameManager.Instance.userPos = userPos;
                                                            
                                                            //if (!string.IsNullOrEmpty(GameManager.Instance.userPos.map_name)) {
                                                                //GameManager.Instance.MoveMap(GameManager.Instance.userPos.map_name);
                                                            //} else {
                                                                GameManager.Instance.MapListOn();//무조건 리스트 표시
                                                            //}
                                                        }
                                                        else
                                                        {
                                                            Debug.LogError("Failed to parse user position data");
                                                            GameManager.Instance.MapListOn();
                                                        }
                                                    } catch (Exception ex) {
                                                        Debug.LogError($"Error processing position data on main thread: {ex.Message}");
                                                        GameManager.Instance.MapListOn();
                                                    }
                                                });
                                            }
                                        });
                                    }
                                } catch (Exception ex) {
                                    Debug.LogError($"추가 요청 처리 중 오류 발생: {ex.Message}");
                                    GameManager.Instance.MapListOn();
                                }
                            });
                        }
                        else
                        {
                            Debug.LogError("로그인 실패: 서버 응답 형식 오류");
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"로그인 처리 중 오류 발생: {ex.Message}");
                    }
                }
                else
                {
                    Debug.LogError("로그인 실패: 잘못된 아이디 또는 비밀번호");
                }
            });
        }
        catch (Exception ex)
        {
            Debug.LogError($"로그인 초기화 중 오류 발생: {ex.Message}");
        }
    }
}