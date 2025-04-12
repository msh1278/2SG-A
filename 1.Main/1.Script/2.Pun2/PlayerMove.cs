using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using TMPro;
using UnityEngine;
using UnityEngine.UIElements;

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

    [SerializeField]
    private Animator animator;
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

    // Start is called before the first frame update
    void Awake()
    {
        // 닉네임
        nickNameText.text = pv.IsMine ? PhotonNetwork.NickName : pv.Owner.NickName;
        if(pv.IsMine)
        {
            //카메라 켜기
            camera.SetActive(true);
        }
    }

    // Update is called once per frame
    void Update()
    {
        Move();

    }
    void Move()
    {
        if (pv.IsMine)
        {
            //이동
            float x = Input.GetAxisRaw("Horizontal");
            float z = Input.GetAxisRaw("Vertical");

            Vector3 axis = speed * transform.TransformDirection(new Vector3(x, 0, z).normalized);


            transform.LookAt(transform.position - new Vector3(cm.direction.normalized.x,0, cm.direction.normalized.z));


            rb.velocity = new Vector3(axis.x, rb.velocity.y, axis.z);

            if (axis != Vector3.zero)
            {
                //animator.SetBool("walk", true);
            }
            //else animator.SetBool("walk", false);


            //점프
            isGround = Physics.Raycast(transform.position, Vector3.down, 1, LayerMask.GetMask("Ground"));

            //animator.SetBool("jump", !isGround);
            if (Input.GetKeyDown(KeyCode.Space) && isGround) pv.RPC("JumpRPC", RpcTarget.All);


        }
    }
    [PunRPC]
    void JumpRPC()
    {
        rb.velocity = Vector3.zero;
        rb.AddForce(Vector3.up * jumpP);
    }
}
