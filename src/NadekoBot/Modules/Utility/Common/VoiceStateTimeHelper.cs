using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace Mitternacht.Modules.Utility.Common
{
    /// <summary>
    /// Provides thread-safe methods to log the time a user is doing something.
    /// </summary>
    public class VoiceStateTimeHelper
    {
        private DateTime _currentIntervalStartTime;
        private ConcurrentDictionary<(ulong UserId, ulong GuildId), (DateTime? Start, List<double> StopTimes)> _userTimeSteps;

        /// <summary>
        /// Stores every UserID which shall be from tracking after the current interval was finished.
        /// </summary>
        public HashSet<(ulong UserId, ulong GuildId)> EndUserTrackingAfterInterval;
        
        /// <summary>
        /// Constructor
        /// </summary>
        public VoiceStateTimeHelper()
        {
            _userTimeSteps = new ConcurrentDictionary<(ulong, ulong), (DateTime?, List<double>)>();
            EndUserTrackingAfterInterval = new HashSet<(ulong, ulong)>();
        }

        /// <summary>
        /// Returns the tracked seconds since _currentIntervalStartTime.
        /// </summary>
        /// <param name="reset">Whether to reset the counter or not.</param>
        /// <returns>A Dictionary </returns>
        public IDictionary<(ulong UserId, ulong GuildId), double> GetUserTimes(bool reset = true)
        {
            var timenow = DateTime.Now;
            var result = _userTimeSteps
                .Where(kv => kv.Value.Start.HasValue ? kv.Value.Start >= _currentIntervalStartTime : true)
                .ToDictionary(kv => kv.Key, kv => 
                    //add positive StopTimes if there are any.
                    (kv.Value.StopTimes.Where(t => t >= 0).Sum() > 0 ? kv.Value.StopTimes.Where(t => t >= 0).Sum() : 0) + 
                    //add open time if kv.Value.Start is not null
                    (kv.Value.Start.HasValue ? (timenow - kv.Value.Start.Value).TotalSeconds : 0));

            if (reset) Reset(timenow);

            return result;
        }

        /// <summary>
        /// Resets the tracker interval.
        /// </summary>
        public void Reset()
            => Reset(DateTime.Now);

        /// <summary>
        /// Resets the _userTimeSteps dictionary.
        /// </summary>
        private void Reset(DateTime newStart)
        {
            var usertimesteps = _userTimeSteps.Where(kv => kv.Value.Start.HasValue && kv.Value.Start.Value >= _currentIntervalStartTime && !EndUserTrackingAfterInterval.Contains(kv.Key)).ToList();
            _userTimeSteps.Clear();
            foreach (var uts in usertimesteps)
            {
                _userTimeSteps.TryAdd(uts.Key, (newStart, new List<double>()));
            }
            _currentIntervalStartTime = newStart;
        }

        /// <summary>
        /// Starts time tracking of a specified user.
        /// </summary>
        /// <param name="userId">ID of the user.</param>
        /// <returns>True, if tracking could be enabled, otherwise false.</returns>
        public bool StartTracking(ulong userId, ulong guildId)
        {
            bool success = false;

            if (_userTimeSteps.TryGetValue((userId, guildId), out var value) && !value.Start.HasValue)
            {
                var valuen = value;
                valuen.Start = DateTime.Now;
                _userTimeSteps.TryUpdate((userId, guildId), valuen, value);
                success = true;
            }
            else success = _userTimeSteps.TryAdd((userId, guildId), (DateTime.Now, new List<double>()));

            if (success) EndUserTrackingAfterInterval.Remove((userId, guildId));
            return success;
        }

        /// <summary>
        /// Stops time tracking of a specified User. If it fails but still should be stopped, the UserID can be added to EndUserTrackingAfterInterval.
        /// </summary>
        /// <param name="userId">ID of the user.</param>
        /// <returns>True, if User was tracked, otherwise false.</returns>
        public bool StopTracking(ulong userId, ulong guildId)
        {
            if (!_userTimeSteps.TryGetValue((userId, guildId), out var value) || !value.Start.HasValue) return false;
            var valuen = value;
            valuen.StopTimes.Add((DateTime.Now - valuen.Start.Value).TotalSeconds);
            valuen.Start = null;
            return _userTimeSteps.TryUpdate((userId, guildId), valuen, value);
        }

        /// <summary>
        /// Removes all current tracked users of a specified guild.
        /// </summary>
        /// <param name="guildId"></param>
        public void StopGuildTracking(ulong guildId)
        {
            var gus = _userTimeSteps.Where(kv => kv.Key.GuildId == guildId).ToList();
            foreach (var gu in gus)
            {
                if (!StopTracking(gu.Key.UserId, gu.Key.GuildId))
                    EndUserTrackingAfterInterval.Add(gu.Key);
            }
        }
    }
}
