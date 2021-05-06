using AOSharp.Common.GameData;
using AOSharp.Core;
using AOSharp.Core.UI;
using AOSharp.Pathfinding;
using System;
using System.Collections.Generic;
using System.Linq;

namespace APFBuddy
{
    public class PatrolState : IState
    {
        public void OnStateEnter()
        {
            Chat.WriteLine("PatrolState::OnStateEnter");

            APFBuddy.PullTarget = null;

            if (!APFBuddy.FindTarget(out SimpleChar target, out EngageMethod engageMethod))
            {
                APFBuddy.MovementController.SetNavMeshDestination(Constants.S13GoalPos, out NavMeshPath path);
                path.DestinationReachedCallback = () => APFBuddy.FSM.Fire(Trigger.ReachedEndOfSector);
            }
        }

        public void OnStateExit()
        {
            Chat.WriteLine("PatrolState::OnStateExit");
        }

        public void Tick()
        {
            if(APFBuddy.FindTarget(out SimpleChar target, out EngageMethod engageMethod))
            {
                if (APFBuddy.IsLeader && engageMethod == EngageMethod.Pull)
                {
                    Chat.WriteLine($"Patrol target set to {target.Name}");
                    APFBuddy.PullTarget = target;
                    APFBuddy.FSM.Fire(Trigger.PullTargetSighted);
                } 
                else if(engageMethod == EngageMethod.Fight)
                {
                    APFBuddy.FSM.Fire(Trigger.MobEnteredCombatRange);
                }
                else
                {
                    if (APFBuddy.MovementController.IsNavigating)
                        APFBuddy.MovementController.Halt();
                }
            }
        }
    }
}
