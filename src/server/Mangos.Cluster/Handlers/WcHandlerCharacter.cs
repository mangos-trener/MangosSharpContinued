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
using Mangos.Common.Enums.Group;
using Mangos.Common.Enums.Guild;
using Mangos.Common.Enums.Misc;
using Mangos.Common.Enums.Player;
using Mangos.Common.Enums.Social;
using Mangos.Common.Globals;
using Mangos.Common.Legacy;
using Microsoft.VisualBasic;
using Microsoft.VisualBasic.CompilerServices;
using System;
using System.Collections.Generic;
using System.Data;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Mangos.Cluster.Handlers;

public class WcHandlerCharacter
{
    private readonly LegacyWorldCluster cluster;

    public WcHandlerCharacter(LegacyWorldCluster cluster)
    {
        this.cluster = cluster;
    }

    public class CharacterObject : IDisposable
    {
        public CharacterObject(ulong g,
                               ClientClass objCharacter,
                               LegacyWorldCluster cluster,
                               WsDbcDatabase database,
                               WcHandlersSocial social,
                               WcNetwork network,
                               WcGuild guild,
                               WsHandlerChannels channels,
                               WcHandlersGroup group)
        {
            ChatFlag = ChatFlag.FLAGS_NONE;
            Guid = g;
            Client = objCharacter;
            this.cluster = cluster; ;
            this.database = database;
            this.social = social;
            this.network = network;
            this.guild = guild;
            this.channels = channels;
            this.group = group;
            ReLoad();
            Access = Client.Access;
            var argobjCharacter = this;
            social.LoadIgnoreList(argobjCharacter);
            cluster.CharacteRsLock.AcquireWriterLock(MangosGlobalConstants.DEFAULT_LOCK_TIMEOUT);
            cluster.CharacteRs.Add(Guid, this);
            cluster.CharacteRsLock.ReleaseWriterLock();
        }

        public ulong Guid;
        public ClientClass Client;
        private readonly LegacyWorldCluster cluster;
        private readonly WsDbcDatabase database;
        private readonly WcHandlersSocial social;
        private readonly WcNetwork network;
        private readonly WcGuild guild;
        private readonly WsHandlerChannels channels;
        private readonly WcHandlersGroup group;
        public bool IsInWorld;
        public uint Map;
        public uint Zone;
        public float PositionX;
        public float PositionY;
        public float PositionZ;
        public float PositionO;
        public AccessLevel Access;
        public string Name;
        public int Level;
        public Races Race;
        public Classes Classe;
        public byte Gender;
        public DateTime Time = DateAndTime.Now;
        public int Latency;
        public List<ulong> IgnoreList = new();
        public List<string> JoinedChannels = new();
        public bool Afk;
        public bool Dnd;
        public string AfkMessage;
        public uint GuildInvited;
        public ulong GuildInvitedBy;
        public WcGuild.Guild Guild;
        public byte GuildRank;
        public WcHandlersGroup.Group Group;
        public bool GroupAssistant;
        public bool GroupInvitedFlag;

        public bool IsInGroup => Group is not null && GroupInvitedFlag == false;

        public bool IsGroupLeader => Group is not null && ReferenceEquals(Group.Members[Group.Leader], this);

        public bool IsInRaid => Group is not null && Group.Type == GroupType.RAID;

        public bool IsInGuild => Guild is not null;

        public bool IsGuildLeader => Guild is not null && Guild.Leader == Guid;

        public bool IsGuildRightSet(GuildRankRights rights)
        {
            return Guild is not null && (Guild.RankRights[GuildRank] & (uint)rights) == (uint)rights;
        }

        public bool Side
        {
            get
            {
                switch (Race)
                {
                    case var @case when @case == Races.RACE_DWARF:
                    case var case1 when case1 == Races.RACE_GNOME:
                    case var case2 when case2 == Races.RACE_HUMAN:
                    case var case3 when case3 == Races.RACE_NIGHT_ELF:
                        {
                            return false;
                        }

                    default:
                        {
                            return true;
                        }
                }
            }
        }

        public IWorld GetWorld => network.WorldServer.Worlds[Map];

