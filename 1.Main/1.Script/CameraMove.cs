using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraMove : MonoBehaviour
{
    private float Yaxis;
    private float Xaxis;

    [SerializeField]
    private Transform target;//Player
    public Vector3 direction;

    private float rotSensitive = 3f;
    private float dis = 10f;
    private float RotationMin = -10f;
    private float RotationMax = 80f;
    private float smoothTime = 0.12f;
    private Vector3 targetRotation;
    private Vector3 currentVel;
    private void Awake()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void LateUpdate()
    {
        Yaxis = Yaxis + Input.GetAxis("Mouse X") * rotSensitive;
        Xaxis = Xaxis - Input.GetAxis("Mouse Y") * rotSensitive;

        Xaxis = Mathf.Clamp(Xaxis, RotationMin, RotationMax);

        targetRotation = Vector3.SmoothDamp(targetRotation, new Vector3(Xaxis, Yaxis), ref currentVel, smoothTime);
        this.transform.eulerAngles = targetRotation;

        Vector3 startPosition = target.position - transform.forward * dis;
        //direction = target.position - startPosition; 반대로
        direction = startPosition - target.position;

        // 레이케스트를 발사
        RaycastHit hit;

        Debug.DrawLine(target.position, target.position + direction, Color.green);


        if (Physics.Raycast(target.position, direction, out hit, direction.magnitude, (-1) -(1<< LayerMask.NameToLayer("Player"))))
        {
            transform.position = hit.point;
        }
        else
        {
            transform.position = startPosition;
        }



        dis = Mathf.Clamp(Input.GetAxis("Mouse ScrollWheel") + dis,3, 13);
    }
}
