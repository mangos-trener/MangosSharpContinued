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

using Mangos.Cluster.DataStores;
using Mangos.Cluster.Globals;
using Mangos.Cluster.Handlers.Guild;
using Mangos.Cluster.Network;
using Mangos.Common.Enums.Chat;
using Mangos.Common.Enums.Global;
using Mangos.Common.Enums.Misc;
using Mangos.Common.Globals;
using Mangos.Common.Legacy;

namespace Mangos.Cluster.Handlers;

public class WcHandlersChat
{
    private readonly LegacyWorldCluster cluster;
    private readonly WsHandlerChannels channels;
    private readonly WcGuild guild;
    private readonly Packets packets;
    private readonly WcHandlerCharacter character;
    private readonly WsDbcDatabase database;

    public WcHandlersChat(LegacyWorldCluster cluster, WsHandlerChannels channels, WcGuild guild, Packets packets, WcHandlerCharacter character, WsDbcDatabase database)
    {
        this.cluster = cluster;
        this.channels = channels;
        this.guild = guild;
        this.packets = packets;
        this.character = character;
        this.database = database;
    }

    public void On_CMSG_CHAT_IGNORED(PacketClass packet, ClientClass client)
    {
        packet.GetInt16();
        var guid = packet.GetUInt64();
        cluster.Log.WriteLine(LogType.DEBUG, "[{0}:{1}] CMSG_CHAT_IGNORED [0x{2}]", client.IP, client.Port, guid);
        if (cluster.CharacteRs.ContainsKey(guid))
        {
            var response = GlobalFunctions.BuildChatMessage(cluster, client.Character.Guid, "", ChatMsg.CHAT_MSG_IGNORED, LANGUAGES.LANG_UNIVERSAL, 0, "");
            cluster.CharacteRs[guid].Client.Send(response);
            response.Dispose();
        }
    }

