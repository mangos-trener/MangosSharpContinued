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

using Mangos.Cluster.Globals;
using Mangos.Cluster.Interfaces;
using Mangos.Cluster.Network;
using Mangos.Common.Enums.Global;
using Mangos.Common.Globals;
using Microsoft.VisualBasic;

namespace Mangos.Cluster.Handlers;

public class WcHandlers : IPacketHandlerRegistrar
{
    private readonly IClusterContext cluster;
    private readonly WcHandlersMisc misc;
    private readonly WcHandlersAuth auth;
    private readonly WcHandlersMovement movement;
    private readonly WcHandlersSocial social;
    private readonly WcHandlersTickets tickets;
    private readonly WcHandlersBattleground battleground;
    private readonly WcHandlersGroup group;
    private readonly WcHandlersGuild guild;
    private readonly WcHandlersChat chat;
    private readonly Packets packets;

    public WcHandlers(IClusterContext cluster,
                      WcHandlersMisc misc,
                      WcHandlersAuth auth,
                      WcHandlersMovement movement,
                      WcHandlersSocial social,
                      WcHandlersTickets tickets,
                      WcHandlersBattleground battleground,
                      WcHandlersGroup group,
                      WcHandlersGuild guild,
                      WcHandlersChat chat,
                      Packets packets)
    {
        this.cluster = cluster;
        this.misc = misc;
        this.auth = auth;
        this.movement = movement;
        this.social = social;
        this.tickets = tickets;
        this.battleground = battleground;
        this.group = group;
        this.guild = guild;
        this.chat = chat;
        this.packets = packets;

        InitializePacketHandlers();
    }

    public void OnUnhandledPacket(PacketClass packet, ClientClass client)
    {
        cluster.Log.WriteLine(LogType.WARNING, "[{0}:{1}] {2} [Unhandled Packet]", client.IP, client.Port, packet.OpCode);
    }

    public void OnClusterPacket(PacketClass packet, ClientClass client)
    {
        cluster.Log.WriteLine(LogType.WARNING, "[{0}:{1}] {2} [Redirected Packet]", client.IP, client.Port, packet.OpCode);
        if (client.Character is null || client.Character.IsInWorld == false)
        {
            cluster.Log.WriteLine(LogType.WARNING, "[{0}:{1}] Unknown Opcode 0x{2:X} [{2}], DataLen={4}", client.IP, client.Port, packet.OpCode, Constants.vbCrLf, packet.Length);
            packets.DumpPacket(packet.Data, client);
        }
        else
        {
            client.Character.GetWorld.ClientPacket(client.Index, packet.Data);
        }
    }

