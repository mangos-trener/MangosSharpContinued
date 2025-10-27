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
using Mangos.Common.Legacy;
using Mangos.Common.Legacy.Databases;
using Mangos.Configuration;
using Mangos.DataStores;
using Mangos.World.Handlers;
using Mangos.World.Player;
using Mangos.World.Services;
using Microsoft.Extensions.Logging;
using Microsoft.VisualBasic;
using Microsoft.VisualBasic.CompilerServices;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Threading.Tasks;

namespace Mangos.World.Maps;

public class WS_GraveYards : IDisposable
{
    private readonly ILogger<WS_GraveYards> logger;
    private readonly DataStoreProvider dataStoreProvider;
    private readonly MangosConfiguration _configuration;
    private readonly WorldDatabase worldDatabase;
    private readonly WS_Maps maps;
    private readonly ICharacterResurrectionService characterResurrectionService;
    private bool _disposedValue;

    public WS_GraveYards(
        ILogger<WS_GraveYards> logger,
        DataStoreProvider dataStoreProvider,
        MangosConfiguration configuration,
        WorldDatabase worldDatabase,
        WS_Maps maps,
        ICharacterResurrectionService characterResurrectionService)

    {
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        this.dataStoreProvider = dataStoreProvider;
        _configuration = configuration;
        this.worldDatabase = worldDatabase;
        this.maps = maps;
        this.characterResurrectionService = characterResurrectionService;
        Graveyards = new Dictionary<int, TGraveyard>();
    }

    public Dictionary<int, TGraveyard> Graveyards;

    public struct TGraveyard
    {
        private float _locationPosX;

        private float _locationPosY;

        private float _locationPosZ;

        public int X
        {
            get => checked((int)Math.Round(_locationPosX));
            set => _locationPosX = value;
        }

        public int Y
        {
            get => checked((int)Math.Round(_locationPosY));
            set => _locationPosY = value;
        }

        public int Z
        {
            get => checked((int)Math.Round(_locationPosZ));
            set => _locationPosZ = value;
        }

        public int Map { get; set; }

        public TGraveyard(float locationPosX, float locationPosY, float locationPosZ, int locationMapID)
        {
            this = default;
            _locationPosX = locationPosX;
            _locationPosY = locationPosY;
            _locationPosZ = locationPosZ;
            Map = locationMapID;
        }
    }

    public void AddGraveYard(int ID, float locationPosX, float locationPosY, float locationPosZ, int locationMapID)
    {
        Graveyards.Add(ID, new TGraveyard(locationPosX, locationPosY, locationPosZ, locationMapID));
    }

    public TGraveyard GetGraveYard(int ID)
    {
        return Graveyards[ID];
    }

    public async Task InitializeGraveyardsAsync()
    {
        checked
        {
            try
            {
                Graveyards.Clear();
                var tmpDBC = await dataStoreProvider.GetDataStoreAsync("WorldSafeLocs.dbc");
                logger.LogInformation("Loading.... {0} Graveyard Locations", tmpDBC.Rows - 1);
                for (var i = 0; i <= tmpDBC.Rows - 1; i++)
                {
                    var locationMapID = tmpDBC.ReadInt(i, 1);
                    if (_configuration.World.Maps.Contains(locationMapID))
                    {
                        var locationIndex = tmpDBC.ReadInt(i, 0);
                        var locationPosX = tmpDBC.ReadFloat(i, 2);
                        var locationPosY = tmpDBC.ReadFloat(i, 3);
                        var locationPosZ = tmpDBC.ReadFloat(i, 4);
                        Graveyards.Add(locationIndex, new TGraveyard(locationPosX, locationPosY, locationPosZ, locationMapID));
                        logger.LogDebug(": Map: {0}  X: {1}  Y: {2}  Z: {3}", locationMapID, locationPosX, locationPosY, locationPosZ);
                    }
                }
                logger.LogInformation("Finished loading Graveyard Locations", tmpDBC.Rows - 1);
                logger.LogInformation("DBC: {0} Graveyards initialized.", tmpDBC.Rows - 1);
            }
            catch (DirectoryNotFoundException ex)
            {
                logger.LogWarning("DBC File : WorldSafeLocs is Missing", ex);
            }
        }
    }

