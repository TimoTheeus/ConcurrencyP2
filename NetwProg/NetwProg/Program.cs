using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Security.Cryptography;
namespace NetwProg
{
    class Program
    {
        static public int MijnPoort;
        static public object locker = new object();
        //Neighbours <port,connection>
        static public Dictionary<int, Connection> Buren = new Dictionary<int, Connection>();
        //distances <port,distance>
        static public Dictionary<int, int> distances = new Dictionary<int, int>();
        //Estimated Distances <port,distance>
        static public Dictionary<int, int> D = new Dictionary<int, int>();
        //Preferred neighbours <v,neighbourport>
        static public Dictionary<int, int> Nbu = new Dictionary<int, int>();
        //Neighbour distances to a port <[neighbourport,v],distance>
        static public Dictionary<int[], int> ndisu = new Dictionary<int[], int>();

        static void Main(string[] args)
        {
            for (int i = 0; i < args.Length; i++)
            {
                Console.WriteLine(args[i]);
            }
            //Zet het netwerk op
            MijnPoort = int.Parse(args[0]);
            new Server(MijnPoort);
            for (int i = 1; i < args.Length; i++)
            {
                int port = int.Parse(args[i]);
                MakeConnection(port);
            }
            //Handel invoer af
            while (true)
            {
                string input = Console.ReadLine();
                string[] splitInput = input.Split(' ');
                switch (splitInput[0])
                {
                    case "R":
                        break;
                    case "B":
                        break;
                    case "C":
                        break;
                    case "D":
                        break;
                    default:
                        break;
                }
            }
        }
        static void Recompute(int v)
        {
            if (v == MijnPoort) { D[v] = 0; Nbu[v] = MijnPoort; }
            else
            {
                var nbuPair = getBestToV(v);
                //neighbour with lowest ndisu [w,v]
                Nbu[v] = nbuPair.Key[0];
                int oldDistance = D[v];
                //Set the estimated distance to V, to neighbour distance + 1 
                int newDistance = nbuPair.Value + 1;
                //If the distance changed, send a <mydist,V,D> to all neighbours so they can update their ndis 
                if (newDistance != oldDistance)
                {
                    D[v] = newDistance;
                    SendDValueToNeighbours(v, newDistance);
                }
            }
        }
        //Return a keyvaluepair <[w,v],distance> of the neighbour with the best ndisu to v 
        static KeyValuePair<int[],int> getBestToV(int v)
        {
            //all pairs in ndisu with v as the 2nd value in their key
            var pairs = ndisu.Where(kvp => secondPortEqualsV(kvp.Key, v));
            //set a default for minimum
            var min = pairs.First();
            foreach (KeyValuePair<int[], int> kvp in pairs)
            {
                //if the distance is smaller, that keyvaluepair becomes the new minimum
                if (kvp.Value < min.Value) min = kvp;
            }
            //return the keyvaluepair with the lowest distance to v 
            return min;
        }

        //Check if the 2nd value of the key equals v
        static bool secondPortEqualsV(int[] ndisuKey,int v)
        {
            return (ndisuKey[1] == v);
        }
        //After a change of distance value to V, send <mydist,V,D> to all connected ports so they can update their ndisu value for this port
        static void SendDValueToNeighbours(int v, int d)
        {
            foreach (KeyValuePair<int, Connection> neighbour in Buren)
            {
                sendUD(neighbour.Key, v, d);
            }
        }
        //Send all distance values to a port, used when new link is made 
        static public void SendAllDValues(int port)
        {
            foreach(KeyValuePair<int,int> distance in D)
            {
                sendUD(port, distance.Key, distance.Value);
            }
        }
        //Send an update distance to a port
        static void sendUD(int port,int v, int d)
        {
            Buren[port].Write.WriteLine("UD {0} {1} {2}", MijnPoort, v, d);
        }
        static void MakeConnection (int port)
        {
            lock (locker)
            {
                if (Buren.ContainsKey(port))
                {
                    Console.WriteLine("Hier is al verbinding naar!");
                }
                else
                {
                    Buren.Add(port, new Connection(port));
                }
            }
            initialiseDistance(port);
            SendAllDValues(port);
        }
        static public void initialiseDistance(int port)
        {
            if (D.ContainsKey(port))
            {
                D[port] = 1;
            }
            else { D.Add(port, 1); }
        }
        static public void updateNdis(int neighbourport, int v, int distance)
        {
            //Make the key that belongs to the distance value of the neighbour to v and update it.
            int[] key = new int[2];
            key[0] = neighbourport; key[1] = v;
            int oldDistance = ndisu[key];
            //If the ndis is changed, do a recompute
            if (oldDistance != distance)
            {
                ndisu[key] = distance;
                Recompute(v);
            }
        }
    }
}
