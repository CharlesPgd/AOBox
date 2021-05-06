using AOSharp.Core;
using AOSharp.Core.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InfBuddy
{
    public class StartMissionState : IState
    {
        private const string QuestStarterName = "One Who Obeys Precepts";

        public IState GetNextState()
        {
            if (!DynelManager.Exists(QuestStarterName))
                return new DefendSpiritState();

            return null;
        }

        public void OnStateEnter()
        {
            Chat.WriteLine("StartMissionState::OnStateEnter");

            Dynel questStarter = DynelManager.Characters.FirstOrDefault(x => x.Name == QuestStarterName && !x.IsPet);

            if (questStarter == null)
            {
                //Should we just reform and try again or try to solve this situation?
                Chat.WriteLine("Unable to locate quest starter.");
                return;
            }

            NpcDialog.Open(questStarter);
        }

        public void OnStateExit()
        {
            Chat.WriteLine("StartMissionState::OnStateExit");
        }

        public void Tick()
        {
        }
    }
}
