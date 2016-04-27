﻿//-----------------------------------------------------------------------
// <copyright file="DeadlineFailureDetectorSpec.cs" company="Akka.NET Project">
//     Copyright (C) 2009-2016 Lightbend Inc. <http://www.lightbend.com>
//     Copyright (C) 2013-2016 Akka.NET project <https://github.com/akkadotnet/akka.net>
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using Akka.TestKit;
using Xunit;

namespace Akka.Remote.Tests
{
    public class DeadlineFailureDetectorSpec : AkkaSpec
    {
        [Fact]
        public void DeadlineFailureDetector_must_mark_node_as_monitored_after_a_series_of_successful_heartbeats()
        {
            var timeInterval = new List<long>() {0, 1000, 100, 100};
            var fd = CreateFailureDetector(TimeSpan.FromSeconds(5),
                FailureDetectorSpecHelpers.FakeTimeGenerator(timeInterval));

            Assert.False(fd.IsMonitoring);

            fd.HeartBeat();
            fd.HeartBeat();
            fd.HeartBeat();

            Assert.True(fd.IsMonitoring);
            Assert.True(fd.IsAvailable);
        }

        [Fact]
        public void DeadlineFailureDetector_must_mark_node_as_dead_if_heartbeats_are_missed()
        {
            var timeInterval = new List<long>() { 0, 1000, 100, 100, 7000 };
            var fd = CreateFailureDetector(TimeSpan.FromSeconds(5),
                FailureDetectorSpecHelpers.FakeTimeGenerator(timeInterval));

            fd.HeartBeat(); //0
            fd.HeartBeat(); //1000
            fd.HeartBeat(); //1100

            Assert.True(fd.IsAvailable); //1200
            Assert.False(fd.IsAvailable); //8200
        }

        [Fact]
        public void
            DeadlineFailureDetector_must_mark_node_as_available_if_it_starts_heartbeat_again_after_being_marked_dead()
        {
            var regularIntervals = new List<long>() {0L}.Concat(Enumerable.Repeat(1000L, 999));
            var timeIntervals =
                regularIntervals.Concat(new List<long>() {(5*60*1000), 100, 900, 100, 7000, 100, 900, 100, 900}).ToList();
            var fd = CreateFailureDetector(TimeSpan.FromSeconds(7),
                FailureDetectorSpecHelpers.FakeTimeGenerator(timeIntervals));

            for(var i = 0; i < 1000; i++) fd.HeartBeat();
            Assert.False(fd.IsAvailable); //after the long pause
            fd.HeartBeat();
            Assert.True(fd.IsAvailable);
            fd.HeartBeat();
            Assert.False(fd.IsAvailable); //after the 7 second pause
            fd.HeartBeat();
            Assert.True(fd.IsAvailable);
            fd.HeartBeat();
            Assert.True(fd.IsAvailable);
        }

        [Fact]
        public void DeadlineFailureDetector_must_accept_some_configured_missing_heartbeats()
        {
            var timeInterval = new List<long>() { 0, 1000, 1000, 1000, 4000, 1000, 1000 };
            var fd = CreateFailureDetector(TimeSpan.FromSeconds(5),
                FailureDetectorSpecHelpers.FakeTimeGenerator(timeInterval));

            fd.HeartBeat(); 
            fd.HeartBeat(); 
            fd.HeartBeat(); 
            fd.HeartBeat();
            Assert.True(fd.IsAvailable);
            fd.HeartBeat();
            Assert.True(fd.IsAvailable);
        }

        [Fact]
        public void DeadlineFailureDetector_must_fail_after_configured_acceptable_missing_heartbeats()
        {
            var timeInterval = new List<long>() { 0, 1000, 1000, 1000, 1000, 1000, 500, 500, 5000 };
            var fd = CreateFailureDetector(TimeSpan.FromSeconds(5),
                FailureDetectorSpecHelpers.FakeTimeGenerator(timeInterval));

            fd.HeartBeat();
            fd.HeartBeat();
            fd.HeartBeat();
            fd.HeartBeat();
            fd.HeartBeat();
            fd.HeartBeat();
            Assert.True(fd.IsAvailable);
            fd.HeartBeat();
            Assert.False(fd.IsAvailable);
        }

        private DeadlineFailureDetector CreateFailureDetector(TimeSpan acceptableLostDuration,
            Clock clock = null)
        {
            return new DeadlineFailureDetector(acceptableLostDuration, clock ?? FailureDetector.DefaultClock);
        }
    }
}

