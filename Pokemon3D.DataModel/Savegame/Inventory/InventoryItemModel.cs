﻿using System.Runtime.Serialization;

// Disable Code Analysis for warning CS0649: Field is never assigned to, and will always have its default value.
#pragma warning disable 0649

namespace Pokemon3D.DataModel.Savegame.Inventory
{
    [DataContract(Namespace = "")]
    public class InventoryItemModel : DataModel<InventoryItemModel>
    {
        [DataMember(Order = 0)]
        public string Id;

        [DataMember(Order = 1)]
        public int Amount;

        public override object Clone()
        {
            return MemberwiseClone();
        }
    }
}
