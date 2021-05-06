using AOSharp.Common.GameData;
using AOSharp.Core;
using AOSharp.Core.Movement;
using AOSharp.Core.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace APFBuddy
{
    public class EnterSectorState : IState
    {
        private const int MinWait = 5;
        private const int MaxWait = 15;
        private CancellationTokenSource _cancellationToken = new CancellationTokenSource();

        public void OnStateEnter()
        {
            Chat.WriteLine("EnterSectorState::OnStateEnter");

            int randomWait = Utils.Next(MinWait, MaxWait);
            Chat.WriteLine($"Idling for {randomWait} seconds..");

            Task.Delay(randomWait * 1000).ContinueWith(x =>
            {
                APFBuddy.MovementController.SetNavMeshDestination(APFBuddy.ActiveGlobalSettings.GetSectorEntrancePos());
            }, _cancellationToken.Token);
        }

        public void OnStateExit()
        {
            Chat.WriteLine("EnterSectorState::OnStateExit");
            _cancellationToken.Cancel();
        }

        public void Tick()
        {
            //if()
            //APFBuddy.MovementController.SetNavMeshDestination(APFBuddy.ActiveGlobalSettings.GetSectorEntrancePos());
        }
    }
}
