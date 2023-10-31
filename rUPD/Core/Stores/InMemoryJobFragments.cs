/*
    File Name: InMemoryJobFragments.cs
    Author: Elio Decolli
    Last Update: 31/10/2023
    Purpose:
                Used to store and re-assemble packet fragments.
 
 */

using rUDP.Core.Interfaces;
using rUDP.Core.Models;
using rUDP.Core.Types;

namespace rUDP.Core.Stores;

public sealed class InMemoryJobFragments : IJobFragments
{
    private readonly Dictionary<int, UdpFragment> _fragments;
    private readonly HashSet<int> _bufferedPackets;
    private readonly int _totalFragments;
    private readonly int _fragmentSize;

    private readonly UdpBuffer _cachedBuffer;

    public InMemoryJobFragments(int totalFragments, int packetLength, short fragmentSize)
    {
        _fragments = new Dictionary<int, UdpFragment>();
        _totalFragments = totalFragments;
        _fragmentSize = fragmentSize;

        _bufferedPackets = new HashSet<int>();

        var initialBuffer = new byte[packetLength];
        Array.Fill<byte>(initialBuffer, 0x00);

        _cachedBuffer = new UdpBuffer(new MemoryStream(initialBuffer), false);
    }

    private void WriteToPosition(byte[] data, int index, Stream stream)
    {
        var position = _fragmentSize * (index - 1);
        stream.Seek(position, SeekOrigin.Begin);

        stream.Write(data, 0, data.Length);
    }

    public UdpBuffer GenerateLatestResult(bool returnIfIncomplete = true)
    {
        if(_cachedBuffer.IsComplete)
        {
            return _cachedBuffer;
        }

        for(int i = 0; i < _totalFragments; i++)
        {
            var fragmentNumber = i + 1;
            if(_fragments.TryGetValue(fragmentNumber, out var fragment))
            {
                if (!_bufferedPackets.Contains(fragment.FragmentNumber))
                {
                    WriteToPosition(Utils.StripHeaders(fragment), fragment.FragmentNumber, _cachedBuffer.Buffer);
                    _bufferedPackets.Add(fragment.FragmentNumber);
                }
            }
            else
            {
                // this means we're missing a fragment, this area will be filled with 0x00s or we're gonna return null.
                if(!returnIfIncomplete)
                {
                    return new UdpBuffer(new MemoryStream(), false);
                }
            }
        }

        _cachedBuffer.IsComplete = (_bufferedPackets.Count == _totalFragments);
        return _cachedBuffer;
    }

    public int GetCurrentNumberOfFragments()
    {
        return _fragments.Count;
    }

    public bool RegisterFragment(UdpFragment fragment)
    {
        if(_fragments.ContainsKey(fragment.FragmentNumber))
        {
            return false;
        }

        _fragments.Add(fragment.FragmentNumber, fragment);
        return true;
    }

    public IEnumerable<int> ReportMissingFragments()
    {
        var missing = new List<int>();
        for(int i = 1; i <= _totalFragments ; i++)
        {
            if(!_fragments.ContainsKey(i))
            {
                missing.Add(i);
            }
        }

        return missing;
    }

    public bool IsCompleted() => _cachedBuffer.IsComplete;
}
