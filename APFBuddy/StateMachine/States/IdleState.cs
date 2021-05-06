using AOSharp.Common.GameData;
using AOSharp.Core;
using AOSharp.Core.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace APFBuddy
{
    public class IdleState : IState
    {
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
            if (APFBuddy.IsLeader && APFBuddy.Running && Team.IsInTeam && Team.IsRaid)
                APFBuddy.Start();
        }
    }
}
