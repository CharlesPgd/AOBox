using AOSharp.Common.GameData;
using AOSharp.Core;
using AOSharp.Core.UI;
using System;

namespace InfBuddy
{
    public class DefendSpiritState : PositionHolder, IState
    {
        private SimpleChar _target;

        public DefendSpiritState() : base(Constants.DefendPos, 3f, 1)
        {

        }

        public IState GetNextState()
        {
            if (Playfield.ModelIdentity.Instance != Constants.InfernoId && Playfield.ModelIdentity.Instance != Constants.NewInfMissionId)
                return new IdleState();

            if ((!DynelManager.Exists(Constants.QuestStarterName) && !DynelManager.Exists(Constants.SpiritNPCName)))
            {
                Chat.WriteLine("Couldn't find quest starter or spirit");
                return new ExitMissionState();
            }

            if (!Mission.List.Exists(x => x.DisplayName.Contains(InfBuddy.ActiveGlobalSettings.GetMissionName())))
            {
                Chat.WriteLine($"Couldn't find mission {InfBuddy.ActiveGlobalSettings.GetMissionName()}");
                return new ExitMissionState();
            }

            if (_target != null)
                return new FightState(_target);

            return null;
        }

        public void OnStateEnter()
        {
            Chat.WriteLine("DefendSpiritState::OnStateEnter");

            HoldPosition();
        }

        public void OnStateExit()
        {
            Chat.WriteLine("DefendSpiritState::OnStateExit");
        }

        public void Tick()
        {
            foreach (SimpleChar npc in DynelManager.NPCs)
            {
                if (!npc.IsAlive)
                    continue;

                if (npc.Name == Constants.SpiritNPCName)
                    continue;

                if (npc.Name == Constants.QuestStarterName)
                    continue;

                //We're plenty capable of pathing to mobs but we get way more consistent results if we just stand still and attack anything that walks up.
                if (!npc.IsInAttackRange())
                    continue;

                _target = npc;
            }

            HoldPosition();
        }
    }
}
