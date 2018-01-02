﻿using System;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;

namespace WatsonTcp
{
    public class ClientMetadata
    {
        #region Public-Members

        public TcpClient Tcp;
        public SslStream Ssl;

        #endregion

        #region Private-Members
          
        #endregion

        private string ipPort;

        #endregion

        #region Constructors-and-Factories

        public ClientMetadata(TcpClient tcp)
        {
            if (tcp == null) throw new ArgumentNullException(nameof(tcp));
            Tcp = tcp;

            ipPort = tcp.Client.RemoteEndPoint.ToString();
        }

        public ClientMetadata(TcpClient tcp, SslStream ssl)
        {
            if (tcp == null) throw new ArgumentNullException(nameof(tcp));
            if (ssl == null) throw new ArgumentNullException(nameof(ssl));

            Tcp = tcp;
            Ssl = ssl;

            ipPort = tcp.Client.RemoteEndPoint.ToString();
        }

        #endregion

        #region Public-Methods

        public string IpPort
        {
            get { return ipPort; }
        }

        #endregion

        #region Private-Methods

        #endregion
    }
}