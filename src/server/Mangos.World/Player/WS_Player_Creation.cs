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
using Mangos.Common.Enums.Misc;
using Mangos.Common.Enums.Player;
using Mangos.Common.Globals;
using Mangos.Common.Legacy;
using Mangos.Common.Legacy.Databases;
using Mangos.World.DataStores;
using Mangos.World.Objects.Factories;
using Microsoft.Extensions.Logging;
using Microsoft.VisualBasic.CompilerServices;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;

namespace Mangos.World.Player;

public class WS_Player_Creation
{
    private readonly ILogger<WS_Player_Creation> logger;
    private readonly WorldState worldState;
    private readonly WS_DBCDatabase database;
    private readonly WorldDatabase worldDatabase;
    private readonly CharacterDatabase characterDatabase;
    private readonly AccountDatabase accountDatabase;
    private readonly WS_Player_Initializator playerInitializator;
    private readonly ItemInfoFactory itemInfoFactory;
    private readonly CharacterObjectFactory characterObjectFactory;

    public WS_Player_Creation(ILogger<WS_Player_Creation> logger, WorldState worldState, WS_DBCDatabase database, WorldDatabase worldDatabase, CharacterDatabase characterDatabase, AccountDatabase accountDatabase, WS_Player_Initializator playerInitializator, ItemInfoFactory itemInfoFactory, CharacterObjectFactory characterObjectFactory)
    {
        this.logger = logger;
        this.worldState = worldState;
        this.database = database;
        this.worldDatabase = worldDatabase;
        this.characterDatabase = characterDatabase;
        this.accountDatabase = accountDatabase;
        this.playerInitializator = playerInitializator;
        this.itemInfoFactory = itemInfoFactory;
        this.characterObjectFactory = characterObjectFactory;
    }

    public int CreateCharacter(string account, string name, byte race, byte characterClass, byte gender, byte skin, byte face, byte hairStyle, byte hairColor, byte facialHair, byte outfitID)
    {
        DataTable MySQLQuery = new();

        var character = characterObjectFactory.Create();

        character.Name = Globals.Functions.CapitalizeName(ref name);
        character.Race = (Races)race;
        character.Classe = (Classes)characterClass;
        character.Gender = (Genders)gender;
        character.Skin = skin;
        character.Face = face;
        character.HairStyle = hairStyle;
        character.HairColor = hairColor;
        character.FacialHair = facialHair;

        accountDatabase.Query($"SELECT id, gmlevel FROM account WHERE username = \"{account}\";", ref MySQLQuery);

        var Account_ID = MySQLQuery.Rows[0].As<int>("id");
        var Account_Access = character.Access = (AccessLevel)MySQLQuery.Rows[0].As<byte>("gmlevel");
        if (!Globals.Functions.ValidateName(character.Name))
        {
            return 70;
        }

        try
        {
            MySQLQuery.Clear();
            characterDatabase.Query($"SELECT char_name FROM characters WHERE char_name = \"{character.Name}\";", ref MySQLQuery);
            if (MySQLQuery.Rows.Count > 0)
            {
                return 49;
            }
        }
        catch (Exception projectError)
        {
            ProjectData.SetProjectError(projectError);
            var CreateCharacter = 48;
            ProjectData.ClearProjectError();
            return CreateCharacter;
        }

        checked
        {
            if (MangosGlobalConstants.SERVER_CONFIG_DISABLED_CLASSES[(int)character.Classe - 1]
                || (MangosGlobalConstants.SERVER_CONFIG_DISABLED_RACES[(int)character.Race - 1]
                && Account_Access < AccessLevel.GameMaster))
            {
                return 50;
            }

            if (Account_Access <= AccessLevel.Player)
            {
                MySQLQuery.Clear();
                characterDatabase.Query($"SELECT char_race FROM characters WHERE account_id = \"{Account_ID}\" LIMIT 1;", ref MySQLQuery);
                if (MySQLQuery.Rows.Count > 0 && character.IsHorde != Globals.Functions.GetCharacterSide(MySQLQuery.Rows[0].As<byte>("char_race")))
                {
                    return 51;
                }
            }

            MySQLQuery.Clear();

            characterDatabase.Query($"SELECT char_name FROM characters WHERE account_id = \"{Account_ID}\";", ref MySQLQuery);
            if (MySQLQuery.Rows.Count >= 10)
            {
                return 52;
            }

            MySQLQuery.Clear();

            characterDatabase.Query($"SELECT char_name FROM characters WHERE account_id = \"{Account_ID}\";", ref MySQLQuery);
            if (MySQLQuery.Rows.Count >= 10)
            {
                return 53;
            }

            try
            {
                playerInitializator.InitializeReputations(ref character);
                CreateCharacter(ref character);
                character.SaveAsNewCharacter(Account_ID);
                CreateCharacterSpells(ref character);
                CreateCharacterItems(ref character);
            }
            catch (Exception ex)
            {
                ProjectData.SetProjectError(ex);
                var err = ex;
                logger.LogError("Error initializing character! {0} {1}", Environment.NewLine, err.ToString());
                var CreateCharacter = 48;
                ProjectData.ClearProjectError();
                return CreateCharacter;
            }
            finally
            {
                character.Dispose();
            }

            return 46;
        }
    }

