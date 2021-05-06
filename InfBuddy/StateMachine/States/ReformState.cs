using AOSharp.Common.GameData;
using AOSharp.Core;
using AOSharp.Core.UI;
using InfBuddy.IPCMessages;
using SmokeLounge.AOtomation.Messaging.Messages.N3Messages.OrgServerMessages;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InfBuddy
{
    public class ReformState : IState
    {
        private const float ReformTimeout = 180;
        private const float InviteAllDelay = 45;
        private const float DisbandDelay = 10;
        private bool _invitedPastTeamMembers = false;
        private double _reformStartedTime;
        private ReformPhase _phase;
        private Identity[] _teamCache;
        private List<Identity> _alreadyInvitedCharacters = new List<Identity>();

        public static ConcurrentQueue<Identity> LFT = new ConcurrentQueue<Identity>();

        public IState GetNextState()
        {
            //Non-leaders will have to wait for a state change from the leader to leave this state.
            if (InfBuddy.IsLeader && _phase == ReformPhase.AwaitingTeammembers)
            {
                if (Time.NormalTime > _reformStartedTime + ReformTimeout)
                {
                    Chat.WriteLine($"Reform timed out.. continuing.");
                    InfBuddy.StartNextRound();
                    return Team.IsInTeam ? (IState)new MoveToQuestGiverState() : new IdleState();
                }

                if (Team.Members.Count == 6 || !_teamCache.Except(Team.Members.Select(x => x.Identity).ToArray()).Any())
                {
                    Chat.WriteLine($"Reform complete.");
                    InfBuddy.StartNextRound();
                    return new MoveToQuestGiverState();
                }
            }

            return null;
        }

        public void OnStateEnter()
        {
            Chat.WriteLine("ReformState::OnStateEnter");

            _phase = ReformPhase.MissionExitBuffer;
            _reformStartedTime = Time.NormalTime;

            if (Team.IsLeader)
            {
                DynelManager.DynelSpawned += OnDynelSpawned;
                DynelManager.CharInPlay += OnCharInPlay;
            }
        }

        public void OnStateExit()
        {
            Chat.WriteLine("ReformState::OnStateExit");

            if (Team.IsLeader)
            {
                DynelManager.DynelSpawned -= OnDynelSpawned;
                DynelManager.CharInPlay -= OnCharInPlay;
            }
        }

        public void Tick()
        {
            if (!InfBuddy.IsLeader)
                return;

            //This gives the bots time to run out gracefully instead of being booted
            if (_phase == ReformPhase.MissionExitBuffer && Time.NormalTime > _reformStartedTime + DisbandDelay)
            {
                _teamCache = Team.Members.Select(x => x.Identity).ToArray();
                Team.Disband();
                _phase = ReformPhase.Disbanding;
                Chat.WriteLine("ReformPhase.Disbanding");
            }

            if(_phase == ReformPhase.Disbanding && !Team.IsInTeam)
            {
                foreach (SimpleChar player in DynelManager.Players.Where(x => _teamCache.Contains(x.Identity) && x.IsInPlay))
                    InvitePlayer(player.Identity);

                _phase = ReformPhase.AwaitingTeammembers;
                Chat.WriteLine("ReformPhase.AwaitingTeammembers");
            }

            if (_phase == ReformPhase.AwaitingTeammembers && !_invitedPastTeamMembers && Time.NormalTime > _reformStartedTime + InviteAllDelay)
            {
                foreach (Identity oldTeammate in _teamCache)
                    InvitePlayer(oldTeammate);

                _invitedPastTeamMembers = true;
            }
        }

        private void InvitePlayer(Identity player)
        {
            if (player == DynelManager.LocalPlayer.Identity || Team.Members.Any(j => j.Identity == player) || _alreadyInvitedCharacters.Any(j => j == player))
                return;

            Team.Invite(player);
            Chat.WriteLine($"Inviting {player}");

            _alreadyInvitedCharacters.Add(player);
        }

        private void OnDynelSpawned(object s, Dynel dynel)
        {
            if (_phase != ReformPhase.AwaitingTeammembers)
                return;

            if (!_teamCache.Contains(dynel.Identity))
                return;

            SimpleChar oldTeammate = dynel.Cast<SimpleChar>();

            if (!oldTeammate.IsInPlay)
                return;

            InvitePlayer(oldTeammate.Identity);
        }

        private void OnCharInPlay(object s, SimpleChar character)
        {
            if (_phase != ReformPhase.AwaitingTeammembers)
                return;

            if (!_teamCache.Contains(character.Identity))
                return;

            InvitePlayer(character.Identity);
        }

        private enum ReformPhase
        {
            MissionExitBuffer,
            Disbanding,
            AwaitingTeammembers
        }
    }
}
