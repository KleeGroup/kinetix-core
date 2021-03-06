﻿using KinetixCore.Monitoring.Impl.Analytics;
using System;
using System.Collections.Generic;

namespace KinetixCore.Monitoring
{
    public class AProcess
    {
        public string Category { get; }
        public string Name { get; }
        public long Start { get; }
        public long End { get; }
        public IDictionary<string, double> Measures { get; }
        public IDictionary<string, string> Tags { get; }
        public IList<AProcess> SubProcesses { get; }

        /// <summary>
        /// Process to hold monitoring data
        /// </summary>
        /// <param name="myCategory"></param>
        /// <param name="myName"></param>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <param name="measures"></param>
        /// <param name="tags"></param>
        /// <param name="subProcesses"></param>
        public AProcess(string myCategory, string myName, DateTime start, DateTime end, IDictionary<string, double> measures, IDictionary<string, string> tags, IList<AProcess> subProcesses)
        {
            this.Category = myCategory;
            this.Name = myName;
            this.Start = DateTimeUtil.ConvertDateTimeToEpoch(start);
            this.End = DateTimeUtil.ConvertDateTimeToEpoch(end);
            this.Measures = measures;
            this.Tags = tags;
            this.SubProcesses = subProcesses;
        }


        public static AProcessBuilder Builder(string category, string name) => new AProcessBuilder(category, name);


        public long DurationMillis() => End - Start;
        
    }
}
