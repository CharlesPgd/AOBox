using AOSharp.Common.GameData;
using AOSharp.Core;
using AOSharp.Core.Inventory;
using AOSharp.Core.UI;
using System;
using System.Linq;

namespace APFBuddy
{
    public class PullState : IState
    {
        public const double PullTimeout = 5f;
        public const double PullAttemptInterval = 1f;
        private double _pullStartTime;
        private double _nextPullAttempt;

        public void OnStateEnter()
        {
            Chat.WriteLine("PullState::OnStateEnter");
            _pullStartTime = Time.NormalTime;
        }

        public void OnStateExit()
        {
            Chat.WriteLine("PullState::OnStateExit");
        }

        public void Tick()
        {
            if (!APFBuddy.PullTarget.IsValid ||
                !APFBuddy.PullTarget.IsAlive ||
                !APFBuddy.PullTarget.IsInLineOfSight)
            {
                APFBuddy.PullTarget = null;
                APFBuddy.FSM.Fire(Trigger.BadTarget);
                return;
            }

            if (Time.NormalTime > _pullStartTime + PullTimeout)
            {
                APFBuddy.FSM.Fire(Trigger.PullTimedOut);
                return;
            }

            if (APFBuddy.FindTarget(out SimpleChar target, out EngageMethod engageMethod) && engageMethod == EngageMethod.Fight)
            {
                APFBuddy.PullTarget = null;
                APFBuddy.FSM.Fire(Trigger.MobEnteredCombatRange);
                return;
            }

            if (Time.NormalTime > _nextPullAttempt &&
                !DynelManager.LocalPlayer.Cooldowns.ContainsKey(Stat.Psychology) &&
                Inventory.Find(83920, 83919, out Item aggroTool))
            {
                aggroTool.Use(APFBuddy.PullTarget, true);
                _nextPullAttempt = Time.NormalTime + PullAttemptInterval;
            }
        }
    }
}
