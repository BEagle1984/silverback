// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using NSubstitute;
using Shouldly;
using Silverback.Diagnostics;
using Silverback.Messaging.Broker.Callbacks.Statistics;
using Xunit;

namespace Silverback.Tests.Integration.Kafka.Messaging.KafkaEvents.Statistics;

public class KafkaStatisticsDeserializerTests
{
    [Fact]
    public void TryDeserialize_ValidStatisticsJson_StatisticsProperlyDeserialized()
    {
        ResourcesHelper resourcesHelper = new(GetType().Assembly);
        string json = resourcesHelper.GetAsString("Silverback.Tests.Integration.Kafka.Resources.statistics.json");

        KafkaStatistics? statistics = KafkaStatisticsDeserializer.TryDeserialize(
            json,
            Substitute.For<ISilverbackLogger>());

        // Global fields
        statistics!.Name.ShouldBe("Test#consumer-1");
        statistics.ClientId.ShouldBe("Test");
        statistics.Type.ShouldBe("consumer");
        statistics.Ts.ShouldBe(7817348490);
        statistics.Time.ShouldBe(1583314688);
        statistics.ReplyQ.ShouldBe(1);
        statistics.MsgCnt.ShouldBe(2);
        statistics.MsgSize.ShouldBe(3);
        statistics.MsgMax.ShouldBe(4);
        statistics.MsgSizeMax.ShouldBe(5);
        statistics.SimpleCnt.ShouldBe(6);
        statistics.MetadataCacheCnt.ShouldBe(1);
        statistics.Tx.ShouldBe(340);
        statistics.TxBytes.ShouldBe(34401);
        statistics.Rx.ShouldBe(339);
        statistics.RxBytes.ShouldBe(44058);
        statistics.TxMsgs.ShouldBe(7);
        statistics.TxMsgBytes.ShouldBe(8);
        statistics.RxMsgs.ShouldBe(43);
        statistics.RxMsgBytes.ShouldBe(3870);

        // Broker fields
        statistics.Brokers.Count.ShouldBe(1);
        statistics.Brokers.ShouldContainKey("kafka1:29092/1");

        BrokerStatistics broker = statistics.Brokers["kafka1:29092/1"];
        broker.Name.ShouldBe("kafka1:29092/1");
        broker.NodeId.ShouldBe(1);
        broker.NodeName.ShouldBe("kafka1:29092");
        broker.Source.ShouldBe("configured");
        broker.State.ShouldBe("UP");
        broker.StateAge.ShouldBe(34996215);
        broker.OutbufCnt.ShouldBe(9);
        broker.OutbufMsgCnt.ShouldBe(10);
        broker.WaitRespCnt.ShouldBe(1);
        broker.WaitRespMsgCnt.ShouldBe(11);
        broker.Tx.ShouldBe(318);
        broker.TxBytes.ShouldBe(31553);
        broker.TxErrs.ShouldBe(12);
        broker.TxRetries.ShouldBe(13);
        broker.ReqTimeouts.ShouldBe(14);
        broker.Rx.ShouldBe(317);
        broker.RxBytes.ShouldBe(42795);
        broker.RxErrs.ShouldBe(15);
        broker.RxCorriderrs.ShouldBe(16);
        broker.RxPartial.ShouldBe(17);
        broker.ZBufGrow.ShouldBe(18);
        broker.BufGrow.ShouldBe(19);
        broker.Wakeups.ShouldBe(681);
        broker.Connects.ShouldBe(1);
        broker.Disconnects.ShouldBe(20);

        broker.IntLatency.ShouldNotBeNull();
        broker.IntLatency.Min.ShouldBe(21);
        broker.IntLatency.Max.ShouldBe(22);
        broker.IntLatency.Avg.ShouldBe(23);
        broker.IntLatency.Sum.ShouldBe(24);
        broker.IntLatency.StdDev.ShouldBe(25);
        broker.IntLatency.P50.ShouldBe(26);
        broker.IntLatency.P75.ShouldBe(27);
        broker.IntLatency.P90.ShouldBe(28);
        broker.IntLatency.P95.ShouldBe(29);
        broker.IntLatency.P99.ShouldBe(30);
        broker.IntLatency.P99_99.ShouldBe(31);
        broker.IntLatency.OutOfRange.ShouldBe(32);
        broker.IntLatency.HdrSize.ShouldBe(11376);
        broker.IntLatency.Cnt.ShouldBe(33);

        broker.OutbufLatency.ShouldNotBeNull();
        broker.OutbufLatency.Min.ShouldBe(13);
        broker.OutbufLatency.Max.ShouldBe(43);
        broker.OutbufLatency.Avg.ShouldBe(19);
        broker.OutbufLatency.Sum.ShouldBe(256);
        broker.OutbufLatency.StdDev.ShouldBe(8);
        broker.OutbufLatency.P50.ShouldBe(16);
        broker.OutbufLatency.P75.ShouldBe(19);
        broker.OutbufLatency.P90.ShouldBe(37);
        broker.OutbufLatency.P95.ShouldBe(37);
        broker.OutbufLatency.P99.ShouldBe(43);
        broker.OutbufLatency.P99_99.ShouldBe(43);
        broker.OutbufLatency.OutOfRange.ShouldBe(34);
        broker.OutbufLatency.HdrSize.ShouldBe(11376);
        broker.OutbufLatency.Cnt.ShouldBe(13);

        broker.Rtt.ShouldNotBeNull();
        broker.Rtt.Min.ShouldBe(1149);
        broker.Rtt.Max.ShouldBe(101831);
        broker.Rtt.Avg.ShouldBe(76277);
        broker.Rtt.Sum.ShouldBe(991605);
        broker.Rtt.StdDev.ShouldBe(38980);
        broker.Rtt.P50.ShouldBe(101375);
        broker.Rtt.P75.ShouldBe(101375);
        broker.Rtt.P90.ShouldBe(101887);
        broker.Rtt.P95.ShouldBe(101887);
        broker.Rtt.P99.ShouldBe(101887);
        broker.Rtt.P99_99.ShouldBe(101887);
        broker.Rtt.OutOfRange.ShouldBe(35);
        broker.Rtt.HdrSize.ShouldBe(13424);
        broker.Rtt.Cnt.ShouldBe(13);

        broker.Throttle.ShouldNotBeNull();
        broker.Throttle.Min.ShouldBe(36);
        broker.Throttle.Max.ShouldBe(37);
        broker.Throttle.Avg.ShouldBe(38);
        broker.Throttle.Sum.ShouldBe(39);
        broker.Throttle.StdDev.ShouldBe(40);
        broker.Throttle.P50.ShouldBe(41);
        broker.Throttle.P75.ShouldBe(42);
        broker.Throttle.P90.ShouldBe(43);
        broker.Throttle.P95.ShouldBe(44);
        broker.Throttle.P99.ShouldBe(45);
        broker.Throttle.P99_99.ShouldBe(46);
        broker.Throttle.OutOfRange.ShouldBe(47);
        broker.Throttle.HdrSize.ShouldBe(17520);
        broker.Throttle.Cnt.ShouldBe(13);

        broker.Requests.ShouldNotBeNull();
        broker.Requests["Fetch"].ShouldBe(313);
        broker.Requests["Offset"].ShouldBe(48);
        broker.Requests["Metadata"].ShouldBe(2);
        broker.Requests["OffsetCommit"].ShouldBe(49);
        broker.Requests["OffsetFetch"].ShouldBe(50);
        broker.Requests["FindCoordinator"].ShouldBe(2);
        broker.Requests["JoinGroup"].ShouldBe(51);
        broker.Requests["Heartbeat"].ShouldBe(52);
        broker.Requests["LeaveGroup"].ShouldBe(53);
        broker.Requests["SyncGroup"].ShouldBe(54);
        broker.Requests["SaslHandshake"].ShouldBe(55);
        broker.Requests["ApiVersion"].ShouldBe(1);

        broker.TopicPartitions.Count.ShouldBe(1);
        broker.TopicPartitions.ShouldContainKey("test-event-0");

        // TopPar fields
        TopicPartitions toppar = broker.TopicPartitions["test-event-0"];
        toppar.Topic.ShouldBe("test-event");
        toppar.Partition.ShouldBe(0);

        // Topic fields
        statistics.Topics.Count.ShouldBe(1);
        statistics.Topics.ShouldContainKey("test-event");

        TopicStatistics topic = statistics.Topics["test-event"];
        topic.Topic.ShouldBe("test-event");
        topic.MetadataAge.ShouldBe(29005);
        topic.BatchSize.ShouldNotBeNull();
        topic.BatchSize.Min.ShouldBe(90);
        topic.BatchSize.Max.ShouldBe(90);
        topic.BatchSize.Avg.ShouldBe(90);
        topic.BatchSize.Sum.ShouldBe(360);
        topic.BatchSize.StdDev.ShouldBe(123);
        topic.BatchSize.P50.ShouldBe(90);
        topic.BatchSize.P75.ShouldBe(90);
        topic.BatchSize.P90.ShouldBe(90);
        topic.BatchSize.P95.ShouldBe(90);
        topic.BatchSize.P99.ShouldBe(90);
        topic.BatchSize.P99_99.ShouldBe(90);
        topic.BatchSize.OutOfRange.ShouldBe(123);
        topic.BatchSize.HdrSize.ShouldBe(14448);
        topic.BatchSize.Cnt.ShouldBe(4);

        topic.BatchCnt.ShouldNotBeNull();
        topic.BatchCnt.Min.ShouldBe(1);
        topic.BatchCnt.Max.ShouldBe(1);
        topic.BatchCnt.Avg.ShouldBe(1);
        topic.BatchCnt.Sum.ShouldBe(4);
        topic.BatchCnt.StdDev.ShouldBe(123);
        topic.BatchCnt.P50.ShouldBe(1);
        topic.BatchCnt.P75.ShouldBe(1);
        topic.BatchCnt.P90.ShouldBe(1);
        topic.BatchCnt.P95.ShouldBe(1);
        topic.BatchCnt.P99.ShouldBe(1);
        topic.BatchCnt.P99_99.ShouldBe(1);
        topic.BatchCnt.OutOfRange.ShouldBe(123);
        topic.BatchCnt.HdrSize.ShouldBe(8304);
        topic.BatchCnt.Cnt.ShouldBe(4);

        // Partition fields
        topic.Partitions.Count.ShouldBe(2);
        topic.Partitions.ShouldContainKey("0");

        PartitionStatistics partition0 = topic.Partitions["0"];
        partition0.Partition.ShouldBe(0);
        partition0.Broker.ShouldBe(1);
        partition0.Leader.ShouldBe(1);
        partition0.Desired.ShouldBe(true);
        partition0.Unknown.ShouldBe(false);
        partition0.MsgqCnt.ShouldBe(123);
        partition0.MsgqBytes.ShouldBe(123);
        partition0.XmitMsgqCnt.ShouldBe(123);
        partition0.XmitMsgqBytes.ShouldBe(123);
        partition0.FetchqCnt.ShouldBe(123);
        partition0.FetchqSize.ShouldBe(123);
        partition0.FetchState.ShouldBe("active");
        partition0.QueryOffset.ShouldBe(-1001);
        partition0.NextOffset.ShouldBe(359);
        partition0.AppOffset.ShouldBe(359);
        partition0.StoredOffset.ShouldBe(359);
        partition0.CommitedOffset.ShouldBe(359);
        partition0.CommittedOffset.ShouldBe(359);
        partition0.EofOffset.ShouldBe(359);
        partition0.LoOffset.ShouldBe(-1001);
        partition0.HiOffset.ShouldBe(359);
        partition0.LsOffset.ShouldBe(359);
        partition0.ConsumerLag.ShouldBe(123);
        partition0.TxMsgs.ShouldBe(123);
        partition0.TxBytes.ShouldBe(123);
        partition0.RxMsgs.ShouldBe(43);
        partition0.RxBytes.ShouldBe(3870);
        partition0.Msgs.ShouldBe(43);
        partition0.RxVerDrops.ShouldBe(123);
        partition0.MsgsInflight.ShouldBe(123);
        partition0.NextAckSeq.ShouldBe(123);
        partition0.NextErrSeq.ShouldBe(123);
        partition0.AckedMsgId.ShouldBe(123);

        PartitionStatistics partition2 = topic.Partitions["-1"];
        partition2.Partition.ShouldBe(-1);
        partition2.Broker.ShouldBe(-1);
        partition2.Leader.ShouldBe(-1);
        partition2.Desired.ShouldBe(false);
        partition2.Unknown.ShouldBe(false);
        partition2.MsgqCnt.ShouldBe(123);
        partition2.MsgqBytes.ShouldBe(123);
        partition2.XmitMsgqCnt.ShouldBe(123);
        partition2.XmitMsgqBytes.ShouldBe(123);
        partition2.FetchqCnt.ShouldBe(123);
        partition2.FetchqSize.ShouldBe(123);
        partition2.FetchState.ShouldBe("none");
        partition2.QueryOffset.ShouldBe(-1001);
        partition2.NextOffset.ShouldBe(123);
        partition2.AppOffset.ShouldBe(-1001);
        partition2.StoredOffset.ShouldBe(-1001);
        partition2.CommitedOffset.ShouldBe(-1001);
        partition2.CommittedOffset.ShouldBe(-1001);
        partition2.EofOffset.ShouldBe(-1001);
        partition2.LoOffset.ShouldBe(-1001);
        partition2.HiOffset.ShouldBe(-1001);
        partition2.LsOffset.ShouldBe(-1001);
        partition2.ConsumerLag.ShouldBe(-1);
        partition2.TxMsgs.ShouldBe(123);
        partition2.TxBytes.ShouldBe(123);
        partition2.RxMsgs.ShouldBe(123);
        partition2.RxBytes.ShouldBe(123);
        partition2.Msgs.ShouldBe(123);
        partition2.RxVerDrops.ShouldBe(123);
        partition2.MsgsInflight.ShouldBe(123);
        partition2.NextAckSeq.ShouldBe(123);
        partition2.NextErrSeq.ShouldBe(123);
        partition2.AckedMsgId.ShouldBe(123);

        // Consumer Group fields
        statistics.ConsumerGroup.ShouldNotBeNull();

        ConsumerGroupStatistics consumerGroup = statistics.ConsumerGroup;
        consumerGroup.State.ShouldBe("up");
        consumerGroup.StateAge.ShouldBe(34005);
        consumerGroup.JoinState.ShouldBe("started");
        consumerGroup.RebalanceAge.ShouldBe(29001);
        consumerGroup.RebalanceCnt.ShouldBe(3);
        consumerGroup.RebalanceReason.ShouldBe("group rejoin");
        consumerGroup.AssignmentSize.ShouldBe(1);

        // EOS fields
        statistics.ExactlyOnceSemantics.ShouldNotBeNull();

        ExactlyOnceSemanticsStatistics eos = statistics.ExactlyOnceSemantics;
        eos.IdempState.ShouldBe("Assigned");
        eos.IdempStateAge.ShouldBe(12345);
        eos.TxnState.ShouldBe("InTransaction");
        eos.TxnStateAge.ShouldBe(123);
        eos.TxnMayEnq.ShouldBe(true);
        eos.ProducerId.ShouldBe(13);
        eos.ProducerEpoch.ShouldBe(12345);
        eos.EpochCnt.ShouldBe(7890);
    }

    [Fact]
    public void TryDeserialize_InvalidStatisticsJson_NullStatisticsReturned()
    {
        string json = "{ WTF?! }";

        KafkaStatistics? statistics = KafkaStatisticsDeserializer.TryDeserialize(
            json,
            Substitute.For<ISilverbackLogger>());

        statistics.ShouldBeNull();
    }
}
