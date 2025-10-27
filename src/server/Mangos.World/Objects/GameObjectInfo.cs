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

using Mangos.Common.Enums.GameObject;
using Mangos.Common.Legacy;
using Mangos.Common.Legacy.Databases;
using Mangos.World.Maps;
using Microsoft.Extensions.Logging;
using Microsoft.VisualBasic.CompilerServices;
using System;
using System.Data;

namespace Mangos.World.Objects;

public class GameObjectInfo : IDisposable
{
    private readonly ILogger<GameObjectInfo> logger;
    private readonly WorldState worldState;
    private readonly WorldDatabase worldDatabase;
    private readonly WS_Maps maps;

    public int ID;

    public int Model;

    public GameObjectType Type;

    public string Name;

    public short Faction;

    public int Flags;

    public float Size;

    public uint[] Fields;

    public string ScriptName;

    private bool _disposedValue;

    public GameObjectInfo(ILogger<GameObjectInfo> logger, WorldState worldState, WorldDatabase worldDatabase, WS_Maps maps, int ID_)
    {
        ID = 0;
        Model = 0;
        Type = GameObjectType.GAMEOBJECT_TYPE_DOOR;
        Name = "";
        Faction = 0;
        Flags = 0;
        Size = 1f;
        Fields = new uint[24];
        ScriptName = "";
        this.logger = logger;
        this.worldState = worldState;
        this.worldDatabase = worldDatabase;
        this.maps = maps;
        ID = ID_;
        worldState.GameObjectsDatabase.Add(ID, this);
        DataTable MySQLQuery = new();
        worldDatabase.Query($"SELECT * FROM gameobject_template WHERE entry = {ID_};", ref MySQLQuery);
        if (MySQLQuery.Rows.Count == 0)
        {
            logger.LogError("gameobject_template {0} not found in SQL database!", ID_);
            return;
        }
        Model = MySQLQuery.Rows[0].As<int>("displayId");
        Type = (GameObjectType)MySQLQuery.Rows[0].As<byte>("type");
        Name = MySQLQuery.Rows[0].As<string>("name");
        Faction = MySQLQuery.Rows[0].As<short>("faction");
        Flags = MySQLQuery.Rows[0].As<int>("flags");
        Size = MySQLQuery.Rows[0].As<float>("size");
        byte i = 0;
        do
        {
            Fields[i] = MySQLQuery.Rows[0].As<uint>("data" + Conversions.ToString(i));
            checked
            {
                i = (byte)unchecked((uint)(i + 1));
            }
        }
        while (i <= 23u);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposedValue)
        {
            worldState.GameObjectsDatabase.Remove(ID);
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
