using AOSharp.Common.GameData;
using AOSharp.Core;
using AOSharp.Core.UI;
using System;
using System.Configuration;

namespace APFBuddy
{
    public class FightState : IState
    {
        public const double FightTimeout = 60f;
        private SimpleChar _target = null;
        private double _fightStartTime;

        public void OnStateEnter()
        {
            Chat.WriteLine($"FightState::OnStateEnter");
        }

        public void OnStateExit()
        {
            Chat.WriteLine("FightState::OnStateExit");

            if (DynelManager.LocalPlayer.IsAttacking)
                DynelManager.LocalPlayer.StopAttack();

            APFBuddy.MovementController.ClearFightTarget();
        }

        public void Tick()
        {
            if (_target != null && (!_target.IsValid ||
                                    !_target.IsAlive ||
                                    Time.NormalTime > _fightStartTime + FightTimeout))
            {
                _target = null;
                APFBuddy.PullTarget = null;
            }

            if (_target == null)
            {
                if (APFBuddy.PullTarget != null &&
                    APFBuddy.PullTarget.IsValid &&
                    APFBuddy.PullTarget.IsAlive &&
                    APFBuddy.PullTarget.IsInLineOfSight)
                {
                    _target = APFBuddy.PullTarget;
                    _fightStartTime = Time.NormalTime;
                }
                else if (APFBuddy.FindTarget(out SimpleChar target, out EngageMethod engageMethod) && engageMethod == EngageMethod.Fight)
                {
                    _target = target;
                    _fightStartTime = Time.NormalTime;
                    APFBuddy.MovementController.SetFightTarget(_target);
                    Chat.WriteLine($"Fighting {_target.Name}");
                }
                else
                {
                    APFBuddy.FSM.Fire(Trigger.NoMobsInRange);
                }
            }
            else
            {
                if (!DynelManager.LocalPlayer.IsAttackPending && !DynelManager.LocalPlayer.IsAttacking)
                    DynelManager.LocalPlayer.Attack(_target);

                Debug.DrawSphere(_target.Position, 0.5f, DebuggingColor.Purple);
                Debug.DrawLine(DynelManager.LocalPlayer.Position, _target.Position, DebuggingColor.Purple);
            }
        }
    }
}
