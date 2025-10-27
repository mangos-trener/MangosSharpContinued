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
using System;

namespace Mangos.Common.Globals;

public static class MangosGlobalConstants
{
    static MangosGlobalConstants()
    {
        movementOrTurningFlagsMask = movementFlagsMask | TurningFlagsMask;
        MAX_AURA_EFFECTs = MAX_AURA_EFFECTs_VISIBLE + MAX_AURA_EFFECTs_PASSIVE;
        MAX_AURA_EFFECT_FLAGs = MAX_AURA_EFFECTs_VISIBLE / 8;
        MAX_AURA_EFFECT_LEVELSs = MAX_AURA_EFFECTs_VISIBLE / 4;
        MAX_NEGATIVE_AURA_EFFECTs = MAX_AURA_EFFECTs_VISIBLE - MAX_POSITIVE_AURA_EFFECTs;
    }

    public static int RevisionDbCharactersVersion = 1;
    public static int RevisionDbCharactersStructure;
    public static int RevisionDbCharactersContent;

    public static int RevisionDbMangosVersion = 21;
    public static int RevisionDbMangosStructure = 7;
    public static int RevisionDbMangosContent = 77;

    public static int RevisionDbRealmVersion = 21;
    public static int RevisionDbRealmStructure = 2;
    public static int RevisionDbRealmContent = 1;

    public static int GROUP_SUBGROUPSIZE = 5;  // (MAX_RAID_SIZE / MAX_GROUP_SIZE)
    public static int GROUP_SIZE = 5;          // Normal Group Size/More then 5, it's a raid group
    public static int GROUP_RAIDSIZE = 40;     // Max Raid Size
    public static ulong GUID_ITEM = 0x4000000000000000UL;
    public static ulong GUID_CONTAINER = 0x4000000000000000UL;
    public static ulong GUID_PLAYER;
    public static ulong GUID_GAMEOBJECT = 0xF110000000000000UL;
    public static ulong GUID_TRANSPORT = 0xF120000000000000UL;
    public static ulong GUID_UNIT = 0xF130000000000000UL;
    public static ulong GUID_PET = 0xF140000000000000UL;
    public static ulong GUID_DYNAMICOBJECT = 0xF100000000000000UL;
    public static ulong GUID_CORPSE = 0xF101000000000000UL;
    public static ulong GUID_MO_TRANSPORT = 0x1FC0000000000000UL;
    public static uint GUID_MASK_LOW = 0xFFFFFFFFU;
    public static ulong GUID_MASK_HIGH = 0xFFFFFFFF00000000UL;
    public static float DEFAULT_DISTANCE_VISIBLE = 155.8f;
    public static float DEFAULT_DISTANCE_DETECTION = 7f;

    // TODO: Is this correct? The amount of time since last pvp action until you go out of combat again
    public static int DEFAULT_PVP_COMBAT_TIME = 6000; // 6 seconds

    public static int DEFAULT_LOCK_TIMEOUT = 2000;
    public static int DEFAULT_INSTANCE_EXPIRE_TIME = 3600;              // 1 hour
    public static int DEFAULT_BATTLEFIELD_EXPIRE_TIME = 3600 * 24;      // 24 hours
    public static bool[] SERVER_CONFIG_DISABLED_CLASSES = { false, false, false, false, false, false, false, false, false, true, false };
    public static bool[] SERVER_CONFIG_DISABLED_RACES = { false, false, false, false, false, false, false, false, true, false, false };
    public static float UNIT_NORMAL_WALK_SPEED = 2.5f;
    public static float UNIT_NORMAL_RUN_SPEED = 7.0f;
    public static float UNIT_NORMAL_SWIM_SPEED = 4.722222f;
    public static float UNIT_NORMAL_SWIM_BACK_SPEED = 2.5f;
    public static float UNIT_NORMAL_WALK_BACK_SPEED = 4.5f;
    public static float UNIT_NORMAL_TURN_RATE = (float)Math.PI;
    public static float UNIT_NORMAL_TAXI_SPEED = 32.0f;
    public static int PLAYER_VISIBLE_ITEM_SIZE = 12;
    public static int PLAYER_SKILL_INFO_SIZE = 384 - 1;
    public static int PLAYER_EXPLORED_ZONES_SIZE = 64 - 1;
    public static int FIELD_MASK_SIZE_PLAYER = ((int)EPlayerFields.PLAYER_END + 32) / 32 * 32;
    public static int FIELD_MASK_SIZE_UNIT = ((int)EUnitFields.UNIT_END + 32) / 32 * 32;
    public static int FIELD_MASK_SIZE_GAMEOBJECT = ((int)EGameObjectFields.GAMEOBJECT_END + 32) / 32 * 32;
    public static int FIELD_MASK_SIZE_DYNAMICOBJECT = ((int)EDynamicObjectFields.DYNAMICOBJECT_END + 32) / 32 * 32;
    public static int FIELD_MASK_SIZE_ITEM = ((int)EContainerFields.CONTAINER_END + 32) / 32 * 32;
    public static int FIELD_MASK_SIZE_CORPSE = ((int)ECorpseFields.CORPSE_END + 32) / 32 * 32;
    public static string[] WorldServerStatus = { "ONLINE/G", "ONLINE/R", "OFFLINE " };
    // Public ConsoleColor As New ConsoleColor
    // 1.12.1 - 5875
    // 1.12.2 - 6005
    // 1.12.3 - 6141

