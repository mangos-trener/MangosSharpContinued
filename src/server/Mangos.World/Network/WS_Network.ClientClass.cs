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
using Mangos.Common.Globals;
using Mangos.Common.Legacy;
using Mangos.World.Globals;
using Mangos.World.Player;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Threading;

namespace Mangos.World.Network;

public partial class WS_Network
{
    public class ClientClass : ClientInfo, IDisposable
    {
        public CharacterObject Character;
        public ConcurrentQueue<Packets.PacketClass> PacketsQueue = new();
        public bool DEBUG_CONNECTION;
        private Thread ProcessQueueThread;
        private readonly ManualResetEvent ProcessQueueSempahore = new(false);
        private volatile bool IsActive = true;

        public ClientClass(ILogger<ClientClass> logger, ICluster cluster, WorldState worldState, ClientInfo ci, bool isDebug = false)
        {
            if (isDebug)
            {
                logger.LogWarning("Creating debug connection!", null);
                DEBUG_CONNECTION = true;
            }

            Access = ci.Access;
            Account = ci.Account;
            Index = ci.Index;
            IP = ci.IP;
            Port = ci.Port;

            ProcessQueueThread = new Thread(QueueProcessor)
            {
                IsBackground = true
            };
            ProcessQueueThread.Start();
            this.logger = logger;
            this.cluster = cluster;
            this.worldState = worldState;
            this.ci = ci;
            this.isDebug = isDebug;
        }

        public void PushPacket(Packets.PacketClass packet)
        {
            if (Character == null)
            {
                return;
            }

            PacketsQueue.Enqueue(packet);

            lock (_sempahoreLock)
            {
                ProcessQueueSempahore.Set();
            }
        }

        private readonly object _sempahoreLock = new();

