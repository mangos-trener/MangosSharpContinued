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
using Mangos.World.Maps;
using Mangos.World.Objects.Factories.Packets;
using Mangos.World.Services;
using Mangos.World.Spells;
using Microsoft.Extensions.Logging;
using Microsoft.VisualBasic.CompilerServices;
using System;
using System.Collections.Generic;

namespace Mangos.World.Objects;

public class WS_DynamicObjects
{
    public class DynamicObject : WS_Base.BaseObject, IDisposable
    {
        public int SpellID;
        private readonly float posX;
        private readonly float posY;
        private readonly float posZ;
        public List<WS_Spells.SpellEffect> Effects;
        public int Duration;
        public float Radius;
        private readonly ILogger<DynamicObject> logger;
        public WS_Base.BaseUnit Caster;
        private readonly WS_Maps maps;
        private readonly IMapTileLoader mapTileLoader;
        private readonly UpdateClassFactory updateClassFactory;
        public int CastTime;
        public int Bytes;
        private bool _disposedValue;

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                worldState.WorldDynamicObjects.Remove(GUID);
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

        public DynamicObject(ILogger<DynamicObject> logger, WorldState worldState, ref WS_Base.BaseUnit caster, WS_Maps maps, IMapTileLoader mapTileLoader, UpdateClassFactory updateClassFactory, int spellId, float posX, float posY, float posZ, int duration, float radius)
            : base(worldState, null)
        {
            SpellID = 0;
            Effects = new List<WS_Spells.SpellEffect>();
            Duration = 0;
            Radius = 0f;
            CastTime = 0;
            Bytes = 1;
            GUID = GetNewGUID();
            worldState.WorldDynamicObjects.Add(GUID, this);
            this.logger = logger;
            Caster = caster;
            this.maps = maps;
            this.mapTileLoader = mapTileLoader;
            this.updateClassFactory = updateClassFactory;
            SpellID = spellId;
            this.posX = posX;
            this.posY = posY;
            this.posZ = posZ;
            positionX = posX;
            positionY = posY;
            positionZ = posZ;
            orientation = 0f;
            MapID = Caster.MapID;
            instance = Caster.instance;
            Duration = duration;
            Radius = radius;
            CastTime = LegacyNativeMethods.TimeGetTime("");
        }

        public void FillAllUpdateFlags(ref Packets.UpdateClass Update)
        {
            Update.SetUpdateFlag(0, GUID);
            Update.SetUpdateFlag(2, 65);
            Update.SetUpdateFlag(4, 0.5f * Radius);
            Update.SetUpdateFlag(6, Caster.GUID);
            Update.SetUpdateFlag(8, Bytes);
            Update.SetUpdateFlag(9, SpellID);
            Update.SetUpdateFlag(10, Radius);
            Update.SetUpdateFlag(11, positionX);
            Update.SetUpdateFlag(12, positionY);
            Update.SetUpdateFlag(13, positionZ);
            Update.SetUpdateFlag(14, orientation);
        }

        public void AddToWorld()
        {
            maps.GetMapTile(positionX, positionY, ref CellX, ref CellY);
            if (maps.Maps[MapID].Tiles[CellX, CellY] == null)
            {
                mapTileLoader.LoadMap(CellX, CellY, MapID);
            }
            try
            {
                maps.Maps[MapID].Tiles[CellX, CellY].DynamicObjectsHere.Add(GUID);
            }
            catch (Exception projectError)
            {
                ProjectData.SetProjectError(projectError);
                logger.LogWarning("AddToWorld failed MapId: {0} Tile XY: {1} {2} GUID: {3}", MapID, CellX, CellY, GUID);
                ProjectData.ClearProjectError();
                return;
            }
            Packets.PacketClass packet = new(Opcodes.SMSG_UPDATE_OBJECT);
            packet.AddInt32(1);
            packet.AddInt8(0);
            var tmpUpdate = updateClassFactory.Create(MangosGlobalConstants.FIELD_MASK_SIZE_DYNAMICOBJECT);
            FillAllUpdateFlags(ref tmpUpdate);
            var updateClass = tmpUpdate;
            var updateObject = this;
            updateClass.AddToPacket(ref packet, ObjectUpdateType.UPDATETYPE_CREATE_OBJECT_SELF, ref updateObject);
            tmpUpdate.Dispose();
            short i = -1;
            checked
            {
                do
                {
                    short j = -1;
                    do
                    {
                        if ((short)unchecked(CellX + i) >= 0 && (short)unchecked(CellX + i) <= 63 && (short)unchecked(CellY + j) >= 0 && (short)unchecked(CellY + j) <= 63 && maps.Maps[MapID].Tiles[(short)unchecked(CellX + i), (short)unchecked(CellY + j)] != null && maps.Maps[MapID].Tiles[(short)unchecked(CellX + i), (short)unchecked(CellY + j)].PlayersHere.Count > 0)
                        {
                            var tMapTile = maps.Maps[MapID].Tiles[(short)unchecked(CellX + i), (short)unchecked(CellY + j)];
                            var list = tMapTile.PlayersHere.ToArray();
                            var array = list;
                            foreach (var plGUID in array)
                            {
                                int num;
                                if (worldState.Characters.ContainsKey(plGUID))
                                {
                                    var characterObject = worldState.Characters[plGUID];
                                    WS_Base.BaseObject objCharacter = this;
                                    num = characterObject.CanSee(ref objCharacter) ? 1 : 0;
                                }
                                else
                                {
                                    num = 0;
                                }
                                if (num != 0)
                                {
                                    worldState.Characters[plGUID].client.SendMultiplyPackets(ref packet);
                                    worldState.Characters[plGUID].dynamicObjectsNear.Add(GUID);
                                    SeenBy.Add(plGUID);
                                }
                            }
                        }
                        j = (short)unchecked(j + 1);
                    }
                    while (j <= 1);
                    i = (short)unchecked(i + 1);
                }
                while (i <= 1);
                packet.Dispose();
            }
        }

