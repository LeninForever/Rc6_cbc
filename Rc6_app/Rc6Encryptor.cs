using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;

namespace Rc6_app
{
    class Rc6
    {
        /// <summary>
        /// Число раундов
        /// </summary>
        public int Rounds { get; set; }

        /// <summary>
        /// Длина ключа в битах (128, 192, 256) или (16, 24, 36)
        /// </summary>
        public int KeyLength { get; set; }
        public byte[] Key { get; set; }

        //Константы экспоненты и золотого сечения
        private const uint P32 = 0xB7E15163;
        private const uint Q32 = 0x9E3779B9;
        private const int portionTextLength = 16; // Длина порции текста в байтах
        private const int W = 32; // Длина машинного слова в битах
        readonly int roundKeysCount;
        public uint[] roundKeys;

        private byte[] _initVector = new byte[portionTextLength];

       /// <summary>
       /// 
       /// </summary>
       /// <param name="rounds">Rounds number</param>
       /// <param name="key">key</param>
       /// <param name="initVector">init vector length of 16</param>
       /// <exception cref="KeyOutOfRangeException"></exception>
        public Rc6(int rounds, byte[] key, byte[] initVector)
        {
            Rounds = rounds;

            initVector.CopyTo(_initVector, 0);
            KeyLength = key.Length % 4 == 0 ? key.Length : key.Length + (4 - key.Length % 4);
            if (KeyLength > 32)
                throw new KeyOutOfRangeException("Key have to be less or equal 32s");
            Key = new byte[KeyLength];
            key.CopyTo(Key, 0);
            roundKeysCount = 2 * Rounds + 4;
            GenerateRoundKeys();
        }

        public void GenerateRoundKeys()
        {
            // Split 128(16) / 192(24) / 256(32) bit key for c part length of 32
            var l = new List<uint>();
            var chunks = new List<byte[]>();
            var buffer = new byte[4];

            for (int i = 0; i < Key.Length; i++)
            {
                buffer[i % 4] = Key[i];
                if ((i + 1) % 4 == 0)
                {
                    chunks.Add(buffer);
                    buffer = new byte[4];
                }
            }

            for (int i = 0; i < chunks.Count; i++)
            {
                l.Add(BitConverter.ToUInt32(chunks[i], 0));
            }
            var keys = new uint[roundKeysCount];

            keys[0] = P32;

            for (int i = 1; i < roundKeysCount; i++)
            {
                keys[i] = keys[i - 1] + Q32;
            }

            uint A = 0, B = 0;
            int iter = 0, jiter = 0;

            var v = 3 * Math.Max(chunks.Count, 2 * Rounds + 3);

            for (int s = 1; s < v; s++)
            {
                A = keys[iter] = LeftShift((keys[iter] + A + B), 3);
                B = l[jiter] = LeftShift(l[jiter] + A + B, (int)(A + B));

                iter = (iter + 1) % (roundKeysCount);
                jiter = (jiter + 1) % chunks.Count;
            }

            roundKeys = keys;
        }
        public string EncryptedBytesText { get; set; }
        public byte[] Encrypt(string text)
        {
            uint A, B, C, D;
            var byteText = Encoding.UTF8.GetBytes(text);
            var builder = new StringBuilder();
            foreach (var _byte in byteText)
            {
                builder.Append(Convert.ToString(_byte, 16) + " ");
            }
            EncryptedBytesText = builder.ToString();
            // get the text length % 16 = 0
            int length = byteText.Length % 16 == 0 ? byteText.Length : byteText.Length + (16 - byteText.Length % 16);

            var appendedByteText = new byte[length];

            byteText.CopyTo(appendedByteText, 0);

            var encryptedText = new byte[length];
            byte[] cbcBlock = new byte[16];
            _initVector.CopyTo(cbcBlock, 0);

            for (int i = 0; i < length; i += 16)
            {
                byte[] block = new byte[16];
                Array.Copy(appendedByteText, i, block, 0, 16);
                for (int k = 0; k < block.Length; k++)
                {
                    block[k] ^= cbcBlock[k];
                }
                A = BitConverter.ToUInt32(block, 0);
                B = BitConverter.ToUInt32(block, 4);
                C = BitConverter.ToUInt32(block, 8);
                D = BitConverter.ToUInt32(block, 12);

                B += roundKeys[0];
                D += roundKeys[1];

                for (int j = 1; j <= Rounds; j++)
                {
                    uint t = LeftShift(B * (2 * B + 1), (int)Math.Log(W, 2));
                    uint u = LeftShift(D * (2 * D + 1), (int)Math.Log(W, 2));

                    A = LeftShift(A ^ t, (int)u) + roundKeys[j * 2];
                    C = LeftShift(C ^ u, (int)t) + roundKeys[j * 2 + 1];

                    uint temp = A;
                    A = B;
                    B = C;
                    C = D;
                    D = temp;
                }

                A += roundKeys[2 * Rounds + 2];

                C += roundKeys[2 * Rounds + 3];
                var encryptedBlock = ToArrayBytes(new uint[4] { A, B, C, D }, 4);
                encryptedBlock.CopyTo(encryptedText, i);
                encryptedBlock.CopyTo(cbcBlock, 0);
            }
            List<byte> bufferBytes = new List<byte>(length + 16);

            for (int i = 0; i < 16; i++)
            {
                bufferBytes.Add(_initVector[i]);
            }
            for (int i = 0; i < length; i++)
            {
                bufferBytes.Add(encryptedText[i]);
            }

            return bufferBytes.ToArray();
        }

