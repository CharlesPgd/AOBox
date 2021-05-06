using AOSharp.Common.GameData;
using AOSharp.Core;
using AOSharp.Core.UI;
using System;
using System.Configuration;

namespace InfBuddy
{
    public class FightState : PositionHolder, IState
    {
        public const double FightTimeout = 60f;
        private SimpleChar _target;
        private double _fightStartTime;

        public FightState(SimpleChar target) : base(Constants.DefendPos, 3f, 1)
        {
            _target = target;
        }

        public IState GetNextState()
        {
            if (!_target.IsValid ||
               !_target.IsAlive ||
               Time.NormalTime > _fightStartTime + FightTimeout ||
               !_target.IsInAttackRange())
                return new DefendSpiritState();

            return null;
        }

        public void OnStateEnter()
        {
            Chat.WriteLine("FightState::OnStateEnter");
            _fightStartTime = Time.NormalTime;
        }

        public void OnStateExit()
        {
            Chat.WriteLine("FightState::OnStateExit");

            if (DynelManager.LocalPlayer.IsAttacking)
                DynelManager.LocalPlayer.StopAttack();
        }

        public void Tick()
        {
            if (!_target.IsValid)
                return;

            if(!DynelManager.LocalPlayer.IsAttackPending && !DynelManager.LocalPlayer.IsAttacking)
                DynelManager.LocalPlayer.Attack(_target);

            HoldPosition();
        }
    }
}