        public void ReLoad()
        {
            // DONE: Get character info from DB
            DataTable mySqlQuery = new();
            database.GetCharacterDatabase().Query(string.Format("SELECT * FROM characters WHERE char_guid = {0};", Guid), ref mySqlQuery);
            if (mySqlQuery.Rows.Count > 0)
            {
                Race = (Races)mySqlQuery.Rows[0].As<byte>("char_race");
                Classe = (Classes)mySqlQuery.Rows[0].As<byte>("char_class");
                Gender = mySqlQuery.Rows[0].As<byte>("char_gender");
                Name = mySqlQuery.Rows[0].As<string>("char_name");
                Level = mySqlQuery.Rows[0].As<byte>("char_level");
                Zone = mySqlQuery.Rows[0].As<uint>("char_zone_id");
                Map = mySqlQuery.Rows[0].As<uint>("char_map_id");
                PositionX = mySqlQuery.Rows[0].As<float>("char_positionX");
                PositionY = mySqlQuery.Rows[0].As<float>("char_positionY");
                PositionZ = mySqlQuery.Rows[0].As<float>("char_positionZ");

                // DONE: Get guild info
                var guildId = mySqlQuery.Rows[0].As<uint>("char_guildId");
                if (guildId > 0L)
                {
                    if (guild.GuilDs.ContainsKey(guildId) == false)
                    {
                        WcGuild.Guild tmpGuild = new(guildId);
                        Guild = tmpGuild;
                    }
                    else
                    {
                        Guild = guild.GuilDs[guildId];
                    }

                    GuildRank = mySqlQuery.Rows[0].As<byte>("char_guildRank");
                }
            }
            else
            {
                cluster.Log.WriteLine(LogType.DATABASE, "Failed to load expected results from:");
                cluster.Log.WriteLine(LogType.DATABASE, string.Format("SELECT * FROM characters WHERE char_guid = {0};", Guid));
            }
        }

        private bool _disposedValue; // To detect redundant calls

        // IDisposable
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                // TODO: free unmanaged resources (unmanaged objects) and override Finalize() below.
                // TODO: set large fields to null.
                Client = null;

                // DONE: Update character status in database
                database.GetCharacterDatabase().Update(string.Format("UPDATE characters SET char_online = 0, char_logouttime = '{1}' WHERE char_guid = '{0}';", Guid, GlobalFunctions.GetTimestamp(DateAndTime.Now)));

                // NOTE: Don't leave group on normal disconnect, only on logout
                if (IsInGroup)
                {
                    // DONE: Tell the group the member is offline
                    var response = GlobalFunctions.BuildPartyMemberStatsOffline(Guid);
                    Group.Broadcast(response);
                    response.Dispose();

                    // DONE: Set new leader and loot master
                    Group.NewLeader(this);
                    Group.SendGroupList();
                }

                // DONE: Notify friends for logout
                var argobjCharacter = this;
                social.NotifyFriendStatus(argobjCharacter, (FriendStatus)FriendResult.FRIEND_OFFLINE);

                // DONE: Notify guild for logout
                if (IsInGuild)
                {
                    var argobjCharacter1 = this;
                    guild.NotifyGuildStatus(argobjCharacter1, GuildEvent.SIGNED_OFF);
                }

                // DONE: Leave chat
                while (JoinedChannels.Count > 0)
                {
                    if (channels.ChatChanneLs.ContainsKey(JoinedChannels[0]))
                    {
                        var argCharacter = this;
                        channels.ChatChanneLs[JoinedChannels[0]].Part(argCharacter);
                    }
                    else
                    {
                        JoinedChannels.RemoveAt(0);
                    }
                }

                cluster.CharacteRsLock.AcquireWriterLock(MangosGlobalConstants.DEFAULT_LOCK_TIMEOUT);
                cluster.CharacteRs.Remove(Guid);
                cluster.CharacteRsLock.ReleaseWriterLock();
            }