    public void On_CMSG_MESSAGECHAT(PacketClass packet, ClientClass client)
    {
        if (packet.Data.Length - 1 < 14)
        {
            return;
        }

        packet.GetInt16();
        ChatMsg msgType = (ChatMsg)packet.GetInt32();
        LANGUAGES msgLanguage = (LANGUAGES)packet.GetInt32();
        cluster.Log.WriteLine(LogType.DEBUG, "[{0}:{1}] CMSG_MESSAGECHAT [{2}:{3}]", client.IP, client.Port, msgType, msgLanguage);
        switch (msgType)
        {
            case var @case when @case == ChatMsg.CHAT_MSG_CHANNEL:
                {
                    var channel = packet.GetString();
                    if (packet.Data.Length - 1 < 14 + channel.Length)
                    {
                        return;
                    }

                    var message = packet.GetString();

                    // DONE: Broadcast to all
                    if (channels.ChatChanneLs.ContainsKey(channel))
                    {
                        channels.ChatChanneLs[channel].Say(message, (int)msgLanguage, client.Character);
                    }

                    return;
                }

            case var case1 when case1 == ChatMsg.CHAT_MSG_WHISPER:
                {
                    var argname = packet.GetString();
                    var toUser = GlobalFunctions.CapitalizeName(argname);
                    if (packet.Data.Length - 1 < 14 + toUser.Length)
                    {
                        return;
                    }

                    var message = packet.GetString();

                    // DONE: Handle admin/gm commands
                    // If ToUser = "Warden" AndAlso client.Character.Access > 0 Then
                    // client.Character.GetWorld.ClientPacket(Client.Index, packet.Data)
                    // Exit Sub
                    // End If

                    // DONE: Send whisper MSG to receiver
                    var guid = 0UL;
                    cluster.CharacteRsLock.AcquireReaderLock(MangosGlobalConstants.DEFAULT_LOCK_TIMEOUT);
                    foreach (var character in cluster.CharacteRs)
                    {
                        if (StringFormatFunctions.UppercaseFirstLetter(character.Value.Name) == StringFormatFunctions.UppercaseFirstLetter(toUser))
                        {
                            guid = character.Value.Guid;
                            break;
                        }
                    }

                    cluster.CharacteRsLock.ReleaseReaderLock();
                    if (guid > 0m && cluster.CharacteRs.ContainsKey(guid))
                    {
                        // DONE: Check if ignoring
                        if (cluster.CharacteRs[guid].IgnoreList.Contains(client.Character.Guid) && client.Character.Access < AccessLevel.GameMaster)
                        {
                            // Client.Character.SystemMessage(String.Format("{0} is ignoring you.", ToUser))
                            client.Character.SendChatMessage(guid, "", ChatMsg.CHAT_MSG_IGNORED, (int)LANGUAGES.LANG_UNIVERSAL, "");
                        }
                        else
                        {
                            // To message
                            client.Character.SendChatMessage(guid, message, ChatMsg.CHAT_MSG_WHISPER_INFORM, (int)msgLanguage, "");
                            if (cluster.CharacteRs[guid].Dnd == false || client.Character.Access >= AccessLevel.GameMaster)
                            {
                                // From message
                                cluster.CharacteRs[guid].SendChatMessage(client.Character.Guid, message, ChatMsg.CHAT_MSG_WHISPER, (int)msgLanguage, "");
                            }
                            else
                            {
                                // DONE: Send the DND message
                                client.Character.SendChatMessage(guid, cluster.CharacteRs[guid].AfkMessage, ChatMsg.CHAT_MSG_DND, (int)msgLanguage, "");
                            }

                            // DONE: Send the AFK message
                            if (cluster.CharacteRs[guid].Afk)
                            {
                                client.Character.SendChatMessage(guid, cluster.CharacteRs[guid].AfkMessage, ChatMsg.CHAT_MSG_AFK, (int)msgLanguage, "");
                            }
                        }
                    }
                    else
                    {
                        PacketClass smsgChatPlayerNotFound = new(Opcodes.SMSG_CHAT_PLAYER_NOT_FOUND);
                        smsgChatPlayerNotFound.AddString(toUser);
                        client.Send(smsgChatPlayerNotFound);
                        smsgChatPlayerNotFound.Dispose();
                    }

                    break;
                }

            case var case2 when case2 == ChatMsg.CHAT_MSG_PARTY:
            case var case3 when case3 == ChatMsg.CHAT_MSG_RAID:
            case var case4 when case4 == ChatMsg.CHAT_MSG_RAID_LEADER:
            case var case5 when case5 == ChatMsg.CHAT_MSG_RAID_WARNING:
                {
                    var message = packet.GetString();

                    // DONE: Check in group
                    if (!client.Character.IsInGroup)
                    {
                        break;
                    }

                    // DONE: Broadcast to party
                    client.Character.Group.SendChatMessage(client.Character, message, msgLanguage, msgType);
                    break;
                }

            case var case6 when case6 == ChatMsg.CHAT_MSG_AFK:
                {
                    var message = packet.GetString();
                    // TODO: Can not be used while in combat!
                    if (string.IsNullOrEmpty(message) || client.Character.Afk == false)
                    {
                        if (client.Character.Afk == false)
                        {
                            if (string.IsNullOrEmpty(message))
                            {
                                message = "Away From Keyboard";
                            }

                            client.Character.AfkMessage = message;
                        }

                        client.Character.Afk = !client.Character.Afk;
                        if (client.Character.Afk && client.Character.Dnd)
                        {
                            client.Character.Dnd = false;
                        }

                        client.Character.ChatFlag = client.Character.Afk ? ChatFlag.FLAGS_AFK : ChatFlag.FLAGS_NONE;
                        // DONE: Pass the packet to the world server so it also knows about it
                        client.Character.GetWorld.ClientPacket(client.Index, packet.Data);
                    }

                    break;
                }

            case var case7 when case7 == ChatMsg.CHAT_MSG_DND:
                {
                    var message = packet.GetString();
                    if (string.IsNullOrEmpty(message) || client.Character.Dnd == false)
                    {
                        if (client.Character.Dnd == false)
                        {
                            if (string.IsNullOrEmpty(message))
                            {
                                message = "Do Not Disturb";
                            }

                            client.Character.AfkMessage = message;
                        }

                        client.Character.Dnd = !client.Character.Dnd;
                        if (client.Character.Dnd && client.Character.Afk)
                        {
                            client.Character.Afk = false;
                        }

                        client.Character.ChatFlag = client.Character.Dnd ? ChatFlag.FLAGS_DND : ChatFlag.FLAGS_NONE;
                        // DONE: Pass the packet to the world server so it also knows about it
                        client.Character.GetWorld.ClientPacket(client.Index, packet.Data);
                    }

                    break;
                }

            case var case8 when case8 == ChatMsg.CHAT_MSG_SAY:
            case var case9 when case9 == ChatMsg.CHAT_MSG_YELL:
            case var case10 when case10 == ChatMsg.CHAT_MSG_EMOTE:
                {
                    client.Character.GetWorld.ClientPacket(client.Index, packet.Data);
                    break;
                }

            case var case11 when case11 == ChatMsg.CHAT_MSG_GUILD:
                {
                    var message = packet.GetString();

                    // DONE: Broadcast to guild
                    guild.BroadcastChatMessageGuild(client.Character, message, msgLanguage, (int)client.Character.Guild.Id);
                    break;
                }

            case var case12 when case12 == ChatMsg.CHAT_MSG_OFFICER:
                {
                    var message = packet.GetString();

                    // DONE: Broadcast to officer chat
                    guild.BroadcastChatMessageOfficer(client.Character, message, msgLanguage, (int)client.Character.Guild.Id);
                    break;
                }

            default:
                {
                    cluster.Log.WriteLine(LogType.FAILED, "[{0}:{1}] Unknown chat message [msgType={2}, msgLanguage={3}]", client.IP, client.Port, msgType, msgLanguage);
                    packets.DumpPacket(packet.Data, client);
                    break;
                }
        }
    }

