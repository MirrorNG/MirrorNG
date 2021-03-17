using System.Collections;
using Mirage.Logging;
using UnityEngine;

namespace Mirage.Authenticators
{
    [AddComponentMenu("Network/Authenticators/BasicAuthenticator")]
    public class BasicAuthenticator : NetworkAuthenticator
    {
        static readonly ILogger logger = LogFactory.GetLogger(typeof(BasicAuthenticator));

        // set these in the inspector
        public string Username;
        public string Password;

        public struct AuthRequestMessage
        {
            // use whatever credentials make sense for your game
            // for example, you might want to pass the accessToken if using oauth
            public string AuthUsername;
            public string AuthPassword;
        }

        public struct AuthResponseMessage
        {
            public byte Code;
            public string Message;
        }

        public override void OnServerAuthenticate(INetworkPlayer player)
        {
            // wait for AuthRequestMessage from client
            Server.MessageHandler.RegisterHandler<AuthRequestMessage>(OnAuthRequestMessage);
        }

        public override void OnClientAuthenticate(INetworkPlayer player)
        {
            Server.MessageHandler.RegisterHandler<AuthResponseMessage>(OnAuthResponseMessage);

            var authRequestMessage = new AuthRequestMessage
            {
                AuthUsername = Username,
                AuthPassword = Password
            };

            player.Send(authRequestMessage);
        }

        public void OnAuthRequestMessage(INetworkPlayer player, AuthRequestMessage msg)
        {
            if (logger.LogEnabled()) logger.LogFormat(LogType.Log, "Authentication Request: {0} {1}", msg.AuthUsername, msg.AuthPassword);

            // check the credentials by calling your web server, database table, playfab api, or any method appropriate.
            if (msg.AuthUsername == Username && msg.AuthPassword == Password)
            {
                // create and send msg to client so it knows to proceed
                var authResponseMessage = new AuthResponseMessage
                {
                    Code = 100,
                    Message = "Success"
                };

                player.Send(authResponseMessage);

                // Invoke the event to complete a successful authentication
                base.OnServerAuthenticate(player);
            }
            else
            {
                // create and send msg to client so it knows to disconnect
                var authResponseMessage = new AuthResponseMessage
                {
                    Code = 200,
                    Message = "Invalid Credentials"
                };

                player.Send(authResponseMessage);

                // disconnect the client after 1 second so that response message gets delivered
                StartCoroutine(DelayedDisconnect(player, 1));
            }
        }

        public IEnumerator DelayedDisconnect(INetworkPlayer player, float waitTime)
        {
            yield return new WaitForSeconds(waitTime);
            player.Connection?.Disconnect();
        }

        public void OnAuthResponseMessage(INetworkPlayer player, AuthResponseMessage msg)
        {
            if (msg.Code == 100)
            {
                if (logger.LogEnabled()) logger.LogFormat(LogType.Log, "Authentication Response: {0}", msg.Message);

                // Invoke the event to complete a successful authentication
                base.OnClientAuthenticate(player);
            }
            else
            {
                logger.LogFormat(LogType.Error, "Authentication Response: {0}", msg.Message);
                // disconnect the client
                player.Connection?.Disconnect();
            }
        }
    }
}
