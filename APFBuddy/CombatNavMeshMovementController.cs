using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AOSharp.Common.GameData;
using AOSharp.Core;
using AOSharp.Core.Movement;
using AOSharp.Core.UI;
using AOSharp.Pathfinding;

namespace APFBuddy
{
    public class CombatNavMeshMovementController : NavMeshMovementController
    {
        private const float TargetPosUpdateInterval = 0.1f;
        private const float MovePredictTimeout = 3;
        private double _nextTargetPathUpdate = 0;
        private SimpleChar _fightTarget;
        private double _fightStartTime;

        public CombatNavMeshMovementController(string navMeshFolderPath, bool drawPath = false) : base(navMeshFolderPath, drawPath)
        {
        }

        public override void Update()
        {
            if (_fightTarget != null && Time.NormalTime > _nextTargetPathUpdate)
            {
                if (!DynelManager.IsValid(_fightTarget) || !_fightTarget.IsAlive)
                {
                    _fightTarget = null;
                }
                else
                {
                    NavigateToCombatPosition();
                }
            }

            base.Update();
        }

        public void SetFightTarget(SimpleChar target)
        {
            _fightTarget = target;
            _fightStartTime = Time.NormalTime;
            NavigateToCombatPosition();
        }

        public void ClearFightTarget()
        {
            _fightTarget = null;

            if (IsNavigating)
                Halt();
        }

        private Vector3 GetBestCombatPosition()
        {
            if (Time.NormalTime < _fightStartTime + MovePredictTimeout && _fightTarget.IsPathing)
            {
                //Correct Y axis using a raycast because mob waypoints do not follow the terrain
                Vector3 rayOrigin = _fightTarget.PathingDestination;
                rayOrigin.Y += 5f;
                Vector3 rayTarget = rayOrigin;
                rayTarget.Y = 0;

                if (Playfield.Raycast(rayOrigin, rayTarget, out Vector3 hitPos, out _))
                    return hitPos;

                return _fightTarget.PathingDestination;
            }

            if (_fightTarget.IsInAttackRange() && _fightTarget.IsInLineOfSight)
                return DynelManager.LocalPlayer.Position;

            return _fightTarget.Position;
        }

        private void NavigateToCombatPosition()
        {
            Vector3 combatPos = GetBestCombatPosition();

            if (combatPos.DistanceFrom(DynelManager.LocalPlayer.Position) > 4f)
            {
                SetNavMeshDestination(combatPos);
                _nextTargetPathUpdate = Time.NormalTime + TargetPosUpdateInterval;
            } 
            else if(IsNavigating)
            {
                Halt();
            }
        }

        protected override bool LoadPather()
        {
            if(!base.LoadPather())
                throw new FileNotFoundException($"Unable to find navmesh file: {_navMeshFolderPath}\\{Playfield.ModelIdentity.Instance}.navmesh");

            Chat.WriteLine($"Loaded {Playfield.ModelIdentity.Instance}.navmesh");

            return true;
        }
    }
}
