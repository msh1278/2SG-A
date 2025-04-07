using System;
using TMPro;
using UnityEngine;

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

    [System.Serializable] //직열화
    public class PostDataSet
    {
        public string stu_number;
        public string stu_name;
        public string stu_password;
    }


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
        DataBase.Instance.Login(id_login.text.Replace("\r\n", ""), pw_pasword.text.Replace("\r\n", ""));
    }
    
    public void SignUp(string _student_num, string _name, string _password)
    {
        // 보내는 데이터 (JSON 형식으로)
        PostDataSet data = new PostDataSet
        {
            stu_number = _student_num,
            stu_name = _name,
            stu_password = _password
        };


        // JSON 형식으로 변환
        string jsonData = JsonUtility.ToJson(data);
        Debug.Log(jsonData);

        DataBase.Instance.SendMessageApi(jsonData, "SignUp", (Success, request) => {

            Debug.LogError(request);

        });
    }
}
