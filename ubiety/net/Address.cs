// Address.cs
//
//Ubiety XMPP Library Copyright (C) 2006 - 2015 Dieter Lunn, 2010 nickwhaley
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
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Heijden.DNS;
//using Serilog;
using Ubiety.States;
using TransportType = Heijden.DNS.TransportType;

namespace Ubiety.Net
{
    /// <summary>
    ///     Resolves the IM server address from the hostname provided by the XID.
    /// </summary>
    internal class Address
    {
        private readonly Resolver _resolver;
        private int _srvAttempts;
        private bool _srvFailed;
        private List<RecordSRV> _srvRecords;

        public Address()
        {
            _resolver = new Resolver("8.8.8.8") {UseCache = true, TimeOut = 5, TransportType = TransportType.Tcp};
            //Log.Debug("Default DNS Servers: {DnsServers}", _resolver.DnsServer);
            _resolver.OnVerbose += _resolver_OnVerbose;
        }

        /// <summary>
        ///     Is the address IPV6?
        /// </summary>
// ReSharper disable InconsistentNaming
        public bool IPv6 { get; private set; }

// ReSharper restore InconsistentNaming

        public string Hostname { get; private set; }

        private void _resolver_OnVerbose(object sender, Resolver.VerboseEventArgs e)
        {
            //Log.Debug("DNS Resolver Verbose Message: {Message}", e.Message);
        }

        public IPAddress NextIpAddress()
        {
            Hostname = !String.IsNullOrEmpty(ProtocolState.Settings.Hostname)
                ? ProtocolState.Settings.Hostname
                : ProtocolState.Settings.Id.Server;

            if (Hostname == "dieter-pc")
            {
                return IPAddress.Parse("127.0.0.1");
            }
            // here changed
            if (_srvRecords == null || (_srvRecords.Count == 0) /*&& _srvFailed*/)
                _srvRecords = FindSrv();

            if (!_srvFailed && _srvRecords != null)
            {
                if (_srvAttempts < _srvRecords.Count)
                {
                    ProtocolState.Settings.Port = _srvRecords[_srvAttempts].PORT;
                    IPAddress ip = Resolve(_srvRecords[_srvAttempts].TARGET);
                    if (ip == null)
                        _srvAttempts++;
                    else
                        return ip;
                }
            }
            return null;
        }

        private List<RecordSRV> FindSrv()
        {
            Response resp = _resolver.Query("_xmpp-client._tcp." + Hostname, QType.SRV, QClass.IN);

            if (resp.header.ANCOUNT > 0)
            {
                _srvFailed = false;
                return resp.Answers.Select(record => record.RECORD as RecordSRV).ToList();
            }

            _srvFailed = true;
            return null;
        }

        public void srvRecordsReset()
        {
            _srvRecords.Clear();
        }

        private IPAddress Resolve(string hostname)
        {
            Response resp = _resolver.Query(hostname, QType.A, QClass.IN);

            IPv6 = false;
            return ((RecordA) resp.Answers[0].RECORD).Address;

            //while (true)
            //{
            //    var req = new Request();

            //    //req.AddQuestion(Socket.OSSupportsIPv6
            //    //                    ? new Question(UbietySettings.Hostname, DnsType.AAAA, DnsClass.IN)
            //    //                    : new Question(UbietySettings.Hostname, DnsType.ANAME, DnsClass.IN));

            //    req.AddQuestion(new Question(UbietySettings.Hostname, DnsType.ANAME, DnsClass.IN));

            //    var res = Resolver.Lookup(req, DnsAddresses[_dnsAttempts]);

            //    if (res.Answers.Length <= 0) continue;

            //    if (res.Answers[0].Type == DnsType.AAAA)
            //    {
            //        IPv6 = true;
            //        var aa = (AAAARecord)res.Answers[0].Record;
            //        return aa.IPAddress;
            //    }

            //    IPv6 = false;
            //    var a = (ANameRecord)res.Answers[0].Record;
            //    return a.IPAddress;
            //}
        }
    }
}