using DiffieHellman.Shared;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiffieHellman.Client.Data
{
    public class ChatClient : IAsyncDisposable
    {
        public static int G = 5;
        public static int P = 23;

        public const string HubUrl = "/chatHub";
        private readonly NavigationManager _navigationManager;
        private HubConnection _hubConnection;
        private readonly string _username;
        private bool _started = false;

        private int myKey;
        private static double privateChatKey;
        private string key = RawTextToSHAkey(privateChatKey.ToString());                  //в чем смысл приватного статического поля?
        public ChatClient(string username, NavigationManager navigationManager)
        {
            _navigationManager = navigationManager;
            _username = username;
        }

        public async Task StartAsync()
        {
            if (!_started)
                _hubConnection = new HubConnectionBuilder().WithUrl(_navigationManager.ToAbsoluteUri(HubUrl)).Build();

            _hubConnection.On(Messages.Receive, (string user, string message) =>
            {
                ReceiveMessage(user, message);
            });
            _hubConnection.On("CalculateChatPrivateKey", (double key) =>
            {
                CalculatePrivateChatKey(key);
            });
            _hubConnection.On("OnJoinedNewUser", (User user, string notification) =>
            {
                DeffieHellman();
            });

            await _hubConnection.StartAsync();
            _started = true;
            User user = UserInitializer.GetNewUser(_username);
            await _hubConnection.SendAsync(Messages.Register, user);

            DeffieHellman();
        }

        private async void DeffieHellman()
        {
            GeneratePrivateKey(100);
            var PK = CalculatePublicKey();
            await SendPublicKey(PK);
        }

        private void ReceiveMessage(string username, string text)
        {
            var message = new string(Formatting.binaryToText(Cryptography.decryptDES(key, text)).ToArray());
            MessageReceived?.Invoke(this, new MessageReceivedEventArgs(username, message));  //delegate and events
        }

        public event MessageReceivedEventHandler MessageReceived;
        public delegate void MessageReceivedEventHandler(object sender, MessageReceivedEventArgs e);

        public async Task SendAsync(string message)
        {
            if (!_started)
                throw new InvalidOperationException("Client is not started");

            var binmessage = Formatting.textToBinary(message);
            await _hubConnection.SendAsync(Messages.Send, _username, Cryptography.encryptDES(key, binmessage));
        }

        private static string RawTextToSHAkey(string text)
        {
            return String.Concat(System.Security.Cryptography.SHA256.Create().ComputeHash(Encoding.Unicode.GetBytes(text)).Take(64).Select(i => Convert.ToString(i, 2)));
        }


        private void GeneratePrivateKey(int max, int min = 1)
        {
            Random random = new Random();
            myKey = random.Next(min, max);
        }



        private double CalculatePublicKey()
        {
            double result = Math.Pow(G, myKey) % P;
            return result;
        }
        public async Task SendPublicKey(double myPublicKey)
        {
            await _hubConnection.InvokeAsync("SendPublicKey", myPublicKey);
        }

        private void CalculatePrivateChatKey(double publicKeyPreviousId)
        {
            privateChatKey = Math.Pow(publicKeyPreviousId, myKey) % P;
        }





        public async Task StopAsync()
        {
            if (_started)
            {
                await _hubConnection.StopAsync();
                await _hubConnection.DisposeAsync();
                _hubConnection = null;
                _started = false;
            }

        }

        public async ValueTask DisposeAsync()
        {
            await StopAsync();
        }
        
    }
    public class MessageReceivedEventArgs : EventArgs
    {
        public string Username { get; set; }
        public string Message { get; set; }

        public MessageReceivedEventArgs(string username, string message)
        {
            Username = username;
            Message = message;
        }
    }
}
