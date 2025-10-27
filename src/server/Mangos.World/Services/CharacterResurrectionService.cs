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

using Mangos.Common.Enums.Player;
using Mangos.Common.Globals;
using Mangos.Common.Legacy.Databases;
using Mangos.World.Player;
using Microsoft.Extensions.Logging;
using System;

namespace Mangos.World.Services;
public class CharacterResurrectionService : ICharacterResurrectionService
{
    private readonly ILogger<CharacterResurrectionService> logger;
    private readonly WorldState worldState;
    private readonly CharacterDatabase characterDatabase;
    private readonly ICellUpdater characterMovementUpdater;

    public CharacterResurrectionService(
        ILogger<CharacterResurrectionService> logger,
        WorldState worldState,
        CharacterDatabase characterDatabase,
        ICellUpdater characterMovementUpdater)
    {
        this.logger = logger;
        this.worldState = worldState;
        this.characterDatabase = characterDatabase;
        this.characterMovementUpdater = characterMovementUpdater;
    }

    public void CharacterResurrect(ref CharacterObject Character)
    {
        if (Character.repopTimer != null)
        {
            Character.repopTimer.Dispose();
            Character.repopTimer = null;
        }

        Character.Mana.Current = 0;
        Character.Rage.Current = 0;
        Character.Energy.Current = 0;
        Character.Life.Current = checked((int)Math.Round(Character.Life.Maximum / 2.0));
        Character.DEAD = false;
        Character.cPlayerFlags &= ~PlayerFlags.PLAYER_FLAGS_DEAD;
        Character.cUnitFlags = 8;
        Character.cDynamicFlags = 0;

        Character.InvisibilityReset();
        characterMovementUpdater.UpdateCell(ref Character);
        Character.SetLandWalk();

        if (Character.Race == Races.RACE_NIGHT_ELF)
        {
            Character.RemoveAuraBySpell(20584);
        }
        else
        {
            Character.RemoveAuraBySpell(8326);
        }

        Character.SetUpdateFlag(22, Character.Life.Current);
        Character.SetUpdateFlag(190, (int)Character.cPlayerFlags);
        Character.SetUpdateFlag(46, Character.cUnitFlags);
        Character.SetUpdateFlag(143, Character.cDynamicFlags);
        Character.SendCharacterUpdate();

        if (decimal.Compare(new decimal(Character.corpseGUID), 0m) != 0)
        {
            if (worldState.WorldCorpseObjects.ContainsKey(Character.corpseGUID))
            {
                worldState.WorldCorpseObjects[Character.corpseGUID].ConvertToBones();
            }
            else
            {
                logger.LogDebug("Corpse wasn't found [{0}]!", checked(Character.corpseGUID - MangosGlobalConstants.GUID_CORPSE));
                characterDatabase.Update($"DELETE FROM corpse WHERE player = \"{Character.GUID}\";");
            }

            Character.corpseGUID = 0uL;
            Character.corpseMapID = 0;
            Character.corpsePositionX = 0f;
            Character.corpsePositionY = 0f;
            Character.corpsePositionZ = 0f;
        }
    }
}