    public void CreateCharacter(ref CharacterObject objCharacter)
    {
        DataTable CreateInfo = new();
        DataTable CreateInfoBars = new();
        DataTable CreateInfoSkills = new();
        DataTable LevelStats = new();
        DataTable ClassLevelStats = new();

        worldDatabase.Query($"SELECT * FROM playercreateinfo WHERE race = {(int)objCharacter.Race} AND class = {(int)objCharacter.Classe};", ref CreateInfo);
        if (CreateInfo.Rows.Count <= 0)
        {
            logger.LogError("No information found in playercreateinfo table for Race: {0}, Class: {1}", objCharacter.Race, objCharacter.Classe);
        }

        worldDatabase.Query($"SELECT * FROM playercreateinfo_action WHERE race = {(int)objCharacter.Race} AND class = {(int)objCharacter.Classe} ORDER BY button;", ref CreateInfoBars);
        if (CreateInfoBars.Rows.Count <= 0)
        {
            logger.LogError("No information found in playercreateinfo_action table for Race: {0}, Class: {1}", objCharacter.Race, objCharacter.Classe);
        }

        worldDatabase.Query($"SELECT * FROM playercreateinfo_skill WHERE race = {(int)objCharacter.Race} AND class = {(int)objCharacter.Classe};", ref CreateInfoSkills);
        if (CreateInfoSkills.Rows.Count <= 0)
        {
            logger.LogError("No information found in playercreateinfo_skill table for Race: {0}, Class: {1}", objCharacter.Race, objCharacter.Classe);
        }

        worldDatabase.Query($"SELECT * FROM player_levelstats WHERE race = {(int)objCharacter.Race} AND class = {(int)objCharacter.Classe} AND level = {objCharacter.Level};", ref LevelStats);
        if (LevelStats.Rows.Count <= 0)
        {
            logger.LogError("No information found in player_levelstats table for Race: {0}, Class: {1}, Level: {2}", objCharacter.Race, objCharacter.Classe, objCharacter.Level);
        }

        worldDatabase.Query($"SELECT * FROM player_classlevelstats WHERE class = {(int)objCharacter.Classe} AND level = {objCharacter.Level};", ref ClassLevelStats);
        if (ClassLevelStats.Rows.Count <= 0)
        {
            logger.LogError("No information found in player_classlevelstats table for Class: {0}, Level: {1}", objCharacter.Classe, objCharacter.Level);
        }

        objCharacter.Copper = 0u;
        objCharacter.XP = 0;
        objCharacter.Size = 1f;
        objCharacter.Life.Base = 0;
        objCharacter.Life.Current = 0;
        objCharacter.Mana.Base = 0;
        objCharacter.Mana.Current = 0;
        objCharacter.Rage.Current = 0;
        objCharacter.Rage.Base = 0;
        objCharacter.Energy.Current = 0;
        objCharacter.Energy.Base = 0;
        objCharacter.ManaType = playerInitializator.GetClassManaType(objCharacter.Classe);
        objCharacter.Model = Globals.Functions.GetRaceModel(objCharacter.Race, (int)objCharacter.Gender);
        objCharacter.Faction = database.CharRaces[(int)objCharacter.Race].FactionID;
        objCharacter.MapID = Conversions.ToUInteger(CreateInfo.Rows[0]["map"]);
        objCharacter.ZoneID = Conversions.ToInteger(CreateInfo.Rows[0]["zone"]);
        objCharacter.positionX = Conversions.ToSingle(CreateInfo.Rows[0]["position_x"]);
        objCharacter.positionY = Conversions.ToSingle(CreateInfo.Rows[0]["position_y"]);
        objCharacter.positionZ = Conversions.ToSingle(CreateInfo.Rows[0]["position_z"]);
        objCharacter.orientation = Conversions.ToSingle(CreateInfo.Rows[0]["orientation"]);

        checked
        {
            objCharacter.bindpoint_map_id = (int)objCharacter.MapID;
            objCharacter.bindpoint_zone_id = objCharacter.ZoneID;
            objCharacter.bindpoint_positionX = objCharacter.positionX;
            objCharacter.bindpoint_positionY = objCharacter.positionY;
            objCharacter.bindpoint_positionZ = objCharacter.positionZ;
            objCharacter.Strength.Base = Conversions.ToInteger(LevelStats.Rows[0]["str"]);
            objCharacter.Agility.Base = Conversions.ToInteger(LevelStats.Rows[0]["agi"]);
            objCharacter.Stamina.Base = Conversions.ToInteger(LevelStats.Rows[0]["sta"]);
            objCharacter.Intellect.Base = Conversions.ToInteger(LevelStats.Rows[0]["inte"]);
            objCharacter.Spirit.Base = Conversions.ToInteger(LevelStats.Rows[0]["spi"]);
            objCharacter.Life.Base = Conversions.ToInteger(ClassLevelStats.Rows[0]["basehp"]);
            objCharacter.Life.Current = objCharacter.Life.Maximum;

            switch (objCharacter.ManaType)
            {
                case ManaTypes.TYPE_MANA:
                    objCharacter.Mana.Base = Conversions.ToInteger(ClassLevelStats.Rows[0]["basemana"]);
                    objCharacter.Mana.Current = objCharacter.Mana.Maximum;
                    break;

                case ManaTypes.TYPE_RAGE:
                    objCharacter.Rage.Base = Conversions.ToInteger(ClassLevelStats.Rows[0]["basemana"]);
                    objCharacter.Rage.Current = 0;
                    break;

                case ManaTypes.TYPE_ENERGY:
                    objCharacter.Energy.Base = Conversions.ToInteger(ClassLevelStats.Rows[0]["basemana"]);
                    objCharacter.Energy.Current = 0;
                    break;
            }

            objCharacter.Damage.Minimum = 5f;
            objCharacter.Damage.Maximum = 10f;

            IEnumerator enumerator = default;
            try
            {
                enumerator = CreateInfoSkills.Rows.GetEnumerator();
                while (enumerator.MoveNext())
                {
                    DataRow row = (DataRow)enumerator.Current;
                    objCharacter.LearnSkill(row.As<int>("Skill"), row.As<short>("SkillMin"), row.As<short>("SkillMax"));
                }
            }
            finally
            {
                if (enumerator is IDisposable)
                {
                    (enumerator as IDisposable).Dispose();
                }
            }

            var i = 0;
            do
            {
                if ((database.CharRaces[(int)objCharacter.Race].TaxiMask & (1 << i)) != 0)
                {
                    objCharacter.TaxiZones.Set(i + 1, value: true);
                }
                i++;
            }
            while (i <= 31);

            IEnumerator enumerator2 = default;
            try
            {
                enumerator2 = CreateInfoBars.Rows.GetEnumerator();
                while (enumerator2.MoveNext())
                {
                    DataRow row = (DataRow)enumerator2.Current;
                    if (Operators.ConditionalCompareObjectGreater(row["action"], 0, TextCompare: false))
                    {
                        var ButtonPos = row.As<int>("button");
                        objCharacter.ActionButtons[(byte)ButtonPos] = new WS_PlayerHelper.TActionButton(row.As<int>("action"), row.As<byte>("type"), 0);
                    }
                }
            }
            finally
            {
                if (enumerator2 is IDisposable)
                {
                    (enumerator2 as IDisposable).Dispose();
                }
            }
        }
    }

