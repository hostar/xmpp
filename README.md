Ubiety XMPP Library
===================

Ubiety is an extensible XMPP library written in C# to be easy and powerful.

This fork have fixed connecting to secured servers and changed logic of sending and receiving messages.
Logging is currently disabled. To enable it, just uncomment lines starting with "//Log".

Connect to XMPP server
----------------------

```c#
using Ubiety;
using Ubiety.Common;

public class Test {
    public static void Main() {
        Xmpp ubiety = new Xmpp();
        ubiety.Settings.ID = new JID("test@ubiety.ca");
        ubiety.Settings.Password = "test";
        
        // add following line, if you have problems with connecting to server
        ubiety.Settings.AuthenticationTypes = MechanismType.Default;
        
        ubiety.Connect();
    }
}
```

Receive XMPP message
--------------------

OnRawMessage event is triggered on every XMPP incoming message. User needs to process it manually through finite state machine.

```c#
ubiety.OnRawMessage += xmpp_OnRawMessage;
...
void xmpp_OnRawMessage(object sender, Ubiety.Infrastructure.StringEventArgs e)
{
    MessageBox.Show(e.content);
}
```

Send XMPP message
-----------------

Messages are sent like strings.

```c#
ubiety.SendString("<message><subject>Hello</subject><body>World!</body></message>");
```

Support
-------

* Forum: ~~<http://discourse.dieterlunn.ca>~~
