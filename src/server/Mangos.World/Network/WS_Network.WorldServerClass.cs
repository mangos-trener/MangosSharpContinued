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

using Mangos.Common.Enums.Group;
using Mangos.Common.Legacy;
using Mangos.Configuration;
using Mangos.DataStores;
using Mangos.World.Globals;
using Mangos.World.Handlers;
using Mangos.World.Maps;
using Mangos.World.Objects.Factories;
using Mangos.World.Objects.Factories.Client;
using Mangos.World.Objects.Factories.Groups;
using Mangos.World.Objects.Factories.Maps;
using Mangos.World.Player;
using Microsoft.Extensions.Logging;
using Microsoft.VisualBasic.CompilerServices;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Mangos.World.Network;

public partial class WS_Network
{
    public class WorldServerClass : IWorld, IDisposable
    {
        private readonly string m_RemoteURI;
        private readonly Timer m_Connection;
        private readonly Timer m_TimerCPU;
        private DateTime _lastInfo;
        private double _lastCPUTime;
        private float _usageCPU;
        private bool _disposedValue;
        private readonly ILogger<WorldServerClass> logger;

        // DI
        private readonly ICluster _cluster;
        private readonly WorldState worldState;
        private readonly WS_Player_Creation _playerCreation;
        private readonly WS_Handlers_Instance _instance;
        private readonly WS_PlayerHelper _playerHelper;
        private readonly WS_Maps maps;
        private readonly WS_Network network;
        private readonly MapFactory mapFactory;
        private readonly GroupFactory groupFactory;
        private readonly ClientClassFactory clientClassFactory;
        private readonly CharacterObjectFactory characterObjectFactory;
        private readonly MangosConfiguration _configuration;
        private readonly DataStoreProvider _dataStoreProvider;

        public ICluster Cluster;
        public bool FlagStopListen;
        public string LocalURI;

