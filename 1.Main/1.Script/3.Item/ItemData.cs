
 
using UnityEngine;

[CreateAssetMenu(fileName = "Item Data", menuName = "DataSet/Hand_Item", order = int.MaxValue)]
public class ItemData : ScriptableObject
{
    [SerializeField]
    private string itemName { get; set; }
    [SerializeField]
    private string useWorldName { get; set; }
    [SerializeField]
    private int itemCode { get; set; }
}