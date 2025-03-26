using UnityEngine;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System;
using UnityEditor.PackageManager.Requests;

public class DataBase : MonoBehaviour
{
    private string saveDataURL = "http://localhost/Api/";

    private static DataBase instance = null;
    //싱글톤
    private void Awake()
    {
        Singleton();
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

    private ClientWebSocket ws = new ClientWebSocket();

    async void Start()
    {
        await ws.ConnectAsync(new System.Uri("ws://127.0.0.1:8080"), CancellationToken.None);
        Debug.Log("Connected to WebSocket Server");

        // 메시지 보내기
        //await SendMessage("Hello from Unity!");

        // 메시지 수신
        //await ReceiveMessage();
    }

    public async void SendMessageApi(string jsonData, string path, Action<bool, string> requestMsg)
    {
        await SendMessage(jsonData, path, requestMsg);
    }
    async Task SendMessage(string jsonData, string path, Action<bool, string> requestMsg)
    {

        try
        {
            byte[] bytes = Encoding.UTF8.GetBytes(jsonData);//메시지를 UTF-8 바이트 배열로 변환
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
            Debug.LogError($"WebSocket 전송 실패: {wsEx.Message}");
            requestMsg?.Invoke(false, $"WebSocket 오류: {wsEx.Message}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"예기치 않은 오류 발생: {ex.Message}");
            requestMsg?.Invoke(false, $"오류 발생: {ex.Message}");
        }
    }

}