using AOSharp.Core;
using System;
using System.Linq;
using AOSharp.Common.GameData;
using AOSharp.Common.Unmanaged.Imports;
using AOSharp.Core.Inventory;
using AOSharp.Core.Movement;
using AOSharp.Core.UI;
using AOSharp.Core.UI.Options;

namespace Sector10
{
    public class Main : AOPluginEntry
    {
        private const int TauntToolLow = 83920;
        private const int TauntToolHigh = 83919;
        private const int LaserCage = 268683;
        private const int LeashDistance = 50;
        private double _effectDelay = 5;
        private double _nextEffect = 0;
        private Menu _menu;

        public override void Run(string pluginDir)
        {
            try
            {
                _menu = new Menu("Sector10", "Sector10");
                _menu.AddItem(new MenuBool("EnableMovement", "Enable movement", false));
                OptionPanel.AddMenu(_menu);
                
                Chat.WriteLine("Sector10 bot loaded!");
                Game.OnUpdate += OnUpdate;
            }
            catch (Exception e)
            {
                Chat.WriteLine(e.Message);
            }
        }

        private void OnUpdate(object sender, float e)
        {
            try
            {
                // Draw possible targets
                double currentTime = Time.NormalTime;
                if (currentTime > _nextEffect)
                {
                    _nextEffect = currentTime + _effectDelay;
                    foreach (SimpleChar potentialTarget in GetValidTargets())
                    {
                        IntPtr pEffectHandler = _EffectHandler_t.GetInstance();
                        if (pEffectHandler != IntPtr.Zero)
                        {
                            _EffectHandler_t.CreateEffect2(pEffectHandler, 43005, potentialTarget.Pointer, 0);
                        }
                    }
                }

                SimpleChar target = GetNextTarget();

                if (target != null)
                {
                    Debug.DrawSphere(target.Position, 1, DebuggingColor.LightBlue);
                    Debug.DrawLine(DynelManager.LocalPlayer.Position, target.Position, DebuggingColor.LightBlue);

                    if (target.IsInAttackRange())
                    {
                        if (DynelManager.LocalPlayer.FightingTarget == null ||
                            DynelManager.LocalPlayer.FightingTarget.Identity != target.Identity)
                        {
                            if (!DynelManager.LocalPlayer.IsAttackPending)
                                DynelManager.LocalPlayer.Attack(target);
                        }

                        if (!target.Buffs.Contains(268684) && !Item.HasPendingUse && target.HealthPercent >= 21 && target.GetStat(Stat.NPCFamily) == 229 && Inventory.Find(LaserCage, out Item cage))
                        {
                            target.Target();
                            cage.Use(target);
                        }
                    }
                    else
                    {
                        float distanceToTarget = target.Position.DistanceFrom(DynelManager.LocalPlayer.Position);

                        if (distanceToTarget >= LeashDistance)
                        {
                            // Run into leash range if movement is enabled
                            if (_menu.GetBool("EnableMovement") && !MovementController.Instance.IsNavigating)
                                MovementController.Instance.SetPath(new Path(target.Position) { DestinationReachedDist = LeashDistance });
                        }
                        else
                        {
                            // stop running if we are running
                            if (!(target.FightingTarget != null &&
                                  target.FightingTarget.Identity == DynelManager.LocalPlayer.Identity))
                            {
                                if (!DynelManager.LocalPlayer.Cooldowns.ContainsKey(Stat.Psychology))
                                    Pull(target);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Chat.WriteLine(ex.ToString());
            }
        }

        private SimpleChar GetNextTarget()
        {
            // target things that are fighting us first
            SimpleChar fightingOpponent = DynelManager.Characters
                .Where(c => c.FightingTarget != null && c.FightingTarget.Identity == DynelManager.LocalPlayer.Identity)
                .OrderBy(c => c.IsPet)
                .ThenByDescending(c => c.IsInAttackRange())
                .ThenByDescending(c => c.HealthPercent < 75)
                .ThenByDescending(c => c.Name == "Alien Larvae")
                .ThenByDescending(c => c.Name == "Alien Communications Officer")
                .ThenByDescending(c => c.Name.Contains("Ankari"))
                .ThenByDescending(c => c.Name.Contains("Ilari"))
                .ThenByDescending(c => c.Name.Contains("Vector"))
                .ThenByDescending(c => c.GetStat(Stat.Level))
                .FirstOrDefault();

            if (fightingOpponent != null)
                return fightingOpponent;

            // find a new mob to fight
            return GetValidTargets().FirstOrDefault();
        }

        private IOrderedEnumerable<SimpleChar> GetValidTargets()
        {
            return DynelManager.NPCs
                .Where(c => c.GetStat(Stat.NPCFamily) == 220 || c.GetStat(Stat.NPCFamily) == 229)
                .Where(c => c.FightingTarget == null)
                .Where(c => c.IsInLineOfSight)
                .Where(c => c.IsAlive)
                .Where(c => _menu.GetBool("EnableMovement") || c.Position.DistanceFrom(DynelManager.LocalPlayer.Position) <= LeashDistance)
                .OrderBy(c => c.Position.DistanceFrom(DynelManager.LocalPlayer.Position));
        }

        private void Pull(SimpleChar target)
        {
            if (Item.HasPendingUse)
                return;

            if (Inventory.Find(TauntToolLow, TauntToolHigh, out Item tauntTool))
            {
                target.Target();
                tauntTool.Use(target);
            }
            else
            {
                Chat.WriteLine("No taunt tool found in inventory");
            }
        }
    }
}