        private void QueueProcessor()
        {
            try
            {
                while (IsActive)
                {
                    if (PacketsQueue.IsEmpty)
                    {
                        ProcessQueueSempahore.WaitOne();

                        if (!IsActive)
                        {
                            break;
                        }

                        lock (_sempahoreLock)
                        {
                            ProcessQueueSempahore.Reset();
                        }
                    }

                    while (PacketsQueue.TryDequeue(out var packet))
                    {
                        var tempPacket = packet;

                        using (tempPacket)
                        {
                            if (!WorldServer.PacketHandlers.ContainsKey(tempPacket.OpCode))
                            {
                                logger.LogWarning($"[{IP}:{Port}] Unknown Opcode 0x{(int)tempPacket.OpCode:X2} [DataLen={tempPacket.Data.Length} {tempPacket.OpCode}]");
                                DumpPacket(tempPacket);
                            }
                            else
                            {
                                var start = LegacyNativeMethods.TimeGetTime("");
                                checked
                                {
                                    try
                                    {
                                        var handlePacket = WorldServer.PacketHandlers[tempPacket.OpCode];
                                        var client = this;
                                        handlePacket(ref packet, ref client);

                                        if (LegacyNativeMethods.TimeGetTime("") - start > 100)
                                        {
                                            logger.LogWarning("Packet processing took too long: {0}, {1}ms", tempPacket.OpCode, LegacyNativeMethods.TimeGetTime("") - start);
                                        }
                                    }
                                    catch (Exception ex3)
                                    {
                                        DumpPacket(tempPacket);
                                        SetError(ex3, $"Opcode handler {tempPacket?.OpCode}:{tempPacket?.OpCode} caused an error: {ex3.Message}{Environment.NewLine}", LogType.FAILED);
                                        SetError(ex3, $"Connection from [{IP}:{Port}] cause error {ex3.Message}{Environment.NewLine}", LogType.FAILED);
                                        Delete();
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (ThreadInterruptedException) { } //Disposing
            catch (Exception ex)
            {
                SetError(ex, $"Connection from [{IP}:{Port}] cause error {ex.Message}{Environment.NewLine}", LogType.FAILED);
                Delete();
            }
        }

        private readonly object lockObj = new();
        private readonly ILogger<ClientClass> logger;
        private ICluster cluster;
        private readonly WorldState worldState;
        private readonly ClientInfo ci;
        private readonly bool isDebug;

        public void Send(ref byte[] data)
        {
            lock (lockObj)
            {
                try
                {
                    cluster.ClientSend(Index, data);
                }
                catch (Exception ex)
                {
                    SetError(ex, $"Connection from [{IP}:{Port}] cause error {ex.Message}{Environment.NewLine}", LogType.CRITICAL);

                    if (DEBUG_CONNECTION)
                    {
                        return;
                    }

                    cluster = null;
                    Delete();
                }
            }
        }

        public void Send(ref Packets.PacketClass packet)
        {
            lock (this)
            {
                try
                {
                    using (packet)
                    {
                        if (packet.OpCode == Opcodes.SMSG_UPDATE_OBJECT)
                        {
                            packet.CompressUpdatePacket();
                        }
                        packet.UpdateLength();

                        cluster?.ClientSend(Index, packet.Data);
                    }
                }
                catch (Exception ex)
                {
                    SetError(ex, $"Connection from [{IP}:{Port}] cause error {ex.Message}{Environment.NewLine}", LogType.CRITICAL);

                    if (DEBUG_CONNECTION)
                    {
                        return;
                    }

                    cluster = null;
                    Delete();
                }
            }
        }

        public void SendMultiplyPackets(ref Packets.PacketClass packet)
        {
            lock (this)
            {
                try
                {
                    if (packet.OpCode == Opcodes.SMSG_UPDATE_OBJECT)
                    {
                        packet.CompressUpdatePacket();
                    }
                    packet.UpdateLength();
                    var data = (byte[])packet.Data.Clone();

                    cluster?.ClientSend(Index, data);
                }
                catch (Exception ex)
                {
                    SetError(ex, $"Connection from [{IP}:{Port}] cause error {ex.Message}{Environment.NewLine}", LogType.CRITICAL);

                    if (DEBUG_CONNECTION)
                    {
                        return;
                    }

                    cluster = null;
                    Delete();
                }
            }
        }

        public void Disconnect()
        {
            Delete();
        }

        public void Delete()
        {
            try
            {
                Dispose();
            }
            catch (Exception ex)
            {
                SetError(ex, "", LogType.FAILED);
            }
        }

        private void SetError(Exception ex, string message, LogType logType)
        {
            logger.Log(LogLevel.Error, message, ex);
        }

        private void DumpPacket(Packets.PacketClass packet)
        {
            if (packet == null)
            {
                logger.LogWarning("Unable to dump packet");
                return;
            }

            try
            {
                Packets.DumpPacket(logger, packet.Data, this, 0);
            }
            catch (Exception ex)
            {
                logger.LogWarning("Unable to dump packet", ex);
            }
        }

        public void Dispose()
        {
            logger.LogInformation($"Connection from [{IP}:{Port}] disposed.");

            IsActive = false;
            ProcessQueueSempahore.Set(); //Allow thread to exit.
            ProcessQueueSempahore?.Dispose();

            try
            {
                ProcessQueueThread?.Interrupt();
                ProcessQueueThread?.Join(1000);
            }
            catch (ThreadInterruptedException ex)
            {
                logger.LogWarning("{0} Thread ID: {1}", ex, Thread.CurrentThread.ManagedThreadId);
            }
            ProcessQueueThread = null;

            PacketsQueue?.Clear();

            try
            {
                if (worldState.ConnectedClients.ContainsKey(Index))
                {
                    worldState.ConnectedClients.Remove(Index);
                }

                cluster?.ClientDrop(Index);

                if (worldState.ConnectedClients.ContainsKey(Index))
                {
                    worldState.ConnectedClients.Remove(Index);
                }

                if (Character != null)
                {
                    Character.client = null;
                    Character.Dispose();
                    Character = null;
                }
            }
            catch (Exception ex)
            {
                logger.LogError($"Connection from [{IP}:{Port}] was not properly disposed.", ex);
            }
        }
    }
}
