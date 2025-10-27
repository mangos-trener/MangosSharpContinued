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
using Mangos.Common.Legacy.Globals;
using Mangos.Configuration;
using Mangos.DataStores;
using Mangos.World.DataStores;
using Mangos.World.Globals;
using Mangos.World.Handlers;
using Mangos.World.Maps;
using Mangos.World.Network;
using Mangos.World.Objects;
using Mangos.World.Quests;
using Mangos.World.Scripts;
using Microsoft.Extensions.Logging;
using Microsoft.VisualBasic;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
//using Microsoft.VisualBasic.CompilerServices;
using System.Threading.Tasks;
using static Mangos.World.Network.WS_Network;

namespace Mangos.World;

public delegate void HandlePacket(ref Packets.PacketClass Packet, ref WS_Network.ClientClass client);

public class WorldServer
{
    public const int ServerSeed = -569166080;
    private readonly ILogger<WorldServer> logger;

    // DI
    private readonly WS_DBCDatabase _database;
    private readonly WS_Handlers _handlers;
    private readonly WS_Transports _transports;
    private readonly MangosConfiguration _configuration;
    private readonly WS_Maps _maps;
    private readonly WS_Quests quests;
    private readonly WS_GraveYards graveYards;

    public static Dictionary<Opcodes, HandlePacket> PacketHandlers;

    public SQL AccountDatabase;
    public SQL CharacterDatabase;
    public SQL WorldDatabase;

    public ICluster Cluster { get; set; }
    public IScriptExecutor ScriptExecutor { get; }
    public WS_Network.WorldServerClass CLSWorldServer { get; }
    public WorldState WorldState { get; }

