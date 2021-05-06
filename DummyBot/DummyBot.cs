using System;
using System.Collections.Generic;
using System.Linq;
using AOSharp.Common.GameData;
using AOSharp.Core;
using AOSharp.Core.Inventory;
using AOSharp.Core.Movement;
using AOSharp.Core.UI;
using AOSharp.Core.UI.Options;

namespace DummyBot
{
    public class DummyBot : AOPluginEntry
    {
        private bool _enabled;
        private float _defenseRadius;
        private Vector3 _posToDefend;
        private List<string> _namesToIgnore;
        private bool _returnToDefensePos;
        private string _chatCommand;
        private float _tetherDistance;
        private const int TauntToolLow = 83920;
        private const int TauntToolHigh = 83919;

        public override void Run(string pluginDir)
        {
            try
            {
                Chat.WriteLine("DummyBot loaded!");

                _enabled = false;
                _defenseRadius = 30;
                _posToDefend = DynelManager.LocalPlayer.Position;
                _namesToIgnore = new List<string>
                {
                    "Guardian Spirit of Purification"
                };
                _returnToDefensePos = true;
                _chatCommand = "dummy";
                _tetherDistance = 3;

                Chat.RegisterCommand(_chatCommand, CommandCallback);
                Game.OnUpdate += OnUpdate;
                Game.TeleportStarted += (sender, args) => _enabled = false;
            }
            catch (Exception e)
            {
                Chat.WriteLine(e.Message);
            }
        }

        private void OnUpdate(object sender, float e)
        {
            // do nothing if the bot is not enabled
            if (!_enabled)
                return;

            // if returnToDefensePos is true try to run back to the spot we want to defend
            if (_returnToDefensePos && DynelManager.LocalPlayer.Position.DistanceFrom(_posToDefend) > _tetherDistance)
            {
                if (!MovementController.Instance.IsNavigating)
                {
                    Chat.WriteLine($"Returning to {_posToDefend}");
                    MovementController.Instance.SetPath(new Path(_posToDefend) { DestinationReachedDist = _tetherDistance });
                }
            }

            SimpleChar target = GetNextTarget();
            if (target != null)
            {
                Debug.DrawSphere(target.Position, 1, DebuggingColor.LightBlue);
                Debug.DrawLine(DynelManager.LocalPlayer.Position, target.Position, DebuggingColor.LightBlue);

                if (DynelManager.LocalPlayer.FightingTarget == null ||
                    DynelManager.LocalPlayer.FightingTarget.Identity != target.Identity)
                {
                    if (!target.IsInAttackRange())
                    {
                        if (!Item.HasPendingUse && !DynelManager.LocalPlayer.Cooldowns.ContainsKey(Stat.Psychology) && Inventory.Find(TauntToolLow, TauntToolHigh, out Item tauntTool))
                        {
                            target.Target();
                            tauntTool.Use(target);
                        }
                    }
                    else
                    {
                        if (!DynelManager.LocalPlayer.IsAttackPending)
                        {
                            DynelManager.LocalPlayer.Attack(target);
                        }
                    }
                }
            }
        }

        protected virtual SimpleChar GetNextTarget()
        {
            SimpleChar target;

            // If we are solo target things that are fighting us first
            if (!Team.IsInTeam)
            {
                target = DynelManager.Characters
                    .Where(c => c.FightingTarget != null && c.FightingTarget.Identity == DynelManager.LocalPlayer.Identity)
                    .Where(c => !_namesToIgnore.Contains(c.Name))
                    .OrderBy(c => c.IsPet)
                    .ThenByDescending(c => c.IsInAttackRange())
                    .ThenByDescending(c => c.HealthPercent < 75)
                    .ThenBy(c => c.Health)
                    .FirstOrDefault();
            }
            else
            {
                // If we are in a team target things that are fighting our team first
                target = DynelManager.Characters
                    .Where(c => !Team.Members.Select(t => t.Identity).Contains(c.Identity))
                    .Where(c => c.FightingTarget != null && Team.Members.Select(t => t.Identity).Contains(c.FightingTarget.Identity))
                    .Where(c => !_namesToIgnore.Contains(c.Name))
                    .OrderBy(c => c.IsPet)
                    .ThenByDescending(c => c.IsInAttackRange())
                    .ThenByDescending(c => c.HealthPercent < 75)
                    .ThenBy(c => c.Health)
                    .FirstOrDefault();
            }

            if (target == null)
            {
                // find a new mob to fight
                target = DynelManager.NPCs
                    .Where(c => c.Position.DistanceFrom(_posToDefend) < _defenseRadius)
                    .Where(c => !_namesToIgnore.Contains(c.Name))
                    .Where(c => c.IsAlive)
                    .Where(c => c.IsInLineOfSight)
                    .OrderBy(c => c.Position.DistanceFrom(DynelManager.LocalPlayer.Position))
                    .FirstOrDefault();
            }

            return target;
        }

        private void CommandCallback(string command, string[] args, ChatWindow chatWindow)
        {
            if (args.Length > 0)
            {
                switch (args[0].ToLower())
                {
                    case "enable":
                    case "start":
                        _enabled = true;
                        chatWindow.WriteLine("Bot enabled");
                        break;
                    case "stop":
                    case "disable":
                        _enabled = false;
                        MovementController.Instance.Halt();
                        chatWindow.WriteLine("Bot disabled");
                        break;
                    case "setpos":
                        chatWindow.WriteLine(
                            $"Updating defense pos from {_posToDefend} to {DynelManager.LocalPlayer.Position}");
                        _posToDefend = DynelManager.LocalPlayer.Position;
                        break;
                    case "ignore":
                        if (args.Length > 1)
                        {
                            string name = string.Join(" ", args.Skip(1));
                            _namesToIgnore.Add(name);
                            chatWindow.WriteLine($"Added \"{name}\" to ignored mob list");
                        }
                        else
                        {
                            chatWindow.WriteLine("Please specify a name");
                        }
                        break;
                    case "unignore":
                        break;
                    case "status":
                        break;
                    case "radius":
                    case "range":
                        if (args.Length > 1 && float.TryParse(args[1], out float newRange))
                        {
                            chatWindow.WriteLine(
                                $"Updating defense radius from {_defenseRadius} to {newRange}");
                            _defenseRadius = newRange;
                        }
                        break;
                    case "pos":
                        break;
                }
            }
        }

        private class Options
        {
            public bool Enabled { get; set; }
            public float DefenseRadius { get; set; }
            public Vector3 PositionToDefend { get; set; }
            public bool TetherToDefensePos { get; set; }
            public float TetherDistance { get; set; }
            public List<string> PatternsToIgnore { get; set; }
            public string ChatCommandName { get; set; }
        }
    }
}