    public void On_CMSG_JOIN_CHANNEL(PacketClass packet, ClientClass client)
    {
        packet.GetInt16();
        var channelName = packet.GetString();
        var password = packet.GetString();
        cluster.Log.WriteLine(LogType.DEBUG, "[{0}:{1}] CMSG_JOIN_CHANNEL [{2}]", client.IP, client.Port, channelName);
        if (!channels.ChatChanneLs.ContainsKey(channelName))
        {
            // The New does a an add to the .Containskey collection above
            WsHandlerChannels.ChatChannelClass newChannel = new(channelName, database, channels);
        }

        channels.ChatChanneLs[channelName].Join(client.Character, password);
    }

    public void On_CMSG_LEAVE_CHANNEL(PacketClass packet, ClientClass client)
    {
        packet.GetInt16();
        var channelName = packet.GetString();
        cluster.Log.WriteLine(LogType.DEBUG, "[{0}:{1}] CMSG_LEAVE_CHANNEL [{2}]", client.IP, client.Port, channelName);
        if (channels.ChatChanneLs.ContainsKey(channelName))
        {
            channels.ChatChanneLs[channelName].Part(client.Character);
        }
    }

    public void On_CMSG_CHANNEL_LIST(PacketClass packet, ClientClass client)
    {
        packet.GetInt16();
        var channelName = packet.GetString();
        cluster.Log.WriteLine(LogType.DEBUG, "[{0}:{1}] CMSG_CHANNEL_LIST [{2}]", client.IP, client.Port, channelName);

        // ChannelName = ChannelName.ToUpper
        if (channels.ChatChanneLs.ContainsKey(channelName))
        {
            channels.ChatChanneLs[channelName].List(client.Character);
        }
    }

    public void On_CMSG_CHANNEL_PASSWORD(PacketClass packet, ClientClass client)
    {
        packet.GetInt16();
        var channelName = packet.GetString();
        var channelNewPassword = packet.GetString();
        cluster.Log.WriteLine(LogType.DEBUG, "[{0}:{1}] CMSG_CHANNEL_PASSWORD [{2}, {3}]", client.IP, client.Port, channelName, channelNewPassword);

        // ChannelName = ChannelName.ToUpper
        if (channels.ChatChanneLs.ContainsKey(channelName))
        {
            channels.ChatChanneLs[channelName].SetPassword(client.Character, channelNewPassword);
        }
    }

