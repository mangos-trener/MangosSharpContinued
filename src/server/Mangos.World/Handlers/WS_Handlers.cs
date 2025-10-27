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

using Mangos.Common.Globals;
using Mangos.World.Auction;
using Mangos.World.Globals;
using Mangos.World.Loots;
using Mangos.World.Network;
using Mangos.World.Objects;
using Mangos.World.Quests;
using Mangos.World.Social;
using Mangos.World.Spells;

namespace Mangos.World.Handlers;

public class WS_Handlers(
    WS_Handlers_Warden warden,
    WS_Handlers_Misc misc,
    WS_Handlers_Chat chat,
    WS_Handlers_Trade trade,
    WS_Handlers_Taxi taxi,
    WS_Handlers_Battleground battleground,
    WS_Handlers_Gamemaster gamemaster,
    CharManagementHandler charHandler,
    WS_CharMovement movement,
    WS_Mail mail,
    WS_Loot loot,
    WS_Combat combat,
    WS_GameObjects gameObjects,
    WS_Items items,
    WS_Creatures creatures,
    WS_NPCs npcs,
    WS_Auction auction,
    WS_Pets pets,
    WS_Spells spells,
    WS_Guilds guilds,
    WS_Quests quests)
{
    private readonly WS_Handlers_Warden _warden = warden;
    private readonly WS_Handlers_Misc _misc = misc;
    private readonly WS_Handlers_Chat _chat = chat;
    private readonly WS_Handlers_Trade _trade = trade;
    private readonly WS_Handlers_Taxi _taxi = taxi;
    private readonly WS_Handlers_Battleground _battleground = battleground;
    private readonly WS_Handlers_Gamemaster _gamemaster = gamemaster;
    private readonly CharManagementHandler _charHandler = charHandler;
    private readonly WS_CharMovement _movement = movement;
    private readonly WS_Mail _mail = mail;
    private readonly WS_Loot _loot = loot;
    private readonly WS_Combat _combat = combat;
    private readonly WS_GameObjects _gameObjects = gameObjects;
    private readonly WS_Items _items = items;
    private readonly WS_Creatures _creatures = creatures;
    private readonly WS_NPCs _npcs = npcs;
    private readonly WS_Auction _auction = auction;
    private readonly WS_Pets _pets = pets;
    private readonly WS_Spells _spells = spells;
    private readonly WS_Guilds _guilds = guilds;
    private readonly WS_Quests _quests = quests;

    public void IntializePacketHandlers()
    {
        WorldServer.PacketHandlers[Opcodes.CMSG_FORCE_MOVE_ROOT_ACK] = OnUnhandledPacket;
        WorldServer.PacketHandlers[Opcodes.CMSG_FORCE_MOVE_UNROOT_ACK] = OnUnhandledPacket;
        WorldServer.PacketHandlers[Opcodes.CMSG_MOVE_WATER_WALK_ACK] = OnUnhandledPacket;
        WorldServer.PacketHandlers[Opcodes.MSG_MOVE_TELEPORT_ACK] = OnUnhandledPacket;
        WorldServer.PacketHandlers[Opcodes.CMSG_WARDEN_DATA] = _warden.On_CMSG_WARDEN_DATA;
        WorldServer.PacketHandlers[Opcodes.CMSG_NAME_QUERY] = _misc.On_CMSG_NAME_QUERY;
        WorldServer.PacketHandlers[Opcodes.CMSG_MESSAGECHAT] = _chat.On_CMSG_MESSAGECHAT;
        WorldServer.PacketHandlers[Opcodes.CMSG_LOGOUT_REQUEST] = _charHandler.On_CMSG_LOGOUT_REQUEST;
        WorldServer.PacketHandlers[Opcodes.CMSG_LOGOUT_CANCEL] = _charHandler.On_CMSG_LOGOUT_CANCEL;
        WorldServer.PacketHandlers[Opcodes.CMSG_CANCEL_TRADE] = _trade.On_CMSG_CANCEL_TRADE;
        WorldServer.PacketHandlers[Opcodes.CMSG_BEGIN_TRADE] = _trade.On_CMSG_BEGIN_TRADE;
        WorldServer.PacketHandlers[Opcodes.CMSG_UNACCEPT_TRADE] = _trade.On_CMSG_UNACCEPT_TRADE;
        WorldServer.PacketHandlers[Opcodes.CMSG_ACCEPT_TRADE] = _trade.On_CMSG_ACCEPT_TRADE;
        WorldServer.PacketHandlers[Opcodes.CMSG_INITIATE_TRADE] = _trade.On_CMSG_INITIATE_TRADE;
        WorldServer.PacketHandlers[Opcodes.CMSG_SET_TRADE_GOLD] = _trade.On_CMSG_SET_TRADE_GOLD;
        WorldServer.PacketHandlers[Opcodes.CMSG_SET_TRADE_ITEM] = _trade.On_CMSG_SET_TRADE_ITEM;
        WorldServer.PacketHandlers[Opcodes.CMSG_CLEAR_TRADE_ITEM] = _trade.On_CMSG_CLEAR_TRADE_ITEM;
        WorldServer.PacketHandlers[Opcodes.CMSG_IGNORE_TRADE] = _trade.On_CMSG_IGNORE_TRADE;
        WorldServer.PacketHandlers[Opcodes.CMSG_BUSY_TRADE] = _trade.On_CMSG_BUSY_TRADE;
        WorldServer.PacketHandlers[Opcodes.MSG_MOVE_START_FORWARD] = _movement.OnMovementPacket;
        WorldServer.PacketHandlers[Opcodes.MSG_MOVE_START_BACKWARD] = _movement.OnMovementPacket;
        WorldServer.PacketHandlers[Opcodes.MSG_MOVE_STOP] = _movement.OnMovementPacket;
        WorldServer.PacketHandlers[Opcodes.MSG_MOVE_START_STRAFE_LEFT] = _movement.OnMovementPacket;
        WorldServer.PacketHandlers[Opcodes.MSG_MOVE_START_STRAFE_RIGHT] = _movement.OnMovementPacket;
        WorldServer.PacketHandlers[Opcodes.MSG_MOVE_STOP_STRAFE] = _movement.OnMovementPacket;
        WorldServer.PacketHandlers[Opcodes.MSG_MOVE_JUMP] = _movement.OnMovementPacket;
        WorldServer.PacketHandlers[Opcodes.MSG_MOVE_START_TURN_LEFT] = _movement.OnMovementPacket;
        WorldServer.PacketHandlers[Opcodes.MSG_MOVE_START_TURN_RIGHT] = _movement.OnMovementPacket;
        WorldServer.PacketHandlers[Opcodes.MSG_MOVE_STOP_TURN] = _movement.OnMovementPacket;
        WorldServer.PacketHandlers[Opcodes.MSG_MOVE_START_PITCH_UP] = _movement.OnMovementPacket;
        WorldServer.PacketHandlers[Opcodes.MSG_MOVE_START_PITCH_DOWN] = _movement.OnMovementPacket;
        WorldServer.PacketHandlers[Opcodes.MSG_MOVE_STOP_PITCH] = _movement.OnMovementPacket;
        WorldServer.PacketHandlers[Opcodes.MSG_MOVE_SET_RUN_MODE] = _movement.OnMovementPacket;
        WorldServer.PacketHandlers[Opcodes.MSG_MOVE_SET_WALK_MODE] = _movement.OnMovementPacket;
        WorldServer.PacketHandlers[Opcodes.MSG_MOVE_START_SWIM] = _movement.OnStartSwim;
        WorldServer.PacketHandlers[Opcodes.MSG_MOVE_STOP_SWIM] = _movement.OnStopSwim;
        WorldServer.PacketHandlers[Opcodes.MSG_MOVE_SET_FACING] = _movement.OnMovementPacket;
        WorldServer.PacketHandlers[Opcodes.MSG_MOVE_SET_PITCH] = _movement.OnMovementPacket;
        WorldServer.PacketHandlers[Opcodes.CMSG_MOVE_FALL_RESET] = _misc.On_CMSG_MOVE_FALL_RESET;
        WorldServer.PacketHandlers[Opcodes.MSG_MOVE_HEARTBEAT] = _movement.On_MSG_MOVE_HEARTBEAT;
        WorldServer.PacketHandlers[Opcodes.CMSG_AREATRIGGER] = _movement.On_CMSG_AREATRIGGER;
        WorldServer.PacketHandlers[Opcodes.MSG_MOVE_FALL_LAND] = _movement.On_MSG_MOVE_FALL_LAND;
        WorldServer.PacketHandlers[Opcodes.CMSG_ZONEUPDATE] = _movement.On_CMSG_ZONEUPDATE;
        WorldServer.PacketHandlers[Opcodes.CMSG_FORCE_RUN_SPEED_CHANGE_ACK] = _movement.OnChangeSpeed;
        WorldServer.PacketHandlers[Opcodes.CMSG_FORCE_RUN_BACK_SPEED_CHANGE_ACK] = _movement.OnChangeSpeed;
        WorldServer.PacketHandlers[Opcodes.CMSG_FORCE_SWIM_SPEED_CHANGE_ACK] = _movement.OnChangeSpeed;
        WorldServer.PacketHandlers[Opcodes.CMSG_FORCE_SWIM_BACK_SPEED_CHANGE_ACK] = _movement.OnChangeSpeed;
        WorldServer.PacketHandlers[Opcodes.CMSG_FORCE_TURN_RATE_CHANGE_ACK] = _movement.OnChangeSpeed;
        WorldServer.PacketHandlers[Opcodes.CMSG_STANDSTATECHANGE] = _charHandler.On_CMSG_STANDSTATECHANGE;
        WorldServer.PacketHandlers[Opcodes.CMSG_SET_SELECTION] = _combat.On_CMSG_SET_SELECTION;
        WorldServer.PacketHandlers[Opcodes.CMSG_REPOP_REQUEST] = _misc.On_CMSG_REPOP_REQUEST;
        WorldServer.PacketHandlers[Opcodes.MSG_CORPSE_QUERY] = _misc.On_MSG_CORPSE_QUERY;
        WorldServer.PacketHandlers[Opcodes.CMSG_SPIRIT_HEALER_ACTIVATE] = _creatures.On_CMSG_SPIRIT_HEALER_ACTIVATE;
        WorldServer.PacketHandlers[Opcodes.CMSG_RECLAIM_CORPSE] = _misc.On_CMSG_RECLAIM_CORPSE;
        WorldServer.PacketHandlers[Opcodes.CMSG_TUTORIAL_FLAG] = _misc.On_CMSG_TUTORIAL_FLAG;
        WorldServer.PacketHandlers[Opcodes.CMSG_TUTORIAL_CLEAR] = _misc.On_CMSG_TUTORIAL_CLEAR;
        WorldServer.PacketHandlers[Opcodes.CMSG_TUTORIAL_RESET] = _misc.On_CMSG_TUTORIAL_RESET;
        WorldServer.PacketHandlers[Opcodes.CMSG_SET_ACTION_BUTTON] = _charHandler.On_CMSG_SET_ACTION_BUTTON;
        WorldServer.PacketHandlers[Opcodes.CMSG_SET_ACTIONBAR_TOGGLES] = _misc.On_CMSG_SET_ACTIONBAR_TOGGLES;
        WorldServer.PacketHandlers[Opcodes.CMSG_TOGGLE_HELM] = _misc.On_CMSG_TOGGLE_HELM;
        WorldServer.PacketHandlers[Opcodes.CMSG_TOGGLE_CLOAK] = _misc.On_CMSG_TOGGLE_CLOAK;
        WorldServer.PacketHandlers[Opcodes.CMSG_MOUNTSPECIAL_ANIM] = _misc.On_CMSG_MOUNTSPECIAL_ANIM;
        WorldServer.PacketHandlers[Opcodes.CMSG_EMOTE] = _misc.On_CMSG_EMOTE;
        WorldServer.PacketHandlers[Opcodes.CMSG_TEXT_EMOTE] = _misc.On_CMSG_TEXT_EMOTE;
        WorldServer.PacketHandlers[Opcodes.CMSG_ITEM_QUERY_SINGLE] = _items.On_CMSG_ITEM_QUERY_SINGLE;
        WorldServer.PacketHandlers[Opcodes.CMSG_ITEM_NAME_QUERY] = _items.On_CMSG_ITEM_NAME_QUERY;
        WorldServer.PacketHandlers[Opcodes.CMSG_SETSHEATHED] = _combat.On_CMSG_SETSHEATHED;
        WorldServer.PacketHandlers[Opcodes.CMSG_SWAP_INV_ITEM] = _items.On_CMSG_SWAP_INV_ITEM;
        WorldServer.PacketHandlers[Opcodes.CMSG_SPLIT_ITEM] = _items.On_CMSG_SPLIT_ITEM;
        WorldServer.PacketHandlers[Opcodes.CMSG_AUTOEQUIP_ITEM] = _items.On_CMSG_AUTOEQUIP_ITEM;
        WorldServer.PacketHandlers[Opcodes.CMSG_AUTOSTORE_BAG_ITEM] = _items.On_CMSG_AUTOSTORE_BAG_ITEM;
        WorldServer.PacketHandlers[Opcodes.CMSG_SWAP_ITEM] = _items.On_CMSG_SWAP_ITEM;
        WorldServer.PacketHandlers[Opcodes.CMSG_DESTROYITEM] = _items.On_CMSG_DESTROYITEM;
        WorldServer.PacketHandlers[Opcodes.CMSG_READ_ITEM] = _items.On_CMSG_READ_ITEM;
        WorldServer.PacketHandlers[Opcodes.CMSG_PAGE_TEXT_QUERY] = _items.On_CMSG_PAGE_TEXT_QUERY;
        WorldServer.PacketHandlers[Opcodes.CMSG_USE_ITEM] = _items.On_CMSG_USE_ITEM;
        WorldServer.PacketHandlers[Opcodes.CMSG_OPEN_ITEM] = _items.On_CMSG_OPEN_ITEM;
        WorldServer.PacketHandlers[Opcodes.CMSG_WRAP_ITEM] = _items.On_CMSG_WRAP_ITEM;
        WorldServer.PacketHandlers[Opcodes.CMSG_SET_AMMO] = _combat.On_CMSG_SET_AMMO;
        WorldServer.PacketHandlers[Opcodes.CMSG_CREATURE_QUERY] = _creatures.On_CMSG_CREATURE_QUERY;
        WorldServer.PacketHandlers[Opcodes.CMSG_GOSSIP_HELLO] = _creatures.On_CMSG_GOSSIP_HELLO;
        WorldServer.PacketHandlers[Opcodes.CMSG_GOSSIP_SELECT_OPTION] = _creatures.On_CMSG_GOSSIP_SELECT_OPTION;
        WorldServer.PacketHandlers[Opcodes.CMSG_NPC_TEXT_QUERY] = _creatures.On_CMSG_NPC_TEXT_QUERY;
        WorldServer.PacketHandlers[Opcodes.CMSG_LIST_INVENTORY] = _npcs.On_CMSG_LIST_INVENTORY;
        WorldServer.PacketHandlers[Opcodes.CMSG_BUY_ITEM_IN_SLOT] = _npcs.On_CMSG_BUY_ITEM_IN_SLOT;
        WorldServer.PacketHandlers[Opcodes.CMSG_BUY_ITEM] = _npcs.On_CMSG_BUY_ITEM;
        WorldServer.PacketHandlers[Opcodes.CMSG_BUYBACK_ITEM] = _npcs.On_CMSG_BUYBACK_ITEM;
        WorldServer.PacketHandlers[Opcodes.CMSG_SELL_ITEM] = _npcs.On_CMSG_SELL_ITEM;
        WorldServer.PacketHandlers[Opcodes.CMSG_REPAIR_ITEM] = _npcs.On_CMSG_REPAIR_ITEM;
        WorldServer.PacketHandlers[Opcodes.CMSG_ATTACKSWING] = _combat.On_CMSG_ATTACKSWING;
        WorldServer.PacketHandlers[Opcodes.CMSG_ATTACKSTOP] = _combat.On_CMSG_ATTACKSTOP;
        WorldServer.PacketHandlers[Opcodes.CMSG_GAMEOBJECT_QUERY] = _gameObjects.On_CMSG_GAMEOBJECT_QUERY;
        WorldServer.PacketHandlers[Opcodes.CMSG_GAMEOBJ_USE] = _gameObjects.On_CMSG_GAMEOBJ_USE;
        WorldServer.PacketHandlers[Opcodes.CMSG_BATTLEFIELD_STATUS] = _misc.On_CMSG_BATTLEFIELD_STATUS;
        WorldServer.PacketHandlers[Opcodes.CMSG_SET_ACTIVE_MOVER] = _misc.On_CMSG_SET_ACTIVE_MOVER;
        WorldServer.PacketHandlers[Opcodes.CMSG_MEETINGSTONE_INFO] = _misc.On_CMSG_MEETINGSTONE_INFO;
        WorldServer.PacketHandlers[Opcodes.MSG_INSPECT_HONOR_STATS] = _misc.On_MSG_INSPECT_HONOR_STATS;
        WorldServer.PacketHandlers[Opcodes.MSG_PVP_LOG_DATA] = _misc.On_MSG_PVP_LOG_DATA;
        WorldServer.PacketHandlers[Opcodes.CMSG_MOVE_TIME_SKIPPED] = _movement.On_CMSG_MOVE_TIME_SKIPPED;
        WorldServer.PacketHandlers[Opcodes.CMSG_GET_MAIL_LIST] = _mail.On_CMSG_GET_MAIL_LIST;
        WorldServer.PacketHandlers[Opcodes.CMSG_SEND_MAIL] = _mail.On_CMSG_SEND_MAIL;
        WorldServer.PacketHandlers[Opcodes.CMSG_MAIL_CREATE_TEXT_ITEM] = _mail.On_CMSG_MAIL_CREATE_TEXT_ITEM;
        WorldServer.PacketHandlers[Opcodes.CMSG_ITEM_TEXT_QUERY] = _mail.On_CMSG_ITEM_TEXT_QUERY;
        WorldServer.PacketHandlers[Opcodes.CMSG_MAIL_DELETE] = _mail.On_CMSG_MAIL_DELETE;
        WorldServer.PacketHandlers[Opcodes.CMSG_MAIL_TAKE_ITEM] = _mail.On_CMSG_MAIL_TAKE_ITEM;
        WorldServer.PacketHandlers[Opcodes.CMSG_MAIL_TAKE_MONEY] = _mail.On_CMSG_MAIL_TAKE_MONEY;
        WorldServer.PacketHandlers[Opcodes.CMSG_MAIL_RETURN_TO_SENDER] = _mail.On_CMSG_MAIL_RETURN_TO_SENDER;
        WorldServer.PacketHandlers[Opcodes.CMSG_MAIL_MARK_AS_READ] = _mail.On_CMSG_MAIL_MARK_AS_READ;
        WorldServer.PacketHandlers[Opcodes.MSG_QUERY_NEXT_MAIL_TIME] = _mail.On_MSG_QUERY_NEXT_MAIL_TIME;
        WorldServer.PacketHandlers[Opcodes.CMSG_AUTOSTORE_LOOT_ITEM] = _loot.On_CMSG_AUTOSTORE_LOOT_ITEM;
        WorldServer.PacketHandlers[Opcodes.CMSG_LOOT_MONEY] = _loot.On_CMSG_LOOT_MONEY;
        WorldServer.PacketHandlers[Opcodes.CMSG_LOOT] = _loot.On_CMSG_LOOT;
        WorldServer.PacketHandlers[Opcodes.CMSG_LOOT_ROLL] = _loot.On_CMSG_LOOT_ROLL;
        WorldServer.PacketHandlers[Opcodes.CMSG_LOOT_RELEASE] = _loot.On_CMSG_LOOT_RELEASE;
        WorldServer.PacketHandlers[Opcodes.CMSG_TAXINODE_STATUS_QUERY] = _taxi.On_CMSG_TAXINODE_STATUS_QUERY;
        WorldServer.PacketHandlers[Opcodes.CMSG_TAXIQUERYAVAILABLENODES] = _taxi.On_CMSG_TAXIQUERYAVAILABLENODES;
        WorldServer.PacketHandlers[Opcodes.CMSG_ACTIVATETAXI] = _taxi.On_CMSG_ACTIVATETAXI;
        WorldServer.PacketHandlers[Opcodes.CMSG_ACTIVATETAXI_FAR] = _taxi.On_CMSG_ACTIVATETAXI_FAR;
        WorldServer.PacketHandlers[Opcodes.CMSG_MOVE_SPLINE_DONE] = _taxi.On_CMSG_MOVE_SPLINE_DONE;
        WorldServer.PacketHandlers[Opcodes.CMSG_CAST_SPELL] = _spells.On_CMSG_CAST_SPELL;
        WorldServer.PacketHandlers[Opcodes.CMSG_CANCEL_CAST] = _spells.On_CMSG_CANCEL_CAST;
        WorldServer.PacketHandlers[Opcodes.CMSG_CANCEL_AURA] = _spells.On_CMSG_CANCEL_AURA;
        WorldServer.PacketHandlers[Opcodes.CMSG_CANCEL_AUTO_REPEAT_SPELL] = _spells.On_CMSG_CANCEL_AUTO_REPEAT_SPELL;
        WorldServer.PacketHandlers[Opcodes.CMSG_CANCEL_CHANNELLING] = _spells.On_CMSG_CANCEL_CHANNELLING;
        WorldServer.PacketHandlers[Opcodes.CMSG_TOGGLE_PVP] = _misc.On_CMSG_TOGGLE_PVP;
        WorldServer.PacketHandlers[Opcodes.MSG_BATTLEGROUND_PLAYER_POSITIONS] = _battleground.On_MSG_BATTLEGROUND_PLAYER_POSITIONS;
        WorldServer.PacketHandlers[Opcodes.CMSG_QUESTGIVER_STATUS_QUERY] = _quests.On_CMSG_QUESTGIVER_STATUS_QUERY;
        WorldServer.PacketHandlers[Opcodes.CMSG_QUESTGIVER_HELLO] = _quests.On_CMSG_QUESTGIVER_HELLO;
        WorldServer.PacketHandlers[Opcodes.CMSG_QUESTGIVER_QUERY_QUEST] = _quests.On_CMSG_QUESTGIVER_QUERY_QUEST;
        WorldServer.PacketHandlers[Opcodes.CMSG_QUESTGIVER_ACCEPT_QUEST] = _quests.On_CMSG_QUESTGIVER_ACCEPT_QUEST;
        WorldServer.PacketHandlers[Opcodes.CMSG_QUESTLOG_REMOVE_QUEST] = _quests.On_CMSG_QUESTLOG_REMOVE_QUEST;
        WorldServer.PacketHandlers[Opcodes.CMSG_QUEST_QUERY] = _quests.On_CMSG_QUEST_QUERY;
        WorldServer.PacketHandlers[Opcodes.CMSG_QUESTGIVER_COMPLETE_QUEST] = _quests.On_CMSG_QUESTGIVER_COMPLETE_QUEST;
        WorldServer.PacketHandlers[Opcodes.CMSG_QUESTGIVER_REQUEST_REWARD] = _quests.On_CMSG_QUESTGIVER_REQUEST_REWARD;
        WorldServer.PacketHandlers[Opcodes.CMSG_QUESTGIVER_CHOOSE_REWARD] = _quests.On_CMSG_QUESTGIVER_CHOOSE_REWARD;
        WorldServer.PacketHandlers[Opcodes.MSG_QUEST_PUSH_RESULT] = _quests.On_MSG_QUEST_PUSH_RESULT;
        WorldServer.PacketHandlers[Opcodes.CMSG_PUSHQUESTTOPARTY] = _quests.On_CMSG_PUSHQUESTTOPARTY;
        WorldServer.PacketHandlers[Opcodes.CMSG_BINDER_ACTIVATE] = _npcs.On_CMSG_BINDER_ACTIVATE;
        WorldServer.PacketHandlers[Opcodes.CMSG_BANKER_ACTIVATE] = _npcs.On_CMSG_BANKER_ACTIVATE;
        WorldServer.PacketHandlers[Opcodes.CMSG_BUY_BANK_SLOT] = _npcs.On_CMSG_BUY_BANK_SLOT;
        WorldServer.PacketHandlers[Opcodes.CMSG_AUTOBANK_ITEM] = _npcs.On_CMSG_AUTOBANK_ITEM;
        WorldServer.PacketHandlers[Opcodes.CMSG_AUTOSTORE_BANK_ITEM] = _npcs.On_CMSG_AUTOSTORE_BANK_ITEM;
        WorldServer.PacketHandlers[Opcodes.MSG_TALENT_WIPE_CONFIRM] = _npcs.On_MSG_TALENT_WIPE_CONFIRM;
        WorldServer.PacketHandlers[Opcodes.CMSG_TRAINER_BUY_SPELL] = _npcs.On_CMSG_TRAINER_BUY_SPELL;
        WorldServer.PacketHandlers[Opcodes.CMSG_TRAINER_LIST] = _npcs.On_CMSG_TRAINER_LIST;
        WorldServer.PacketHandlers[Opcodes.MSG_AUCTION_HELLO] = _auction.On_MSG_AUCTION_HELLO;
        WorldServer.PacketHandlers[Opcodes.CMSG_AUCTION_SELL_ITEM] = _auction.On_CMSG_AUCTION_SELL_ITEM;
        WorldServer.PacketHandlers[Opcodes.CMSG_AUCTION_REMOVE_ITEM] = _auction.On_CMSG_AUCTION_REMOVE_ITEM;
        WorldServer.PacketHandlers[Opcodes.CMSG_AUCTION_LIST_ITEMS] = _auction.On_CMSG_AUCTION_LIST_ITEMS;
        WorldServer.PacketHandlers[Opcodes.CMSG_AUCTION_LIST_OWNER_ITEMS] = _auction.On_CMSG_AUCTION_LIST_OWNER_ITEMS;
        WorldServer.PacketHandlers[Opcodes.CMSG_AUCTION_PLACE_BID] = _auction.On_CMSG_AUCTION_PLACE_BID;
        WorldServer.PacketHandlers[Opcodes.CMSG_AUCTION_LIST_BIDDER_ITEMS] = _auction.On_CMSG_AUCTION_LIST_BIDDER_ITEMS;
        WorldServer.PacketHandlers[Opcodes.CMSG_PETITION_SHOWLIST] = _guilds.On_CMSG_PETITION_SHOWLIST;
        WorldServer.PacketHandlers[Opcodes.CMSG_PETITION_BUY] = _guilds.On_CMSG_PETITION_BUY;
        WorldServer.PacketHandlers[Opcodes.CMSG_PETITION_SHOW_SIGNATURES] = _guilds.On_CMSG_PETITION_SHOW_SIGNATURES;
        WorldServer.PacketHandlers[Opcodes.CMSG_PETITION_QUERY] = _guilds.On_CMSG_PETITION_QUERY;
        WorldServer.PacketHandlers[Opcodes.CMSG_OFFER_PETITION] = _guilds.On_CMSG_OFFER_PETITION;
        WorldServer.PacketHandlers[Opcodes.CMSG_PETITION_SIGN] = _guilds.On_CMSG_PETITION_SIGN;
        WorldServer.PacketHandlers[Opcodes.MSG_PETITION_RENAME] = _guilds.On_MSG_PETITION_RENAME;
        WorldServer.PacketHandlers[Opcodes.MSG_PETITION_DECLINE] = _guilds.On_MSG_PETITION_DECLINE;
        WorldServer.PacketHandlers[Opcodes.CMSG_BATTLEMASTER_HELLO] = _battleground.On_CMSG_BATTLEMASTER_HELLO;
        WorldServer.PacketHandlers[Opcodes.CMSG_BATTLEFIELD_LIST] = _battleground.On_CMSG_BATTLEMASTER_HELLO;
        WorldServer.PacketHandlers[Opcodes.CMSG_DUEL_CANCELLED] = _spells.On_CMSG_DUEL_CANCELLED;
        WorldServer.PacketHandlers[Opcodes.CMSG_DUEL_ACCEPTED] = _spells.On_CMSG_DUEL_ACCEPTED;
        WorldServer.PacketHandlers[Opcodes.CMSG_RESURRECT_RESPONSE] = _spells.On_CMSG_RESURRECT_RESPONSE;
        WorldServer.PacketHandlers[Opcodes.CMSG_LEARN_TALENT] = _spells.On_CMSG_LEARN_TALENT;
        WorldServer.PacketHandlers[Opcodes.CMSG_WORLD_TELEPORT] = _gamemaster.On_CMSG_WORLD_TELEPORT;
        WorldServer.PacketHandlers[Opcodes.CMSG_SET_FACTION_ATWAR] = _misc.On_CMSG_SET_FACTION_ATWAR;
        WorldServer.PacketHandlers[Opcodes.CMSG_SET_FACTION_INACTIVE] = _misc.On_CMSG_SET_FACTION_INACTIVE;
        WorldServer.PacketHandlers[Opcodes.CMSG_SET_WATCHED_FACTION] = _misc.On_CMSG_SET_WATCHED_FACTION;
        WorldServer.PacketHandlers[Opcodes.CMSG_PET_NAME_QUERY] = _pets.On_CMSG_PET_NAME_QUERY;
        WorldServer.PacketHandlers[Opcodes.CMSG_REQUEST_PET_INFO] = _pets.On_CMSG_REQUEST_PET_INFO;
        WorldServer.PacketHandlers[Opcodes.CMSG_PET_ACTION] = _pets.On_CMSG_PET_ACTION;
        WorldServer.PacketHandlers[Opcodes.CMSG_PET_CANCEL_AURA] = _pets.On_CMSG_PET_CANCEL_AURA;
        WorldServer.PacketHandlers[Opcodes.CMSG_PET_ABANDON] = _pets.On_CMSG_PET_ABANDON;
        WorldServer.PacketHandlers[Opcodes.CMSG_PET_RENAME] = _pets.On_CMSG_PET_RENAME;
        WorldServer.PacketHandlers[Opcodes.CMSG_PET_SET_ACTION] = _pets.On_CMSG_PET_SET_ACTION;
        WorldServer.PacketHandlers[Opcodes.CMSG_PET_SPELL_AUTOCAST] = _pets.On_CMSG_PET_SPELL_AUTOCAST;
        WorldServer.PacketHandlers[Opcodes.CMSG_PET_STOP_ATTACK] = _pets.On_CMSG_PET_STOP_ATTACK;
        WorldServer.PacketHandlers[Opcodes.CMSG_PET_UNLEARN] = _pets.On_CMSG_PET_UNLEARN;
    }

    public void OnUnhandledPacket(ref Packets.PacketClass packet, ref WS_Network.ClientClass client)
    {
        //WorldServer.Log.WriteLine(LogType.WARNING, "[{0}:{1}] {2} [Unhandled Packet]", client.IP, client.Port, packet.OpCode);
    }

    //public void OnWorldPacket(ref Packets.PacketClass packet, ref WS_Network.ClientClass client)
    //{
    //    WorldServer.Log.WriteLine(LogType.WARNING, "[{0}:{1}] {2} [Redirected Packet]", client.IP, client.Port, packet.OpCode);
    //    if (client.Character == null || !client.Character.FullyLoggedIn)
    //    {
    //        WorldServer.Log.WriteLine(LogType.WARNING, "[{0}:{1}] Unknown Opcode 0x{2:X} [{2}], DataLen={4}", client.IP, client.Port, packet.OpCode, Environment.NewLine, packet.Length);
    //        Packets.DumpPacket(packet.Data, client);
    //    }
    //}
}
