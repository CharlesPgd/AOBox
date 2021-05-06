using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AOSharp.Core;
using AOSharp.Core.UI;
using AOSharp.Core.Movement;
using AOSharp.Common.GameData;
using System.IO;
using AOSharp.Core.GameData;
using AOSharp.Core.UI.Options;
using AOSharp.Pathfinding;
using System.Data;
using AOSharp.Core.IPC;
using InfBuddy.IPCMessages;
using System.Collections.Concurrent;

namespace InfBuddy
{
    public class InfBuddy : AOPluginEntry
    {
        private StateMachine _stateMachine;
        public static NavMeshMovementController NavMeshMovementController { get; private set; }
        public static IPCChannel IPCChannel { get; private set; }
        public static Config Config { get; private set; }
        public static GlobalSettings ActiveGlobalSettings;
        public static bool Running = false;
        public static Identity Leader = Identity.None;
        public static bool IsLeader = false;

        public override void Run(string pluginDir)
        {
            Chat.WriteLine("InfBuddy Loaded!");

            try
            {
                Config = Config.Load($"{Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)}\\AOSharp\\InfBuddy\\Config.json");
                ActiveGlobalSettings = Config.GlobalSettings;
                _stateMachine = new StateMachine(new IdleState());
                NavMeshMovementController = new NavMeshMovementController($"{pluginDir}\\NavMeshes", true);
                MovementController.Set(NavMeshMovementController);

                Chat.RegisterCommand("infbuddy", InfbuddyCommand);

                IPCChannel = new IPCChannel(10);
                IPCChannel.RegisterCallback((int)IPCOpcode.Start, OnStartMessage);
                IPCChannel.RegisterCallback((int)IPCOpcode.Stop, OnStopMessage);

                NpcDialog.AnswerListChanged += NpcDialog_AnswerListChanged;
                Team.TeamRequest += OnTeamRequest;
                Game.OnUpdate += OnUpdate;
            }
            catch(Exception e)
            {
                Chat.WriteLine(e.Message);
            }
        }

        private void Start()
        {
            Running = true;

            if(!IsLeader && !(_stateMachine.CurrentState is IdleState))
                _stateMachine.SetState(new IdleState());
        }

        private void Stop()
        {
            Running = false;
            _stateMachine.SetState(new IdleState());
            NavMeshMovementController.Halt();
        }

        public static void StartNextRound()
        {
            IPCChannel.Broadcast(new StartMessage()
            {
                MissionDifficulty = Config.GlobalSettings.MissionDifficulty,
                MissionFaction = Config.GlobalSettings.MissionFaction
            });

            ActiveGlobalSettings = Config.GlobalSettings.Clone();
        }

        private void OnStartMessage(int sender, IPCMessage msg)
        {
            StartMessage startMsg = (StartMessage)msg;

            Chat.WriteLine("OnStartMessage");
            ActiveGlobalSettings = new GlobalSettings
            {
                MissionDifficulty = startMsg.MissionDifficulty,
                MissionFaction = startMsg.MissionFaction
            };
            Leader = new Identity(IdentityType.SimpleChar, sender);
            Start();
        }

        private void OnStopMessage(int sender, IPCMessage msg)
        {
            Chat.WriteLine("OnStopMessage");
            Stop();
        }

        private void OnUpdate(object s, float deltaTime)
        {
            if (Game.IsZoning)
                return;

            _stateMachine.Tick();

            if (!IsLeader)
                return;
        }

        private void OnTeamRequest(object s, TeamRequestEventArgs e)
        {
            if (e.Requester != Leader)
            {
                if(Running)
                    e.Ignore();

                return;
            }

            e.Accept();
        }

        private void PrintInfBuddyCommandUsage(ChatWindow chatWindow)
        {
            string help = "Usage:\nStart - Begins doing bot things\n" +
                            "Stop - Stops doing bot things\n" +
                            "Config - Displays the current config\n" +
                            "\t\tDifficulty - [easy|medium|hard]\n"+
                            "\t\tFaction - [neut|clan|omni]\n" +"" +
                            "\t\tIsLeech - [true|false]\n\n" +
                            "Examples:\n" +
                            "\t/infbuddy config difficulty medium - Will set the mission difficulty to Medium\n" +
                            "\t/infbuddy config isleech true - Will put the current character in leech mode\n" +
                            "\t/infbuddy config isleech - Will display the current character's isleech value\n" + 
                            "etc...";

            chatWindow.WriteLine(help, ChatColor.LightBlue);
        }

