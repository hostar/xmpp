// StartTLSState.cs
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

using Ubiety.Common;
using Ubiety.Registries;
using Ubiety.Core;

namespace Ubiety.States
{
	/// <summary>
	/// 
	/// </summary>
	public class StartTLSState : State
	{
		/// <summary>
		/// 
		/// </summary>
		/// <param name="data">
		/// A <see cref="System.Object"/>
		/// </param>
		public override void Execute(Tag data = null)
		{
			if (data != null && data.LocalName != "proceed") return;

            // here changed
            //string starttls = "<starttls xmlns=\"urn:ietf:params:xml:ns:xmpp-tls\"/>";
            var starttls = TagRegistry.GetTag<StartTls>("starttls", Namespaces.StartTls);
            ProtocolState.Socket.Write(starttls);

            ProtocolState.State = new ProceedState();
            //ProtocolState.State.Execute();

			/*ProtocolState.Socket.StartSecure();
			ProtocolState.State = new ConnectedState();
			ProtocolState.State.Execute();*/
		}
	}
}