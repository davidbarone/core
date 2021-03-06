﻿using Dbarone.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Dbarone.Schedule
{

    /// <summary>
    /// Parses cron expressions, and calculates the next scheduled date
    /// for a cron expression.
    /// </summary>
    public class Cron
    {

        #region ICronNode classes

        public interface ICronNode { }

        /// <summary>
        /// Represents a single number
        /// </summary>
        public class CronNumber : ICronNode
        {
            public int Number;
            public CronNumber(int number)
            {
                this.Number = number;
            }
        }

        /// <summary>
        /// Represents a range of numbers.
        /// </summary>
        public class CronRange : ICronNode
        {
            public int Start;
            public int End;

            public CronRange(int start, int end)
            {
                this.Start = start;
                this.End = end;
            }
        }

        /// <summary>
        /// Represents the wildcard character '*'.
        /// </summary>
        public class CronWild : ICronNode
        { }

        /// <summary>
        /// Represents a repeat syntax, like 1/5, meaning
        /// repeat every 5 units, starting at 1.
        /// </summary>
        public class CronRepeat : ICronNode
        {
            public int Start;
            public int Repeat;
            public CronRepeat(int start, int repeat)
            {
                this.Start = start;
                this.Repeat = repeat;
            }
        }

        #endregion

        #region Regex

        private Regex regNum = new Regex(@"^(?<num>\d+)$");
        private Regex regWild = new Regex(@"^[*]$");
        private Regex regRepeat = new Regex(@"^(?<start>\d+)\/(?<repeat>\d+)$");
        private Regex regRange = new Regex(@"^(?<start>\d+)\-(?<end>\d+)$");

        #endregion

        #region Cron string fields

        private string Second { get; set; }
        private string Minute { get; set; }
        private string Hour { get; set; }
        private string DayOfMonth { get; set; }
        private string Month { get; set; }
        private string DayOfWeek { get; set; }
        private string Year { get; set; }
        private string Command { get; set; }

        #endregion

        #region Cron nodes for string fields

        private List<ICronNode> nSec = new List<ICronNode>();
        private List<ICronNode> nMin = new List<ICronNode>();
        private List<ICronNode> nHr = new List<ICronNode>();
        private List<ICronNode> nDOM = new List<ICronNode>();
        private List<ICronNode> nMon = new List<ICronNode>();
        private List<ICronNode> nDOW = new List<ICronNode>();
        private List<ICronNode> nYr = new List<ICronNode>();

        #endregion

        #region Validation

        /// <summary>
        /// Validates the string arguments passed in during the cron instantiation.
        /// Throws an exeption if anything is wrong.
        /// </summary>
        private void Validate()
        {
            // process arguments and store in ICronNode lists.
            this.nSec = processArg(Second, 0, 59);
            this.nMin = processArg(Minute, 0, 59);
            this.nHr = processArg(Hour, 0, 23);
            this.nDOM = processArg(DayOfMonth, 1, 31);
            this.nMon = processArg(Month, 1, 12);
            this.nDOW = processArg(DayOfWeek, 1, 7);
            this.nYr = processArg(Year, 1970, 2099);
        }

        private List<ICronNode> processArg(string s, int min, int max)
        {
            List<ICronNode> result = new List<ICronNode>();
            var elements = s.Split(',');
            foreach (var element in elements)
            {
                result.Add(processElement(element, min, max));
            }
            return result;
        }

        private ICronNode processElement(string s, int min, int max)
        {
            // wildcard?
            var match = regWild.Match(s);
            if (match.Success)
                return new CronWild();

            // number
            match = regNum.Match(s);
            if (match.Success)
            {
                var number = int.Parse(match.Groups["num"].Value);
                if (number >= min && number <= max)
                    return new CronNumber(number);
                else
                    throw new Exception("Number out of range.");
            }

            // Range
            match = regRange.Match(s);
            if (match.Success)
            {
                var start = int.Parse(match.Groups["start"].Value);
                var end = int.Parse(match.Groups["end"].Value);
                if (start >= min && start <= max && end >= min && end <= max && start < end)
                    return new CronRange(start, end);
                else
                    throw new Exception("Numbers out of range.");
            }

            // Repeat
            match = regRepeat.Match(s);
            if (match.Success)
            {
                var start = int.Parse(match.Groups["start"].Value);
                var repeat = int.Parse(match.Groups["repeat"].Value);
                if (start >= min && start <= max && repeat >= 1 && repeat <= max)
                    return new CronRepeat(start, repeat);
                else
                    throw new Exception("Numbers out of range.");
            }

            // if got here, then invalid format
            throw new Exception(string.Format("Invalid cron format: [{0}].", s));
        }

        #endregion

        #region Next() Methods

        private bool MatchCron(DateTime dateTime, Func<DateTime, int> datePart, List<ICronNode> cronNodes)
        {
            int num = datePart.Invoke(dateTime);

            foreach (ICronNode node in cronNodes)
            {
                if (node as CronWild != null) return true;
                if (node as CronNumber != null && ((CronNumber)node).Number == num) return true;
                if (node as CronRange != null && ((CronRange)node).Start <= num && ((CronRange)node).End >= num) return true;
                if (node as CronRepeat != null && num >= ((CronRepeat)node).Start && ((num - ((CronRepeat)node).Start) % ((CronRepeat)node).Repeat == 0)) return true;
            }
            return false;

        }

        #endregion

        #region Public methods

        /// <summary>
        /// Creates a cron record from a crontab expression.
        /// Note that this requires 7-part format: [sec] [min]
        /// [hr] [dom] [mon] [dow] [yr] [command]. Each of the cron fields only accept the following:
        /// <para/>
        /// [nn] [nn-mm] [nn,mm] [*]
        /// </summary>
        /// <param name="s">The crontab string expression.</param>
        /// <returns>A configured, validated Cron instance.</returns>
        public static Cron Create(string s)
        {
            Cron cron = new Cron();
            var args = s.ParseArgs();
            // Need min 7 args - the 'command' arg can be ommitted if
            // cron used purely for scheduling.
            if (args.Length < 7)
            {
                throw new Exception("Too few arguments for cron expression.");
            }
            cron.Second = args[0];
            cron.Minute = args[1];
            cron.Hour = args[2];
            cron.DayOfMonth = args[3];
            cron.Month = args[4];
            cron.DayOfWeek = args[5];
            cron.Year = args[6];
            if (args.Length>7)
                cron.Command = string.Join(" ", args, 7, args.Length - 7);
            cron.Validate();
            return cron;
        }

        /// <summary>
        /// Gets the next scheduled occurence based on the crontab.
        /// If no date in the future is matched, returns null.
        /// </summary>
        /// <param name="startDateTime">Starting date to use.</param>
        /// <returns>The next scheduled occurence.</returns>
        public DateTime? Next(DateTime startDateTime)
        {
            DateTime startDate = startDateTime.Date;

            // Get next date
            DateTime nextDate = GetDate(startDate);
            // Get next date/time
            if (nextDate.Date == startDate.Date)
                nextDate = startDateTime;
            DateTime? nextDateTime = GetDateTime(nextDate);

            if (nextDate.Date == startDate.Date && nextDateTime == null)
            {
                // if the next date is same as start date
                // and no time match, get the next date (without time)
                // on schedule
                nextDate = GetDate(nextDate.AddDays(1).Date);
                nextDateTime = GetDateTime(nextDate);
            }

            // At this point, if no nextDateTime
            // then null value will automatically be
            // returned.
            return nextDateTime;
        }

        /// <summary>
        /// Gets the next scheduled occurence based on the crontab.
        /// If no date in the future is matched, returns null.
        /// </summary>
        /// <returns>The next scheduled occurence.</returns>
        public DateTime? Next()
        {
            return Next(DateTime.Now);
        }


        /// <summary>
        /// Returns the next date match or 31/12/2099, whichever is sooner.
        /// </summary>
        /// <param name="startDate"></param>
        /// <returns></returns>
        private DateTime GetDate(DateTime startDate)
        {
            while (startDate <= new DateTime(2099, 12, 31))
            {
                if (
                    MatchCron(startDate, (d) => { return d.Year; }, nYr) &&
                    MatchCron(startDate, (d) => { return d.Month; }, nMon) &&
                    MatchCron(startDate, (d) => { return d.Day; }, nDOM) &&
                    MatchCron(startDate, (d) => { return (int)d.DayOfWeek + 1; }, nDOW))
                    break;

                startDate = startDate.AddDays(1);
            }
            return startDate;
        }


        /// <summary>
        /// Returns the time within the matched date where
        /// the first match occurs. If no match found on the 
        /// date (for example found on today's date, but
        /// today's schedule already passed), then null value
        /// is returned.
        /// </summary>
        /// <param name="startDateTime">The start date / time.</param>
        /// <returns></returns>
        private DateTime? GetDateTime(DateTime startDateTime)
        {
            DateTime endDateTime = startDateTime;
            while (endDateTime.Date == startDateTime.Date)
            {
                if (
                    MatchCron(endDateTime, (d) => { return d.Hour; }, nHr) &&
                    MatchCron(endDateTime, (d) => { return d.Minute; }, nMin) &&
                    MatchCron(endDateTime, (d) => { return d.Second; }, nSec))
                    break;

                endDateTime = endDateTime.AddSeconds(1);
            }
            if (endDateTime.Date == startDateTime.Date)
                // If we're still in the same day, then previous
                // loop successfully broken early.
                return endDateTime;
            else
                // no match for time.
                return null;
        }

        #endregion

    }
}

