using AOSharp.Common.GameData;
using AOSharp.Core;
using AOSharp.Core.Movement;
using AOSharp.Core.UI;

namespace APFBuddy
{
    public class LeechState : IState
    {
        public void OnStateEnter()
        {
            Chat.WriteLine("LeechState::OnStateEnter");
        }

        public void OnStateExit()
        {
            Chat.WriteLine("LeechState::OnStateExit");
        }

        public void Tick()
        {
        }
    }
}