    public void On_CMSG_CHANNEL_SET_OWNER(PacketClass packet, ClientClass client)
    {
        packet.GetInt16();
        var channelName = packet.GetString();
        var channelNewOwner = packet.GetString();
        cluster.Log.WriteLine(LogType.DEBUG, "[{0}:{1}] CMSG_CHANNEL_SET_OWNER [{2}, {3}]", client.IP, client.Port, channelName, channelNewOwner);

        // ChannelName = ChannelName.ToUpper
        if (channels.ChatChanneLs.ContainsKey(channelName))
        {
            if (channels.ChatChanneLs[channelName].CanSetOwner(client.Character, channelNewOwner))
            {
                foreach (var guid in channels.ChatChanneLs[channelName].Joined.ToArray())
                {
                    if ((cluster.CharacteRs[guid].Name.ToUpper() ?? "") == (channelNewOwner.ToUpper() ?? ""))
                    {
                        var tmp = cluster.CharacteRs;
                        var argCharacter = tmp[guid];
                        channels.ChatChanneLs[channelName].SetOwner(argCharacter);
                        tmp[guid] = argCharacter;
                        break;
                    }
                }
            }
        }
    }

    public void On_CMSG_CHANNEL_OWNER(PacketClass packet, ClientClass client)
    {
        packet.GetInt16();
        var channelName = packet.GetString();
        cluster.Log.WriteLine(LogType.DEBUG, "[{0}:{1}] CMSG_CHANNEL_OWNER [{2}]", client.IP, client.Port, channelName);

        // ChannelName = ChannelName.ToUpper
        if (channels.ChatChanneLs.ContainsKey(channelName))
        {
            channels.ChatChanneLs[channelName].GetOwner(client.Character);
        }
    }

    public void On_CMSG_CHANNEL_MODERATOR(PacketClass packet, ClientClass client)
    {
        packet.GetInt16();
        var channelName = packet.GetString();
        var channelUser = packet.GetString();
        cluster.Log.WriteLine(LogType.DEBUG, "[{0}:{1}] CMSG_CHANNEL_MODERATOR [{2}, {3}]", client.IP, client.Port, channelName, channelUser);

        // ChannelName = ChannelName.ToUpper
        if (channels.ChatChanneLs.ContainsKey(channelName))
        {
            channels.ChatChanneLs[channelName].SetModerator(client.Character, channelUser);
        }
    }

    public void On_CMSG_CHANNEL_UNMODERATOR(PacketClass packet, ClientClass client)
    {
        packet.GetInt16();
        var channelName = packet.GetString();
        var channelUser = packet.GetString();
        cluster.Log.WriteLine(LogType.DEBUG, "[{0}:{1}] CMSG_CHANNEL_UNMODERATOR [{2}, {3}]", client.IP, client.Port, channelName, channelUser);

        // ChannelName = ChannelName.ToUpper
        if (channels.ChatChanneLs.ContainsKey(channelName))
        {
            channels.ChatChanneLs[channelName].SetUnModerator(client.Character, channelUser);
        }
    }

    public void On_CMSG_CHANNEL_MUTE(PacketClass packet, ClientClass client)
    {
        packet.GetInt16();
        var channelName = packet.GetString();
        var channelUser = packet.GetString();
        cluster.Log.WriteLine(LogType.DEBUG, "[{0}:{1}] CMSG_CHANNEL_MUTE [{2}, {3}]", client.IP, client.Port, channelName, channelUser);

        // ChannelName = ChannelName.ToUpper
        if (channels.ChatChanneLs.ContainsKey(channelName))
        {
            channels.ChatChanneLs[channelName].SetMute(client.Character, channelUser);
        }
    }

    public void On_CMSG_CHANNEL_UNMUTE(PacketClass packet, ClientClass client)
    {
        packet.GetInt16();
        var channelName = packet.GetString();
        var channelUser = packet.GetString();
        cluster.Log.WriteLine(LogType.DEBUG, "[{0}:{1}] CMSG_CHANNEL_UNMUTE [{2}, {3}]", client.IP, client.Port, channelName, channelUser);

        // ChannelName = ChannelName.ToUpper
        if (channels.ChatChanneLs.ContainsKey(channelName))
        {
            channels.ChatChanneLs[channelName].SetUnMute(client.Character, channelUser);
        }
    }

