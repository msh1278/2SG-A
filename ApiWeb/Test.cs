using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Test : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        Save("2401017", "김도균", "kim1234", "1234");
    }

    [System.Serializable] //직열화
    public class PostDataSet
    {
        public string student_num;
        public string name;
        public string id;
        public string password;
    }

    public void Save(string _student_num, string _name, string _id, string _password)
    {
        // 보내는 데이터 객체 (JSON 형식으로)
        PostDataSet data = new PostDataSet
        {
            student_num = _student_num,
            name = _name,
            id = _id,
            password = _password
        };

        // 객체를 JSON 형식으로 변환
        string jsonData = JsonUtility.ToJson(data);
        DataBase.Instance.SendMessageApi(jsonData, "Login/Login.php", (Success, request) => {

            Debug.LogError(request);

        });
        

    }
}