    public WorldServer(
        ILogger<WorldServer> logger,
        ICluster cluster,
        IScriptExecutor scriptExecutor,
        MangosConfiguration configuration,
        WorldServerClass worldServer,
        DataStoreProvider dataStoreProvider,
        WS_DBCDatabase database,
        WS_Handlers handlers,
        WS_Transports transports,
        WS_Maps maps,
        WS_Quests quests,
        WS_GraveYards graveYards)
    {
        PacketHandlers = new Dictionary<Opcodes, HandlePacket>();
        AccountDatabase = new SQL();
        CharacterDatabase = new SQL();
        WorldDatabase = new SQL();
        this.logger = logger;
        Cluster = cluster;
        ScriptExecutor = scriptExecutor;
        CLSWorldServer = worldServer;

        _database = database;
        _handlers = handlers;
        _transports = transports;
        _configuration = configuration;
        _maps = maps;
        this.quests = quests;
        this.graveYards = graveYards;
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
    public void LoadConfig()
    {
        try
        {
            var args = Environment.GetCommandLineArgs();
            var array = args;
            var configuration = _configuration.World;

            Console.WriteLine(".[done]");

            if (!configuration.VMapsEnabled)
            {
                configuration.LineOfSightEnabled = false;
                configuration.HeightCalcEnabled = false;
            }

            var accountDBSettings = Strings.Split(configuration.AccountDatabase, ";");
            if (accountDBSettings.Length == 6)
            {
                AccountDatabase.SQLDBName = accountDBSettings[4];
                AccountDatabase.SQLHost = accountDBSettings[2];
                AccountDatabase.SQLPort = accountDBSettings[3];
                AccountDatabase.SQLUser = accountDBSettings[0];
                AccountDatabase.SQLPass = accountDBSettings[1];
                AccountDatabase.SQLTypeServer = (SQL.DB_Type)Conversion.Int(Enum.Parse(typeof(SQL.DB_Type), accountDBSettings[5]));
            }
            else
            {
                Console.WriteLine("Invalid connect string for the account database!");
            }

            var characterDBSettings = Strings.Split(configuration.CharacterDatabase, ";");
            if (characterDBSettings.Length == 6)
            {
                CharacterDatabase.SQLDBName = characterDBSettings[4];
                CharacterDatabase.SQLHost = characterDBSettings[2];
                CharacterDatabase.SQLPort = characterDBSettings[3];
                CharacterDatabase.SQLUser = characterDBSettings[0];
                CharacterDatabase.SQLPass = characterDBSettings[1];
                CharacterDatabase.SQLTypeServer = (SQL.DB_Type)Conversion.Int(Enum.Parse(typeof(SQL.DB_Type), characterDBSettings[5]));
            }
            else
            {
                Console.WriteLine("Invalid connect string for the character database!");
            }

            var worldDBSettings = Strings.Split(configuration.WorldDatabase, ";");
            if (worldDBSettings.Length == 6)
            {
                WorldDatabase.SQLDBName = worldDBSettings[4];
                WorldDatabase.SQLHost = worldDBSettings[2];
                WorldDatabase.SQLPort = worldDBSettings[3];
                WorldDatabase.SQLUser = worldDBSettings[0];
                WorldDatabase.SQLPass = worldDBSettings[1];
                WorldDatabase.SQLTypeServer = (SQL.DB_Type)Conversion.Int(Enum.Parse(typeof(SQL.DB_Type), worldDBSettings[5]));
            }
            else
            {
                Console.WriteLine("Invalid connect string for the world database!");
            }

            _maps.RESOLUTION_ZMAP = checked(configuration.MapResolution - 1);
            if (_maps.RESOLUTION_ZMAP < 63)
            {
                _maps.RESOLUTION_ZMAP = 63;
            }

            if (_maps.RESOLUTION_ZMAP > 255)
            {
                _maps.RESOLUTION_ZMAP = 255;
            }
        }
        catch (Exception ex)
        {
            var e = ex;
            Console.WriteLine(e.ToString());
        }
    }

    public void AccountSQLEventHandler(SQL.EMessages messageID, string outBuf)
    {
        ArgumentNullException.ThrowIfNull(outBuf);

        switch (messageID)
        {
            case SQL.EMessages.ID_Error:
                logger.LogError("[ACCOUNT] " + outBuf);
                break;

            case SQL.EMessages.ID_Message:
                logger.LogInformation("[ACCOUNT] " + outBuf);
                break;
        }
    }

    public void CharacterSQLEventHandler(SQL.EMessages messageID, string outBuf)
    {
        ArgumentNullException.ThrowIfNull(outBuf);

        switch (messageID)
        {
            case SQL.EMessages.ID_Error:
                logger.LogError("[CHARACTER] " + outBuf);
                break;

            case SQL.EMessages.ID_Message:
                logger.LogError("[CHARACTER] " + outBuf);
                break;
        }
    }

    public void WorldSQLEventHandler(SQL.EMessages messageID, string outBuf)
    {
        ArgumentNullException.ThrowIfNull(outBuf);

        switch (messageID)
        {
            case SQL.EMessages.ID_Error:
                logger.LogError("[WORLD] " + outBuf);
                break;

            case SQL.EMessages.ID_Message:
                logger.LogError("[WORLD] " + outBuf);
                break;
        }
    }

    [MTAThread]
    public async Task StartAsync()
    {
        Console.BackgroundColor = ConsoleColor.Black;
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("{0}", ((AssemblyProductAttribute)Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(AssemblyProductAttribute), inherit: false)[0]).Product);
        Console.WriteLine(((AssemblyCopyrightAttribute)Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(AssemblyCopyrightAttribute), inherit: false)[0]).Copyright);
        Console.WriteLine();
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("  __  __      _  _  ___  ___  ___   __   __ ___               ");
        Console.WriteLine(" |  \\/  |__ _| \\| |/ __|/ _ \\/ __|  \\ \\ / /| _ )      We Love ");
        Console.WriteLine(" | |\\/| / _` | .` | (_ | (_) \\__ \\   \\ V / | _ \\   Vanilla Wow");
        Console.WriteLine(" |_|  |_\\__,_|_|\\_|\\___|\\___/|___/    \\_/  |___/              ");
        Console.WriteLine("                                                              ");
        Console.WriteLine(" Website / Forum / Support: https://www.getmangos.eu/          ");
        Console.WriteLine("");
        Console.ForegroundColor = ConsoleColor.Magenta;
        Console.ForegroundColor = ConsoleColor.White;
        Console.WriteLine(((AssemblyTitleAttribute)Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(AssemblyTitleAttribute), inherit: false)[0]).Title);
        Console.WriteLine(" version {0}", Assembly.GetExecutingAssembly().GetName().Version);
        Console.ForegroundColor = ConsoleColor.White;
        Console.WriteLine("");
        Console.ForegroundColor = ConsoleColor.Gray;

        var dateTimeStarted = DateTime.Now;
        logger.LogInformation("[{0}] World Server Starting...", Strings.Format(DateAndTime.TimeOfDay, "hh:mm:ss"));

        var currentDomain = AppDomain.CurrentDomain;
        currentDomain.UnhandledException += new UnhandledExceptionEventHandler(GenericExceptionHandler);

        LoadConfig();

        Console.ForegroundColor = ConsoleColor.Gray;
        AccountDatabase.SQLMessage += AccountSQLEventHandler;
        CharacterDatabase.SQLMessage += CharacterSQLEventHandler;
        WorldDatabase.SQLMessage += WorldSQLEventHandler;

        var ReturnValues = AccountDatabase.Connect();
        if (ReturnValues > 0)
        {
            Console.WriteLine("[{0}] An SQL Error has occurred", Strings.Format(DateAndTime.TimeOfDay, "hh:mm:ss"));
            Console.WriteLine("*************************");
            Console.WriteLine("* Press any key to exit *");
            Console.WriteLine("*************************");
            Console.ReadKey();
        }

        AccountDatabase.Update("SET NAMES 'utf8';");
        ReturnValues = CharacterDatabase.Connect();
        if (ReturnValues > 0)
        {
            Console.WriteLine("[{0}] An SQL Error has occurred", Strings.Format(DateAndTime.TimeOfDay, "hh:mm:ss"));
            Console.WriteLine("*************************");
            Console.WriteLine("* Press any key to exit *");
            Console.WriteLine("*************************");
            Console.ReadKey();
        }

        CharacterDatabase.Update("SET NAMES 'utf8';");
        ReturnValues = WorldDatabase.Connect();
        if (ReturnValues > 0)
        {
            Console.WriteLine("[{0}] An SQL Error has occurred", Strings.Format(DateAndTime.TimeOfDay, "hh:mm:ss"));
            Console.WriteLine("*************************");
            Console.WriteLine("* Press any key to exit *");
            Console.WriteLine("*************************");
            Console.ReadKey();
        }

        WorldDatabase.Update("SET NAMES 'utf8';");
        var areDbVersionsOk = true;
        if (!LegacyGlobalFunctions.CheckRequiredDbVersion(AccountDatabase, ServerDb.Realm))
        {
            areDbVersionsOk = false;
        }

        if (!LegacyGlobalFunctions.CheckRequiredDbVersion(CharacterDatabase, ServerDb.Character))
        {
            areDbVersionsOk = false;
        }

        if (!LegacyGlobalFunctions.CheckRequiredDbVersion(WorldDatabase, ServerDb.World))
        {
            areDbVersionsOk = false;
        }

        if (!areDbVersionsOk)
        {
            Console.WriteLine("*************************");
            Console.WriteLine("* Press any key to exit *");
            Console.WriteLine("*************************");
            Console.ReadKey();
        }

        await _database.InitializeInternalDatabaseAsync();

        _handlers.IntializePacketHandlers();

        quests.LoadAllQuests();
        //WorldState.QuestsService.LoadAllQuests();
        await graveYards.InitializeGraveyardsAsync();
        //await WorldState.GraveyardsService.InitializeGraveyardsAsync();
        _transports.LoadTransports();

        var worldConfiguration = _configuration.World;
        CLSWorldServer.ClusterConnect();
        GC.Collect();

        if (Process.GetCurrentProcess().PriorityClass == ProcessPriorityClass.High)
        {
            logger.LogError("Setting Process Priority to HIGH..[done]");
        }
        else
        {
            logger.LogError("Setting Process Priority to NORMAL..[done]");
        }

        logger.LogInformation(" Load Time:   {0}", Strings.Format(DateAndTime.DateDiff(DateInterval.Second, dateTimeStarted, DateAndTime.Now), "0 seconds"));
        logger.LogInformation(" Used Memory: {0}", Strings.Format(GC.GetTotalMemory(forceFullCollection: false), "### ### ##0 bytes"));
    }

    public void WaitConsoleCommand()
    {
        var tmp = "";
        var cmd = Array.Empty<string>();
        while (!CLSWorldServer.FlagStopListen)
        {
            try
            {
                tmp = Console.ReadLine();
                var CommandList = tmp.Split(";");
                var num = Information.LBound(CommandList);
                var num2 = Information.UBound(CommandList);

                for (var varList = num; varList <= num2; varList = checked(varList + 1))
                {
                    var cmds = Strings.Split(CommandList[varList], " ", 2);
                    if (CommandList[varList].Length > 0)
                    {
                        switch (cmds[0].ToLower())
                        {
                            case "shutdown":
                                logger.LogError("Server shutting down...");
                                CLSWorldServer.FlagStopListen = true;
                                break;

                            case "info":
                                logger.LogInformation("Used memory: {0}", Strings.Format(GC.GetTotalMemory(forceFullCollection: false), "### ### ##0 bytes"));
                                break;

                            case "help":
                                Console.ForegroundColor = ConsoleColor.Blue;
                                Console.WriteLine("'WorldServer' Command list:");
                                Console.ForegroundColor = ConsoleColor.White;
                                Console.WriteLine("---------------------------------");
                                Console.WriteLine("");
                                Console.WriteLine("");
                                Console.WriteLine("'help' - Brings up the 'WorldServer' Command list (this).");
                                Console.WriteLine("");
                                Console.WriteLine("'info' - Brings up a context menu showing server information (such as memory used).");
                                Console.WriteLine("");
                                Console.WriteLine("'shutdown' - Shuts down 'WorldServer'.");
                                break;

                            default:
                                Console.ForegroundColor = ConsoleColor.Red;
                                Console.WriteLine("Error! Cannot find specified command. Please type 'help' for information on 'WorldServer' console commands.");
                                Console.ForegroundColor = ConsoleColor.Gray;
                                break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                var e = ex;
                logger.LogError("Error executing command [{0}]. {2}{1}", Strings.Format(DateAndTime.TimeOfDay, "hh:mm:ss"), tmp, e.ToString(), Environment.NewLine);
            }
        }
    }

    private async void GenericExceptionHandler(object sender, UnhandledExceptionEventArgs e)
    {
        try
        {
            Exception EX = (Exception)e.ExceptionObject;

            logger.LogCritical(EX + Environment.NewLine);
            logger.LogError("Unexpected error has occured. An 'WorldServer-Error-yyyy-mmm-d-h-mm.log' file has been created. Check your log folder for more information.");

            var filename = @"""""""""WorldServer-Error-"" + ""{(DateTime.Now, "" + ""yyyy-MMM-d-H-mm"" + "")}.log""""""""";
            filename = @"{filename}";
            await new StreamWriter(new FileStream(filename, FileMode.Append)).WriteAsync(EX.Message + EX.StackTrace);
            //await new StreamWriter(new FileStream(filename, FileMode.Append)).DisposeAsync();
            //new StreamWriter(new FileStream(filename, FileMode.Append)).Close();

        }
        finally
        {
            Thread.Sleep(5000); //Wait 5 Seconds to Ensure logs are created and the Operator has a chance to view the Exception in Console.
            Environment.FailFast("An Unhandled Exception has occured and the Server has Crashed!"); //Named event log and Ensure the Server closes out at all times.
        }
    }

    public int QueryWorldDatabase(string query, ref DataTable dataTable)
    {
        var result = WorldDatabase.Query(query, ref dataTable);

        return result;
    }
}
