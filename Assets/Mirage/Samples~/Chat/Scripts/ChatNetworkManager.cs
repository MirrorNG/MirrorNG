using UnityEngine;

namespace Mirage.Examples.Chat
{
    [AddComponentMenu("")]
    public class ChatNetworkManager : NetworkManager
    {
        public string PlayerName { get; set; }

        public ChatWindow chatWindow;

        public Player playerPrefab;

        void Awake()
        {
            Server.Authenticated.AddListener(OnServerAuthenticated);
            Client.Authenticated.AddListener(OnClientAuthenticated);
        }

        public struct CreateCharacterMessage
        {
            public string name;
        }

        public void OnServerAuthenticated(INetworkPlayer player)
        {
            Server.MessageHandler.RegisterHandler<CreateCharacterMessage>(OnCreatePlayer);
        }

        public void OnClientAuthenticated(INetworkPlayer player)
        {
            // tell the server to create a player with this name
            player.Send(new CreateCharacterMessage { name = PlayerName });
        }

        private void OnCreatePlayer(INetworkPlayer player, CreateCharacterMessage createCharacterMessage)
        {
            // create a gameobject using the name supplied by client
            GameObject playergo = Instantiate(playerPrefab).gameObject;
            playergo.GetComponent<Player>().playerName = createCharacterMessage.name;

            // set it as the player
            ServerObjectManager.AddCharacter(player, playergo);

            chatWindow.gameObject.SetActive(true);
        }
    }
}
