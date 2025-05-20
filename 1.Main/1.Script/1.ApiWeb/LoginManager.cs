using System;
using TMPro;
using UnityEngine;

[System.Serializable] //직렬화
public class PostDataSet
{
    public string stu_number;
    public string stu_name;
    public string stu_password;
}
public class LoginManager : MonoBehaviour
{
    
    [SerializeField]
    private TMP_InputField id_login;
    [SerializeField]
    private TMP_InputField pw_pasword;

    [SerializeField]
    private TMP_InputField pw;
    [SerializeField]
    private TMP_InputField id;
    [SerializeField]
    private TMP_InputField name;

    public void SignUp()
    {
        SignUp(
            id.text.Replace("\r\n", "")
            , name.text.Replace("\r\n", "")
            , pw.text.Replace("\r\n", "")
            );
    }

    public void Login()
    {
        if (string.IsNullOrEmpty(id_login.text) || string.IsNullOrEmpty(pw_pasword.text))
        {
            Debug.LogWarning("이메일과 비밀번호를 모두 입력해주세요.");
            return;
        }

        // 이메일 형식 검증
        if (!IsValidEmail(id_login.text))
        {
            Debug.LogWarning("올바른 이메일 형식이 아닙니다.");
            return;
        }

        DataBase.Instance.Login(id_login.text.Replace("\r\n", ""), pw_pasword.text.Replace("\r\n", ""));
    }
    
    public void SignUp(string _email, string _name, string _password)
    {
        if (string.IsNullOrEmpty(_email) || string.IsNullOrEmpty(_name) || string.IsNullOrEmpty(_password))
        {
            Debug.LogWarning("모든 필드를 입력해주세요.");
            return;
        }

        if (!IsValidEmail(_email))
        {
            Debug.LogWarning("올바른 이메일 형식이 아닙니다.");
            return;
        }

        // 데이터 객체 생성 (JSON 직렬화용)
        PostDataSet data = new PostDataSet
        {
            stu_number = _email,
            stu_name = _name,
            stu_password = _password
        };

        // JSON 데이터로 변환
        string jsonData = JsonUtility.ToJson(data);
        Debug.Log(jsonData);

        DataBase.Instance.SendMessageApi(jsonData, "SignUp", (Success, request) => {
            Debug.LogError(request);
        });
    }

    private bool IsValidEmail(string email)
    {
        try
        {
            var addr = new System.Net.Mail.MailAddress(email);
            return addr.Address == email;
        }
        catch
        {
            return false;
        }
    }

    public void OnWeb()
    {
        Application.OpenURL("https://metaplay.kr/");
    }
}