        public void RemoveFromWorld()
        {
            maps.GetMapTile(positionX, positionY, ref CellX, ref CellY);
            maps.Maps[MapID].Tiles[CellX, CellY].DynamicObjectsHere.Remove(GUID);
            var array = SeenBy.ToArray();
            foreach (var plGUID in array)
            {
                if (worldState.Characters[plGUID].dynamicObjectsNear.Contains(GUID))
                {
                    worldState.Characters[plGUID].guidsForRemoving_Lock.AcquireWriterLock(MangosGlobalConstants.DEFAULT_LOCK_TIMEOUT);
                    worldState.Characters[plGUID].guidsForRemoving.Add(GUID);
                    worldState.Characters[plGUID].guidsForRemoving_Lock.ReleaseWriterLock();
                    worldState.Characters[plGUID].dynamicObjectsNear.Remove(GUID);
                }
            }
        }

        public void AddEffect(WS_Spells.SpellEffect EffectInfo)
        {
            Effects.Add(EffectInfo);
        }

        public void RemoveEffect(WS_Spells.SpellEffect EffectInfo)
        {
            Effects.Remove(EffectInfo);
        }

        public bool Update()
        {
            if (Caster == null)
            {
                return true;
            }
            var DeleteThis = false;
            checked
            {
                if (Duration > 1000)
                {
                    Duration -= 1000;
                }
                else
                {
                    DeleteThis = true;
                }
            }
            foreach (var effect in Effects)
            {
                var Effect = effect;
                if (Effect.GetRadius == 0f)
                {
                    if (Effect.Amplitude == 0 || checked(WS_Spells.SPELLs[SpellID].GetDuration - Duration) % Effect.Amplitude == 0)
                    {
                        var obj = WS_Spells.AURAs[Effect.ApplyAuraIndex];
                        ref var caster = ref Caster;
                        WS_Base.BaseObject baseObject = this;
                        obj(ref caster, ref baseObject, ref Effect, SpellID, 1, AuraAction.AURA_UPDATE);
                    }
                    continue;
                }
                var Targets = spells.GetEnemyAtPoint(ref Caster, positionX, positionY, positionZ, Effect.GetRadius);
                foreach (var item in Targets)
                {
                    var Target = item;
                    if (Effect.Amplitude == 0 || checked(WS_Spells.SPELLs[SpellID].GetDuration - Duration) % Effect.Amplitude == 0)
                    {
                        var obj2 = WS_Spells.AURAs[Effect.ApplyAuraIndex];
                        WS_Base.BaseObject baseObject = this;
                        obj2(ref Target, ref baseObject, ref Effect, SpellID, 1, AuraAction.AURA_UPDATE);
                    }
                }
            }
            if (DeleteThis)
            {
                Caster.dynamicObjects.Remove(this);
                return true;
            }
            return false;
        }

        public void Spawn()
        {
            AddToWorld();
            Packets.PacketClass packet = new(Opcodes.SMSG_GAMEOBJECT_SPAWN_ANIM);
            packet.AddUInt64(GUID);
            SendToNearPlayers(ref packet);
            packet.Dispose();
        }

        public void Delete()
        {
            if (Caster != null && Caster.dynamicObjects.Contains(this))
            {
                Caster.dynamicObjects.Remove(this);
            }
            Packets.PacketClass packet = new(Opcodes.SMSG_GAMEOBJECT_DESPAWN_ANIM);
            packet.AddUInt64(GUID);
            SendToNearPlayers(ref packet);
            packet.Dispose();
            RemoveFromWorld();
            Dispose();
        }
    }

    private static ulong GetNewGUID()
    {
        return ++WorldState.DynamicObjectsGuidCounter;
    }
}
