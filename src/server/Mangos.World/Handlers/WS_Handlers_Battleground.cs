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
using Mangos.World.DataStores;
using Mangos.World.Globals;
using Mangos.World.Network;
using Microsoft.Extensions.Logging;

namespace Mangos.World.Handlers;

public class WS_Handlers_Battleground
{
    private readonly ILogger<WS_Handlers_Battleground> logger;
    private readonly ICluster cluster;
    private readonly WorldState worldState;
    private readonly WS_DBCDatabase database;

    public WS_Handlers_Battleground(
        ILogger<WS_Handlers_Battleground> logger,
        ICluster cluster,
        WorldState worldState,
        WS_DBCDatabase database)
    {
        this.logger = logger;
        this.cluster = cluster;
        this.worldState = worldState;
        this.database = database;
    }

    public void On_CMSG_BATTLEMASTER_HELLO(ref Packets.PacketClass packet, ref WS_Network.ClientClass client)
    {
        if (checked(packet.Data.Length - 1) < 13)
        {
            return;
        }

        packet.GetInt16();
        var GUID = packet.GetUInt64();
        logger.LogDebug("[{0}:{1}] CMSG_BATTLEMASTER_HELLO [{2:X}]", client.IP, client.Port, GUID);
        if (!worldState.WorldCreatures.ContainsKey(GUID) || (worldState.WorldCreatures[GUID].CreatureInfo.cNpcFlags & 0x800) == 0 || !database.Battlemasters.ContainsKey(worldState.WorldCreatures[GUID].ID))
        {
            return;
        }
        var BGType = database.Battlemasters[worldState.WorldCreatures[GUID].ID];
        if (!database.Battlegrounds.ContainsKey(BGType))
        {
            return;
        }
        if (database.Battlegrounds[BGType].MinLevel > (uint)client.Character.Level || database.Battlegrounds[BGType].MaxLevel < (uint)client.Character.Level)
        {
            Globals.Functions.SendMessageNotification(ref client, "You don't meet Battleground level requirements");
            return;
        }
        Packets.PacketClass response = new(Opcodes.SMSG_BATTLEFIELD_LIST);
        try
        {
            response.AddUInt64(client.Character.GUID);
            response.AddInt32(BGType);
            response.AddInt8(0);
            var Battlegrounds = cluster.BattlefieldList(BGType);
            response.AddInt32(Battlegrounds.Count);
            foreach (var Instance in Battlegrounds)
            {
                response.AddInt32(Instance);
            }
            client.Send(ref response);
        }
        finally
        {
            response.Dispose();
        }
    }

    public void On_MSG_BATTLEGROUND_PLAYER_POSITIONS(ref Packets.PacketClass packet, ref WS_Network.ClientClass client)
    {
        packet.GetUInt32();
    }
}