        public WorldServerClass(
            ILogger<WorldServerClass> logger,
            ICluster cluster,
            MangosConfiguration configuration,
            WorldState worldState,
            DataStoreProvider dataStoreProvider,
            WS_Player_Creation playerCreation,
            WS_Handlers_Instance instance,
            WS_PlayerHelper playerHelper,
            WS_Maps maps,
            WS_Network network,
            MapFactory mapFactory,
            GroupFactory groupFactory,
            ClientClassFactory clientClassFactory,
            CharacterObjectFactory characterObjectFactory)
        {
            FlagStopListen = false;
            _lastCPUTime = 0.0;
            _usageCPU = 0f;
            Cluster = null;

            var worldConfiguration = configuration.World;

            m_RemoteURI = $"http://{worldConfiguration.ClusterConnectHost}:{worldConfiguration.ClusterConnectPort}";
            LocalURI = $"http://{worldConfiguration.LocalConnectHost}:{worldConfiguration.LocalConnectPort}";
            Cluster = null;

            network.LastPing = LegacyNativeMethods.TimeGetTime("");
            m_Connection = new Timer(CheckConnection, null, 10000, 10000);
            m_TimerCPU = new Timer(CheckCPU, null, 1000, 1000);

            _dataStoreProvider = dataStoreProvider;
            this.logger = logger;
            _cluster = cluster;
            this.worldState = worldState;
            _playerCreation = playerCreation;
            _instance = instance;
            _playerHelper = playerHelper;
            this.maps = maps;
            this.network = network;
            this.mapFactory = mapFactory;
            this.groupFactory = groupFactory;
            this.clientClassFactory = clientClassFactory;
            this.characterObjectFactory = characterObjectFactory;
            _configuration = configuration;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                ClusterDisconnect();
                FlagStopListen = true;
                m_TimerCPU.Dispose();
                m_Connection.Dispose();
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

        public void ClusterConnect()
        {
            while (Cluster == null)
            {
                try
                {
                    Cluster = _cluster;
                    if (Cluster != null)
                    {
                        var worldConfiguration = _configuration.World;
                        if (Cluster.Connect(LocalURI, worldConfiguration.Maps.Select(x => Conversions.ToUInteger(x)).ToList(), this))
                        {
                            break;
                        }

                        Cluster.Disconnect(LocalURI, worldConfiguration.Maps.Select(x => Conversions.ToUInteger(x)).ToList());
                    }
                }
                catch (Exception ex)
                {
                    var e = ex;
                    logger.LogError("Unable to connect to cluster. [{0}]", e.Message);
                }

                Cluster = null;
                Thread.Sleep(3000);
            }

            logger.LogInformation("Contacted cluster [{0}]", m_RemoteURI);
        }

        public void ClusterDisconnect()
        {
            try
            {
                Cluster.Disconnect(LocalURI, _configuration.World.Maps.Select(x => Conversions.ToUInteger(x)).ToList());
            }
            catch (Exception ex)
            {
                logger.LogWarning("Cluster Disconnected [{0}]", ex);
            }
            finally
            {
                Cluster = null;
            }
        }

        public void ClientTransfer(uint ID, float posX, float posY, float posZ, float ori, int map)
        {
            checked
            {
                if (!maps.Maps.ContainsKey((uint)map))
                {
                    worldState.ConnectedClients[ID].Character.Dispose();
                    worldState.ConnectedClients[ID].Delete();
                }
                Cluster.ClientTransfer(ID, posX, posY, posZ, ori, (uint)map);
            }
        }

        public void ClientConnect(uint id, ClientInfo client)
        {
            logger.LogInformation("[{0:000000}] Client connected", id);
            if (client == null)
            {
                throw new ApplicationException("Client doesn't exist!");
            }

            var objCharacter = clientClassFactory.Create(client);
            if (worldState.ConnectedClients.ContainsKey(id))
            {
                worldState.ConnectedClients.Remove(id);
            }

            worldState.ConnectedClients.Add(id, objCharacter);
        }

        void IWorld.ClientConnect(uint id, ClientInfo client)
        {
            //ILSpy generated this explicit interface implementation from .override directive in ClientConnect
            ClientConnect(id, client);
        }

        public void ClientDisconnect(uint id)
        {
            logger.LogInformation("[{0:000000}] Client disconnected", id);
            worldState.ConnectedClients[id].Character?.Save();
            worldState.ConnectedClients[id].Delete();
            worldState.ConnectedClients.Remove(id);
        }

        void IWorld.ClientDisconnect(uint id)
        {
            //ILSpy generated this explicit interface implementation from .override directive in ClientDisconnect
            ClientDisconnect(id);
        }

        public void ClientLogin(uint id, ulong guid)
        {
            logger.LogInformation("[{0:000000}] Client login [0x{1:X}]", id, guid);
            try
            {
                var client = worldState.ConnectedClients[id];

                var Character = characterObjectFactory.Create(ref client, guid);
                worldState.CharactersLock.EnterWriteLock();
                worldState.Characters[guid] = Character;
                worldState.CharactersLock.ExitWriteLock();
                Globals.Functions.SendCorpseReclaimDelay(logger, ref client, ref Character);

                _playerHelper.InitializeTalentSpells(Character);

                Character.Login();

                logger.LogInformation("[{0}:{1}] Player login complete [0x{2:X}]", client.IP, client.Port, guid);
            }
            catch (Exception ex)
            {
                var e = ex;
                logger.LogError("Error on login: {0}", e.ToString());
            }
        }

        void IWorld.ClientLogin(uint id, ulong guid)
        {
            //ILSpy generated this explicit interface implementation from .override directive in ClientLogin
            ClientLogin(id, guid);
        }

        public void ClientLogout(uint id)
        {
            logger.LogInformation("[{0:000000}] Client logout", id);
            worldState.ConnectedClients[id].Character.Logout();
        }

        void IWorld.ClientLogout(uint id)
        {
            //ILSpy generated this explicit interface implementation from .override directive in ClientLogout
            ClientLogout(id);
        }

        public void ClientPacket(uint id, byte[] data)
        {
            if (data == null)
            {
                throw new ApplicationException("Packet doesn't contain data!");
            }

            try
            {
                if (worldState.ConnectedClients.TryGetValue(id, out var _client))
                {
                    Packets.PacketClass p = new(ref data);
                    _client?.PushPacket(p);
                }
                else
                {
                    logger.LogWarning("Client ID doesn't contain a key!: {0}", ToString());
                }
            }
            catch (Exception ex2)
            {
                var ex = ex2;
                logger.LogError("Error on Client OnPacket: {0}", ex.ToString());
            }
        }

        void IWorld.ClientPacket(uint id, byte[] data)
        {
            //ILSpy generated this explicit interface implementation from .override directive in ClientPacket
            ClientPacket(id, data);
        }

        public int ClientCreateCharacter(string account, string name, byte race, byte classe, byte gender, byte skin, byte face, byte hairStyle, byte hairColor, byte facialHair, byte outfitId)
        {
            if (string.IsNullOrEmpty(account))
            {
                throw new ArgumentException($"'{nameof(account)}' cannot be null or empty", nameof(account));
            }

            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentException($"'{nameof(name)}' cannot be null or empty", nameof(name));
            }

            logger.LogInformation("Account {0} Created a character with Name {1}, Race {2}, Class {3}, Gender {4}, Skin {5}, Face {6}, HairStyle {7}, HairColor {8}, FacialHair {9}, outfitID {10}", account, name, race, classe, gender, skin, face, hairStyle, hairColor, facialHair, outfitId);

            return _playerCreation.CreateCharacter(account, name, race, classe, gender, skin, face, hairStyle, hairColor, facialHair, outfitId);
        }

