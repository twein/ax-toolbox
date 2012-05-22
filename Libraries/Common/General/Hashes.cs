using System;

public static class Hashes
{
    public static UInt32 MurmurHash2(Byte[] data, UInt32 seed = 0xc58f1a7b)
    {
        const UInt32 m = 0x5bd1e995;

        var length = data.Length;
        UInt32 h = seed ^ (UInt32)length;

        if (length > 0)
        {
            UInt32 k;
            int currentIndex = 0;

            //body
            while (length - currentIndex >= 4)
            {
                k = GetKey(data, currentIndex);
                k *= m;
                k ^= k >> 24;
                k *= m;

                h *= m;
                h ^= k;

                currentIndex += 4;
            }

            //tail
            k = GetKey(data, currentIndex);

            h *= m;

            // finalization
            h ^= h >> 13;
            h *= m;
            h ^= h >> 15;
        }

        return h;
    }

    public static UInt32 MurmurHash3(byte[] data, UInt32 seed = 0xc58f1a7b)
    {
        const UInt32 c1 = 0xcc9e2d51;
        const UInt32 c2 = 0x1b873593;

        var length = data.Length;
        UInt32 h = seed;

        if (length > 0)
        {
            UInt32 k = 0;
            int currentIndex = 0;

            // body
            while (length - currentIndex >= 4)
            {
                k = GetKey(data, currentIndex);
                k *= c1;
                k = Rotl32(k, 15);
                k *= c2;

                h ^= k;
                h = Rotl32(h, 13);
                h = h * 5 + 0xe6546b64;

                currentIndex += 4;
            }


            // tail
            k = GetKey(data, currentIndex);
            k *= c1;
            k = Rotl32(k, 15);
            k *= c2;

            h ^= k;


            // finalization
            length = data.Length;
            h ^= (UInt32)length;

            h ^= h >> 16;
            h *= 0x85ebca6b;
            h ^= h >> 13;
            h *= 0xc2b2ae35;
            h ^= h >> 16;
        }

        return h;
    }
    private static UInt32 GetKey(byte[] data, int currentIndex)
    {
        UInt32 k = data[currentIndex++];
        if (currentIndex < data.Length) k |= (UInt32)(data[currentIndex++] << 8);
        if (currentIndex < data.Length) k |= (UInt32)(data[currentIndex++] << 16);
        if (currentIndex < data.Length) k |= (UInt32)(data[currentIndex++] << 24);

        return k;
    }
    private static UInt32 Rotl32(UInt32 x, byte r)
    {
        return (x << r) | (x >> (32 - r));
    }
}