        public string Decrypt(byte[] text)
        {
            uint A, B, C, D;
            
            var byteText = text.Skip(16).ToArray();

            int length = byteText.Length % 16 == 0 ? byteText.Length : byteText.Length + (16 - byteText.Length % 16);

            var appendedByteText = new byte[length];
            var cbcBlock = text.Take(16).ToArray();
            byteText.CopyTo(appendedByteText, 0);

            var decryptedText = new byte[length];

            for (int i = 0; i < length; i += 16)
            {
                byte[] block = new byte[16];

                Array.Copy(appendedByteText, i, block, 0, 16);
                A = BitConverter.ToUInt32(block, 0);
                B = BitConverter.ToUInt32(block, 4);
                C = BitConverter.ToUInt32(block, 8);
                D = BitConverter.ToUInt32(block, 12);

                C -= roundKeys[2 * Rounds + 3];
                A -= roundKeys[2 * Rounds  + 2];

                for (int j = Rounds; j >= 1; j--)
                {
                    uint temp = D;
                    D = C;
                    C = B;
                    B = A;
                    A = temp;

                    uint u = LeftShift(D * (2 * D + 1), (int)Math.Log(W, 2));
                    uint t = LeftShift(B * (2 * B + 1), (int)Math.Log(W, 2));

                    C = RightShift((C - roundKeys[2 * j + 1]), (int)t) ^ u;
                    A = RightShift((A - roundKeys[2 * j]), (int)u) ^ t;
                }

                D -= roundKeys[1];
                B -= roundKeys[0];

                var decryptedBlock = ToArrayBytes(new uint[4] { A, B, C, D }, 4);


                for (int k = 0; k < decryptedBlock.Length; k++)
                {
                    decryptedBlock[k] ^= cbcBlock[k];
                }

                decryptedBlock.CopyTo(decryptedText, i);
                block.CopyTo(cbcBlock, 0);
            }
            DecodedBytes = decryptedText;
            return Encoding.UTF8.GetString(decryptedText);
        }
        public byte[] DecodedBytes { get; set; }
        public uint RightShift(uint value, int shift)
        {
            return ((value >> shift) | (value << (W - shift)));
        }
       
        public uint LeftShift(uint value, int shift)
        {
            return ((value << shift) | (value >> (W - shift)));
        }
        private static byte[] ToArrayBytes(uint[] uints, int length)
        {
            byte[] arrayBytes = new byte[length * 4];
            for (int i = 0; i < length; i++)
            {
                byte[] temp = BitConverter.GetBytes(uints[i]);
                temp.CopyTo(arrayBytes, i * 4);
            }
            return arrayBytes;
        }
    }
}