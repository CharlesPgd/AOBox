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

namespace InfBuddy
{
    public class MoveToEntranceState : IState
    {
        private const int MinWait = 5;
        private const int MaxWait = 15;
        private CancellationTokenSource _cancellationToken = new CancellationTokenSource();

        public IState GetNextState()
        {
            if (Playfield.ModelIdentity.Instance == Constants.NewInfMissionId)
            {
                if (Team.IsLeader || !InfBuddy.Config.IsLeech)
                    return new MoveToQuestStarterState();
                else
                    return new LeechState();
            }

            return null;
        }

        public void OnStateEnter()
        {
            Chat.WriteLine("MoveToEntranceState::OnStateEnter");

            int randomWait = Utils.Next(MinWait, MaxWait);
            Chat.WriteLine($"Idling for {randomWait} seconds..");

            Task.Delay(randomWait * 1000).ContinueWith(x =>
            {
                InfBuddy.NavMeshMovementController.SetNavMeshDestination(Constants.EntrancePos);
                InfBuddy.NavMeshMovementController.AppendDestination(Constants.EntranceFinalPos);
            }, _cancellationToken.Token);
        }

        public void OnStateExit()
        {
            Chat.WriteLine("MoveToEntranceState::OnStateExit");
            _cancellationToken.Cancel();
        }

        public void Tick()
        {
        }
    }
}