    // New Auto Detection Build
    public static int Required_Build_1_12_1 = 5875;

    public static int Required_Build_1_12_2 = 6005;
    public static int Required_Build_1_12_3 = 6141;
    public static int ConnectionSleepTime = 100;
    public static int GUILD_RANK_MAX = 9; // Max Ranks Per Guild
    public static int GUILD_RANK_MIN = 5; // Min Ranks Per Guild
    public static string GOSSIP_TEXT_BANK = "The Bank";
    public static string GOSSIP_TEXT_WINDRIDER = "Wind rider master";
    public static string GOSSIP_TEXT_GRYPHON = "Gryphon Master";
    public static string GOSSIP_TEXT_BATHANDLER = "Bat Handler";
    public static string GOSSIP_TEXT_HIPPOGRYPH = "Hippogryph Master";
    public static string GOSSIP_TEXT_FLIGHTMASTER = "Flight Master";
    public static string GOSSIP_TEXT_AUCTIONHOUSE = "Auction House";
    public static string GOSSIP_TEXT_GUILDMASTER = "Guild Master";
    public static string GOSSIP_TEXT_INN = "The Inn";
    public static string GOSSIP_TEXT_MAILBOX = "Mailbox";
    public static string GOSSIP_TEXT_STABLEMASTER = "Stable Master";
    public static string GOSSIP_TEXT_WEAPONMASTER = "Weapons Trainer";
    public static string GOSSIP_TEXT_BATTLEMASTER = "Battlemaster";
    public static string GOSSIP_TEXT_CLASSTRAINER = "Class Trainer";
    public static string GOSSIP_TEXT_PROFTRAINER = "Profession Trainer";
    public static string GOSSIP_TEXT_OFFICERS = "The officers` lounge";
    public static string GOSSIP_TEXT_ALTERACVALLEY = "Alterac Valley";
    public static string GOSSIP_TEXT_ARATHIBASIN = "Arathi Basin";
    public static string GOSSIP_TEXT_WARSONGULCH = "Warsong Gulch";
    public static string GOSSIP_TEXT_IRONFORGE_BANK = "Bank of Ironforge";
    public static string GOSSIP_TEXT_STORMWIND_BANK = "Bank of Stormwind";
    public static string GOSSIP_TEXT_DEEPRUNTRAM = "Deeprun Tram";
    public static string GOSSIP_TEXT_ZEPPLINMASTER = "Zeppelin master";
    public static string GOSSIP_TEXT_FERRY = "Rut'theran Ferry";
    public static string GOSSIP_TEXT_DRUID = "Druid";
    public static string GOSSIP_TEXT_HUNTER = "Hunter";
    public static string GOSSIP_TEXT_PRIEST = "Priest";
    public static string GOSSIP_TEXT_ROGUE = "Rogue";
    public static string GOSSIP_TEXT_WARRIOR = "Warrior";
    public static string GOSSIP_TEXT_PALADIN = "Paladin";
    public static string GOSSIP_TEXT_SHAMAN = "Shaman";
    public static string GOSSIP_TEXT_MAGE = "Mage";
    public static string GOSSIP_TEXT_WARLOCK = "Warlock";
    public static string GOSSIP_TEXT_ALCHEMY = "Alchemy";
    public static string GOSSIP_TEXT_BLACKSMITHING = "Blacksmithing";
    public static string GOSSIP_TEXT_COOKING = "Cooking";
    public static string GOSSIP_TEXT_ENCHANTING = "Enchanting";
    public static string GOSSIP_TEXT_ENGINEERING = "Engineering";
    public static string GOSSIP_TEXT_FIRSTAID = "First Aid";
    public static string GOSSIP_TEXT_HERBALISM = "Herbalism";
    public static string GOSSIP_TEXT_LEATHERWORKING = "Leatherworking";
    public static string GOSSIP_TEXT_POISONS = "Poisons";
    public static string GOSSIP_TEXT_TAILORING = "Tailoring";
    public static string GOSSIP_TEXT_MINING = "Mining";
    public static string GOSSIP_TEXT_FISHING = "Fishing";
    public static string GOSSIP_TEXT_SKINNING = "Skinning";

