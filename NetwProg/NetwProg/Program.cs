using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NetwProg
{
    class Program
    {
        static public int MijnPoort;

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
            Console.Write("Op welke poort ben ik server? ");
            MijnPoort = int.Parse(Console.ReadLine());
            new Server(MijnPoort);

            Console.WriteLine("Typ [verbind poortnummer] om verbinding te maken, bijvoorbeeld: verbind 1100");
            Console.WriteLine("Typ [poortnummer bericht] om een bericht te sturen, bijvoorbeeld: 1100 hoi hoi");

            while (true)
            {
                string input = Console.ReadLine();
                if (input.StartsWith("verbind"))
                {
                    int poort = int.Parse(input.Split()[1]);
                    if (Buren.ContainsKey(poort))
                        Console.WriteLine("Hier is al verbinding naar!");
                    else
                    {
                        // Leg verbinding aan (als client)
                        Buren.Add(poort, new Connection(poort));
                    }
                }
                else
                {
                    // Stuur berichtje
                    string[] delen = input.Split(new char[] { ' ' }, 2);
                    int poort = int.Parse(delen[0]);
                    if (!Buren.ContainsKey(poort))
                        Console.WriteLine("Hier is al verbinding naar!");
                    else
                        Buren[poort].Write.WriteLine(MijnPoort + ": " + delen[1]);
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
                    SendDValue(v, newDistance);
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
        static void SendDValue(int v, int d)
        {
            foreach (KeyValuePair<int, Connection> neighbour in Buren)
            {
                Buren[neighbour.Key].Write.WriteLine("UD {0} {1} {2}",MijnPoort,v,d);
            }
        }
        //When a new link is made, send all d values 
        static public void SendAllDValues(int port)
        {

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
