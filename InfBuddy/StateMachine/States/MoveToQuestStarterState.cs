using AOSharp.Common.GameData;
using AOSharp.Core;
using AOSharp.Core.UI;
using System;

namespace InfBuddy
{
    public class MoveToQuestStarterState : IState
    {
        public IState GetNextState()
        {
            if (!InfBuddy.NavMeshMovementController.IsNavigating && IsAtQuestStarter())
                return InfBuddy.IsLeader ? (IState)new StartMissionState() : (IState)new DefendSpiritState();

            return null;
        }

        public void OnStateEnter()
        {
            Chat.WriteLine("MoveToQuestStarter::OnStateEnter");

            InfBuddy.NavMeshMovementController.SetNavMeshDestination(Constants.QuestStarterPos);
        }

        public void OnStateExit()
        {
            Chat.WriteLine("MoveToQuestStarter::OnStateExit");
        }

        public void Tick()
        {
        }

        private bool IsAtQuestStarter()
        {
            return DynelManager.LocalPlayer.Position.DistanceFrom(Constants.QuestStarterPos) < 4f;
        }
    }
}
