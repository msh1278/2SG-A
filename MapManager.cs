using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MapManager : MonoBehaviour
{
    [SerializeField]
    private int respawnArea;
    void Awake()
    {
        GameManager.Instance.respawnArea = respawnArea;
    }
}