    public void CreateCharacterSpells(ref CharacterObject objCharacter)
    {
        DataTable CreateInfoSpells = new();
        worldDatabase.Query($"SELECT * FROM playercreateinfo_spell WHERE race = {(int)objCharacter.Race} AND class = {(int)objCharacter.Classe};", ref CreateInfoSpells);
        if (CreateInfoSpells.Rows.Count <= 0)
        {
            logger.LogError("No information found in playercreateinfo_spell table Race: {0}, Class: {1}", objCharacter.Race, objCharacter.Classe);
        }
        IEnumerator enumerator = default;
        try
        {
            enumerator = CreateInfoSpells.Rows.GetEnumerator();
            while (enumerator.MoveNext())
            {
                DataRow row = (DataRow)enumerator.Current;
                objCharacter.LearnSpell(row.As<int>("Spell"));
            }
        }
        finally
        {
            if (enumerator is IDisposable)
            {
                (enumerator as IDisposable).Dispose();
            }
        }
    }

    public void CreateCharacterItems(ref CharacterObject objCharacter)
    {
        DataTable CreateInfoItems = new();
        worldDatabase.Query($"SELECT * FROM playercreateinfo_item WHERE race = {(int)objCharacter.Race} AND class = {(int)objCharacter.Classe};", ref CreateInfoItems);
        if (CreateInfoItems.Rows.Count <= 0)
        {
            logger.LogError("No information found in playercreateinfo_item table for Race: {0}, Class: {1}", objCharacter.Race, objCharacter.Classe);
        }
        Dictionary<int, int> Items = new();
        List<int> Used = new();
        IEnumerator enumerator = default;
        try
        {
            enumerator = CreateInfoItems.Rows.GetEnumerator();
            while (enumerator.MoveNext())
            {
                DataRow row = (DataRow)enumerator.Current;
                Items.Add(row.As<int>("itemid"), row.As<int>("amount"));
            }
        }
        finally
        {
            if (enumerator is IDisposable)
            {
                (enumerator as IDisposable).Dispose();
            }
        }
        foreach (var Item2 in Items)
        {
            if (!worldState.ItemDatabase.ContainsKey(Item2.Key))
            {
                var newItem = itemInfoFactory.Create(Item2.Key);
            }
            if (worldState.ItemDatabase[Item2.Key].ContainerSlots <= 0)
            {
                continue;
            }
            var Slots2 = worldState.ItemDatabase[Item2.Key].GetSlots;
            var array = Slots2;
            foreach (var tmpSlot2 in array)
            {
                if (!objCharacter.Items.ContainsKey(tmpSlot2))
                {
                    objCharacter.ItemADD(Item2.Key, 0, tmpSlot2, Item2.Value);
                    Used.Add(Item2.Key);
                    break;
                }
            }
        }
        foreach (var Item in Items)
        {
            if (Used.Contains(Item.Key))
            {
                continue;
            }
            var Slots = worldState.ItemDatabase[Item.Key].GetSlots;
            var array2 = Slots;
            var num = 0;
            while (true)
            {
                if (num < array2.Length)
                {
                    var tmpSlot = array2[num];
                    if (!objCharacter.Items.ContainsKey(tmpSlot))
                    {
                        objCharacter.ItemADD(Item.Key, 0, tmpSlot, Item.Value);
                        break;
                    }
                    num = checked(num + 1);
                    continue;
                }
                objCharacter.ItemADD(Item.Key, byte.MaxValue, byte.MaxValue, Item.Value);
                break;
            }
        }
    }
}
