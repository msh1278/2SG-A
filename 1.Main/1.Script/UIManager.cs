using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Photon.Pun.Demo.PunBasics;

public class UIManager : MonoBehaviour
{
    [SerializeField]
    private GameObject joystick;
    
    [SerializeField]
    private GameObject onMute;  // 음소거 버튼 GameObject

    // Start is called before the first frame update
    void Start()
    {
        if (joystick == null)
        {
            Debug.LogError("Joystick GameObject is not assigned in UIManager!");
            return;
        }

#if UNITY_ANDROID || UNITY_IOS//|| UNITY_EDITOR
        Debug.Log("모바일 플랫폼에서 실행");
        joystick.SetActive(true);
#else
        joystick.SetActive(false);
#endif

        if (onMute == null)
        {
            Debug.LogError("onMute GameObject is not assigned in the inspector!");
            return;
        }

        if (GameManager.Instance == null)
        {
            Debug.LogError("GameManager.Instance is not available!");
            return;
        }

        // 음소거 상태에 따라 버튼 UI 업데이트
        UpdateMuteButton();
    }

    private void UpdateMuteButton()
    {
        if (onMute != null && GameManager.Instance != null)
        {
            // 음소거 버튼의 상태를 GameManager의 mute 상태와 동기화
            onMute.SetActive(GameManager.Instance.mute);
        }
    }

    // 음소거 버튼 클릭 시 호출되는 메서드
    public void ToggleMute()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.mute = !GameManager.Instance.mute;
            GameManager.Instance.Mute(GameManager.Instance.mute);
            UpdateMuteButton();
        }
    }

    public void MoveCustom()
    {
        GameManager.Instance.MoveMap("Custom"); // 커스텀으로 이동
    }
    private void Update()
    {
        if(Input.GetKeyDown(KeyCode.C)&& GameManager.Instance.userPos.map_name != "SignUp")
        {
            MoveCustom();
        }
    }
}