        int IWorld.ClientCreateCharacter(string account, string name, byte race, byte classe, byte gender, byte skin, byte face, byte hairStyle, byte hairColor, byte facialHair, byte outfitId)
        {
            if (string.IsNullOrEmpty(account))
            {
                throw new ArgumentException($"'{nameof(account)}' cannot be null or empty", nameof(account));
            }

            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentException($"'{nameof(name)}' cannot be null or empty", nameof(name));
            }

            //ILSpy generated this explicit interface implementation from .override directive in ClientCreateCharacter
            return ClientCreateCharacter(account, name, race, classe, gender, skin, face, hairStyle, hairColor, facialHair, outfitId);
        }

        public int Ping(int timestamp, int latency)
        {
            checked
            {
                logger.LogInformation("Cluster ping: [{0}ms]", LegacyNativeMethods.TimeGetTime("") - timestamp);
                network.LastPing = LegacyNativeMethods.TimeGetTime("");
                network.WC_MsTime = timestamp + latency;
                return LegacyNativeMethods.TimeGetTime("");
            }
        }

        int IWorld.Ping(int timestamp, int latency)
        {
            //ILSpy generated this explicit interface implementation from .override directive in Ping
            return Ping(timestamp, latency);
        }

        public void CheckConnection(object State)
        {
            if ((LegacyNativeMethods.TimeGetTime("") - network.LastPing) > 40000)
            {
                if (Cluster != null)
                {
                    logger.LogError("Cluster timed out. Reconnecting");
                    ClusterDisconnect();
                }

                ClusterConnect();
                network.LastPing = LegacyNativeMethods.TimeGetTime("");
            }
        }

        public void CheckCPU(object State)
        {
            var TimeSinceLastCheck = DateTime.Now.Subtract(_lastInfo);

            _usageCPU = (float)((Process.GetCurrentProcess().TotalProcessorTime.TotalMilliseconds - _lastCPUTime) / TimeSinceLastCheck.TotalMilliseconds * 100.0);
            _lastInfo = DateTime.Now;
            _lastCPUTime = Process.GetCurrentProcess().TotalProcessorTime.TotalMilliseconds;
        }

        public ServerInfo GetServerInfo()
        {
            ServerInfo serverInfo = new()
            {
                CpuUsage = _usageCPU,
                MemoryUsage = checked((ulong)Math.Round(Process.GetCurrentProcess().WorkingSet64 / 1048576.0))
            };

            return serverInfo;
        }

        ServerInfo IWorld.GetServerInfo()
        {
            //ILSpy generated this explicit interface implementation from .override directive in GetServerInfo
            return GetServerInfo();
        }