    // VMAPS
    public static string VMAP_MAGIC = "VMAP_2.0";

    public static float VMAP_MAX_CAN_FALL_DISTANCE = 10.0f;
    public static float VMAP_INVALID_HEIGHT = -100000.0f; // for check
    public static float VMAP_INVALID_HEIGHT_VALUE = -200000.0f; // real assigned value in unknown height case

    // MAPS
    public static float SIZE = 533.3333f;

    public static int RESOLUTION_WATER = 128 - 1;
    public static int RESOLUTION_FLAGS = 16 - 1;
    public static int RESOLUTION_TERRAIN = 16 - 1;
    public static int groundFlagsMask = unchecked((int)(0xFFFFFFFF & (int)~(MovementFlags.MOVEMENTFLAG_LEFT | MovementFlags.MOVEMENTFLAG_RIGHT | MovementFlags.MOVEMENTFLAG_BACKWARD | MovementFlags.MOVEMENTFLAG_FORWARD | MovementFlags.MOVEMENTFLAG_WALK)));
    public static int movementFlagsMask = (int)(MovementFlags.MOVEMENTFLAG_FORWARD | MovementFlags.MOVEMENTFLAG_BACKWARD | MovementFlags.MOVEMENTFLAG_STRAFE_LEFT | MovementFlags.MOVEMENTFLAG_STRAFE_RIGHT | MovementFlags.MOVEMENTFLAG_PITCH_UP | MovementFlags.MOVEMENTFLAG_PITCH_DOWN | MovementFlags.MOVEMENTFLAG_JUMPING | MovementFlags.MOVEMENTFLAG_FALLING | MovementFlags.MOVEMENTFLAG_SWIMMING | MovementFlags.MOVEMENTFLAG_SPLINE);
    public static int TurningFlagsMask = (int)(MovementFlags.MOVEMENTFLAG_LEFT | MovementFlags.MOVEMENTFLAG_RIGHT);
    public static int movementOrTurningFlagsMask;
    public static byte ITEM_SLOT_NULL = 255;
    public static long ITEM_BAG_NULL = -1;
    public static int PETITION_GUILD_PRICE = 1000;
    public static int PETITION_GUILD = 5863;       // Guild Charter, ItemFlags = &H2000
    public static int GUILD_TABARD_ITEM = 5976;
    public static int CREATURE_MAX_SPELLS = 4;
    public static int MAX_OWNER_DIS = 100;
    public static int SPELL_DURATION_INFINITE = -1;
    public static int MAX_AURA_EFFECTs_VISIBLE = 48;                  // 48 AuraSlots (32 buff, 16 debuff)
    public static int MAX_AURA_EFFECTs_PASSIVE = 192;
    public static int MAX_AURA_EFFECTs;
    public static int MAX_AURA_EFFECT_FLAGs;
    public static int MAX_AURA_EFFECT_LEVELSs;
    public static int MAX_POSITIVE_AURA_EFFECTs = 32;
    public static int MAX_NEGATIVE_AURA_EFFECTs;
    public static uint UINT32_MAX = 0xFFFFFFFF;
    public static int UINT32_MIN;
    public static long MpqId = 441536589L;
    public static long MpqHeaderSize = 32L;

    // TODO: read these values from config
    public static bool GlobalAuctionEnabled = false;
    public static int SaveTimer = 120000;
    public static int WeatherTimer = 600000;
}
