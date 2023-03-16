using Microsoft.Extensions.Logging.Abstractions;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;
using GiveawayProject.Commands;

namespace GiveawayProject.Commands
{
    public class timeUnit
    {
        private timeOptions _timeUnit;
        private int _timeNumber;

        public timeUnit(timeOptions timeUnit, int timeNumber)
        {
            _timeUnit = timeUnit;
            _timeNumber = timeNumber;
        }

        public timeOptions getTimeUnit()
        {
            return _timeUnit;
        }

        public long getTimeNumber()
        {
            return _timeNumber;
        }

        public long getDuration()
        {
            // Assign to temporary value
            long value = _timeNumber;

            // Convert the value to seconds based on the unit
            switch (_timeUnit)
            {
                case timeOptions.days:
                    return value * 86400;
                case timeOptions.hours:
                    return value * 3600;
                case timeOptions.minutes:
                    return value * 60;
                default:
                    return value;
            }
        }
    }
}
