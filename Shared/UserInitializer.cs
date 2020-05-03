using DiffieHellman.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DiffieHellman.Shared
{
    public static class UserInitializer
    {
        private static int lastId = 0;
        public static User GetNewUser(string name)
        {
            User user = new User
            {
                UserId = Interlocked.Increment(ref lastId),
                Name = name
            };

            return user;
        }
    }
}
