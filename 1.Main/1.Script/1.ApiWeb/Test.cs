using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Test : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        Save("2401017", "홍길동", "1234");
    }

    [System.Serializable] //직렬화
    public class PostDataSet
    {
        public string stu_number;
        public string stu_name;
        public string stu_password;
    }

    public void Save(string _student_num, string _name, string _password)
    {
        // 데이터 객체 생성 (JSON 직렬화용)
        PostDataSet data = new PostDataSet
        {
            stu_number = _student_num,
            stu_name = _name,
            stu_password = _password
        };

        // 객체를 JSON 데이터로 변환
        string jsonData = JsonUtility.ToJson(data);
        Debug.Log(jsonData);

        DataBase.Instance.SendMessageApi(jsonData, "SignUp", (Success, request) => {
            Debug.LogError(request);
        });
    }
}
