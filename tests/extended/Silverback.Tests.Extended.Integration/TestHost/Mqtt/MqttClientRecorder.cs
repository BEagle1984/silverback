// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MQTTnet;
using MQTTnet.Diagnostics.PacketInspection;

namespace Silverback.Tests.Extended.Integration.TestHost.Mqtt;

// Records the deliveries received by a native MQTT client and the acknowledgments it sends, as they appear on the wire. The
// connections are counted by their successful CONNACK, which precedes all the deliveries of the connection. Create it before the
// host, so that its handlers run before the ones of Silverback, which can block on backpressure.
internal sealed class MqttClientRecorder
{
    private const int ConnAckType = 2;

    private const int PublishType = 3;

    private const int PubAckType = 4;

    private const int PubRecType = 5;

    private const int PubCompType = 7;

    private readonly System.Threading.Lock _lock = new();

    private readonly List<bool> _sessionPresentFlags = []; // The flag of each connection, in order

    private readonly List<Delivery> _deliveries = [];

    private readonly List<Acknowledgment> _acknowledgments = [];

    private readonly List<Acknowledgment> _completions = [];

    private int _disconnections;

    private int _inFlight;

    private int _maxInFlight;

    private (int Acknowledgment, Action Action)? _beforeAcknowledgment;

    public MqttClientRecorder(IMqttClient client)
    {
        client.InspectPacketAsync += OnInspectPacketAsync;
        client.ApplicationMessageReceivedAsync += OnApplicationMessageReceivedAsync;
        client.DisconnectedAsync += OnDisconnectedAsync;
    }

    public int Connections
    {
        get
        {
            lock (_lock)
            {
                return _sessionPresentFlags.Count;
            }
        }
    }

    public int Disconnections
    {
        get
        {
            lock (_lock)
            {
                return _disconnections;
            }
        }
    }

    // Whether the broker resumed a session with the last connection
    public bool IsSessionPresent
    {
        get
        {
            lock (_lock)
            {
                return _sessionPresentFlags is [.., true];
            }
        }
    }

    // The maximum number of QoS 1 and 2 deliveries that were waiting for acknowledgment at the same time, within a connection
    public int MaxInFlight
    {
        get
        {
            lock (_lock)
            {
                return _maxInFlight;
            }
        }
    }

    public IReadOnlyList<Delivery> GetDeliveries()
    {
        lock (_lock)
        {
            return [.. _deliveries];
        }
    }

    // The PUBACK (QoS 1) and PUBREC (QoS 2) packets, in the order they were sent
    public IReadOnlyList<Acknowledgment> GetAcknowledgments()
    {
        lock (_lock)
        {
            return [.. _acknowledgments];
        }
    }

    // The PUBCOMP packets completing the QoS 2 deliveries, in the order they were sent
    public IReadOnlyList<Acknowledgment> GetCompletions()
    {
        lock (_lock)
        {
            return [.. _completions];
        }
    }

    // Invokes the action once, before the specified PUBACK (QoS 1) or PUBREC (QoS 2) of the current connection is sent (1 for the first
    // one). MQTTnet inspects each packet before writing it, so the action can cut the connection right before the acknowledgment is sent.
    public void InvokeBeforeAcknowledgment(int acknowledgment, Action action)
    {
        lock (_lock)
        {
            _beforeAcknowledgment = (acknowledgment, action);
        }
    }

    // The variable header follows the fixed header byte and the remaining length, a variable byte integer whose bytes flag the next
    // one with their most significant bit
    private static int GetVariableHeaderOffset(byte[] buffer)
    {
        int offset = 1;

        while ((buffer[offset] & 0x80) != 0)
        {
            offset++;
        }

        return offset + 1;
    }

    private static ushort ReadUInt16(byte[] buffer, int offset) => (ushort)((buffer[offset] << 8) | buffer[offset + 1]);

    private Task OnInspectPacketAsync(InspectMqttPacketEventArgs args)
    {
        byte[] buffer = args.Buffer;

        if (buffer.Length < 2)
            return Task.CompletedTask;

        int type = buffer[0] >> 4;
        int offset = GetVariableHeaderOffset(buffer);
        Action? action = null;

        lock (_lock)
        {
            if (args.Direction == MqttPacketFlowDirection.Inbound)
                RecordInboundPacket(type, buffer, offset);
            else
                RecordOutboundPacket(type, buffer, offset);

            if (type is PubAckType or PubRecType && args.Direction == MqttPacketFlowDirection.Outbound &&
                _beforeAcknowledgment is { } beforeAcknowledgment &&
                _acknowledgments.Count(acknowledgment => acknowledgment.Connection == _sessionPresentFlags.Count) ==
                beforeAcknowledgment.Acknowledgment)
            {
                action = beforeAcknowledgment.Action;
                _beforeAcknowledgment = null;
            }
        }

        // Invoked outside the lock, since the action can trigger the disconnection, which other threads record
        action?.Invoke();

        return Task.CompletedTask;
    }

    private void RecordInboundPacket(int type, byte[] buffer, int offset)
    {
        switch (type)
        {
            // The deliveries of a resumed session are sent right after the CONNACK, so the connection is counted before receiving them
            case ConnAckType when buffer[offset + 1] == 0:
                _sessionPresentFlags.Add((buffer[offset] & 0x01) != 0);
                _inFlight = 0;
                break;
            case PublishType when ((buffer[0] >> 1) & 0x03) > 0:
                _inFlight++;
                _maxInFlight = Math.Max(_maxInFlight, _inFlight);
                break;
        }
    }

    private void RecordOutboundPacket(int type, byte[] buffer, int offset)
    {
        switch (type)
        {
            case PubAckType:
                _acknowledgments.Add(new Acknowledgment(_sessionPresentFlags.Count, ReadUInt16(buffer, offset)));
                _inFlight--;
                break;
            case PubRecType:
                _acknowledgments.Add(new Acknowledgment(_sessionPresentFlags.Count, ReadUInt16(buffer, offset)));
                break;
            case PubCompType:
                _completions.Add(new Acknowledgment(_sessionPresentFlags.Count, ReadUInt16(buffer, offset)));
                _inFlight--;
                break;
        }
    }

    private Task OnApplicationMessageReceivedAsync(MqttApplicationMessageReceivedEventArgs args)
    {
        lock (_lock)
        {
            _deliveries.Add(
                new Delivery(
                    _sessionPresentFlags.Count,
                    args.PacketIdentifier,
                    args.ApplicationMessage.Dup,
                    args.ApplicationMessage.Payload.ToArray()));
        }

        return Task.CompletedTask;
    }

    private Task OnDisconnectedAsync(MqttClientDisconnectedEventArgs args)
    {
        lock (_lock)
        {
            // Only the disconnections of an established connection, not the failed connection attempts
            if (args.ClientWasConnected)
                _disconnections++;
        }

        return Task.CompletedTask;
    }

    public sealed record Delivery(int Connection, ushort PacketIdentifier, bool IsDuplicate, byte[] Payload);

    public sealed record Acknowledgment(int Connection, ushort PacketIdentifier);
}
