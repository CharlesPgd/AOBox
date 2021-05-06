using AOSharp.Common.GameData;
using AOSharp.Core;
using AOSharp.Core.Movement;
using AOSharp.Core.UI;

namespace InfBuddy
{
    public class LeechState : IState
    {
        private bool _missionsLoaded = false;

        public IState GetNextState()
        {
            if (Playfield.ModelIdentity.Instance == Constants.InfernoId)
                return new ReformState();

            if (_missionsLoaded && !Mission.List.Exists(x => x.DisplayName.Contains(InfBuddy.ActiveGlobalSettings.GetMissionName())))
                return new ExitMissionState();

            return null;
        }

        public void OnStateEnter()
        {
            Chat.WriteLine("LeechState::OnStateEnter");

            DynelManager.LocalPlayer.Position = Constants.LeechSpot;
            MovementController.Instance.SetMovement(MovementAction.Update);
        }

        public void OnStateExit()
        {
            Chat.WriteLine("LeechState::OnStateExit");
        }

        public void Tick()
        {
            //Fix for check happening before missions are loaded on zone
            if (Mission.List.Exists(x => x.DisplayName.Contains(InfBuddy.ActiveGlobalSettings.GetMissionName())))
                _missionsLoaded = true;
        }
    }
}
