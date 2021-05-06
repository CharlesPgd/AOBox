using AOSharp.Common.GameData;
using AOSharp.Core;
using AOSharp.Core.UI;
using APFBuddy.IPCMessages;
using SmokeLounge.AOtomation.Messaging.Messages.N3Messages.OrgServerMessages;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace APFBuddy
{
    public class ReformState : IState
    {
        private const float ReformTimeout = 180;
        private const float InviteAllDelay = 45;
        private const float PreReformBuffer = 5;
        private const float PostReformBuffer = 5;
        private bool _invitedPastTeamMembers = false;
        private double _reformStartedTime;
        private double _zoneChangedTime;
        private double _reformFinishedTime;
        private ReformPhase _phase;
        private Identity[] _teamCache;
        private List<Identity> _alreadyInvitedCharacters = new List<Identity>();

        public void OnStateEnter()
        {
            Chat.WriteLine("ReformState::OnStateEnter");

            _phase = ReformPhase.Disbanding;
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

            DynelManager.DynelSpawned -= OnDynelSpawned;
            DynelManager.CharInPlay -= OnCharInPlay;
        }

        public void Tick()
        {
            if(_phase == ReformPhase.PostReformBuffer && Time.NormalTime > _reformFinishedTime + PostReformBuffer)
            {
                if (Team.IsInTeam && Team.IsRaid)
                {
                    APFBuddy.Start();
                    return;
                }
                else
                {
                    Chat.WriteLine("Reform failed? Expected to be in raid.");
                    APFBuddy.FSM.Fire(Trigger.TeamReformFailed);
                    return;
                }
            }

            if (_phase == ReformPhase.Disbanding)
            {
                _teamCache = Team.Members.Select(x => x.Identity).ToArray();
                Team.Disband();
                _phase = ReformPhase.AwaitingZoneChange;
                Chat.WriteLine("ReformPhase.AwaitingZoneChange");
            }

            if (_phase == ReformPhase.AwaitingZoneChange && Playfield.ModelIdentity.Instance == Constants.APFHubId)
            {
                _zoneChangedTime = Time.NormalTime;
                _phase = ReformPhase.PreReformBuffer;
                Chat.WriteLine("ReformPhase.PreReformBuffer");
            }

            if (_phase == ReformPhase.PreReformBuffer && Time.NormalTime > _zoneChangedTime + PreReformBuffer)
            {
                _phase = ReformPhase.BulkInvite;
                Chat.WriteLine("ReformPhase.BulkInvite");
            }

            if (_phase == ReformPhase.BulkInvite)
            {
                foreach (SimpleChar player in DynelManager.Players.Where(x => _teamCache.Contains(x.Identity) && x.IsInPlay))
                    InvitePlayer(player.Identity);

                _phase = ReformPhase.AwaitingTeammembers;
                Chat.WriteLine("ReformPhase.AwaitingTeammembers");
            }

            if (_phase == ReformPhase.AwaitingTeammembers)
            {
                if (!_invitedPastTeamMembers && Time.NormalTime > _reformStartedTime + InviteAllDelay)
                {
                    foreach (Identity oldTeammate in _teamCache)
                        InvitePlayer(oldTeammate);

                    _invitedPastTeamMembers = true;
                }

                if (Time.NormalTime < _reformStartedTime + ReformTimeout && Team.Members.Count < 6 && _teamCache.Except(Team.Members.Select(x => x.Identity).ToArray()).Any())
                    return;

                if (Time.NormalTime > _reformStartedTime + ReformTimeout)
                    Chat.WriteLine($"Reform timed out..");
                else
                    Chat.WriteLine("Reform complete.");

                _phase = ReformPhase.PostReformBuffer;
                _reformFinishedTime = Time.NormalTime;
                Team.ConvertToRaid();
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
            Disbanding,
            AwaitingZoneChange,
            PreReformBuffer,
            BulkInvite,
            AwaitingTeammembers,
            PostReformBuffer
        }
    }
}