            _disposedValue = true;
        }

        // This code added by Visual Basic to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code.  Put cleanup code in Dispose(ByVal disposing As Boolean) above.
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public void Transfer(float posX, float posY, float posZ, float ori, int thisMap)
        {
            PacketClass p = new(Opcodes.SMSG_TRANSFER_PENDING);
            p.AddInt32(thisMap);
            Client.Send(p);
            p.Dispose();

            // Actions Here
            IsInWorld = false;
            GetWorld.ClientDisconnect(Client.Index);
            database.GetCharacterDatabase().Update(string.Format("UPDATE characters SET char_positionX = {0}, char_positionY = {1}, char_positionZ = {2}, char_orientation = {3}, char_map_id = {4} WHERE char_guid = {5};", Strings.Trim(Conversion.Str(posX)), Strings.Trim(Conversion.Str(posY)), Strings.Trim(Conversion.Str(posZ)), Strings.Trim(Conversion.Str(ori)), thisMap, Guid));

            // Do global transfer
            network.WorldServer.ClientTransfer(Client.Index, posX, posY, posZ, ori, (uint)thisMap);
        }

        public void Transfer(float posX, float posY, float posZ, float ori)
        {
            PacketClass p = new(Opcodes.SMSG_TRANSFER_PENDING);
            p.AddInt32((int)Map);
            Client.Send(p);
            p.Dispose();

            // Actions Here
            IsInWorld = false;
            GetWorld.ClientDisconnect(Client.Index);
            database.GetCharacterDatabase().Update(string.Format("UPDATE characters SET char_positionX = {0}, char_positionY = {1}, char_positionZ = {2}, char_orientation = {3}, char_map_id = {4} WHERE char_guid = {5};", Strings.Trim(Conversion.Str(posX)), Strings.Trim(Conversion.Str(posY)), Strings.Trim(Conversion.Str(posZ)), Strings.Trim(Conversion.Str(ori)), Map, Guid));

            // Do global transfer
            network.WorldServer.ClientTransfer(Client.Index, posX, posY, posZ, ori, Map);
        }

        // Login
        public void OnLogin()
        {
            // DONE: Update character status in database
            database.GetCharacterDatabase().Update("UPDATE characters SET char_online = 1 WHERE char_guid = " + Guid + ";");

            // DONE: SMSG_ACCOUNT_DATA_MD5
            var argcharacter = this;
            GlobalFunctions.SendAccountMd5(cluster, Client, argcharacter);

            // DONE: SMSG_TRIGGER_CINEMATIC
            DataTable q = new();
            database.GetCharacterDatabase().Query(string.Format("SELECT char_moviePlayed FROM characters WHERE char_guid = {0} AND char_moviePlayed = 0;", Guid), ref q);
            if (q.Rows.Count > 0)
            {
                database.GetCharacterDatabase().Update("UPDATE characters SET char_moviePlayed = 1 WHERE char_guid = " + Guid + ";");
                var argcharacter1 = this;
                GlobalFunctions.SendTriggerCinematic(cluster, database, Client, argcharacter1);
            }

            // DONE: SMSG_LOGIN_SETTIMESPEED
            var argcharacter2 = this;
            GlobalFunctions.SendGameTime(cluster, Client, argcharacter2);

            // DONE: Server Message Of The Day
            GlobalFunctions.SendMessageMotd(cluster, Client, "Welcome to World of Warcraft.");
            GlobalFunctions.SendMessageMotd(cluster, Client, string.Format("This server is using {0} v.{1}",
                GlobalFunctions.SetColor($"[MangosSharp, written in C# {RuntimeInformation.FrameworkDescription}]", 4, 147, 11),
                Assembly.GetExecutingAssembly().GetName().Version));

            // DONE: Guild Message Of The Day
            var argobjCharacter = this;
            guild.SendGuildMotd(argobjCharacter);

            // DONE: Social lists
            var argcharacter3 = this;
            social.SendFriendList(Client, argcharacter3);
            var argcharacter4 = this;
            social.SendIgnoreList(Client, argcharacter4);

            // DONE: Send "Friend online"
            var argobjCharacter1 = this;
            social.NotifyFriendStatus(argobjCharacter1, (FriendStatus)FriendResult.FRIEND_ONLINE);

            // DONE: Send online notify for guild
            var argobjCharacter2 = this;
            guild.NotifyGuildStatus(argobjCharacter2, GuildEvent.SIGNED_ON);

            // DONE: Put back character in group if disconnected
            foreach (var tmpGroup in group.GrouPs)
            {
                for (byte i = 0, loopTo = (byte)(tmpGroup.Value.Members.Length - 1); i <= loopTo; i++)
                {
                    if (tmpGroup.Value.Members[i] is not null && tmpGroup.Value.Members[i].Guid == Guid)
                    {
                        tmpGroup.Value.Members[i] = this;
                        tmpGroup.Value.SendGroupList();
                        PacketClass response = new(0) { Data = GetWorld.GroupMemberStats(Guid, 0) };
                        var argobjCharacter3 = this;
                        tmpGroup.Value.BroadcastToOther(response, argobjCharacter3);
                        response.Dispose();
                        return;
                    }
                }
            }
        }

        public void OnLogout()
        {
            // DONE: Leave group
            if (IsInGroup)
            {
                var argobjCharacter = this;
                Group.Leave(argobjCharacter);
            }

            // DONE: Leave chat
            while (JoinedChannels.Count > 0)
            {
                if (channels.ChatChanneLs.ContainsKey(JoinedChannels[0]))
                {
                    var argCharacter = this;
                    channels.ChatChanneLs[JoinedChannels[0]].Part(argCharacter);
                }
                else
                {
                    JoinedChannels.RemoveAt(0);
                }
            }
        }

        public void SendGuildUpdate()
        {
            var guildId = 0U;
            if (Guild is not null)
            {
                guildId = Guild.Id;
            }

            GetWorld.GuildUpdate(Guid, guildId, GuildRank);
        }

        public ChatFlag ChatFlag;

        public void SendChatMessage(ulong thisguid, string message, ChatMsg msgType, int msgLanguage, string channelName)
        {
            if (thisguid == 0m)
            {
                thisguid = Guid;
            }

            if (string.IsNullOrEmpty(channelName))
            {
                channelName = "Global";
            }

            var msgChatFlag = ChatFlag;
            if (msgType is ChatMsg.CHAT_MSG_WHISPER_INFORM or ChatMsg.CHAT_MSG_WHISPER)
            {
                msgChatFlag = cluster.CharacteRs[thisguid].ChatFlag;
            }

            var packet = GlobalFunctions.BuildChatMessage(cluster, thisguid, message, msgType, (LANGUAGES)msgLanguage, (byte)msgChatFlag, channelName);
            Client.Send(packet);
            packet.Dispose();
        }
    }

    public ulong GetCharacterGuidByName(LegacyWorldCluster cluster, WsDbcDatabase database, string name)
    {
        var guid = 0UL;
        cluster.CharacteRsLock.AcquireReaderLock(MangosGlobalConstants.DEFAULT_LOCK_TIMEOUT);
        foreach (var objCharacter in cluster.CharacteRs)
        {
            if (StringFormatFunctions.UppercaseFirstLetter(objCharacter.Value.Name) == StringFormatFunctions.UppercaseFirstLetter(name))
            {
                guid = objCharacter.Value.Guid;
                break;
            }
        }

        cluster.CharacteRsLock.ReleaseReaderLock();
        if (guid == 0m)
        {
            DataTable q = new();
            database.GetCharacterDatabase().Query(string.Format("SELECT char_guid FROM characters WHERE char_name = \"{0}\";", GlobalFunctions.EscapeString(name)), ref q);

            return q.Rows.Count > 0 ? q.Rows[0].As<ulong>("char_guid") : 0UL;
        }

        return guid;
    }

    public string GetCharacterNameByGuid(LegacyWorldCluster cluster, WsDbcDatabase database, string guid)
    {
        if (cluster.CharacteRs.ContainsKey(Conversions.ToULong(guid)))
        {
            return cluster.CharacteRs[Conversions.ToULong(guid)].Name;
        }

        DataTable q = new();
        database.GetCharacterDatabase().Query(string.Format("SELECT char_name FROM characters WHERE char_guid = \"{0}\";", guid), ref q);

        return q.Rows.Count > 0 ? q.Rows[0].As<string>("char_name") : "";
    }
}
