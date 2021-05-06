using AOSharp.Common.GameData;
using AOSharp.Core;

namespace InfBuddy
{
    public class PositionHolder
    {
        private readonly Vector3 _holdPos;
        private readonly float _HoldDist;
        private readonly int _entropy;

        public PositionHolder(Vector3 holdPos, float holdDist ,int entropy)
        {
            _holdPos = holdPos;
            _HoldDist = holdDist;
            _entropy = entropy;
        }

        public void HoldPosition()
        {
            if (!InfBuddy.NavMeshMovementController.IsNavigating && !IsNearDefenseSpot())
            {
                Vector3 randomHoldPos = _holdPos;
                randomHoldPos.AddRandomness(_entropy);

                InfBuddy.NavMeshMovementController.SetNavMeshDestination(randomHoldPos);
            }
        }

        private bool IsNearDefenseSpot()
        {
            return DynelManager.LocalPlayer.Position.DistanceFrom(Constants.DefendPos) < _HoldDist;
        }
    }
}
