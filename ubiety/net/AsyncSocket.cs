// AsyncSocket.cs
//
//Ubiety XMPP Library Copyright (C) 2006 - 2015 Dieter Lunn
//
//This library is free software; you can redistribute it and/or modify it under
//the terms of the GNU Lesser General Public License as published by the Free
//Software Foundation; either version 3 of the License, or (at your option)
//any later version.
//
//This library is distributed in the hope that it will be useful, but WITHOUT
//ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS
//FOR A PARTICULAR PURPOSE. See the GNU Lesser General Public License for more
//
//You should have received a copy of the GNU Lesser General Public License along
//with this library; if not, write to the Free Software Foundation, Inc., 59
//Temple Place, Suite 330, Boston, MA 02111-1307 USA

using System;
using System.IO;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
//using Serilog;
using Ubiety.Common;
using Ubiety.Infrastructure;
using Ubiety.Infrastructure.Extensions;
using Ubiety.Registries;
using Ubiety.States;

namespace Ubiety.Net
{
    /// <remarks>
    ///     AsyncSocket is the class that communicates with the server.
    /// </remarks>
    internal class AsyncSocket
    {
        // Timeout after 5 seconds by default
        /*
                private const int Timeout = 5000;
        */
        private const int BufferSize = 4096;
        private readonly byte[] _bufferBytes = new byte[BufferSize];
        private readonly Address _destinationAddress;
        private readonly ManualResetEvent _timeoutEvent = new ManualResetEvent(false);
        private readonly UTF8Encoding _utf = new UTF8Encoding();
        private ICompression _compression;
        private bool _compressed;
        private Socket _socket;
        private Stream _stream;

        // here changed
        private EventWaitHandle wait;

        #region Properties

        public AsyncSocket()
        {
            wait = new EventWaitHandle(false, EventResetMode.AutoReset);
            _destinationAddress = new Address();
        }

        /// <summary>
        ///     Gets the current status of the socket.
        /// </summary>
        public bool Connected { get; private set; }

        /*
                /// <summary>
                /// </summary>
                public string Hostname
                {
                    get { return _destinationAddress.Hostname; }
                }
        */

        /*
                /// <summary>
                /// </summary>
                public bool Secure { get; set; }
        */

        #endregion

        /// <summary>
        ///     Establishes a connection to the specified remote host.
        /// </summary>
        /// <returns>True if we connected, false if we didn't</returns>
        public void Connect()
        {
            IPAddress address = _destinationAddress.NextIpAddress();
            IPEndPoint end;
            if (address != null)
            {
                end = new IPEndPoint(address, ProtocolState.Settings.Port);
            }
            else
            {
                ProtocolState.Events.Error(this, ErrorType.ConnectionTimeout, ErrorSeverity.Fatal, "Unable to obtain server IP address.");
                return;
            }

            _socket = !_destinationAddress.IPv6 ? new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp) : new Socket(AddressFamily.InterNetworkV6, SocketType.Stream, ProtocolType.Tcp);
            _socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);

            try
            {
                _socket.BeginConnect(end, FinishConnect, _socket);
                //if (!_timeoutEvent.WaitOne(Timeout))
                //{
                //    Errors.Instance.SendError(this, ErrorType.ConnectionTimeout, "Timed out connecting to server.");
                //    return;
                //}
            }
            catch (SocketException /*e*/)
            {
                //Log.Error(e, "Error in connecting socket.");
                ProtocolState.Events.Error(this, ErrorType.ConnectionTimeout, ErrorSeverity.Fatal, "Unable to connect to server.");
            }
        }

        private void FinishConnect(IAsyncResult ar)
        {
            try
            {
                var socket = (Socket)ar.AsyncState;
                socket.EndConnect(ar);

                Connected = true;

                var netstream = new NetworkStream(socket);
                _stream = netstream;

                _stream.BeginRead(_bufferBytes, 0, BufferSize, Receive, null);

                ProtocolState.State = new ConnectedState();
                ProtocolState.State.Execute();
            }
            finally
            {
                _timeoutEvent.Set();
            }
        }

        /// <summary>
        ///     Disconnects the socket from the server.
        /// </summary>
        public void Disconnect()
        {
            Connected = false;
            _stream.Close();
            _socket.Shutdown(SocketShutdown.Both);
            _socket.Disconnect(true);
            _destinationAddress.srvRecordsReset();
        }

        /// <summary>
        ///     Encrypts the connection using SSL/TLS
        /// </summary>
        public void StartSecure()
        {
            var sslstream = new SslStream(_stream, true, RemoteValidation);
            try
            {
                sslstream.AuthenticateAsClient(_destinationAddress.Hostname, null, SslProtocols.Tls, false);
                if (sslstream.IsAuthenticated)
                {
                    _stream = sslstream;
                    //wait.Set();
                }
            }
            catch (Exception /*e*/)
            {
                //Log.Error(e, "Error is starting secure connection.");
                ProtocolState.Events.Error(this, ErrorType.XmlError, ErrorSeverity.Fatal, "Cannot connect with SSL.");
            }
        }

        private static bool RemoteValidation(object sender, X509Certificate cert, X509Chain chain,
            SslPolicyErrors errors)
        {
            if (errors == SslPolicyErrors.None)
            {
                return true;
            }

            return false;
        }

        /// <summary>
        ///     Writes data to the current connection.
        /// </summary>
        /// <param name="msg">Message to send</param>
        public void Write(string msg)
        {
            //Log.Debug("Outgoing message: {Message}", msg);

            if (!Connected) return;
            byte[] mesg = _utf.GetBytes(msg);
            mesg = _compressed ? _compression.Deflate(mesg) : mesg;
            _stream.Write(mesg, 0, mesg.Length);
        }

        /*public void SetWait(string id)
        {
            wait = EventWaitHandle.OpenExisting("ubiety.proceed.wait" + id);
        }*/

        private void Receive(IAsyncResult ar)
        {
            try
            {
                _stream.EndRead(ar);

                byte[] t = _bufferBytes.TrimNull();

                string m = _utf.GetString(_compressed ? _compression.Inflate(t, t.Length) : t);

                //Log.Debug("Incoming Message: {Message}", m);

                // here changed
                /*if (m.Contains("<proceed"))
                { // ssl
                    wait.Set();
                    wait.WaitOne();
                    ProtocolState.State = new ConnectedState();
                    ProtocolState.State.Execute();
                }*/

                ProtocolParser.Parse(m);

                // Clear the buffer otherwise we get leftover tags and it confuses the parser.
                _bufferBytes.Clear();

                if (!Connected || ProtocolState.State is DisconnectedState) return;

                _stream.BeginRead(_bufferBytes, 0, _bufferBytes.Length, Receive, null);
            }
            catch (SocketException /*e*/)
            {
                //Log.Error(e, "Error in socket receiving data.");
            }
            catch (InvalidOperationException /*e*/)
            {
                //Log.Error(e, "Socket committed an invalid operation trying to receive data.");
            }
        }

        /// <summary>
        /// </summary>
        /// <param name="algorithm"></param>
        public void StartCompression(string algorithm)
        {
            _compression = CompressionRegistry.GetCompression(algorithm);
            _compressed = true;
        }
    }
}