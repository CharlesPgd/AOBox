using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AOSharp.Core;
using AOSharp.Core.UI;
using AOSharp.Core.Movement;
using AOSharp.Core.IPC;
using AOSharp.Pathfinding;
using APFBuddy.IPCMessages;
using AOSharp.Common.GameData;
using AOSharp.Core.Inventory;

namespace APFBuddy
{
    public class APFBuddy : AOPluginEntry
    {
        public static FSM<State, Trigger> FSM;
        public static CombatNavMeshMovementController MovementController { get; private set; }
        public static IPCChannel IPCChannel { get; private set; }
        public static Config Config { get; private set; }
        public static GlobalSettings ActiveGlobalSettings;
        public static bool Running = false;
        public static Identity Leader = Identity.None;
        public static bool IsLeader = false;
        public static SimpleChar PullTarget;


        public override void Run(string pluginDir)
        {
            Chat.WriteLine("APFBuddy Loaded!");

            try
            {
                Config = Config.Load($"{Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)}\\AOSharp\\APFBuddy\\Config.json");
                ActiveGlobalSettings = Config.GlobalSettings;
                FSM = BuildFSM();
                MovementController = new CombatNavMeshMovementController($"{pluginDir}\\NavMeshes", true);
                AOSharp.Core.Movement.MovementController.Set(MovementController);

                Chat.RegisterCommand("apfbuddy", APFBuddyCommand);

                IPCChannel = new IPCChannel(11);
                IPCChannel.RegisterCallback((int)IPCOpcode.Start, OnStartMessage);
                IPCChannel.RegisterCallback((int)IPCOpcode.Stop, OnStopMessage);

                Team.TeamRequest += OnTeamRequest;
                Game.OnUpdate += OnUpdate;
                Game.TeleportEnded += OnZoned;
            }
            catch (Exception e)
            {
                Chat.WriteLine(e.Message);
            }
        }

        private FSM<State, Trigger> BuildFSM()
        {
            FSM<State, Trigger> fsm = new FSM<State, Trigger>(State.Idle);

            fsm.AddState(State.Idle, typeof(IdleState))
                .PermitIf(Trigger.StartNewRun, State.EnterSector, () => Team.IsInTeam && Team.IsRaid && Playfield.ModelIdentity.Instance == Constants.APFHubId)
                .PermitIf(Trigger.StartNewRun, State.Patrol, () => Playfield.ModelIdentity.Instance == ActiveGlobalSettings.GetSectorId())
                .Ignore(Trigger.APFHubEntered);

            fsm.AddState(State.EnterSector, typeof(EnterSectorState))
                .Ignore(Trigger.StartNewRun)
                .PermitIf(Trigger.SectorEntered, State.Patrol, () => Team.IsLeader || !Config.IsLeech)
                .PermitIf(Trigger.SectorEntered, State.Leech, () => !Team.IsLeader && Config.IsLeech);

            fsm.AddState(State.Patrol, typeof(PatrolState))
                .Ignore(Trigger.StartNewRun)
                .Permit(Trigger.PullTargetSighted, State.Pull)
                .Permit(Trigger.MobEnteredCombatRange, State.Fight)
                .PermitIf(Trigger.ReachedEndOfSector, State.Idle, () => !Team.IsLeader)
                .PermitIf(Trigger.ReachedEndOfSector, State.Reform, () => Team.IsLeader)
                .PermitIf(Trigger.APFHubEntered, State.EnterSector, () => Team.IsInTeam);

            fsm.AddState(State.Pull, typeof(PullState))
                .Ignore(Trigger.StartNewRun)
                .Permit(Trigger.BadTarget, State.Patrol)
                .Permit(Trigger.PullTimedOut, State.Fight)
                .Permit(Trigger.MobEnteredCombatRange, State.Fight)
                .PermitIf(Trigger.APFHubEntered, State.EnterSector, () => Team.IsInTeam);

            fsm.AddState(State.Fight, typeof(FightState))
                .Ignore(Trigger.StartNewRun)
                .Permit(Trigger.NoMobsInRange, State.Patrol)
                .PermitIf(Trigger.APFHubEntered, State.EnterSector, () => Team.IsInTeam);

            fsm.AddState(State.Reform, typeof(ReformState))
                .Ignore(Trigger.APFHubEntered)
                .Permit(Trigger.TeamReformFailed, State.Idle)
                .Permit(Trigger.StartNewRun, State.EnterSector);

            fsm.AddState(State.Leech, typeof(LeechState))
                .Ignore(Trigger.StartNewRun);

            fsm.AddGlobalPermit(Trigger.Halt, State.Idle);
            fsm.AddGlobalPermit(Trigger.UnknownPlayfieldEntered, State.Idle);

            //Clear all movement on state change.
            fsm.OnTransitioned((x) => MovementController.Halt());

            fsm.OnUnhandledTrigger((state, trigger) => 
            { 
                Chat.WriteLine($"Unhandled trigger: {state}+{trigger}");
                Stop();
            });

            return fsm;
        }

