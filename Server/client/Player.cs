using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Server.client
{
    public class Player
    {
        public int id;
        public string username;

        public Vector3 position;
        public Quaternion rotation;

        public Player(int id, string username, Vector3 spawnPosition)
        {
            this.id = id;
            this.username = username;
            this.position = spawnPosition;
            rotation = Quaternion.Identity;
        }
    }
}
