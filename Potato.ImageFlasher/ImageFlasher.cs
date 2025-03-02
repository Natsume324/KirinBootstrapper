using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Ports;
using System.Linq;

namespace Potato.ImageFlasher
{
    public class ImageFlasher
    {
        private const int BAUDRATE = 115200;
        private const int MAX_DATA_LEN = 0x400;
        private readonly static byte[] HeadFrame = { 0xFE, 0x00, 0xFF, 0x01 };
        private readonly static byte[] DataFrame = { 0xDA };
        private readonly static byte[] TailFrame = { 0xED };

        private SerialPort port;

        public void Open(string portName)
        {
            if (port != null && port.IsOpen)
                throw new InvalidOperationException("Port is already open.");

            port = new SerialPort
            {
                PortName = portName,
                BaudRate = BAUDRATE,
                DtrEnable = true,
                RtsEnable = true,
                ReadTimeout = 1000,
                WriteTimeout = 1000
            };
            port.Open();
        }

        public void Close()
        {
            if (port != null)
            {
                port.Close();
                port.Dispose();
                port = null;
            }
        }

        public void Write(string path, int address, bool sendTailFrame = true, Action<int> reportProgress = null)
        {
            if (!File.Exists(path))
                throw new FileNotFoundException($"File not found: {path}");

            using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read))
            {
                int totalLength = (int)stream.Length;
                int remainingLength = totalLength;
                int frameCount = (int)Math.Ceiling((double)totalLength / MAX_DATA_LEN);
                int frameNumber = 0;

                SendHeadFrame(totalLength, address);

                byte[] buffer = new byte[MAX_DATA_LEN];
                while (remainingLength > 0)
                {
                    int bytesToRead = Math.Min(MAX_DATA_LEN, remainingLength);
                    stream.Read(buffer, 0, bytesToRead);

                    frameNumber++;
                    SendDataFrame(frameNumber, buffer.Take(bytesToRead).ToArray());

                    remainingLength -= bytesToRead;


                    if (frameCount > 250 ? frameNumber % 10 == 0 : frameNumber % 3 == 0)
                    {
                        reportProgress?.Invoke(frameNumber * 100 / frameCount);
                    }
                }

                reportProgress?.Invoke(100);


                if (sendTailFrame)
                {
                    SendTailFrame(frameNumber + 1);
                }
            }
        }

        private void SendHeadFrame(int length, int address)
        {
            var data = new List<byte>(HeadFrame);

            data.AddRange(BitConverter.GetBytes(length).Reverse());
            data.AddRange(BitConverter.GetBytes(address).Reverse());

            SendFrame(data.ToArray());
        }

        private void SendDataFrame(int frameNumber, byte[] data)
        {
            var frame = new List<byte>(DataFrame)
            {
                (byte)(frameNumber & 0xFF),
                (byte)((~frameNumber) & 0xFF)
            };

            frame.AddRange(data);

            SendFrame(frame.ToArray());
        }

        private void SendTailFrame(int frameNumber)
        {
            var frame = new List<byte>(TailFrame)
            {
                (byte)(frameNumber & 0xFF),
                (byte)((~frameNumber) & 0xFF)
            };

            SendFrame(frame.ToArray());
        }

        private void SendFrame(byte[] data)
        {
            var crc = CRC.GetChecksum(data);

            var frameWithCRC = new List<byte>(data)
            {
                (byte)((crc >> 8) & 0xFF),
                (byte)(crc & 0xFF)
            };

            port.Write(frameWithCRC.ToArray(), 0, frameWithCRC.Count);


            int ack;
            try
            {
                ack = port.ReadByte();
            }
            catch (TimeoutException)
            {
                throw new IOException("Timeout waiting for ACK from the device.");
            }

            if (ack != 0xAA)
            {
                throw new InvalidDataException($"Invalid ACK received: 0x{ack:X2}. Expected: 0xAA.");
            }

            port.DiscardInBuffer();
            port.DiscardOutBuffer();
        }
    }
}
