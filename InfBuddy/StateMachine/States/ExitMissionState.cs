using AOSharp.Common.GameData;
using AOSharp.Core;
using AOSharp.Core.Movement;
using AOSharp.Core.UI;

namespace InfBuddy
{
    public class ExitMissionState : IState
    {
        public IState GetNextState()
        {
            if (Playfield.ModelIdentity.Instance == Constants.InfernoId)
                return new ReformState();

            return null;
        }

        public void OnStateEnter()
        {
            Chat.WriteLine("ExitMissionState::OnStateEnter");

            if (!Team.IsLeader && InfBuddy.Config.IsLeech)
                DynelManager.LocalPlayer.Position = Constants.LeechMissionExit;
            else
                InfBuddy.NavMeshMovementController.SetNavMeshDestination(Constants.ExitPos);

            InfBuddy.NavMeshMovementController.AppendDestination(Constants.ExitFinalPos);
        }

        public void OnStateExit()
        {
            Chat.WriteLine("ExitMissionState::OnStateExit");
        }

        public void Tick()
        {
        }
    }
}
