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

using Mangos.Common.Globals;
using Mangos.Common.Legacy;
using Mangos.Common.Legacy.Databases;
using Mangos.Configuration;
using Mangos.World.AI;
using Mangos.World.DataStores;
using Mangos.World.Globals;
using Mangos.World.Handlers;
using Mangos.World.Loots;
using Mangos.World.Maps;
using Mangos.World.Network;
using Mangos.World.Objects.Factories;
using Mangos.World.Player;
using Mangos.World.Services;
using Mangos.World.Spells;
using Microsoft.Extensions.Logging;
using System.Collections;
using System.Data;

namespace Mangos.World.Objects;

public class WS_Pets
{
    public class PetObject : WS_Creatures.CreatureObject
    {
        public string PetName;

        public bool Renamed;

        public WS_Base.BaseUnit Owner;

        public bool FollowOwner;

        public byte Command;

        public byte State;

        public ArrayList Spells;

        public int XP;

        public PetObject(
            ILogger<PetObject> logger,
            WorldState worldState,
            MangosConfiguration configuration,
            WS_DBCDatabase database,
            CharacterDatabase characterDatabase,
            WS_Maps maps,
            WS_Creatures creatures,
            WS_Combat combat,
            WS_Loot loot,
            WS_Spells spells,
            WS_Network network,
            IMapTileLoader tileLoader,
            CreatureInfoFactory creatureInfoFactory,
            ulong GUID_,
            int CreatureID)
            : base(logger, null, worldState, configuration, database, maps, creatures, combat, loot, network, tileLoader, lootObjectFactory: null, creatureInfoFactory: creatureInfoFactory, baseActiveSpellFactory: null, spellTargetsFactory: null, castSpellParametersFactory: null, updateClassFactory: null, critterAiFactory: null, defaultAiFactory: null, guardAiFactory: null, guardWaypointAiFactory: null, petAiFactory: null, standStillAiFactory: null, waypointAiFactory: null, GUID_: GUID_, ID_: CreatureID)
        {
            PetName = "";
            Renamed = false;
            Owner = null;
            FollowOwner = true;
            Command = 7;
            State = 6;
            XP = 0;
        }

        public void Spawn()
        {
            AddToWorld();

            if (Owner is CharacterObject @object)
            {
                @object.GroupUpdateFlag |= 0x7FC00u;
            }

            ref var owner = ref Owner;
            CharacterObject Caster = (CharacterObject)owner;
            WS_Base.BaseUnit Pet = this;

            SendPetInitialize(ref Caster, ref Pet);
            owner = Caster;
        }

        public void Hide()
        {
            RemoveFromWorld();
            if (Owner is CharacterObject @object)
            {
                @object.GroupUpdateFlag |= 0x7FC00u;
                Packets.PacketClass packet = new(Opcodes.SMSG_PET_SPELLS);
                packet.AddUInt64(0uL);
                @object.client.Send(ref packet);
                packet.Dispose();
            }
        }
    }

    public class PetAI : WS_Creatures_AI.DefaultAI
    {
        public PetAI(ILogger<PetAI> logger, WorldState worldState, ref WS_Creatures.CreatureObject Creature, WS_Maps maps, WS_Loot loot, WS_Creatures creatures, WS_Combat combat)
            : base(logger, worldState, ref Creature, maps, loot, creatures, combat)
        {
            AllowedMove = false;
        }
    }

    public int[] LevelUpLoyalty;

    public int[] LevelStartLoyalty;
    private readonly ILogger<WS_Pets> logger;
    private readonly WorldState worldState;
    private readonly CharacterDatabase characterDatabase;
    private readonly PetObjectFactory petObjectFactory;

    public WS_Pets(ILogger<WS_Pets> logger, WorldState worldState, CharacterDatabase characterDatabase, PetObjectFactory petObjectFactory)
    {
        LevelUpLoyalty = new int[7];
        LevelStartLoyalty = new int[7];
        this.logger = logger;
        this.worldState = worldState;
        this.characterDatabase = characterDatabase;
        this.petObjectFactory = petObjectFactory;
    }