        private void OnZoned(object s, EventArgs e)
        {
            Chat.WriteLine($"Zoned.");

            try
            {
                if (Playfield.ModelIdentity.Instance == ActiveGlobalSettings.GetSectorId())
                    FSM.Fire(Trigger.SectorEntered);
                else if(Playfield.ModelIdentity.Instance == Constants.APFHubId)
                    FSM.Fire(Trigger.APFHubEntered);
                else
                    FSM.Fire(Trigger.UnknownPlayfieldEntered);
            }
            catch(Exception ex)
            {
                Chat.WriteLine(ex.Message);
            }
        }

        public static void Start()
        {
            Running = true;

            FSM.Fire(Trigger.StartNewRun);

            if(IsLeader)
                StartNextRound();
        }

        private void Stop()
        {
            Running = false;
            FSM.Fire(Trigger.Halt);
            MovementController.Halt();
        }

        private static void StartNextRound()
        {
            IPCChannel.Broadcast(new StartMessage()
            {
                Sector = Config.GlobalSettings.Sector
            });

            ActiveGlobalSettings = Config.GlobalSettings.Clone();

            Chat.WriteLine("StartNextRound");
        }

        public static bool FindTarget(out SimpleChar target, out EngageMethod engageMethod)
        {
            TeamMember teamLead = Team.Members.FirstOrDefault(x => x.IsLeader);

            engageMethod = EngageMethod.Pull;
            target = DynelManager.NPCs.Where(c => c.Name != "Zix")
                                        .Where(c => DynelManager.LocalPlayer.GetLogicalRangeToTarget(c) < Constants.MaxPullDistance)
                                        .Where(c => c.IsAlive)
                                        .Where(c => c.IsInLineOfSight)
                                        .OrderByDescending(c => teamLead.Character != null && teamLead.Character.FightingTarget != null && teamLead.Character.FightingTarget.Identity == c.Identity)
                                        .OrderBy(c => c.Position.DistanceFrom(DynelManager.LocalPlayer.Position))
                                        .FirstOrDefault();

            if (target != null)
            {
                SimpleChar targetCopy = target;
                if(Team.Members.Where(x => x.Character != null).Any(x => x.Character.GetLogicalRangeToTarget(targetCopy) < Constants.FightDistance || (x.Character.FightingTarget != null && x.Character.FightingTarget.Identity == targetCopy.Identity)))
                    engageMethod = EngageMethod.Fight;
                else
                    engageMethod = EngageMethod.Pull;
            }

            return target != null;
        }