        private void InfbuddyCommand(string command, string[] param, ChatWindow chatWindow)
        {
            try
            {
                if (param.Length < 1)
                {
                    PrintInfBuddyCommandUsage(chatWindow);
                    return;
                }

                switch (param[0].ToLower())
                {
                    case "start":
                        if (!Team.IsLeader)
                        {
                            chatWindow.WriteLine("Only the team leader can use start", ChatColor.Yellow);
                            return;
                        }

                        IsLeader = true;
                        Start();
                        StartNextRound();
                        break;
                    case "stop":
                        Stop();
                        IPCChannel.Broadcast(new StopMessage());
                        break;
                    case "config":
                        if (param.Length == 1)
                        {
                            chatWindow.WriteLine($"Infbuddy Config:", ChatColor.LightBlue);
                            chatWindow.WriteLine($"\tMissionDifficulty(Global): {Config.GlobalSettings.MissionDifficulty}", ChatColor.LightBlue);
                            chatWindow.WriteLine($"\tMissionFaction(Global): {Config.GlobalSettings.MissionFaction}", ChatColor.LightBlue);
                            chatWindow.WriteLine($"\tIsLeech({DynelManager.LocalPlayer.Name}): {Config.IsLeech}", ChatColor.LightBlue);
                            return;
                        }

                        if (param.Length < 2)
                        {
                            PrintInfBuddyCommandUsage(chatWindow);
                            return;
                        }

                        switch (param[1].ToLower())
                        {
                            case "difficulty":
                                if (param.Length == 2)
                                {
                                    chatWindow.WriteLine($"MissionDifficulty(Global): {Config.GlobalSettings.MissionDifficulty}", ChatColor.LightBlue);
                                    return;
                                }

                                MissionDifficulty difficultyValue;
                                if (!Enum.TryParse(param[2], true, out difficultyValue))
                                {
                                    chatWindow.WriteLine($"Invalid value. Options for difficulty are [{string.Join("|", Enum.GetNames(typeof(MissionDifficulty)))}]", ChatColor.Red);
                                    return;
                                }

                                Config.GlobalSettings.MissionDifficulty = difficultyValue;
                                chatWindow.WriteLine($"Difficulty is now set to {difficultyValue}");

                                break;
                            case "faction":
                                if (param.Length == 2)
                                {
                                    chatWindow.WriteLine($"MissionFaction(Global): {Config.GlobalSettings.MissionFaction}", ChatColor.LightBlue);
                                    return;
                                }

                                MissionFaction factionValue;
                                if (!Enum.TryParse(param[2], true, out factionValue))
                                {
                                    chatWindow.WriteLine($"Invalid value. Options for faction are [{string.Join("|", Enum.GetNames(typeof(MissionFaction)))}]", ChatColor.Red);
                                    return;
                                }

                                Config.GlobalSettings.MissionFaction = factionValue;
                                chatWindow.WriteLine($"Faction is now set to {factionValue}");

                                break;
                            case "isleech":
                                if (param.Length == 2)
                                {
                                    chatWindow.WriteLine($"IsLeech({DynelManager.LocalPlayer.Name}): {Config.IsLeech}", ChatColor.LightBlue);
                                    return;
                                }

                                bool leechValue;
                                if (!bool.TryParse(param[2], out leechValue))
                                {
                                    chatWindow.WriteLine($"Invalid value. Options for isleech are [true|false]", ChatColor.Red);
                                    return;
                                }

                                if (Config.CharSettings == null)
                                    Config.CharSettings = new Dictionary<int, CharacterSettings>();

                                if (!Config.CharSettings.ContainsKey(Game.ClientInst))
                                    Config.CharSettings.Add(Game.ClientInst, new CharacterSettings() { IsLeech = leechValue });
                                else
                                    Config.CharSettings[Game.ClientInst].IsLeech = leechValue;

                                chatWindow.WriteLine($"IsLeech is now set to {leechValue}");

                                break;
                            default:
                                chatWindow.WriteLine($"Config options are [difficulty|faction|isleech]", ChatColor.Red);
                                return;
                        }

                        Config.Save();

                        break;
                    case "test":
                        _stateMachine.SetState(new MoveToQuestGiverState());
                        break;
                    default:
                        PrintInfBuddyCommandUsage(chatWindow);
                        break;
                }
            } catch (Exception e)
            {
                Chat.WriteLine(e.Message);
            }
        }

        private void NpcDialog_AnswerListChanged(object s, Dictionary<int, string> options)
        {
            SimpleChar dialogNpc = DynelManager.GetDynel((Identity)s).Cast<SimpleChar>();

            if (dialogNpc.Name == Constants.QuestGiverName)
            {
                foreach (KeyValuePair<int, string> option in options)
                {
                    if (option.Value == "Is there anything I can help you with?" ||
                        (ActiveGlobalSettings.MissionFaction == MissionFaction.Clan && option.Value == "I will defend against the Unredeemed!") ||
                        (ActiveGlobalSettings.MissionFaction == MissionFaction.Omni && option.Value == "I will defend against the Redeemed!") ||
                        (ActiveGlobalSettings.MissionFaction == MissionFaction.Neut && option.Value == "I will defend against the creatures of the brink!") ||
                        (ActiveGlobalSettings.MissionDifficulty == MissionDifficulty.Easy && option.Value == "I will deal with only the weakest aversaries") || //Brink missions have a typo
                        (ActiveGlobalSettings.MissionDifficulty == MissionDifficulty.Easy && option.Value == "I will deal with only the weakest adversaries") ||
                        (ActiveGlobalSettings.MissionDifficulty == MissionDifficulty.Medium && option.Value == "I will challenge these invaders, as long as there aren't too many") ||
                        (ActiveGlobalSettings.MissionDifficulty == MissionDifficulty.Hard && option.Value == "I will purge the temple of any and all assailants"))
                        NpcDialog.SelectAnswer(dialogNpc.Identity, option.Key);
                }
            } 
            else if (dialogNpc.Name == Constants.QuestStarterName)
            {
                foreach (KeyValuePair<int, string> option in options)
                {
                    if (option.Value == "Yes, I am ready.")
                        NpcDialog.SelectAnswer(dialogNpc.Identity, option.Key);
                }
            }
        }
    }
}