    public void InitializeLevelUpLoyalty()
    {
        LevelUpLoyalty[0] = 0;
        LevelUpLoyalty[1] = 5500;
        LevelUpLoyalty[2] = 11500;
        LevelUpLoyalty[3] = 17000;
        LevelUpLoyalty[4] = 23500;
        LevelUpLoyalty[5] = 31000;
        LevelUpLoyalty[6] = 39500;
    }

    public void InitializeLevelStartLoyalty()
    {
        LevelStartLoyalty[0] = 0;
        LevelStartLoyalty[1] = 2000;
        LevelStartLoyalty[2] = 4500;
        LevelStartLoyalty[3] = 7000;
        LevelStartLoyalty[4] = 10000;
        LevelStartLoyalty[5] = 13500;
        LevelStartLoyalty[6] = 17500;
    }

    public void On_CMSG_PET_NAME_QUERY(ref Packets.PacketClass packet, ref WS_Network.ClientClass client)
    {
        packet.GetInt16();
        var PetNumber = packet.GetInt32();
        var PetGUID = packet.GetUInt64();
        logger.LogDebug("CMSG_PET_NAME_QUERY [Number={0} GUID={1:X}", PetNumber, PetGUID);
        SendPetNameQuery(ref client, PetGUID, PetNumber);
    }

    public void On_CMSG_REQUEST_PET_INFO(ref Packets.PacketClass packet, ref WS_Network.ClientClass client)
    {
        logger.LogDebug("CMSG_REQUEST_PET_INFO");
        Packets.DumpPacket(logger, packet.Data, client, 6);
    }

    public void On_CMSG_PET_ACTION(ref Packets.PacketClass packet, ref WS_Network.ClientClass client)
    {
        packet.GetInt16();
        var PetGUID = packet.GetUInt64();
        var SpellID = packet.GetUInt16();
        var SpellFlag = packet.GetUInt16();
        var TargetGUID = packet.GetUInt64();
        logger.LogDebug("CMSG_PET_ACTION [GUID={0:X} Spell={1} Flag={2:X} Target={3:X}]", PetGUID, SpellID, SpellFlag, TargetGUID);
    }

    public void On_CMSG_PET_CANCEL_AURA(ref Packets.PacketClass packet, ref WS_Network.ClientClass client)
    {
        logger.LogDebug("CMSG_PET_CANCEL_AURA");
        Packets.DumpPacket(logger, packet.Data, client, 6);
    }

    public void On_CMSG_PET_ABANDON(ref Packets.PacketClass packet, ref WS_Network.ClientClass client)
    {
        packet.GetInt16();
        var PetGUID = packet.GetUInt64();
        logger.LogDebug("CMSG_PET_ABANDON [GUID={0:X}]", PetGUID);
    }

    public void On_CMSG_PET_RENAME(ref Packets.PacketClass packet, ref WS_Network.ClientClass client)
    {
        packet.GetInt16();
        var PetGUID = packet.GetUInt64();
        var PetName = packet.GetString();
        logger.LogDebug("CMSG_PET_RENAME [GUID={0:X} Name={1}]", PetGUID, PetName);
    }

    public void On_CMSG_PET_SET_ACTION(ref Packets.PacketClass packet, ref WS_Network.ClientClass client)
    {
        packet.GetInt16();
        var PetGUID = packet.GetUInt64();
        var Position = packet.GetInt32();
        var SpellID = packet.GetUInt16();
        var ActionState = packet.GetInt16();
        logger.LogDebug("CMSG_PET_SET_ACTION [GUID={0:X} Pos={1} Spell={2} Action={3}]", PetGUID, Position, SpellID, ActionState);
    }

    public void On_CMSG_PET_SPELL_AUTOCAST(ref Packets.PacketClass packet, ref WS_Network.ClientClass client)
    {
        logger.LogDebug("CMSG_PET_SPELL_AUTOCAST");
        Packets.DumpPacket(logger, packet.Data, client, 6);
    }

