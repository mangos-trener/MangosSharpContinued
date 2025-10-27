//
// Copyright (C) 2013-2025 getMaNGOS <https://www.getmangos.eu>
//
// This program is free software. You can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation. either version 2 of the License, or
// (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY. Without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program. If not, write to the Free Software
// Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307  USA
//

using Mangos.Common.Enums.Global;
using Mangos.Common.Enums.Item;
using Mangos.Common.Enums.Spell;
using Mangos.Common.Globals;
using Mangos.Common.Legacy;
using Mangos.Common.Legacy.Databases;
using Mangos.World.DataStores;
using Mangos.World.Globals;
using Mangos.World.Loots;
using Mangos.World.Network;
using Mangos.World.Objects.Factories;
using Mangos.World.Objects.Factories.Loot;
using Mangos.World.Objects.Factories.Packets;
using Mangos.World.Player;
using Mangos.World.Spells;
using Microsoft.Extensions.Logging;
using Microsoft.VisualBasic;
using Microsoft.VisualBasic.CompilerServices;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Runtime.CompilerServices;

namespace Mangos.World.Objects;

public sealed class ItemObject : IDisposable
{
    public ItemObject(
        ILogger<ItemObject> logger,
        WorldState worldState,
        WS_DBCDatabase database,
        CharacterDatabase characterDatabase,
        WorldDatabase worldDatabase,
        ItemInfoFactory itemInfoFactory,
        Func<LootObjectFactory> lootObjectFactory,
        UpdateClassFactory updateClassFactory)
    {
        this.logger = logger;
        this.worldState = worldState;
        this.database = database;
        this.characterDatabase = characterDatabase;
        this.worldDatabase = worldDatabase;
        this.itemInfoFactory = itemInfoFactory;
        this.lootObjectFactory = lootObjectFactory;
        this.updateClassFactory = updateClassFactory;
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
    public ItemObject(
        ILogger<ItemObject> logger,
        WorldState worldState,
        WS_DBCDatabase database,
        CharacterDatabase characterDatabase,
        WorldDatabase worldDatabase,
        ItemInfoFactory itemInfoFactory,
        Func<LootObjectFactory> lootObjectFactory,
        UpdateClassFactory updateClassFactory,
        ulong guidVal,
        CharacterObject owner = null,
        bool equipped = false)
    : this(logger, worldState, database, characterDatabase, worldDatabase, itemInfoFactory, lootObjectFactory, updateClassFactory)
    {
        GiftCreatorGUID = 0uL;
        StackCount = 1;
        Durability = 1;
        ChargesLeft = 0;
        _flags = 0;
        Items = null;
        RandomProperties = 0;
        SuffixFactor = 0;
        Enchantments = new Dictionary<byte, WS_Items.TEnchantmentInfo>();
        _loot = null;
        ItemText = 0;

        DataTable mySqlQuery = new();
        characterDatabase.Query($"SELECT * FROM characters_inventory WHERE item_guid = \"{guidVal}\";", ref mySqlQuery);
        if (mySqlQuery.Rows.Count == 0)
        {
            Information.Err().Raise(1, "ItemObject.New", $"itemGuid {guidVal} not found in SQL database!");
        }

        GUID = Conversions.ToULong(Operators.AddObject(mySqlQuery.Rows[0]["item_guid"], MangosGlobalConstants.GUID_ITEM));
        CreatorGUID = mySqlQuery.Rows[0].As<ulong>("item_creator");
        OwnerGUID = mySqlQuery.Rows[0].As<ulong>("item_owner");
        GiftCreatorGUID = mySqlQuery.Rows[0].As<ulong>("item_giftCreator");
        StackCount = mySqlQuery.Rows[0].As<int>("item_stackCount");
        Durability = mySqlQuery.Rows[0].As<int>("item_durability");
        ChargesLeft = mySqlQuery.Rows[0].As<int>("item_chargesLeft");
        RandomProperties = mySqlQuery.Rows[0].As<int>("item_random_properties");
        ItemEntry = mySqlQuery.Rows[0].As<int>("item_id");
        _flags = mySqlQuery.Rows[0].As<int>("item_flags");
        ItemText = mySqlQuery.Rows[0].As<int>("item_textId");
        var tmp = Strings.Split(mySqlQuery.Rows[0].As<string>("item_enchantment"));
        checked
        {
            if (tmp.Length > 0)
            {
                var num = tmp.Length - 1;
                for (var i = 0; i <= num; i++)
                {
                    if (Operators.CompareString(Strings.Trim(tmp[i]), "", TextCompare: false) != 0)
                    {
                        var tmp2 = Strings.Split(tmp[i], ":");
                        Enchantments.Add(Conversions.ToByte(tmp2[0]), new WS_Items.TEnchantmentInfo(Conversions.ToInteger(tmp2[1]), Conversions.ToInteger(tmp2[2]), Conversions.ToInteger(tmp2[3])));
                        if (equipped)
                        {
                            AddEnchantBonus(Conversions.ToByte(tmp2[0]), owner);
                        }
                    }
                }
            }
            if (!worldState.ItemDatabase.ContainsKey(ItemEntry))
            {
                var tmpItem2 = itemInfoFactory.Create(ItemEntry);
            }

            InitializeBag();

            mySqlQuery.Clear();
            characterDatabase.Query($"SELECT * FROM characters_inventory WHERE item_bag = {GUID};", ref mySqlQuery);
            IEnumerator enumerator = default;
            try
            {
                enumerator = mySqlQuery.Rows.GetEnumerator();
                while (enumerator.MoveNext())
                {
                    DataRow row = (DataRow)enumerator.Current;
                    if (Operators.ConditionalCompareObjectNotEqual(row["item_slot"], MangosGlobalConstants.ITEM_SLOT_NULL, TextCompare: false))
                    {
                        ItemObject tmpItem = new(logger, worldState, database, characterDatabase, worldDatabase, itemInfoFactory, lootObjectFactory, updateClassFactory, row.As<long, ulong>("item_guid"));

                        Items[row.As<byte>("item_slot")] = tmpItem;
                    }
                }
            }
            finally
            {
                if (enumerator is IDisposable)
                {
                    (enumerator as IDisposable).Dispose();
                }
            }

            worldState.WorldItems.Add(GUID, this);
        }
    }

    public ItemObject(
        ILogger<ItemObject> logger,
        WorldState worldState,
        WS_DBCDatabase database,
        CharacterDatabase characterDatabase,
        WorldDatabase worldDatabase,
        ItemInfoFactory itemInfoFactory,
        Func<LootObjectFactory> lootObjectFactory,
        UpdateClassFactory updateClassFactory,
        int itemId,
        ulong owner)
    : this(logger, worldState, database, characterDatabase, worldDatabase, itemInfoFactory, lootObjectFactory, updateClassFactory)
    {
        GiftCreatorGUID = 0uL;
        StackCount = 1;
        Durability = 1;
        ChargesLeft = 0;
        _flags = 0;
        Items = null;
        RandomProperties = 0;
        SuffixFactor = 0;
        Enchantments = new Dictionary<byte, WS_Items.TEnchantmentInfo>();
        _loot = null;
        ItemText = 0;
        try
        {
            if (!worldState.ItemDatabase.ContainsKey(itemId))
            {
                var tmpItem = itemInfoFactory.Create(itemId);
            }

            ItemEntry = itemId;
            OwnerGUID = owner;
            Durability = worldState.ItemDatabase[ItemEntry].Durability;

            var i = 0;
            do
            {
                if ((worldState.ItemDatabase[ItemEntry].Spells[i].SpellTrigger == ITEM_SPELLTRIGGER_TYPE.USE
                    || worldState.ItemDatabase[ItemEntry].Spells[i].SpellTrigger == ITEM_SPELLTRIGGER_TYPE.NO_DELAY_USE)
                    && worldState.ItemDatabase[ItemEntry].Spells[i].SpellCharges != 0)
                {
                    ChargesLeft = worldState.ItemDatabase[ItemEntry].Spells[i].SpellCharges;
                    break;
                }

                i = checked(i + 1);
            }

            while (i <= 4);
            GUID = GetNewGUID();
            InitializeBag();
            SaveAsNew();
            worldState.WorldItems.Add(GUID, this);
        }
        catch (Exception ex)
        {
            ProjectData.SetProjectError(ex);
            logger.LogWarning("Duplicate Key Warning ITEMID:{0} OWNERGUID:{1}", itemId, owner);
            ProjectData.ClearProjectError();
        }
    }

    public readonly int ItemEntry;

    public ulong GUID;

    public ulong OwnerGUID;

    public readonly ulong GiftCreatorGUID;

    public readonly ulong CreatorGUID;

    public int StackCount;

    public int Durability;

    public int ChargesLeft;

    private int _flags;

    public Dictionary<byte, ItemObject> Items;

    public readonly int RandomProperties;

    public int SuffixFactor;

    public readonly Dictionary<byte, WS_Items.TEnchantmentInfo> Enchantments;

    private readonly WS_DBCDatabase database;
    private readonly ItemInfoFactory itemInfoFactory;
    private readonly Func<LootObjectFactory> lootObjectFactory;
    private readonly UpdateClassFactory updateClassFactory;
    private readonly WorldState worldState;
    private readonly ILogger<ItemObject> logger;
    private readonly CharacterDatabase characterDatabase;
    private readonly WorldDatabase worldDatabase;
    private WS_Loot.LootObject _loot;

    public int ItemText;

    private bool _disposedValue;

    public WS_Items.ItemInfo ItemInfo => worldState.ItemDatabase[ItemEntry];

    public bool IsFree => Items.Count <= 0;

    public bool IsRanged => ItemInfo.ObjectClass == ITEM_CLASS.ITEM_CLASS_WEAPON && (ItemInfo.SubClass == ITEM_SUBCLASS.ITEM_SUBCLASS_LIQUID || ItemInfo.SubClass == ITEM_SUBCLASS.ITEM_SUBCLASS_CROSSBOW || ItemInfo.SubClass == ITEM_SUBCLASS.ITEM_SUBCLASS_POTION);

    public byte GetBagSlot
    {
        get
        {
            if (!worldState.Characters.ContainsKey(OwnerGUID))
            {
                return byte.MaxValue;
            }
            var characterObject = worldState.Characters[OwnerGUID];
            byte i = 19;
            do
            {
                checked
                {
                    if (characterObject.Items.ContainsKey(i))
                    {
                        var b = (byte)(characterObject.Items[i].ItemInfo.ContainerSlots - 1);
                        byte j = 0;
                        while (j <= (uint)b)
                        {
                            if (characterObject.Items[i].Items.ContainsKey(j) && characterObject.Items[i].Items[j] == this)
                            {
                                return i;
                            }
                            j = (byte)unchecked((uint)(j + 1));
                        }
                    }
                    i = (byte)unchecked((uint)(i + 1));
                }
            }
            while (i <= 22u);
            return byte.MaxValue;
        }
    }

    public int GetSlot
    {
        get
        {
            if (!worldState.Characters.ContainsKey(OwnerGUID))
            {
                return -1;
            }
            var characterObject = worldState.Characters[OwnerGUID];
            byte i = 0;
            do
            {
                if (characterObject.Items.ContainsKey(i) && characterObject.Items[i] == this)
                {
                    return i;
                }
                checked
                {
                    i = (byte)unchecked((uint)(i + 1));
                }
            }
            while (i <= 38u);
            byte k = 81;
            do
            {
                if (characterObject.Items.ContainsKey(k) && characterObject.Items[k] == this)
                {
                    return k;
                }
                checked
                {
                    k = (byte)unchecked((uint)(k + 1));
                }
            }
            while (k <= 112u);
            byte j = 19;
            do
            {
                checked
                {
                    if (characterObject.Items.ContainsKey(j))
                    {
                        var b = (byte)(characterObject.Items[j].ItemInfo.ContainerSlots - 1);
                        byte l = 0;
                        while (l <= (uint)b)
                        {
                            if (characterObject.Items[j].Items.ContainsKey(l) && characterObject.Items[j].Items[l] == this)
                            {
                                return l;
                            }
                            l = (byte)unchecked((uint)(l + 1));
                        }
                    }
                    j = (byte)unchecked((uint)(j + 1));
                }
            }
            while (j <= 22u);
            return -1;
        }
    }

    public int GetSkill => ItemInfo.GetReqSkill;

    public uint GetDurabulityCost
    {
        get
        {
            checked
            {
                try
                {
                    var lostDurability = worldState.ItemDatabase[ItemEntry].Durability - Durability;
                    if (lostDurability > 300)
                    {
                        lostDurability = 300;
                    }

                    var subClass = (ItemInfo.ObjectClass != ITEM_CLASS.ITEM_CLASS_WEAPON) ? ((int)ItemInfo.SubClass + 21) : (int)ItemInfo.SubClass;
                    var durabilityCost = (uint)Math.Round(lostDurability * (database.DurabilityCosts[ItemInfo.Level, subClass] / 40.0 * 100.0));

                    logger.LogDebug("Durability cost: {0}", durabilityCost);

                    return durabilityCost;
                }
                catch (Exception projectError)
                {
                    ProjectData.SetProjectError(projectError);
                    var GetDurabulityCost = 0u;
                    ProjectData.ClearProjectError();
                    return GetDurabulityCost;
                }
            }
        }
    }

    public bool IsSoulBound => (_flags & 1) == 1;

    [MethodImpl(MethodImplOptions.Synchronized)]
    private ulong GetNewGUID()
    {
        return ++WorldState.ItemGuidCounter;
    }

    public void FillAllUpdateFlags(ref Packets.UpdateClass update)
    {
        checked
        {
            if (ItemInfo.ContainerSlots > 0)
            {
                update.SetUpdateFlag(0, GUID);
                update.SetUpdateFlag(2, 7);
                update.SetUpdateFlag(3, ItemEntry);
                update.SetUpdateFlag(4, 1f);
                update.SetUpdateFlag(6, OwnerGUID);
                update.SetUpdateFlag(8, OwnerGUID);
                if (decimal.Compare(new decimal(CreatorGUID), 0m) > 0)
                {
                    update.SetUpdateFlag(10, CreatorGUID);
                }
                update.SetUpdateFlag(12, GiftCreatorGUID);
                update.SetUpdateFlag(14, StackCount);
                update.SetUpdateFlag(21, _flags);
                update.SetUpdateFlag(48, ItemInfo.ContainerSlots);
                byte i = 0;
                do
                {
                    if (Items.ContainsKey(i))
                    {
                        update.SetUpdateFlag(50 + (i * 2), (long)Items[i].GUID);
                    }
                    else
                    {
                        update.SetUpdateFlag(50 + (i * 2), 0);
                    }
                    i = (byte)unchecked((uint)(i + 1));
                }
                while (i <= 35u);
                return;
            }
            update.SetUpdateFlag(0, GUID);
            update.SetUpdateFlag(2, 3);
            update.SetUpdateFlag(3, ItemEntry);
            update.SetUpdateFlag(4, 1f);
            update.SetUpdateFlag(6, OwnerGUID);
            update.SetUpdateFlag(8, OwnerGUID);
            if (decimal.Compare(new decimal(CreatorGUID), 0m) > 0)
            {
                update.SetUpdateFlag(10, CreatorGUID);
            }
            update.SetUpdateFlag(12, GiftCreatorGUID);
            update.SetUpdateFlag(14, StackCount);
            var j = 0;
            do
            {
                if (ItemInfo.Spells[j].SpellTrigger is ITEM_SPELLTRIGGER_TYPE.USE or ITEM_SPELLTRIGGER_TYPE.NO_DELAY_USE)
                {
                    update.SetUpdateFlag(16 + j, ChargesLeft);
                }
                else
                {
                    update.SetUpdateFlag(16 + j, -1);
                }
                j++;
            }
            while (j <= 4);
            update.SetUpdateFlag(21, _flags);
            update.SetUpdateFlag(44, RandomProperties);
            foreach (var enchant in Enchantments)
            {
                update.SetUpdateFlag(22 + (enchant.Key * 3), enchant.Value.ID);
                update.SetUpdateFlag(22 + (enchant.Key * 3) + 1, enchant.Value.Duration);
                update.SetUpdateFlag(22 + (enchant.Key * 3) + 2, enchant.Value.Charges);
            }
            update.SetUpdateFlag(45, ItemText);
            update.SetUpdateFlag(46, Durability);
            update.SetUpdateFlag(47, worldState.ItemDatabase[ItemEntry].Durability);
        }
    }

    public void SendContainedItemsUpdate(ref WS_Network.ClientClass client, int updatetype = 2)
    {
        Packets.PacketClass packet = new(Opcodes.SMSG_UPDATE_OBJECT);
        packet.AddInt32(Items.Count);
        packet.AddInt8(0);
        foreach (var item in Items)
        {
            var tmpUpdate = updateClassFactory.Create(MangosGlobalConstants.FIELD_MASK_SIZE_ITEM);
            item.Value.FillAllUpdateFlags(ref tmpUpdate);
            var updateClass = tmpUpdate;
            var updateObject = item.Value;
            updateClass.AddToPacket(ref packet, (ObjectUpdateType)updatetype, ref updateObject);
            tmpUpdate.Dispose();
        }
        client.Send(ref packet);
    }

    private void InitializeBag()
    {
        Items = worldState.ItemDatabase[ItemEntry].IsContainer ? new Dictionary<byte, ItemObject>() : null;
    }

    public bool GenerateLoot()
    {
        if (_loot != null)
        {
            return true;
        }

        DataTable mySqlQuery = new();
        worldDatabase.Query($"SELECT * FROM item_loot WHERE entry = {ItemEntry};", ref mySqlQuery);
        if (mySqlQuery.Rows.Count == 0)
        {
            return false;
        }

        _loot = lootObjectFactory().Create(GUID, LootType.LOOTTYPE_CORPSE);
        WS_Loot.LootTemplates_Item.GetLoot(ItemEntry)?.Process(ref _loot, 0);
        _loot.LootOwner = 0uL;
        return true;
    }

    private void SaveAsNew()
    {
        var tmpCmd = "INSERT INTO characters_inventory (item_guid";
        var tmpValues = " VALUES (" + Conversions.ToString(checked(GUID - MangosGlobalConstants.GUID_ITEM));
        tmpCmd += ", item_owner";
        tmpValues = tmpValues + ", \"" + Conversions.ToString(OwnerGUID) + "\"";
        tmpCmd += ", item_creator";
        tmpValues = tmpValues + ", " + Conversions.ToString(CreatorGUID);
        tmpCmd += ", item_giftCreator";
        tmpValues = tmpValues + ", " + Conversions.ToString(GiftCreatorGUID);
        tmpCmd += ", item_stackCount";
        tmpValues = tmpValues + ", " + Conversions.ToString(StackCount);
        tmpCmd += ", item_durability";
        tmpValues = tmpValues + ", " + Conversions.ToString(Durability);
        tmpCmd += ", item_chargesLeft";
        tmpValues = tmpValues + ", " + Conversions.ToString(ChargesLeft);
        tmpCmd += ", item_random_properties";
        tmpValues = tmpValues + ", " + Conversions.ToString(RandomProperties);
        tmpCmd += ", item_id";
        tmpValues = tmpValues + ", " + Conversions.ToString(ItemEntry);
        tmpCmd += ", item_flags";
        tmpValues = tmpValues + ", " + Conversions.ToString(_flags);
        ArrayList temp = new();
        foreach (var enchantment in Enchantments)
        {
            temp.Add($"{enchantment.Key}:{enchantment.Value.ID}:{enchantment.Value.Duration}:{enchantment.Value.Charges}");
        }
        tmpCmd += ", item_enchantment";
        tmpValues = tmpValues + ", '" + Strings.Join(temp.ToArray()) + "'";
        tmpCmd += ", item_textId";
        tmpValues = tmpValues + ", " + Conversions.ToString(ItemText);
        tmpCmd = tmpCmd + ") " + tmpValues + ");";
        characterDatabase.Update(tmpCmd);
    }

    public void Save(bool saveAll = true)
    {
        var tmp = "UPDATE characters_inventory SET";
        tmp = tmp + " item_owner=\"" + Conversions.ToString(OwnerGUID) + "\"";
        tmp = tmp + ", item_creator=" + Conversions.ToString(CreatorGUID);
        tmp = tmp + ", item_giftCreator=" + Conversions.ToString(GiftCreatorGUID);
        tmp = tmp + ", item_stackCount=" + Conversions.ToString(StackCount);
        tmp = tmp + ", item_durability=" + Conversions.ToString(Durability);
        tmp = tmp + ", item_chargesLeft=" + Conversions.ToString(ChargesLeft);
        tmp = tmp + ", item_random_properties=" + Conversions.ToString(RandomProperties);
        tmp = tmp + ", item_flags=" + Conversions.ToString(_flags);
        ArrayList temp = new();
        foreach (var enchantment in Enchantments)
        {
            temp.Add($"{enchantment.Key}:{enchantment.Value.ID}:{enchantment.Value.Duration}:{enchantment.Value.Charges}");
        }
        tmp = tmp + ", item_enchantment=\"" + Strings.Join(temp.ToArray()) + "\"";
        tmp = tmp + ", item_textId=" + Conversions.ToString(ItemText);
        tmp = tmp + " WHERE item_guid = \"" + Conversions.ToString(checked(GUID - MangosGlobalConstants.GUID_ITEM)) + "\";";
        characterDatabase.Update(tmp);
        if (!worldState.ItemDatabase[ItemEntry].IsContainer || !saveAll)
        {
            return;
        }
        foreach (var item in Items)
        {
            item.Value.Save();
        }
    }

    public void Delete()
    {
        checked
        {
            if (ItemEntry == MangosGlobalConstants.PETITION_GUILD)
            {
                characterDatabase.Update("DELETE FROM petitions WHERE petition_itemGuid = " + Conversions.ToString(GUID - MangosGlobalConstants.GUID_ITEM) + ";");
            }
            characterDatabase.Update($"DELETE FROM characters_inventory WHERE item_guid = {GUID - MangosGlobalConstants.GUID_ITEM}");
            if (worldState.ItemDatabase[ItemEntry].IsContainer)
            {
                foreach (var item in Items)
                {
                    item.Value.Delete();
                }
            }
            Dispose();
        }
    }

    private void Dispose(bool disposing)
    {
        if (!_disposedValue)
        {
            worldState.WorldItems.Remove(GUID);
            if (worldState.ItemDatabase[ItemEntry].IsContainer)
            {
                foreach (var item in Items)
                {
                    item.Value.Dispose();
                }
            }
            if (!Information.IsNothing(_loot))
            {
                _loot.Dispose();
            }
        }
        _disposedValue = true;
    }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    void IDisposable.Dispose()
    {
        //ILSpy generated this explicit interface implementation from .override directive in Dispose
        Dispose();
    }

    public bool IsBroken()
    {
        return Durability == 0 && ItemInfo.Durability > 0;
    }

    public void ModifyDurability(float percent, ref WS_Network.ClientClass client)
    {
        if (worldState.ItemDatabase[ItemEntry].Durability > 0)
        {
            ref var durability = ref Durability;
            durability = checked((int)Math.Round(durability - Conversion.Fix(worldState.ItemDatabase[ItemEntry].Durability * percent)));

            if (Durability < 0)
            {
                Durability = 0;
            }

            if (Durability > worldState.ItemDatabase[ItemEntry].Durability)
            {
                Durability = worldState.ItemDatabase[ItemEntry].Durability;
            }

            UpdateDurability(ref client);
        }
    }

    public void ModifyToDurability(float percent, ref WS_Network.ClientClass client)
    {
        if (worldState.ItemDatabase[ItemEntry].Durability > 0)
        {
            Durability = checked((int)(worldState.ItemDatabase[ItemEntry].Durability * percent));
            if (Durability < 0)
            {
                Durability = 0;
            }
            if (Durability > worldState.ItemDatabase[ItemEntry].Durability)
            {
                Durability = worldState.ItemDatabase[ItemEntry].Durability;
            }
            UpdateDurability(ref client);
        }
    }

    private void UpdateDurability(ref WS_Network.ClientClass client)
    {
        Packets.PacketClass packet = new(Opcodes.SMSG_UPDATE_OBJECT);
        packet.AddInt32(1);
        packet.AddInt8(0);
        var tmpUpdate = updateClassFactory.Create(MangosGlobalConstants.FIELD_MASK_SIZE_ITEM);
        tmpUpdate.SetUpdateFlag(46, Durability);
        var updateObject = this;
        tmpUpdate.AddToPacket(ref packet, ObjectUpdateType.UPDATETYPE_VALUES, ref updateObject);
        tmpUpdate.Dispose();
        client.Send(ref packet);
    }

    public void AddEnchantment(int id, byte slot, int duration = 0, int charges = 0)
    {
        if (Enchantments.ContainsKey(slot))
        {
            RemoveEnchantment(slot);
        }
        Enchantments.Add(slot, new WS_Items.TEnchantmentInfo(id, duration, charges));
        CharacterObject objCharacter = null;
        AddEnchantBonus(slot, objCharacter);
    }

    public void AddEnchantBonus(byte slot, CharacterObject objCharacter = null)
    {
        if (objCharacter == null)
        {
            if (!worldState.Characters.ContainsKey(OwnerGUID))
            {
                return;
            }
            objCharacter = worldState.Characters[OwnerGUID];
        }
        if (objCharacter == null || !database.SpellItemEnchantments.ContainsKey(Enchantments[slot].ID))
        {
            return;
        }
        byte i = 0;
        do
        {
            checked
            {
                if (database.SpellItemEnchantments[Enchantments[slot].ID].SpellID[i] != 0 && WS_Spells.SPELLs.ContainsKey(database.SpellItemEnchantments[Enchantments[slot].ID].SpellID[i]))
                {
                    var spellInfo = WS_Spells.SPELLs[database.SpellItemEnchantments[Enchantments[slot].ID].SpellID[i]];
                    byte j = 0;
                    do
                    {
                        if (spellInfo.SpellEffects[j] != null)
                        {
                            var iD = spellInfo.SpellEffects[j].ID;
                            if (iD == SpellEffects_Names.SPELL_EFFECT_APPLY_AURA)
                            {
                                var obj = WS_Spells.AURAs[spellInfo.SpellEffects[j].ApplyAuraIndex];
                                WS_Base.BaseUnit Target = objCharacter;
                                WS_Base.BaseObject Caster = objCharacter;
                                obj(ref Target, ref Caster, ref spellInfo.SpellEffects[j], spellInfo.ID, 1, AuraAction.AURA_ADD);
                                objCharacter = (CharacterObject)Target;
                            }
                        }
                        j = (byte)unchecked((uint)(j + 1));
                    }
                    while (j <= 2u);
                }
                i = (byte)unchecked((uint)(i + 1));
            }
        }
        while (i <= 2u);
    }

    public void RemoveEnchantBonus(byte slot)
    {
        if (!worldState.Characters.ContainsKey(OwnerGUID) || !database.SpellItemEnchantments.ContainsKey(Enchantments[slot].ID))
        {
            return;
        }
        byte i = 0;
        do
        {
            checked
            {
                if (database.SpellItemEnchantments[Enchantments[slot].ID].SpellID[i] != 0 && WS_Spells.SPELLs.ContainsKey(database.SpellItemEnchantments[Enchantments[slot].ID].SpellID[i]))
                {
                    var spellInfo = WS_Spells.SPELLs[database.SpellItemEnchantments[Enchantments[slot].ID].SpellID[i]];
                    byte j = 0;
                    do
                    {
                        if (spellInfo.SpellEffects[j] != null)
                        {
                            var iD = spellInfo.SpellEffects[j].ID;
                            if (iD == SpellEffects_Names.SPELL_EFFECT_APPLY_AURA)
                            {
                                var obj = WS_Spells.AURAs[spellInfo.SpellEffects[j].ApplyAuraIndex];
                                Dictionary<ulong, CharacterObject> cHARACTERs;
                                ulong ownerGUID;
                                WS_Base.BaseUnit Target = (cHARACTERs = worldState.Characters)[ownerGUID = OwnerGUID];
                                Dictionary<ulong, CharacterObject> cHARACTERs2;
                                ulong ownerGUID2;
                                WS_Base.BaseObject Caster = (cHARACTERs2 = worldState.Characters)[ownerGUID2 = OwnerGUID];
                                obj(ref Target, ref Caster, ref spellInfo.SpellEffects[j], spellInfo.ID, 1, AuraAction.AURA_REMOVE);
                                cHARACTERs2[ownerGUID2] = (CharacterObject)Caster;
                                cHARACTERs[ownerGUID] = (CharacterObject)Target;
                            }
                        }
                        j = (byte)unchecked((uint)(j + 1));
                    }
                    while (j <= 2u);
                }
                i = (byte)unchecked((uint)(i + 1));
            }
        }
        while (i <= 2u);
    }

    private void RemoveEnchantment(byte slot)
    {
        checked
        {
            if (Enchantments.ContainsKey(slot))
            {
                RemoveEnchantBonus(slot);
                Enchantments.Remove(slot);
                if (worldState.Characters.ContainsKey(OwnerGUID))
                {
                    Packets.PacketClass packet = new(Opcodes.SMSG_UPDATE_OBJECT);
                    packet.AddInt32(1);
                    packet.AddInt8(0);
                    var tmpUpdate = updateClassFactory.Create(MangosGlobalConstants.FIELD_MASK_SIZE_ITEM);
                    tmpUpdate.SetUpdateFlag(22 + (slot * 3), 0);
                    tmpUpdate.SetUpdateFlag(22 + (slot * 3) + 1, 0);
                    tmpUpdate.SetUpdateFlag(22 + (slot * 3) + 2, 0);
                    var updateObject = this;
                    tmpUpdate.AddToPacket(ref packet, ObjectUpdateType.UPDATETYPE_VALUES, ref updateObject);
                    worldState.Characters[OwnerGUID].client.Send(ref packet);
                    packet.Dispose();
                    tmpUpdate.Dispose();
                }
            }
        }
    }

    public void SoulbindItem(WS_Network.ClientClass client = null)
    {
        if ((_flags & 1) != 1)
        {
            _flags |= 1;
            Save();
            if (client != null)
            {
                Packets.PacketClass packet = new(Opcodes.SMSG_UPDATE_OBJECT);
                packet.AddInt32(1);
                packet.AddInt8(0);
                var tmpUpdate = updateClassFactory.Create(MangosGlobalConstants.FIELD_MASK_SIZE_ITEM);
                tmpUpdate.SetUpdateFlag(21, _flags);
                var updateObject = this;
                tmpUpdate.AddToPacket(ref packet, ObjectUpdateType.UPDATETYPE_VALUES, ref updateObject);
                client.Send(ref packet);
                packet.Dispose();
                tmpUpdate.Dispose();
            }
        }
    }
}
