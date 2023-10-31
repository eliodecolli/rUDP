using rUDP.Core.Enums;
using rUDP.Core.Models;
using System.Net;

namespace rUDP.Core;

public static class Utils
{
    public static UdpPacketWrapper ParsePacket(byte[] data)
    {
        using var memoryStream = new MemoryStream(data);
        using var reader = new BinaryReader(memoryStream);

        var header = (UdpHeader)reader.ReadByte();
        if (header == UdpHeader.CloseChannel)  // closing a channel doesn't really need additional data
        {
            return new UdpPacketWrapper(header, new byte[] { });
        }

        var dataLen = reader.ReadInt32();
        var packetData = reader.ReadBytes(dataLen);

        return new UdpPacketWrapper(header, packetData);
    }

    public static byte[] SerializeUdpPacket(UdpHeader header, byte[] data)
    {
        using var memoryStream = new MemoryStream();
        using var writer = new BinaryWriter(memoryStream);

        writer.Write((byte)header);
        writer.Write(data.Length);
        writer.Write(data);
        writer.Flush();

        return memoryStream.ToArray();
    }

    public static byte[] SerializeFragmentPacket(UdpFragment fragment)
    {
        using var memoryStream = new MemoryStream();
        using var writer = new BinaryWriter(memoryStream);

        writer.Write((byte)UdpHeader.UdpFragment);
        writer.Write(fragment.Buffer.Length);
        writer.Write(fragment.Buffer);
        writer.Flush();

        return memoryStream.ToArray();
    }

    public static JobResponse ParseJobResponse(byte[] data)
    {
        using var memoryStream = new MemoryStream(data);
        using var reader  = new BinaryReader(memoryStream);

        var type = (JobResponseType)reader.ReadByte();
        var jobId = Guid.Parse(reader.ReadString());

        switch (type)
        {
            case JobResponseType.FragmentNAck:
            case JobResponseType.FragmentAck:
                var fragmentNumber = reader.ReadInt32();
                return new FragmentAckResponse(type, jobId, fragmentNumber);
            case JobResponseType.JobEnd:
                return new JobResponse(type, jobId);
            default:
                throw new Exception("Invalid job response received.");
        }
    }

    public static UdpFragment ParseUdpFragment(byte[] data)
    {
        using var memoryStream = new MemoryStream(data);
        using var reader = new BinaryReader(memoryStream);

        var jobId = Guid.Parse(reader.ReadString());
        var totalFragments = reader.ReadInt32();
        var totalLength = reader.ReadInt32();
        var fragmentNumber = reader.ReadInt32();
        var dataLen = reader.ReadInt32();
        var fragmentData = reader.ReadBytes(dataLen);
        
        return new UdpFragment(jobId, fragmentNumber, totalFragments, totalLength, fragmentData);
    }

    public static byte[] SerializeJobResponse(JobResponse response)
    {
        using var memoryStream = new MemoryStream();
        using var writer = new BinaryWriter(memoryStream);

        writer.Write((byte)response.ResponseType);
        writer.Write(response.JobId.ToString());

        if (response is FragmentAckResponse)
        {
            writer.Write(((FragmentAckResponse)response).FragmentNumber);
        }

        writer.Flush();

        return memoryStream.ToArray();
    }

    public static byte[] StripHeaders(UdpFragment fragment)
    {
        using var memoryStream = new MemoryStream(fragment.Buffer);
        using var reader = new BinaryReader(memoryStream);

        // skip the headers
        reader.ReadString();  // job id
        reader.ReadInt32();  // total fragments
        reader.ReadInt32();  // total length
        reader.ReadInt32();  // fragment number

        var dataLen = reader.ReadInt32();
        var data = reader.ReadBytes(dataLen);

        return data;
    }

    public static int GetPort(HashSet<int> currentlyRunningPorts)
    {
        var port = new Random().Next(4000, 5000);
        if(currentlyRunningPorts.Contains(port))
        {
            return GetPort(currentlyRunningPorts);
        }

        return port;
    }

    public static async Task<List<UdpFragment>> FragmentData(byte[] data, int fragmentLength, Guid jobId)
    {
        return await Task.Run(() =>
        {
            var batches = new List<byte[]>();
            int currentLen = 1;
            List<byte> currentBatch = new List<byte>();

            for (int i = 0; i < data.Length; i++)
            {
                if(currentLen > fragmentLength)
                {
                    batches.Add(currentBatch.ToArray());

                    currentLen = 1;
                    currentBatch = new List<byte>();
                }

                currentBatch.Add(data[i]);
                currentLen++;
            }

            // add the remainder
            if(currentBatch.Count > 0)
            {
                batches.Add(currentBatch.ToArray());
            }

            int totalFragments = batches.Count;

            var retval = new List<UdpFragment>();

            for (int i = 0; i < totalFragments; i++)
            {
                retval.Add(new UdpFragment(jobId, i + 1, totalFragments, data.Length, batches[i], null));
            }
            return retval;
        });
    }
}
