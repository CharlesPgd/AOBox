using AOSharp.Common.GameData;
using AOSharp.Core;
using AOSharp.Core.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InfBuddy
{
    public class IdleState : IState
    {
        public IState GetNextState()
        {
            if (!InfBuddy.Running)
                return null;

            //We have no possible action if you aren't in a team.
            if(!DynelManager.LocalPlayer.IsInTeam())
                return null;

            if(Playfield.ModelIdentity.Instance == Constants.InfernoId)
            {
                //The provided navmesh is rather small in scope so we will take no action if we are not near Ergo.
                if(DynelManager.LocalPlayer.Position.DistanceFrom(Constants.ErgoVicinity) > 100f)
                    return null;

                if (Mission.Exists(InfBuddy.ActiveGlobalSettings.GetMissionName()))
                    return new MoveToEntranceState();
                else
                    return new MoveToQuestGiverState();
            } 
            else if(Playfield.ModelIdentity.Instance == Constants.NewInfMissionId)
            {
                if(DynelManager.Exists(Constants.QuestStarterName))
                    return new MoveToQuestStarterState();

                if (DynelManager.Exists(Constants.SpiritNPCName))
                    return new DefendSpiritState();

                if (InfBuddy.Config.IsLeech)
                    return new LeechState();
            }

            return null;
        }

        public void OnStateEnter()
        {
            Chat.WriteLine("IdleState::OnStateEnter");
        }

        public void OnStateExit()
        {
            Chat.WriteLine("IdleState::OnStateExit");
        }

        public void Tick()
        {
        }
    }
}
