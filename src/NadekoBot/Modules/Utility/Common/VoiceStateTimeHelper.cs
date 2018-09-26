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
        private ConcurrentDictionary<ulong, (DateTime? Start, List<double> StopTimes)> _userTimeSteps;

        /// <summary>
        /// Stores every UserID which shall be from tracking after the current interval was finished.
        /// </summary>
        public HashSet<ulong> EndUserTrackingAfterInterval;
        
        /// <summary>
        /// Constructor
        /// </summary>
        public VoiceStateTimeHelper()
        {
            _userTimeSteps = new ConcurrentDictionary<ulong, (DateTime?, List<double>)>();
            EndUserTrackingAfterInterval = new HashSet<ulong>();
        }

        /// <summary>
        /// Returns the tracked seconds since _currentIntervalStartTime.
        /// </summary>
        /// <param name="reset">Whether to reset the counter or not.</param>
        /// <returns>A Dictionary </returns>
        public IDictionary<ulong, double> GetUserTimes(bool reset = true)
        {
            var result = _userTimeSteps
                .Where(kv => kv.Value.Start >= _currentIntervalStartTime)
                .ToDictionary(kv => kv.Key, kv => 
                    //add positive StopTimes if there are any.
                    (kv.Value.StopTimes.Where(t => t >= 0).Sum() > 0 ? kv.Value.StopTimes.Where(t => t >= 0).Sum() : 0) + 
                    //add open time if kv.Value.Start is not null
                    (kv.Value.Start.HasValue ? (kv.Value.Start.Value - _currentIntervalStartTime).TotalSeconds : 0));

            if (reset) Reset();

            return result;
        }

        /// <summary>
        /// Resets the _userTimeSteps dictionary.
        /// </summary>
        public void Reset()
        {
            var currentIntervalStartNew = DateTime.Now;
            var usertimesteps = _userTimeSteps.Where(kv => kv.Value.Start >= _currentIntervalStartTime && !EndUserTrackingAfterInterval.Contains(kv.Key)).ToList();
            _userTimeSteps.Clear();
            foreach (var uts in usertimesteps)
            {
                _userTimeSteps.TryAdd(uts.Key, (currentIntervalStartNew, new List<double>()));
            }
            _currentIntervalStartTime = currentIntervalStartNew;
        }

        /// <summary>
        /// Starts time tracking of a specified user.
        /// </summary>
        /// <param name="userId">ID of the user.</param>
        /// <returns>True, if tracking could be enabled, otherwise false.</returns>
        public bool StartTracking(ulong userId)
        {
            bool success = false;

            if (_userTimeSteps.TryGetValue(userId, out var value) && !value.Start.HasValue)
            {
                var valuen = value;
                valuen.Start = DateTime.Now;
                _userTimeSteps.TryUpdate(userId, valuen, value);
                success = true;
            }
            else success = _userTimeSteps.TryAdd(userId, (DateTime.Now, new List<double>()));

            if (success) EndUserTrackingAfterInterval.Remove(userId);
            return success;
        }

        /// <summary>
        /// Stops time tracking of a specified User. If it fails but still should be stopped, the UserID can be added to EndUserTrackingAfterInterval.
        /// </summary>
        /// <param name="userId">ID of the user.</param>
        /// <returns>True, if User was tracked, otherwise false.</returns>
        public bool StopTracking(ulong userId)
        {
            if (!_userTimeSteps.TryGetValue(userId, out var value) || !value.Start.HasValue) return false;
            var valuen = value;
            valuen.StopTimes.Add((DateTime.Now - valuen.Start.Value).TotalSeconds);
            valuen.Start = null;
            return _userTimeSteps.TryUpdate(userId, valuen, value);
        }
    }
}
