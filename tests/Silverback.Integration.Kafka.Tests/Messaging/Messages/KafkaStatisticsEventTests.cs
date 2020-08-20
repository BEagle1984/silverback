// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using FluentAssertions;
using Silverback.Messaging.Messages;
using Xunit;

namespace Silverback.Tests.Integration.Kafka.Messaging.Messages
{
    public class KafkaStatisticsEventTests
    {
        [Fact]
        public void Constructor_ValidStatisticsJSON_StatisticsProperlyDeserialized()
        {
            var resourcesHelper = new ResourcesHelper(GetType().Assembly);
            var json = resourcesHelper.GetAsString("Silverback.Tests.Integration.Kafka.Resources.statistics.json");

            var statisticsEvent = new KafkaStatisticsEvent(json);

            // Global fields
            statisticsEvent.Statistics.Name.Should().Be("Test#consumer-1");
            statisticsEvent.Statistics.ClientId.Should().Be("Test");
            statisticsEvent.Statistics.Type.Should().Be("consumer");
            statisticsEvent.Statistics.Ts.Should().Be(7817348490);
            statisticsEvent.Statistics.Time.Should().Be(1583314688);
            statisticsEvent.Statistics.ReplyQ.Should().Be(1);
            statisticsEvent.Statistics.MsgCnt.Should().Be(2);
            statisticsEvent.Statistics.MsgSize.Should().Be(3);
            statisticsEvent.Statistics.MsgMax.Should().Be(4);
            statisticsEvent.Statistics.MsgSizeMax.Should().Be(5);
            statisticsEvent.Statistics.SimpleCnt.Should().Be(6);
            statisticsEvent.Statistics.MetadataCacheCnt.Should().Be(1);
            statisticsEvent.Statistics.Tx.Should().Be(340);
            statisticsEvent.Statistics.TxBytes.Should().Be(34401);
            statisticsEvent.Statistics.Rx.Should().Be(339);
            statisticsEvent.Statistics.RxBytes.Should().Be(44058);
            statisticsEvent.Statistics.TxMsgs.Should().Be(7);
            statisticsEvent.Statistics.TxMsgBytes.Should().Be(8);
            statisticsEvent.Statistics.RxMsgs.Should().Be(43);
            statisticsEvent.Statistics.RxMsgBytes.Should().Be(3870);

            // Broker fields
            statisticsEvent.Statistics.Brokers.Should().HaveCount(1);
            statisticsEvent.Statistics.Brokers.Should().ContainKey("kafka1:29092/1");

            var broker = statisticsEvent.Statistics.Brokers["kafka1:29092/1"];
            broker.Name.Should().Be("kafka1:29092/1");
            broker.NodeId.Should().Be(1);
            broker.NodeName.Should().Be("kafka1:29092");
            broker.Source.Should().Be("configured");
            broker.State.Should().Be("UP");
            broker.StateAge.Should().Be(34996215);
            broker.OutbufCnt.Should().Be(9);
            broker.OutbufMsgCnt.Should().Be(10);
            broker.WaitRespCnt.Should().Be(1);
            broker.WaitRespMsgCnt.Should().Be(11);
            broker.Tx.Should().Be(318);
            broker.TxBytes.Should().Be(31553);
            broker.TxErrs.Should().Be(12);
            broker.TxRetries.Should().Be(13);
            broker.ReqTimeouts.Should().Be(14);
            broker.Rx.Should().Be(317);
            broker.RxBytes.Should().Be(42795);
            broker.RxErrs.Should().Be(15);
            broker.RxCorriderrs.Should().Be(16);
            broker.RxPartial.Should().Be(17);
            broker.ZBufGrow.Should().Be(18);
            broker.BufGrow.Should().Be(19);
            broker.Wakeups.Should().Be(681);
            broker.Connects.Should().Be(1);
            broker.Disconnects.Should().Be(20);

            broker.IntLatency.Should().NotBeNull();
            broker.IntLatency.Min.Should().Be(21);
            broker.IntLatency.Max.Should().Be(22);
            broker.IntLatency.Avg.Should().Be(23);
            broker.IntLatency.Sum.Should().Be(24);
            broker.IntLatency.StdDev.Should().Be(25);
            broker.IntLatency.P50.Should().Be(26);
            broker.IntLatency.P75.Should().Be(27);
            broker.IntLatency.P90.Should().Be(28);
            broker.IntLatency.P95.Should().Be(29);
            broker.IntLatency.P99.Should().Be(30);
            broker.IntLatency.P99_99.Should().Be(31);
            broker.IntLatency.OutOfRange.Should().Be(32);
            broker.IntLatency.HdrSize.Should().Be(11376);
            broker.IntLatency.Cnt.Should().Be(33);

            broker.OutbufLatency.Should().NotBeNull();
            broker.OutbufLatency.Min.Should().Be(13);
            broker.OutbufLatency.Max.Should().Be(43);
            broker.OutbufLatency.Avg.Should().Be(19);
            broker.OutbufLatency.Sum.Should().Be(256);
            broker.OutbufLatency.StdDev.Should().Be(8);
            broker.OutbufLatency.P50.Should().Be(16);
            broker.OutbufLatency.P75.Should().Be(19);
            broker.OutbufLatency.P90.Should().Be(37);
            broker.OutbufLatency.P95.Should().Be(37);
            broker.OutbufLatency.P99.Should().Be(43);
            broker.OutbufLatency.P99_99.Should().Be(43);
            broker.OutbufLatency.OutOfRange.Should().Be(34);
            broker.OutbufLatency.HdrSize.Should().Be(11376);
            broker.OutbufLatency.Cnt.Should().Be(13);

            broker.Rtt.Should().NotBeNull();
            broker.Rtt.Min.Should().Be(1149);
            broker.Rtt.Max.Should().Be(101831);
            broker.Rtt.Avg.Should().Be(76277);
            broker.Rtt.Sum.Should().Be(991605);
            broker.Rtt.StdDev.Should().Be(38980);
            broker.Rtt.P50.Should().Be(101375);
            broker.Rtt.P75.Should().Be(101375);
            broker.Rtt.P90.Should().Be(101887);
            broker.Rtt.P95.Should().Be(101887);
            broker.Rtt.P99.Should().Be(101887);
            broker.Rtt.P99_99.Should().Be(101887);
            broker.Rtt.OutOfRange.Should().Be(35);
            broker.Rtt.HdrSize.Should().Be(13424);
            broker.Rtt.Cnt.Should().Be(13);

            broker.Throttle.Should().NotBeNull();
            broker.Throttle.Min.Should().Be(36);
            broker.Throttle.Max.Should().Be(37);
            broker.Throttle.Avg.Should().Be(38);
            broker.Throttle.Sum.Should().Be(39);
            broker.Throttle.StdDev.Should().Be(40);
            broker.Throttle.P50.Should().Be(41);
            broker.Throttle.P75.Should().Be(42);
            broker.Throttle.P90.Should().Be(43);
            broker.Throttle.P95.Should().Be(44);
            broker.Throttle.P99.Should().Be(45);
            broker.Throttle.P99_99.Should().Be(46);
            broker.Throttle.OutOfRange.Should().Be(47);
            broker.Throttle.HdrSize.Should().Be(17520);
            broker.Throttle.Cnt.Should().Be(13);

            broker.Requests.Should().NotBeNull();
            broker.Requests["Fetch"].Should().Be(313);
            broker.Requests["Offset"].Should().Be(48);
            broker.Requests["Metadata"].Should().Be(2);
            broker.Requests["OffsetCommit"].Should().Be(49);
            broker.Requests["OffsetFetch"].Should().Be(50);
            broker.Requests["FindCoordinator"].Should().Be(2);
            broker.Requests["JoinGroup"].Should().Be(51);
            broker.Requests["Heartbeat"].Should().Be(52);
            broker.Requests["LeaveGroup"].Should().Be(53);
            broker.Requests["SyncGroup"].Should().Be(54);
            broker.Requests["SaslHandshake"].Should().Be(55);
            broker.Requests["ApiVersion"].Should().Be(1);

            broker.TopicPartitions.Should().HaveCount(1);
            broker.TopicPartitions.Should().ContainKey("test-event-0");

            // TopPar fields
            var toppar = broker.TopicPartitions["test-event-0"];
            toppar.Topic.Should().Be("test-event");
            toppar.Partition.Should().Be(0);

            // Topic fields
            statisticsEvent.Statistics.Topics.Should().HaveCount(1);
            statisticsEvent.Statistics.Topics.Should().ContainKey("test-event");

            var topic = statisticsEvent.Statistics.Topics["test-event"];
            topic.Topic.Should().Be("test-event");
            topic.MetadataAge.Should().Be(29005);
            topic.BatchSize.Should().NotBeNull();
            topic.BatchSize.Min.Should().Be(90);
            topic.BatchSize.Max.Should().Be(90);
            topic.BatchSize.Avg.Should().Be(90);
            topic.BatchSize.Sum.Should().Be(360);
            topic.BatchSize.StdDev.Should().Be(123);
            topic.BatchSize.P50.Should().Be(90);
            topic.BatchSize.P75.Should().Be(90);
            topic.BatchSize.P90.Should().Be(90);
            topic.BatchSize.P95.Should().Be(90);
            topic.BatchSize.P99.Should().Be(90);
            topic.BatchSize.P99_99.Should().Be(90);
            topic.BatchSize.OutOfRange.Should().Be(123);
            topic.BatchSize.HdrSize.Should().Be(14448);
            topic.BatchSize.Cnt.Should().Be(4);

            topic.BatchCnt.Should().NotBeNull();
            topic.BatchCnt.Min.Should().Be(1);
            topic.BatchCnt.Max.Should().Be(1);
            topic.BatchCnt.Avg.Should().Be(1);
            topic.BatchCnt.Sum.Should().Be(4);
            topic.BatchCnt.StdDev.Should().Be(123);
            topic.BatchCnt.P50.Should().Be(1);
            topic.BatchCnt.P75.Should().Be(1);
            topic.BatchCnt.P90.Should().Be(1);
            topic.BatchCnt.P95.Should().Be(1);
            topic.BatchCnt.P99.Should().Be(1);
            topic.BatchCnt.P99_99.Should().Be(1);
            topic.BatchCnt.OutOfRange.Should().Be(123);
            topic.BatchCnt.HdrSize.Should().Be(8304);
            topic.BatchCnt.Cnt.Should().Be(4);

            // Partition fields
            topic.Partitions.Should().HaveCount(2);
            topic.Partitions.Should().ContainKey("0");

            var partition0 = topic.Partitions["0"];
            partition0.Partition.Should().Be(0);
            partition0.Broker.Should().Be(1);
            partition0.Leader.Should().Be(1);
            partition0.Desired.Should().BeTrue();
            partition0.Unknown.Should().BeFalse();
            partition0.MsgqCnt.Should().Be(123);
            partition0.MsgqBytes.Should().Be(123);
            partition0.XmitMsgqCnt.Should().Be(123);
            partition0.XmitMsgqBytes.Should().Be(123);
            partition0.FetchqCnt.Should().Be(123);
            partition0.FetchqSize.Should().Be(123);
            partition0.FetchState.Should().Be("active");
            partition0.QueryOffset.Should().Be(-1001);
            partition0.NextOffset.Should().Be(359);
            partition0.AppOffset.Should().Be(359);
            partition0.StoredOffset.Should().Be(359);
            partition0.CommitedOffset.Should().Be(359);
            partition0.CommittedOffset.Should().Be(359);
            partition0.EofOffset.Should().Be(359);
            partition0.LoOffset.Should().Be(-1001);
            partition0.HiOffset.Should().Be(359);
            partition0.LsOffset.Should().Be(359);
            partition0.ConsumerLag.Should().Be(123);
            partition0.TxMsgs.Should().Be(123);
            partition0.TxBytes.Should().Be(123);
            partition0.RxMsgs.Should().Be(43);
            partition0.RxBytes.Should().Be(3870);
            partition0.Msgs.Should().Be(43);
            partition0.RxVerDrops.Should().Be(123);
            partition0.MsgsInflight.Should().Be(123);
            partition0.NextAckSeq.Should().Be(123);
            partition0.NextErrSeq.Should().Be(123);
            partition0.AckedMsgId.Should().Be(123);

            var partition2 = topic.Partitions["-1"];
            partition2.Partition.Should().Be(-1);
            partition2.Broker.Should().Be(-1);
            partition2.Leader.Should().Be(-1);
            partition2.Desired.Should().BeFalse();
            partition2.Unknown.Should().BeFalse();
            partition2.MsgqCnt.Should().Be(123);
            partition2.MsgqBytes.Should().Be(123);
            partition2.XmitMsgqCnt.Should().Be(123);
            partition2.XmitMsgqBytes.Should().Be(123);
            partition2.FetchqCnt.Should().Be(123);
            partition2.FetchqSize.Should().Be(123);
            partition2.FetchState.Should().Be("none");
            partition2.QueryOffset.Should().Be(-1001);
            partition2.NextOffset.Should().Be(123);
            partition2.AppOffset.Should().Be(-1001);
            partition2.StoredOffset.Should().Be(-1001);
            partition2.CommitedOffset.Should().Be(-1001);
            partition2.CommittedOffset.Should().Be(-1001);
            partition2.EofOffset.Should().Be(-1001);
            partition2.LoOffset.Should().Be(-1001);
            partition2.HiOffset.Should().Be(-1001);
            partition2.LsOffset.Should().Be(-1001);
            partition2.ConsumerLag.Should().Be(-1);
            partition2.TxMsgs.Should().Be(123);
            partition2.TxBytes.Should().Be(123);
            partition2.RxMsgs.Should().Be(123);
            partition2.RxBytes.Should().Be(123);
            partition2.Msgs.Should().Be(123);
            partition2.RxVerDrops.Should().Be(123);
            partition2.MsgsInflight.Should().Be(123);
            partition2.NextAckSeq.Should().Be(123);
            partition2.NextErrSeq.Should().Be(123);
            partition2.AckedMsgId.Should().Be(123);

            // Consumer Group fields
            statisticsEvent.Statistics.ConsumerGroup.Should().NotBeNull();

            var consumerGroup = statisticsEvent.Statistics.ConsumerGroup;
            consumerGroup.State.Should().Be("up");
            consumerGroup.StateAge.Should().Be(34005);
            consumerGroup.JoinState.Should().Be("started");
            consumerGroup.RebalanceAge.Should().Be(29001);
            consumerGroup.RebalanceCnt.Should().Be(3);
            consumerGroup.RebalanceReason.Should().Be("group rejoin");
            consumerGroup.AssignmentSize.Should().Be(1);

            // EOS fields
            statisticsEvent.Statistics.ExactlyOnceSemantics.Should().NotBeNull();

            var eos = statisticsEvent.Statistics.ExactlyOnceSemantics;
            eos.IdempState.Should().Be("Assigned");
            eos.IdempStateAge.Should().Be(12345);
            eos.TxnState.Should().Be("InTransaction");
            eos.TxnStateAge.Should().Be(123);
            eos.TxnMayEnq.Should().BeTrue();
            eos.ProducerId.Should().Be(13);
            eos.ProducerEpoch.Should().Be(12345);
            eos.EpochCnt.Should().Be(7890);
        }
    }
}
