using DiffieHellman.Client.Data;
using DiffieHellman.Shared;
using Microsoft.AspNetCore.SignalR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DiffieHellman.Server.Hubs
{
    public class ChatHub : Hub
    {
        private static readonly List<User> userLookup = new List<User>();
        
        public async Task SendMessage(string username, string message)
        {
            await Clients.All.SendAsync(Messages.Receive, username, message);
        }

        public async Task SendPublicKey(double key)
        {
            var connectionId = Context.ConnectionId;

            int index = 0;
            for(int i = 0; i < userLookup.Count - 1; i++)
            {
                if(userLookup[i].ConnectionId == connectionId)
                {
                    index = i++;
                    break;
                }
            }

            User nextUser = userLookup[index];
                
            await Clients.Client(nextUser.ConnectionId).SendAsync("CalculateChatPrivateKey", key);
        }


        public async Task Register(User user)
        {
            var connectionId = Context.ConnectionId;
            user.ConnectionId = connectionId;
            
            if(!userLookup.Contains(user))
            {
                userLookup.Add(user);
                await Clients.AllExcept(connectionId).SendAsync("OnJoinedNewUser", user, $"{user.Name} joined the chat.");
            }
        }
        public override Task OnConnectedAsync()
        {
            Console.WriteLine("Connected");
            return base.OnConnectedAsync();
        }
        public override async Task OnDisconnectedAsync(Exception exception)
        {
            Console.WriteLine($"Disconnected {exception?.Message} {Context.ConnectionId}");
            string id = Context.ConnectionId;
            User user;
            for (int i = 0; i < userLookup.Count; i++)
            {
                if (userLookup[i].ConnectionId == id)
                {
                    user = userLookup[i];
                    await Clients.AllExcept(id).SendAsync(Messages.Receive, user, $"{user.Name} has left the chat");
                    userLookup.Remove(user);
                    break;
                }
            }
           
            await base.OnDisconnectedAsync(exception);
        }
    }
}