        private void OnStartMessage(int sender, IPCMessage msg)
        {
            StartMessage startMsg = (StartMessage)msg;

            Chat.WriteLine("OnStartMessage");
            ActiveGlobalSettings = new GlobalSettings
            {
                Sector = startMsg.Sector
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

            FSM.Tick();

            foreach (SimpleChar character in DynelManager.Characters)
            {
                if (character.IsInLineOfSight)
                    Debug.DrawLine(DynelManager.LocalPlayer.Position, character.Position, DebuggingColor.Green);

                if (character.IsPathing)
                {
                    Vector3 rayOrigin = character.PathingDestination;
                    rayOrigin.Y += 5;
                    Vector3 rayTarget = rayOrigin;
                    rayTarget.Y = 0;

                    if (Playfield.Raycast(rayOrigin, rayTarget, out Vector3 hitPos, out _))
                    {
                        Debug.DrawLine(rayOrigin, rayTarget, DebuggingColor.White);
                        Debug.DrawSphere(hitPos, 0.2f, DebuggingColor.White);
                    }

                    Debug.DrawLine(character.Position, character.PathingDestination, DebuggingColor.LightBlue);
                    Debug.DrawSphere(character.PathingDestination, 0.2f, DebuggingColor.LightBlue);
                }
            }

            if (!IsLeader)
                return;
        }

        private void OnTeamRequest(object s, TeamRequestEventArgs e)
        {
            if (e.Requester != Leader)
            {
                if (Running)
                    e.Ignore();

                return;
            }

            e.Accept();
        }

        private void PrintAPFBuddyCommandUsage(ChatWindow chatWindow)
        {
            string help = "Usage:\nStart - Begins doing bot things\n" +
                            "Stop - Stops doing bot things\n" +
                            "Config - Displays the current config\n" +
                            "\t\tIsLeech - [true|false]\n\n" +
                            "Examples:\n" +
                            "\t/APFBuddy config isleech true - Will put the current character in leech mode\n" +
                            "\t/APFBuddy config isleech - Will display the current character's isleech value\n" +
                            "etc...";

            chatWindow.WriteLine(help, ChatColor.LightBlue);
        }

        private void APFBuddyCommand(string command, string[] param, ChatWindow chatWindow)
        {
            try
            {
                if (param.Length < 1)
                {
                    PrintAPFBuddyCommandUsage(chatWindow);
                    return;
                }

                switch (param[0].ToLower())
                {
                    case "start":
                        if (!Team.IsLeader)
                        {
                            chatWindow.WriteLine("Error: Only the team leader can use start", ChatColor.Yellow);
                            return;
                        }

                        if (!Team.IsInTeam || !Team.IsRaid)
                        {
                            chatWindow.WriteLine("Error: Must be in a raid to start", ChatColor.Yellow);
                            return;
                        }

                        if (!Inventory.Find(83920, 83919, out _))
                        {
                            chatWindow.WriteLine("Error: You need an Aggression Enhancer/Aggression Multiplier in your main inventory to run this bot.", ChatColor.Yellow);
                            return;
                        }

                        IsLeader = true;
                        Start();
                        break;
                    case "stop":
                        Stop();
                        IPCChannel.Broadcast(new StopMessage());
                        break;
                    case "config":
                        if (param.Length == 1)
                        {
                            chatWindow.WriteLine($"APFBuddy Config:", ChatColor.LightBlue);
                            chatWindow.WriteLine($"\tIsLeech({DynelManager.LocalPlayer.Name}): {Config.IsLeech}", ChatColor.LightBlue);
                            return;
                        }

                        if (param.Length < 2)
                        {
                            PrintAPFBuddyCommandUsage(chatWindow);
                            return;
                        }

                        switch (param[1].ToLower())
                        {
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
                                chatWindow.WriteLine($"Config options are [isleech]", ChatColor.Red);
                                return;
                        }

                        Config.Save();

                        break;
                    case "test":
                        FSM.Fire(Trigger.StartNewRun);
                        break;
                    default:
                        PrintAPFBuddyCommandUsage(chatWindow);
                        break;
                }
            }
            catch (Exception e)
            {
                Chat.WriteLine(e.Message);
            }
        }
    }
}
