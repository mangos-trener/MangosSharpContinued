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

using Mangos.World.Objects.Factories;
using Microsoft.Extensions.Logging;
using System;

namespace Mangos.World.Loots;

public partial class WS_Loot
{
    public class LootItem : IDisposable
    {
        public int ItemID;

        public byte ItemCount;

        private bool _disposedValue;
        private readonly ILogger<LootItem> logger;
        private readonly WorldState worldState;
        private readonly ItemInfoFactory itemInfoFactory;
        private readonly LootStoreItem item;

        public int ItemModel
        {
            get
            {
                if (!worldState.ItemDatabase.ContainsKey(ItemID))
                {
                    try
                    {
                        worldState.ItemDatabase.Remove(ItemID);
                        var tmpItem = itemInfoFactory.Create(ItemID);
                        worldState.ItemDatabase.Add(ItemID, tmpItem);
                    }
                    catch (Exception ex)
                    {
                        logger.LogDebug("Error on ItemModel [Item ID {0} : Exception {1}]", ItemID, ex);
                    }
                }
                return worldState.ItemDatabase[ItemID].Model;
            }
        }

        public LootItem(ILogger<LootItem> logger, WorldState worldState, ItemInfoFactory itemInfoFactory, ref LootStoreItem Item)
        {
            this.logger = logger;
            ItemID = 0;
            ItemCount = 0;
            ItemID = Item.ItemID;
            checked
            {
                ItemCount = (byte)WorldState.Rnd.Next(Item.MinCountOrRef, Item.MaxCount + 1);
            }

            this.worldState = worldState;
            this.itemInfoFactory = itemInfoFactory;
            item = Item;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
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
}
