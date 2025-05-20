

using UnityEngine;

[CreateAssetMenu(fileName = "Item Data", menuName = "DataSet/Hand_Item", order = int.MaxValue)] //모두 손으로 잡을 수 있음
public class ItemData : ScriptableObject
{
    [SerializeField]
    private string itemName;
    public string ItemName { get { return itemName; } } //아이템 이름

    [SerializeField]
    private int itemCountMax;
    public int ItemCountMax { get { return itemCountMax; } } //아이템 최대 갯수


    [SerializeField]
    private int atk;
    public int Atk { get { return atk; } } //아이템 데미지

    [SerializeField]
    private AnimationType animationType;
    public AnimationType AnimationType { get { return animationType; } } // 공격 모션 none이면 공격 안함

}

public enum AnimationType
{
    None,
    Sword
}
