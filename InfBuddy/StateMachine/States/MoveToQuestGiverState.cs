using AOSharp.Common.GameData;
using AOSharp.Core;
using AOSharp.Core.UI;
using NavmeshMovementController;
using System;
using System.Threading;
using System.Threading.Tasks;


namespace InfBuddy
{
    public class MoveToQuestGiverState : IState
    {
        private const int Entropy = 3;
        private const int MinWait = 10;
        private const int MaxWait = 45;
        private CancellationTokenSource _cancellationToken = new CancellationTokenSource();
        public IState GetNextState()
        {
            if (!InfBuddy.NavMeshMovementController.IsNavigating && IsAtQuestGiver())
                return new GrabMissionState();

            return null;
        }

        public void OnStateEnter()
        {
            Chat.WriteLine("MoveToQuestGiverState::OnStateEnter");

            if (!IsAtQuestGiver())
            {
                Vector3 randoPos = Constants.QuestGiverPos;
                randoPos.AddRandomness(Entropy);

                int randomWait = Utils.Next(MinWait, MaxWait);
                Chat.WriteLine($"Idling for {randomWait} seconds..");
                Task.Delay(randomWait * 1000).ContinueWith(x =>
                {
                    Chat.WriteLine("Moving to quest giver");

                    try
                    {
                        InfBuddy.NavMeshMovementController.SetNavMeshDestination(randoPos);
                    }
                    catch (Exception e)
                    {
                        InfBuddy.NavMeshMovementController.SetDestination(Constants.WallBugFixSpot);
                        InfBuddy.NavMeshMovementController.AppendNavMeshDestination(randoPos);
                    }
                }, _cancellationToken.Token);
            }
        }

        public void OnStateExit()
        {
            Chat.WriteLine("MoveToQuestGiverState::OnStateExit");
            _cancellationToken.Cancel();
        }

        public void Tick()
        {
            //Possibly retry pathing if it's having issues?
            //Not sure if it's needed yet.
        }

        private bool IsAtQuestGiver()
        {
            return DynelManager.LocalPlayer.Position.DistanceFrom(Constants.QuestGiverPos) < 6f;
        }
    }
}