    public void GoToNearestGraveyard(ref CharacterObject Character, bool Alive, bool Teleport)
    {
        checked
        {
            DataTable GraveQuery = new();
            var foundNear = false;
            var distNear = 0f;
            TGraveyard entryNear = default;
            TGraveyard entryFar = default;
            if (maps.Maps[Character.MapID].IsDungeon | maps.Maps[Character.MapID].IsBattleGround | maps.Maps[Character.MapID].IsRaid)
            {
                Character.ZoneCheckInstance();
                var Ghostzone = maps.AreaTable[maps.GetAreaIDByMapandParent((int)Character.MapID, maps.AreaTable[maps.GetAreaFlag(Character.resurrectPositionX, Character.resurrectPositionY, (int)Character.MapID)].Zone)].ID;
                worldDatabase.Query($"SELECT id, faction FROM game_graveyard_zone WHERE ghost_zone = {Ghostzone} and (faction = 0 or faction = {Character.Team}) ", ref GraveQuery);
                if (GraveQuery.Rows.Count == 0)
                {
                    logger.LogInformation("GraveYards: No near graveyards for map [{0}], zone [{1}]", Character.MapID, Character.ZoneID);
                    return;
                }
                if (maps.Maps[Character.MapID].IsDungeon | maps.Maps[Character.MapID].IsBattleGround | maps.Maps[Character.MapID].IsRaid)
                {
                    if (Graveyards.ContainsKey(Conversions.ToInteger(GraveQuery.Rows[0]["id"])))
                    {
                        entryFar = Graveyards[Conversions.ToInteger(GraveQuery.Rows[0]["id"])];
                        entryNear = entryFar;
                    }
                    else
                    {
                        logger.LogInformation("GraveYard: {0} is missing for map [{1}], zone [{2}]", GraveQuery.Rows[0]["id"], Character.MapID, Character.ZoneID);
                    }
                }
                else
                {
                    IEnumerator enumerator = default;
                    try
                    {
                        enumerator = GraveQuery.Rows.GetEnumerator();
                        while (enumerator.MoveNext())
                        {
                            DataRow row = (DataRow)enumerator.Current;
                            var GraveyardID2 = row.As<int>("id");
                            var GraveyardFaction2 = row.As<int>("faction");
                            if (!Graveyards.ContainsKey(GraveyardID2))
                            {
                                logger.LogInformation("GraveYards: Graveyard link invalid [{0}]", GraveyardID2);
                            }
                            else if (Character.MapID != Graveyards[GraveyardID2].Map)
                            {
                                if (Information.IsNothing(entryFar))
                                {
                                    entryFar = Graveyards[GraveyardID2];
                                }
                            }
                            else
                            {
                                if (GraveyardFaction2 != 0 && GraveyardFaction2 != Character.Team)
                                {
                                    continue;
                                }
                                var dist3 = WS_Combat.GetDistance(Character.positionX, Graveyards[GraveyardID2].X, Character.positionY, Graveyards[GraveyardID2].Y, Character.positionZ, Graveyards[GraveyardID2].Z);
                                if (foundNear)
                                {
                                    if (dist3 < distNear)
                                    {
                                        distNear = dist3;
                                        entryNear = Graveyards[GraveyardID2];
                                    }
                                }
                                else
                                {
                                    foundNear = true;
                                    distNear = dist3;
                                    entryNear = Graveyards[GraveyardID2];
                                }
                            }
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
                var selectedGraveyard2 = entryNear;
                if (Information.IsNothing(selectedGraveyard2))
                {
                    selectedGraveyard2 = entryFar;
                }
                logger.LogInformation("GraveYards: GraveYard.Map[{0}], GraveYard.X[{1}], GraveYard.Y[{2}], GraveYard.Z[{3}]", selectedGraveyard2.Map, selectedGraveyard2.X, selectedGraveyard2.Y, selectedGraveyard2.Z);
                Character.Teleport(selectedGraveyard2.X, selectedGraveyard2.Y, selectedGraveyard2.Z, 0f, selectedGraveyard2.Map);
                Character.SendDeathReleaseLoc(selectedGraveyard2.X, selectedGraveyard2.Y, selectedGraveyard2.Z, selectedGraveyard2.Map);
                return;
            }
            Character.ZoneCheck();
            worldDatabase.Query($"SELECT id, faction FROM game_graveyard_zone WHERE ghost_zone = {Character.ZoneID}", ref GraveQuery);
            if (GraveQuery.Rows.Count == 0)
            {
                logger.LogInformation("GraveYards: No near graveyards for map [{0}], zone [{1}]", Character.MapID, Character.ZoneID);
                return;
            }
            IEnumerator enumerator2 = default;
            try
            {
                enumerator2 = GraveQuery.Rows.GetEnumerator();
                while (enumerator2.MoveNext())
                {
                    DataRow row = (DataRow)enumerator2.Current;
                    var GraveyardID = row.As<int>("id");
                    var GraveyardFaction = row.As<int>("faction");
                    if (!Graveyards.ContainsKey(GraveyardID))
                    {
                        logger.LogInformation("GraveYards: Graveyard link invalid [{0}]", GraveyardID);
                    }
                    else if (Character.MapID != Graveyards[GraveyardID].Map)
                    {
                        if (Information.IsNothing(entryFar))
                        {
                            entryFar = Graveyards[GraveyardID];
                        }
                    }
                    else
                    {
                        if (GraveyardFaction != 0 && GraveyardFaction != Character.Team)
                        {
                            continue;
                        }
                        var dist2 = WS_Combat.GetDistance(Character.positionX, Graveyards[GraveyardID].X, Character.positionY, Graveyards[GraveyardID].Y, Character.positionZ, Graveyards[GraveyardID].Z);
                        if (foundNear)
                        {
                            if (dist2 < distNear)
                            {
                                distNear = dist2;
                                entryNear = Graveyards[GraveyardID];
                            }
                        }
                        else
                        {
                            foundNear = true;
                            distNear = dist2;
                            entryNear = Graveyards[GraveyardID];
                        }
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
            var selectedGraveyard = entryNear;
            if (Information.IsNothing(selectedGraveyard))
            {
                selectedGraveyard = entryFar;
            }
            if (Teleport)
            {
                if (Alive & Character.DEAD)
                {
                    characterResurrectionService.CharacterResurrect(ref Character);
                    Character.Life.Current = Character.Life.Maximum;
                    if (Character.ManaType == ManaTypes.TYPE_MANA)
                    {
                        Character.Mana.Current = Character.Mana.Maximum;
                    }
                    if (selectedGraveyard.Map == Character.MapID)
                    {
                        Character.SetUpdateFlag(22, Character.Life.Current);
                        if (Character.ManaType == ManaTypes.TYPE_MANA)
                        {
                            Character.SetUpdateFlag(23, Character.Mana.Current);
                        }
                        Character.SendCharacterUpdate();
                    }
                }
                logger.LogInformation("GraveYards: GraveYard.Map[{0}], GraveYard.X[{1}], GraveYard.Y[{2}], GraveYard.Z[{3}]", selectedGraveyard.Map, selectedGraveyard.X, selectedGraveyard.Y, selectedGraveyard.Z);
                Character.Teleport(selectedGraveyard.X, selectedGraveyard.Y, selectedGraveyard.Z, 0f, selectedGraveyard.Map);
                Character.SendDeathReleaseLoc(selectedGraveyard.X, selectedGraveyard.Y, selectedGraveyard.Z, selectedGraveyard.Map);
            }
            else
            {
                Character.positionX = selectedGraveyard.X;
                Character.positionY = selectedGraveyard.Y;
                Character.positionZ = selectedGraveyard.Z;
                Character.MapID = (uint)selectedGraveyard.Map;
            }
        }
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposedValue || disposing)
        {
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
}