    public void On_CMSG_CHANNEL_INVITE(PacketClass packet, ClientClass client)
    {
        if (packet.Data.Length - 1 < 6)
        {
            return;
        }

        packet.GetInt16();
        var channelName = packet.GetString();
        if (packet.Data.Length - 1 < 6 + channelName.Length + 1)
        {
            return;
        }

        var playerName = GlobalFunctions.CapitalizeName(packet.GetString());
        cluster.Log.WriteLine(LogType.DEBUG, "[{0}:{1}] CMSG_CHANNEL_INVITE [{2}, {3}]", client.IP, client.Port, channelName, playerName);

        // ChannelName = ChannelName.ToUpper
        if (channels.ChatChanneLs.ContainsKey(channelName))
        {
            channels.ChatChanneLs[channelName].Invite(client.Character, playerName);
        }
    }

    public void On_CMSG_CHANNEL_KICK(PacketClass packet, ClientClass client)
    {
        if (packet.Data.Length - 1 < 6)
        {
            return;
        }

        packet.GetInt16();
        var channelName = packet.GetString();
        if (packet.Data.Length - 1 < 6 + channelName.Length + 1)
        {
            return;
        }

        var playerName = GlobalFunctions.CapitalizeName(packet.GetString());
        cluster.Log.WriteLine(LogType.DEBUG, "[{0}:{1}] CMSG_CHANNEL_KICK [{2}, {3}]", client.IP, client.Port, channelName, playerName);

        // ChannelName = ChannelName.ToUpper
        if (channels.ChatChanneLs.ContainsKey(channelName))
        {
            channels.ChatChanneLs[channelName].Kick(client.Character, playerName);
        }
    }

    public void On_CMSG_CHANNEL_ANNOUNCEMENTS(PacketClass packet, ClientClass client)
    {
        packet.GetInt16();
        var channelName = packet.GetString();
        cluster.Log.WriteLine(LogType.DEBUG, "[{0}:{1}] CMSG_CHANNEL_ANNOUNCEMENTS [{2}]", client.IP, client.Port, channelName);

        // ChannelName = ChannelName.ToUpper
        if (channels.ChatChanneLs.ContainsKey(channelName))
        {
            channels.ChatChanneLs[channelName].SetAnnouncements(client.Character);
        }
    }

    public void On_CMSG_CHANNEL_BAN(PacketClass packet, ClientClass client)
    {
        if (packet.Data.Length - 1 < 6)
        {
            return;
        }

        packet.GetInt16();
        var channelName = packet.GetString();
        if (packet.Data.Length - 1 < 6 + channelName.Length + 1)
        {
            return;
        }

        var playerName = GlobalFunctions.CapitalizeName(packet.GetString());
        cluster.Log.WriteLine(LogType.DEBUG, "[{0}:{1}] CMSG_CHANNEL_BAN [{2}, {3}]", client.IP, client.Port, channelName, playerName);

        // ChannelName = ChannelName.ToUpper
        if (channels.ChatChanneLs.ContainsKey(channelName))
        {
            channels.ChatChanneLs[channelName].Ban(client.Character, playerName);
        }
    }

    public void On_CMSG_CHANNEL_UNBAN(PacketClass packet, ClientClass client)
    {
        if (packet.Data.Length - 1 < 6)
        {
            return;
        }

        packet.GetInt16();
        var channelName = packet.GetString();
        if (packet.Data.Length - 1 < 6 + channelName.Length + 1)
        {
            return;
        }

        var playerName = GlobalFunctions.CapitalizeName(packet.GetString());
        cluster.Log.WriteLine(LogType.DEBUG, "[{0}:{1}] CMSG_CHANNEL_UNBAN [{2}, {3}]", client.IP, client.Port, channelName, playerName);

        // ChannelName = ChannelName.ToUpper
        if (channels.ChatChanneLs.ContainsKey(channelName))
        {
            channels.ChatChanneLs[channelName].UnBan(client.Character, playerName);
        }
    }

    public void On_CMSG_CHANNEL_MODERATE(PacketClass packet, ClientClass client)
    {
        packet.GetInt16();
        var channelName = packet.GetString();
        cluster.Log.WriteLine(LogType.DEBUG, "[{0}:{1}] CMSG_CHANNEL_MODERATE [{2}]", client.IP, client.Port, channelName);

        // ChannelName = ChannelName.ToUpper
        if (channels.ChatChanneLs.ContainsKey(channelName))
        {
            channels.ChatChanneLs[channelName].SetModeration(client.Character);
        }
    }
}
