using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnterScript : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        GameManager.Instance.DownLoad("PlayerModel", false);//플레이어 모델 미리 받기

        GameManager.Instance.MoveMap("SignUp"); //로그인 화면으로 이동
    }

}
