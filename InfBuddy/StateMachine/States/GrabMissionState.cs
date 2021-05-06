using AOSharp.Core;
using AOSharp.Core.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InfBuddy
{
    public class GrabMissionState : IState
    {
        public IState GetNextState()
        {
            if (Mission.Exists(InfBuddy.ActiveGlobalSettings.GetMissionName()))
                return new MoveToEntranceState();

            return null;
        }

        public void OnStateEnter()
        {
            Chat.WriteLine("GrabMissionState::OnStateEnter");

            //Ignore pets because I already know you people are going to try that.
            //There should be a NPC template associated with this mob that we can use to accurately identify it but i will wait until there's a need.
            Dynel questGiver = DynelManager.Characters.FirstOrDefault(x => x.Name == Constants.QuestGiverName && !x.IsPet);

            if(questGiver == null)
            {
                //Goto MoveToQuestGiver State?
                Chat.WriteLine("Unable to locate quest giver.");
                return;
            }

            NpcDialog.Open(questGiver);
        }

        public void OnStateExit()
        {
            Chat.WriteLine("GrabMissionState::OnStateExit");
        }

        public void Tick()
        {
        }
    }
}