        public async Task InstanceCreateAsync(uint mapId)
        {
            if (!maps.Maps.ContainsKey(mapId))
            {
                var map = mapFactory.Create(checked((int)mapId), await _dataStoreProvider.GetDataStoreAsync("Map.dbc"));
            }
        }

        async Task IWorld.InstanceCreateAsync(uint mapId)
        {
            //ILSpy generated this explicit interface implementation from .override directive in InstanceCreate
            await InstanceCreateAsync(mapId).ConfigureAwait(false);
        }

        public void InstanceDestroy(uint mapId)
        {
            maps.Maps[mapId].Dispose();
        }

        void IWorld.InstanceDestroy(uint mapId)
        {
            //ILSpy generated this explicit interface implementation from .override directive in InstanceDestroy
            InstanceDestroy(mapId);
        }

        public bool InstanceCanCreate(int type)
        {
            var configuration = _configuration.World;

            return type switch
            {
                3 => configuration.CreateBattlegrounds,
                1 => configuration.CreatePartyInstances,
                2 => configuration.CreateRaidInstances,
                0 => configuration.CreateOther,
                _ => false,
            };
        }

        bool IWorld.InstanceCanCreate(int type)
        {
            //ILSpy generated this explicit interface implementation from .override directive in InstanceCanCreate
            return InstanceCanCreate(type);
        }

        public void ClientSetGroup(uint id, long groupId)
        {
            if (!worldState.ConnectedClients.ContainsKey(id))
            {
                return;
            }

            if (groupId == -1)
            {
                logger.LogInformation("[{0:000000}] Client group set [G NULL]", id);
                worldState.ConnectedClients[id].Character.Group = null;
                _instance.InstanceMapLeave(worldState.ConnectedClients[id].Character);
                return;
            }

            logger.LogInformation("[{0:000000}] Client group set [G{1:00000}]", id, groupId);

            if (!WorldServiceLocator.WSGroup.Groups.ContainsKey(groupId))
            {
                var Group = groupFactory.Create(groupId);
                Cluster.GroupRequestUpdate(id);
            }

            worldState.ConnectedClients[id].Character.Group = WorldServiceLocator.WSGroup.Groups[groupId];
            _instance.InstanceMapEnter(worldState.ConnectedClients[id].Character, maps);
        }

        void IWorld.ClientSetGroup(uint ID, long GroupID)
        {
            //ILSpy generated this explicit interface implementation from .override directive in ClientSetGroup
            ClientSetGroup(ID, GroupID);
        }

        public void GroupUpdate(long GroupID, byte GroupType, ulong GroupLeader, ulong[] Members)
        {
            if (!WorldServiceLocator.WSGroup.Groups.ContainsKey(GroupID))
            {
                return;
            }

            List<ulong> list = new();
            foreach (var GUID in Members)
            {
                if (worldState.Characters.ContainsKey(GUID))
                {
                    list.Add(GUID);
                }
            }

            logger.LogInformation("[G{0:00000}] Group update [{2}, {1} local members]", GroupID, list.Count, (GroupType)GroupType);
            if (list.Count == 0)
            {
                WorldServiceLocator.WSGroup.Groups[GroupID].Dispose();
                return;
            }

            WorldServiceLocator.WSGroup.Groups[GroupID].Type = (GroupType)GroupType;
            WorldServiceLocator.WSGroup.Groups[GroupID].Leader = GroupLeader;
            WorldServiceLocator.WSGroup.Groups[GroupID].LocalMembers = list;
        }

        void IWorld.GroupUpdate(long GroupID, byte GroupType, ulong GroupLeader, ulong[] Members)
        {
            //ILSpy generated this explicit interface implementation from .override directive in GroupUpdate
            GroupUpdate(GroupID, GroupType, GroupLeader, Members);
        }

        public void GroupUpdateLoot(long GroupID, byte Difficulty, byte Method, byte Threshold, ulong Master)
        {
            if (WorldServiceLocator.WSGroup.Groups.ContainsKey(GroupID))
            {
                logger.LogInformation("[G{0:00000}] Group update loot", GroupID);
                WorldServiceLocator.WSGroup.Groups[GroupID].DungeonDifficulty = (GroupDungeonDifficulty)Difficulty;
                WorldServiceLocator.WSGroup.Groups[GroupID].LootMethod = (GroupLootMethod)Method;
                WorldServiceLocator.WSGroup.Groups[GroupID].LootThreshold = (GroupLootThreshold)Threshold;
                WorldServiceLocator.WSGroup.Groups[GroupID].LocalLootMaster = worldState.Characters.ContainsKey(Master) ? worldState.Characters[Master] : null;
            }
        }

