﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace WatsonTcp
{
    /// <summary>
    /// Event arguments for when a stream is received from the server.
    /// </summary>
    public class StreamReceivedFromServerEventArgs<TMetadata>
    {
        internal StreamReceivedFromServerEventArgs(TMetadata metadata, long contentLength, Stream stream)
        {
            Metadata = metadata;
            ContentLength = contentLength;
            DataStream = stream;
        }

        /// <summary>
        /// The metadata received from the server.
        /// </summary>
        public TMetadata Metadata
        {
            get
            {
                return _Metadata;
            }
            set
            {
                if (value == null) _Metadata = default(TMetadata);
                else _Metadata = value;
            }
        }

        /// <summary>
        /// The number of data bytes that should be read from DataStream.
        /// </summary>
        public long ContentLength { get; }

        /// <summary>
        /// The stream containing the message data.
        /// </summary>
        public Stream DataStream { get; }

        /// <summary>
        /// The data from DataStream.
        /// Using Data will fully read the contents of DataStream.
        /// </summary>
        public byte[] Data
        {
            get
            {
                if (_Data != null) return _Data;
                if (ContentLength <= 0) return null;
                _Data = StreamToBytes(DataStream);
                return _Data;
            }
        }

        private TMetadata _Metadata = default(TMetadata);
        private byte[] _Data = null;

        private byte[] StreamToBytes(Stream input)
        {
            if (input == null) throw new ArgumentNullException(nameof(input));
            if (!input.CanRead) throw new InvalidOperationException("Input stream is not readable");

            byte[] buffer = new byte[16 * 1024];
            using (MemoryStream ms = new MemoryStream())
            {
                int read;

                while ((read = input.Read(buffer, 0, buffer.Length)) > 0)
                {
                    ms.Write(buffer, 0, read);
                }

                return ms.ToArray();
            }
        }
    }
}