    public void On_CMSG_PET_STOP_ATTACK(ref Packets.PacketClass packet, ref WS_Network.ClientClass client)
    {
        logger.LogDebug("CMSG_PET_STOP_ATTACK");
        Packets.DumpPacket(logger, packet.Data, client, 6);
    }

    public void On_CMSG_PET_UNLEARN(ref Packets.PacketClass packet, ref WS_Network.ClientClass client)
    {
        logger.LogDebug("CMSG_PET_UNLEARN");
        Packets.DumpPacket(logger, packet.Data, client, 6);
    }

    public void SendPetNameQuery(ref WS_Network.ClientClass client, ulong PetGUID, int PetNumber)
    {
        if (worldState.WorldCreatures.ContainsKey(PetGUID) && worldState.WorldCreatures[PetGUID] is PetObject @object)
        {
            Packets.PacketClass response = new(Opcodes.SMSG_PET_NAME_QUERY_RESPONSE);
            response.AddInt32(PetNumber);
            response.AddString(@object.PetName);
            response.AddInt32(LegacyNativeMethods.TimeGetTime(""));
            client.Send(ref response);
            response.Dispose();
        }
    }

    public void LoadPet(ref CharacterObject objCharacter)
    {
        if (objCharacter.Pet != null)
        {
            return;
        }

        DataTable PetQuery = new();
        characterDatabase.Query($"SELECT * FROM character_pet WHERE owner = '{objCharacter.GUID}';", ref PetQuery);

        if (PetQuery.Rows.Count != 0)
        {
            var row = PetQuery.Rows[0];
            objCharacter.Pet = petObjectFactory.Create(checked(row.As<ulong>("id") + MangosGlobalConstants.GUID_PET), row.As<int>("entry"));
            objCharacter.Pet.Renamed = row.As<byte>("renamed") != 0;
            objCharacter.Pet.Faction = objCharacter.Faction;
            objCharacter.Pet.positionX = objCharacter.positionX;
            objCharacter.Pet.positionY = objCharacter.positionY;
            objCharacter.Pet.positionZ = objCharacter.positionZ;
            objCharacter.Pet.MapID = objCharacter.MapID;
            logger.LogDebug("Loaded pet [{0}] for character [{1}].", objCharacter.Pet.GUID, objCharacter.GUID);
        }
    }

    public static void SendPetInitialize(ref CharacterObject Caster, ref WS_Base.BaseUnit Pet)
    {
        if (Pet is WS_Creatures.CreatureObject or CharacterObject)
        {
        }
        ushort Command = 7;
        ushort State = 6;
        byte AddList = 0;
        if (Pet is PetObject @object)
        {
            Command = @object.Command;
            State = @object.State;
        }
        Packets.PacketClass packet = new(Opcodes.SMSG_PET_SPELLS);
        packet.AddUInt64(Pet.GUID);
        packet.AddInt32(0);
        packet.AddInt32(16842752);
        packet.AddInt16(2);
        checked
        {
            packet.AddInt16((short)unchecked((ushort)(Command << 8)));
            packet.AddInt16(1);
            packet.AddInt16((short)unchecked((ushort)(Command << 8)));
            packet.AddInt16(0);
            packet.AddInt16((short)unchecked((ushort)(Command << 8)));
            var i = 0;
            do
            {
                packet.AddInt16(0);
                packet.AddInt16(0);
                i++;
            }
            while (i <= 3);
            packet.AddInt16(2);
            packet.AddInt16((short)unchecked((ushort)(State << 8)));
            packet.AddInt16(1);
            packet.AddInt16((short)unchecked((ushort)(State << 8)));
            packet.AddInt16(0);
            packet.AddInt16((short)unchecked((ushort)(State << 8)));
            packet.AddInt8(AddList);
            packet.AddInt8(1);
            packet.AddInt32(24592);
            packet.AddInt32(0);
            packet.AddInt32(0);
            packet.AddInt16(0);
            Caster.client.Send(ref packet);
            packet.Dispose();
        }
    }
}
