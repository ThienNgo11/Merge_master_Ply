using System;
using UnityEngine;

namespace GameToolSample.Scripts.UI.ResourcesItems
{
    [CreateAssetMenu(fileName = "ItemResourceData", menuName = "ScriptableObject/ItemResourceData")]
    public class ItemResourceData : ScriptableObject
    {
        public ItemResourceInfo[] listItemResource;

        public ItemResourceInfo GetItemResourceInfo(ItemResourceType itemResourceType)
        {
            foreach (ItemResourceInfo info in listItemResource)
            {
                if (info.type == itemResourceType)
                {
                    return info;
                }
            }

            return null;
        }
    }

    [Serializable]
    public class ItemResourceInfo
    {
        public ItemResourceType type;
        public Sprite icon;
    }

    [Serializable]
    public enum ItemResourceType
    {
        None,
        Coin,
        Diamond,
        Skin,
    }

    [Serializable]
    public struct CurrencyInfo
    {
        public ItemResourceType itemResourceType;
        public int value;

        public CurrencyInfo(ItemResourceType itemResourceType, int value)
        {
            this.itemResourceType = itemResourceType;
            this.value = value;
        }
    }
}