        void IWorld.GroupUpdateLoot(long GroupID, byte Difficulty, byte Method, byte Threshold, ulong Master)
        {
            //ILSpy generated this explicit interface implementation from .override directive in GroupUpdateLoot
            GroupUpdateLoot(GroupID, Difficulty, Method, Threshold, Master);
        }

        public byte[] GroupMemberStats(ulong GUID, int Flag)
        {
            if (Flag == 0)
            {
                Flag = 1015;
            }
            var wS_Group = WorldServiceLocator.WSGroup;
            Dictionary<ulong, CharacterObject> cHARACTERs;
            ulong key;
            var objCharacter = (cHARACTERs = worldState.Characters)[key = GUID];
            var packetClass = wS_Group.BuildPartyMemberStats(ref objCharacter, checked((uint)Flag));
            cHARACTERs[key] = objCharacter;
            var p = packetClass;
            p.UpdateLength();
            return p.Data;
        }

        byte[] IWorld.GroupMemberStats(ulong GUID, int Flag)
        {
            //ILSpy generated this explicit interface implementation from .override directive in GroupMemberStats
            return GroupMemberStats(GUID, Flag);
        }

        public void GuildUpdate(ulong GUID, uint GuildID, byte GuildRank)
        {
            worldState.Characters[GUID].GuildID = GuildID;
            worldState.Characters[GUID].GuildRank = GuildRank;
            worldState.Characters[GUID].SetUpdateFlag(191, GuildID);
            worldState.Characters[GUID].SetUpdateFlag(192, GuildRank);
            worldState.Characters[GUID].SendCharacterUpdate();
        }

        void IWorld.GuildUpdate(ulong GUID, uint GuildID, byte GuildRank)
        {
            //ILSpy generated this explicit interface implementation from .override directive in GuildUpdate
            GuildUpdate(GUID, GuildID, GuildRank);
        }

        public void BattlefieldCreate(int BattlefieldID, byte BattlefieldMapType, uint Map)
        {
            logger.LogInformation("[B{0:0000}] Battlefield created", BattlefieldID);
        }

        void IWorld.BattlefieldCreate(int BattlefieldID, byte BattlefieldMapType, uint Map)
        {
            //ILSpy generated this explicit interface implementation from .override directive in BattlefieldCreate
            BattlefieldCreate(BattlefieldID, BattlefieldMapType, Map);
        }

        public void BattlefieldDelete(int BattlefieldID)
        {
            logger.LogInformation("[B{0:0000}] Battlefield deleted", BattlefieldID);
        }

        void IWorld.BattlefieldDelete(int BattlefieldID)
        {
            //ILSpy generated this explicit interface implementation from .override directive in BattlefieldDelete
            BattlefieldDelete(BattlefieldID);
        }

        public void BattlefieldJoin(int BattlefieldID, ulong GUID)
        {
            logger.LogInformation("[B{0:0000}] Character [0x{1:X}] joined battlefield", BattlefieldID, GUID);
        }

        void IWorld.BattlefieldJoin(int BattlefieldID, ulong GUID)
        {
            //ILSpy generated this explicit interface implementation from .override directive in BattlefieldJoin
            BattlefieldJoin(BattlefieldID, GUID);
        }

        public void BattlefieldLeave(int BattlefieldID, ulong GUID)
        {
            logger.LogInformation("[B{0:0000}] Character [0x{1:X}] left battlefield", BattlefieldID, GUID);
        }

        void IWorld.BattlefieldLeave(int BattlefieldID, ulong GUID)
        {
            //ILSpy generated this explicit interface implementation from .override directive in BattlefieldLeave
            BattlefieldLeave(BattlefieldID, GUID);
        }
    }
}