    public void InitializePacketHandlers()
    {
        {
            // NOTE: These opcodes are not used in any way
            // _WorldCluster.PacketHandlers[OPCODES.CMSG_MOVE_TIME_SKIPPED] = AddressOf On_CMSG_MOVE_TIME_SKIPPED
            cluster.GetPacketHandlers()[Opcodes.CMSG_NEXT_CINEMATIC_CAMERA] = misc.On_CMSG_NEXT_CINEMATIC_CAMERA;
            cluster.GetPacketHandlers()[Opcodes.CMSG_COMPLETE_CINEMATIC] = misc.On_CMSG_COMPLETE_CINEMATIC;
            cluster.GetPacketHandlers()[Opcodes.CMSG_UPDATE_ACCOUNT_DATA] = auth.On_CMSG_UPDATE_ACCOUNT_DATA;
            cluster.GetPacketHandlers()[Opcodes.CMSG_REQUEST_ACCOUNT_DATA] = auth.On_CMSG_REQUEST_ACCOUNT_DATA;

            // NOTE: These opcodes are only partialy handled by Cluster and must be handled by WorldServer
            cluster.GetPacketHandlers()[Opcodes.MSG_MOVE_HEARTBEAT] = movement.On_MSG_MOVE_HEARTBEAT;
            cluster.GetPacketHandlers()[Opcodes.MSG_MOVE_START_BACKWARD] = movement.On_MSG_START_BACKWARD;
            cluster.GetPacketHandlers()[Opcodes.MSG_MOVE_START_FORWARD] = movement.On_MSG_MOVE_START_FORWARD;
            cluster.GetPacketHandlers()[Opcodes.MSG_MOVE_START_PITCH_DOWN] = movement.On_MSG_MOVE_START_PITCH_DOWN;
            cluster.GetPacketHandlers()[Opcodes.MSG_MOVE_START_PITCH_UP] = movement.On_MSG_MOVE_START_PITCH_UP;
            cluster.GetPacketHandlers()[Opcodes.MSG_MOVE_START_STRAFE_LEFT] = movement.On_MSG_MOVE_STRAFE_LEFT;
            cluster.GetPacketHandlers()[Opcodes.MSG_MOVE_START_STRAFE_RIGHT] = movement.On_MSG_MOVE_STRAFE_LEFT;
            cluster.GetPacketHandlers()[Opcodes.MSG_MOVE_START_SWIM] = movement.On_MSG_MOVE_START_SWIM;
            cluster.GetPacketHandlers()[Opcodes.MSG_MOVE_START_TURN_LEFT] = movement.On_MSG_MOVE_START_TURN_LEFT;
            cluster.GetPacketHandlers()[Opcodes.MSG_MOVE_START_TURN_RIGHT] = movement.On_MSG_MOVE_START_TURN_RIGHT;
            cluster.GetPacketHandlers()[Opcodes.MSG_MOVE_STOP] = movement.On_MSG_MOVE_STOP;
            cluster.GetPacketHandlers()[Opcodes.MSG_MOVE_STOP_PITCH] = movement.On_MSG_MOVE_STOP_PITCH;
            cluster.GetPacketHandlers()[Opcodes.MSG_MOVE_STOP_STRAFE] = movement.On_MSG_MOVE_STOP_STRAFE;
            cluster.GetPacketHandlers()[Opcodes.MSG_MOVE_STOP_SWIM] = movement.On_MSG_MOVE_STOP_SWIM;
            cluster.GetPacketHandlers()[Opcodes.MSG_MOVE_STOP_TURN] = movement.On_MSG_MOVE_STOP_TURN;
            cluster.GetPacketHandlers()[Opcodes.MSG_MOVE_SET_FACING] = movement.On_MSG_MOVE_SET_FACING;
            cluster.GetPacketHandlers()[Opcodes.CMSG_CANCEL_TRADE] = misc.On_CMSG_CANCEL_TRADE;
            cluster.GetPacketHandlers()[Opcodes.CMSG_LOGOUT_CANCEL] = misc.On_CMSG_LOGOUT_CANCEL;

            // NOTE: These opcodes below must be exluded form WorldServer
            cluster.GetPacketHandlers()[Opcodes.CMSG_PING] = auth.On_CMSG_PING;
            cluster.GetPacketHandlers()[Opcodes.CMSG_AUTH_SESSION] = auth.On_CMSG_AUTH_SESSION;
            cluster.GetPacketHandlers()[Opcodes.CMSG_CHAR_ENUM] = auth.On_CMSG_CHAR_ENUM;
            cluster.GetPacketHandlers()[Opcodes.CMSG_CHAR_CREATE] = auth.On_CMSG_CHAR_CREATE;
            cluster.GetPacketHandlers()[Opcodes.CMSG_CHAR_DELETE] = auth.On_CMSG_CHAR_DELETE;
            cluster.GetPacketHandlers()[Opcodes.CMSG_CHAR_RENAME] = auth.On_CMSG_CHAR_RENAME;
            cluster.GetPacketHandlers()[Opcodes.CMSG_PLAYER_LOGIN] = auth.On_CMSG_PLAYER_LOGIN;
            cluster.GetPacketHandlers()[Opcodes.CMSG_PLAYER_LOGOUT] = auth.On_CMSG_PLAYER_LOGOUT;
            cluster.GetPacketHandlers()[Opcodes.MSG_MOVE_WORLDPORT_ACK] = auth.On_MSG_MOVE_WORLDPORT_ACK;
            cluster.GetPacketHandlers()[Opcodes.CMSG_QUERY_TIME] = misc.On_CMSG_QUERY_TIME;
            cluster.GetPacketHandlers()[Opcodes.CMSG_INSPECT] = misc.On_CMSG_INSPECT;
            cluster.GetPacketHandlers()[Opcodes.CMSG_WHO] = social.On_CMSG_WHO;
            cluster.GetPacketHandlers()[Opcodes.CMSG_WHOIS] = tickets.On_CMSG_WHOIS;
            cluster.GetPacketHandlers()[Opcodes.CMSG_PLAYED_TIME] = misc.On_CMSG_PLAYED_TIME;
            cluster.GetPacketHandlers()[Opcodes.CMSG_NAME_QUERY] = misc.On_CMSG_NAME_QUERY;
            cluster.GetPacketHandlers()[Opcodes.CMSG_BUG] = tickets.On_CMSG_BUG;
            cluster.GetPacketHandlers()[Opcodes.CMSG_GMTICKET_GETTICKET] = tickets.On_CMSG_GMTICKET_GETTICKET;
            cluster.GetPacketHandlers()[Opcodes.CMSG_GMTICKET_CREATE] = tickets.On_CMSG_GMTICKET_CREATE;
            cluster.GetPacketHandlers()[Opcodes.CMSG_GMTICKET_SYSTEMSTATUS] = tickets.On_CMSG_GMTICKET_SYSTEMSTATUS;
            cluster.GetPacketHandlers()[Opcodes.CMSG_GMTICKET_DELETETICKET] = tickets.On_CMSG_GMTICKET_DELETETICKET;
            cluster.GetPacketHandlers()[Opcodes.CMSG_GMTICKET_UPDATETEXT] = tickets.On_CMSG_GMTICKET_UPDATETEXT;
            cluster.GetPacketHandlers()[Opcodes.CMSG_BATTLEMASTER_JOIN] = battleground.On_CMSG_BATTLEMASTER_JOIN;
            cluster.GetPacketHandlers()[Opcodes.CMSG_BATTLEFIELD_PORT] = battleground.On_CMSG_BATTLEFIELD_PORT;
            cluster.GetPacketHandlers()[Opcodes.CMSG_LEAVE_BATTLEFIELD] = battleground.On_CMSG_LEAVE_BATTLEFIELD;
            cluster.GetPacketHandlers()[Opcodes.MSG_BATTLEGROUND_PLAYER_POSITIONS] = battleground.On_MSG_BATTLEGROUND_PLAYER_POSITIONS;
            cluster.GetPacketHandlers()[Opcodes.CMSG_FRIEND_LIST] = social.On_CMSG_FRIEND_LIST;
            cluster.GetPacketHandlers()[Opcodes.CMSG_ADD_FRIEND] = social.On_CMSG_ADD_FRIEND;
            cluster.GetPacketHandlers()[Opcodes.CMSG_ADD_IGNORE] = social.On_CMSG_ADD_IGNORE;
            cluster.GetPacketHandlers()[Opcodes.CMSG_DEL_FRIEND] = social.On_CMSG_DEL_FRIEND;
            cluster.GetPacketHandlers()[Opcodes.CMSG_DEL_IGNORE] = social.On_CMSG_DEL_IGNORE;
            cluster.GetPacketHandlers()[Opcodes.CMSG_REQUEST_RAID_INFO] = group.On_CMSG_REQUEST_RAID_INFO;
            cluster.GetPacketHandlers()[Opcodes.CMSG_GROUP_INVITE] = group.On_CMSG_GROUP_INVITE;
            cluster.GetPacketHandlers()[Opcodes.CMSG_GROUP_CANCEL] = group.On_CMSG_GROUP_CANCEL;
            cluster.GetPacketHandlers()[Opcodes.CMSG_GROUP_ACCEPT] = group.On_CMSG_GROUP_ACCEPT;
            cluster.GetPacketHandlers()[Opcodes.CMSG_GROUP_DECLINE] = group.On_CMSG_GROUP_DECLINE;
            cluster.GetPacketHandlers()[Opcodes.CMSG_GROUP_UNINVITE] = group.On_CMSG_GROUP_UNINVITE;
            cluster.GetPacketHandlers()[Opcodes.CMSG_GROUP_UNINVITE_GUID] = group.On_CMSG_GROUP_UNINVITE_GUID;
            cluster.GetPacketHandlers()[Opcodes.CMSG_GROUP_DISBAND] = group.On_CMSG_GROUP_DISBAND;
            cluster.GetPacketHandlers()[Opcodes.CMSG_GROUP_RAID_CONVERT] = group.On_CMSG_GROUP_RAID_CONVERT;
            cluster.GetPacketHandlers()[Opcodes.CMSG_GROUP_SET_LEADER] = group.On_CMSG_GROUP_SET_LEADER;
            cluster.GetPacketHandlers()[Opcodes.CMSG_GROUP_CHANGE_SUB_GROUP] = group.On_CMSG_GROUP_CHANGE_SUB_GROUP;
            cluster.GetPacketHandlers()[Opcodes.CMSG_GROUP_SWAP_SUB_GROUP] = group.On_CMSG_GROUP_SWAP_SUB_GROUP;
            cluster.GetPacketHandlers()[Opcodes.CMSG_LOOT_METHOD] = group.On_CMSG_LOOT_METHOD;
            cluster.GetPacketHandlers()[Opcodes.MSG_MINIMAP_PING] = group.On_MSG_MINIMAP_PING;
            cluster.GetPacketHandlers()[Opcodes.MSG_RANDOM_ROLL] = group.On_MSG_RANDOM_ROLL;
            cluster.GetPacketHandlers()[Opcodes.MSG_RAID_READY_CHECK] = group.On_MSG_RAID_READY_CHECK;
            cluster.GetPacketHandlers()[Opcodes.MSG_RAID_ICON_TARGET] = group.On_MSG_RAID_ICON_TARGET;
            cluster.GetPacketHandlers()[Opcodes.CMSG_REQUEST_PARTY_MEMBER_STATS] = group.On_CMSG_REQUEST_PARTY_MEMBER_STATS;
            cluster.GetPacketHandlers()[Opcodes.CMSG_TURN_IN_PETITION] = guild.On_CMSG_TURN_IN_PETITION;
            cluster.GetPacketHandlers()[Opcodes.CMSG_GUILD_QUERY] = guild.On_CMSG_GUILD_QUERY;
            cluster.GetPacketHandlers()[Opcodes.CMSG_GUILD_CREATE] = guild.On_CMSG_GUILD_CREATE;
            cluster.GetPacketHandlers()[Opcodes.CMSG_GUILD_DISBAND] = guild.On_CMSG_GUILD_DISBAND;
            cluster.GetPacketHandlers()[Opcodes.CMSG_GUILD_ROSTER] = guild.On_CMSG_GUILD_ROSTER;
            cluster.GetPacketHandlers()[Opcodes.CMSG_GUILD_INFO] = guild.On_CMSG_GUILD_INFO;
            cluster.GetPacketHandlers()[Opcodes.CMSG_GUILD_RANK] = guild.On_CMSG_GUILD_RANK;
            cluster.GetPacketHandlers()[Opcodes.CMSG_GUILD_ADD_RANK] = guild.On_CMSG_GUILD_ADD_RANK;
            cluster.GetPacketHandlers()[Opcodes.CMSG_GUILD_DEL_RANK] = guild.On_CMSG_GUILD_DEL_RANK;
            cluster.GetPacketHandlers()[Opcodes.CMSG_GUILD_PROMOTE] = guild.On_CMSG_GUILD_PROMOTE;
            cluster.GetPacketHandlers()[Opcodes.CMSG_GUILD_DEMOTE] = guild.On_CMSG_GUILD_DEMOTE;
            cluster.GetPacketHandlers()[Opcodes.CMSG_GUILD_LEADER] = guild.On_CMSG_GUILD_LEADER;
            cluster.GetPacketHandlers()[Opcodes.MSG_SAVE_GUILD_EMBLEM] = guild.On_MSG_SAVE_GUILD_EMBLEM;
            cluster.GetPacketHandlers()[Opcodes.CMSG_GUILD_SET_OFFICER_NOTE] = guild.On_CMSG_GUILD_SET_OFFICER_NOTE;
            cluster.GetPacketHandlers()[Opcodes.CMSG_GUILD_SET_PUBLIC_NOTE] = guild.On_CMSG_GUILD_SET_PUBLIC_NOTE;
            cluster.GetPacketHandlers()[Opcodes.CMSG_GUILD_MOTD] = guild.On_CMSG_GUILD_MOTD;
            cluster.GetPacketHandlers()[Opcodes.CMSG_GUILD_INVITE] = guild.On_CMSG_GUILD_INVITE;
            cluster.GetPacketHandlers()[Opcodes.CMSG_GUILD_ACCEPT] = guild.On_CMSG_GUILD_ACCEPT;
            cluster.GetPacketHandlers()[Opcodes.CMSG_GUILD_DECLINE] = guild.On_CMSG_GUILD_DECLINE;
            cluster.GetPacketHandlers()[Opcodes.CMSG_GUILD_REMOVE] = guild.On_CMSG_GUILD_REMOVE;
            cluster.GetPacketHandlers()[Opcodes.CMSG_GUILD_LEAVE] = guild.On_CMSG_GUILD_LEAVE;
            cluster.GetPacketHandlers()[Opcodes.CMSG_CHAT_IGNORED] = chat.On_CMSG_CHAT_IGNORED;
            cluster.GetPacketHandlers()[Opcodes.CMSG_MESSAGECHAT] = chat.On_CMSG_MESSAGECHAT;
            cluster.GetPacketHandlers()[Opcodes.CMSG_JOIN_CHANNEL] = chat.On_CMSG_JOIN_CHANNEL;
            cluster.GetPacketHandlers()[Opcodes.CMSG_LEAVE_CHANNEL] = chat.On_CMSG_LEAVE_CHANNEL;
            cluster.GetPacketHandlers()[Opcodes.CMSG_CHANNEL_LIST] = chat.On_CMSG_CHANNEL_LIST;
            cluster.GetPacketHandlers()[Opcodes.CMSG_CHANNEL_PASSWORD] = chat.On_CMSG_CHANNEL_PASSWORD;
            cluster.GetPacketHandlers()[Opcodes.CMSG_CHANNEL_SET_OWNER] = chat.On_CMSG_CHANNEL_SET_OWNER;
            cluster.GetPacketHandlers()[Opcodes.CMSG_CHANNEL_OWNER] = chat.On_CMSG_CHANNEL_OWNER;
            cluster.GetPacketHandlers()[Opcodes.CMSG_CHANNEL_MODERATOR] = chat.On_CMSG_CHANNEL_MODERATOR;
            cluster.GetPacketHandlers()[Opcodes.CMSG_CHANNEL_UNMODERATOR] = chat.On_CMSG_CHANNEL_UNMODERATOR;
            cluster.GetPacketHandlers()[Opcodes.CMSG_CHANNEL_MUTE] = chat.On_CMSG_CHANNEL_MUTE;
            cluster.GetPacketHandlers()[Opcodes.CMSG_CHANNEL_UNMUTE] = chat.On_CMSG_CHANNEL_UNMUTE;
            cluster.GetPacketHandlers()[Opcodes.CMSG_CHANNEL_KICK] = chat.On_CMSG_CHANNEL_KICK;
            cluster.GetPacketHandlers()[Opcodes.CMSG_CHANNEL_INVITE] = chat.On_CMSG_CHANNEL_INVITE;
            cluster.GetPacketHandlers()[Opcodes.CMSG_CHANNEL_BAN] = chat.On_CMSG_CHANNEL_BAN;
            cluster.GetPacketHandlers()[Opcodes.CMSG_CHANNEL_UNBAN] = chat.On_CMSG_CHANNEL_UNBAN;
            cluster.GetPacketHandlers()[Opcodes.CMSG_CHANNEL_ANNOUNCEMENTS] = chat.On_CMSG_CHANNEL_ANNOUNCEMENTS;
            cluster.GetPacketHandlers()[Opcodes.CMSG_CHANNEL_MODERATE] = chat.On_CMSG_CHANNEL_MODERATE;

            // Opcodes redirected from the WorldServer
            cluster.GetPacketHandlers()[Opcodes.CMSG_CREATURE_QUERY] = OnClusterPacket;
            cluster.GetPacketHandlers()[Opcodes.CMSG_GAMEOBJECT_QUERY] = OnClusterPacket;

            // NOTE: TODO Opcodes
            // none
        }
    }
}
