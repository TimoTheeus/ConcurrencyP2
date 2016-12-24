using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Security.Cryptography;
namespace NetwProg
{
    //THIS CODE IS USED SO THAT THE CONTAINSKEY METHOD OF THE NDISU DICTIONARY WORKS CORRECTLY. I DID NOT WRITE THIS CODE MYSELF, EVEN THO THE Equals(..) METHOD
    //SPEAKS FOR ITSELF.
    //IT IS SOLELY USED FOR THE NDISU DICTIONARY SO THAT IT CAN COMPARE ITS KEYS
    //THE SOURCE: http://stackoverflow.com/questions/14663168/an-integer-array-as-a-key-for-dictionary
    public class MyEqualityComparer : IEqualityComparer<int[]>
    {
        public bool Equals(int[] x, int[] y)
        {
            if (x.Length != y.Length)
            {
                return false;
            }
            for (int i = 0; i < x.Length; i++)
            {
                if (x[i] != y[i])
                {
                    return false;
                }
            }
            return true;
        }

        public int GetHashCode(int[] obj)
        {
            int result = 17;
            for (int i = 0; i < obj.Length; i++)
            {
                unchecked
                {
                    result = result * 23 + obj[i];
                }
            }
            return result;
        }
    }
    class Program
    {
        static public int MijnPoort;
        static public object locker = new object();

        //Neighbours <port,connection>
        static public Dictionary<int, Connection> Buren = new Dictionary<int, Connection>();
        //Estimated Distances <port,distance>
        static public Dictionary<int, int> D = new Dictionary<int, int>();
        //Preferred neighbours <v,neighbourport>
        static public Dictionary<int, int> Nbu = new Dictionary<int, int>();
        //Neighbour distances to a port <[neighbourport,v],distance>
        static public Dictionary<int[], int> ndisu = new Dictionary<int[], int>(new MyEqualityComparer());

        static void Main(string[] args)
        {
            for (int i = 0; i < args.Length; i++)
            {
          //      Console.WriteLine(args[i]);
            }
            //Zet het netwerk op
            MijnPoort = int.Parse(args[0]);
            //Initialiseer voor eigen port
            setDValue(MijnPoort, 0);
            setNbuValue(MijnPoort, MijnPoort);
            new Server(MijnPoort);

            //Maak verbinding
            for (int i = 1; i < args.Length; i++)
            {
                int port = int.Parse(args[i]);
                // Wordt client van ports met hoger nummer
                if (port > MijnPoort)
                {
                    MakeConnection(port);
                }
            }
            
            // Handle the input
            while (true)
            {
                string input = Console.ReadLine();
                string[] splitInput = input.Split(' ');
                switch (splitInput[0])
                {
                    // Print the routing table
                    case "R":
                        printRoutingTable();
                        break;

                    // Send a message
                    case "B":
                        //Split the string to get rid of the B letter
                        string[] temp = new string[splitInput.Length - 1];
                        for (int i = 0; i < temp.Length; i++) temp[i] = splitInput[i + 1];
                        string message = string.Join(" ", temp);
                        sendMessage(message);
                        break;

                    // Make a port a direct neighbour
                    case "C":
                        MakeConnection(int.Parse(splitInput[1])); 
                        break;

                    // Cut the connection with a port
                    case "D":
                        cutConnection(splitInput[1]);
                        break;
                    default:
                        break;
                }
            }
        }
        
        static public void Recompute(int v)
        {
            if (v == MijnPoort) { setDValue(v, 0); }
            else
            {
                //neighbour with lowest ndisu [w,v]
                var nbuPair = getBestToV(v);
                //Als er netwerk partitie op treed
                if (nbuPair.Value + 1 >= 25)
                {
                    Console.WriteLine("Onbereikbaar: {0}", v);
                    removeConnection(v);
                    return;
                }
                //Verander Nbu en Distance 
                lock (locker)
                {
                    setNbuValue(v, nbuPair.Key[0]);
                    setDValue(v, nbuPair.Value+1);
                }
                Console.WriteLine("afstand naar {0} is nu {1} via {2}", v, nbuPair.Value + 1, nbuPair.Key[0]);
            }
        }

        //Set Nbu value
        static void setNbuValue(int v, int preferredNeighbour)
        {
            if (Nbu.ContainsKey(v))
                Nbu[v] = preferredNeighbour;
            else Nbu.Add(v, preferredNeighbour);
        }

        //Set distance value
        static public void setDValue(int v, int newDistance)
        {
            if (!D.ContainsKey(v))
            {
                D.Add(v, newDistance);
                SendDValueToNeighbours(v, newDistance);
            }
            else
            {
                int oldDistance = D[v];
                // Set the estimated distance to V, to neighbour distance + 1 
                // If the distance changed, send a <mydist,V,D> to all neighbours so they can update their ndis 
                if (newDistance != oldDistance)
                {
                    D[v] = newDistance;
                    SendDValueToNeighbours(v, newDistance);
                }
            }
        }

        // Return a keyvaluepair <[w,v],distance> of the neighbour with the best ndisu to v 
        static KeyValuePair<int[],int> getBestToV(int v)
        {
            // All pairs in ndisu with v as the 2nd value in their key
            var pairs = ndisu.Where(kvp => secondPortEqualsV(kvp.Key, v));
            // Set a default for minimum
            var min = pairs.First();
            foreach (KeyValuePair<int[], int> kvp in pairs)
            {
                // If the distance is smaller, that keyvaluepair becomes the new minimum
                if (kvp.Value < min.Value) min = kvp;
            }
            // Return the keyvaluepair with the lowest distance to v 
            return min;
        }

        // Check if the 2nd value of the key equals v
        static bool secondPortEqualsV(int[] ndisuKey,int v)
        {
            return (ndisuKey[1] == v);
        }
        // Check if the 2nd value of the key equals v
        static bool firstPortEqualsV(int[] ndisuKey, int v)
        {
            return (ndisuKey[0] == v);
        }

        // After a change of distance value to V, send <mydist,V,D> to all connected ports so they can update their ndisu value for this port
        static public void SendDValueToNeighbours(int v, int d)
        {
            foreach (KeyValuePair<int, Connection> neighbour in Buren)
            {
                    //Console.WriteLine("{0} sent {1} to {2}", MijnPoort, v, neighbour.Key);
                    sendUD(neighbour.Key, v, d);
            }
        }

        // Send all distance values to a port, used when new link is made 
        static public void SendAllDValues(int port)
        {
            //Console.WriteLine("sent all {0} D values to {1}",D.Count,port);
            foreach (KeyValuePair<int,int> distance in D)
            {
                    sendUD(port, distance.Key, distance.Value);
            }
            // Inform the neighbours of the new connection
            SendDValueToNeighbours(port, 1);
        }

        // Send an update distance to a port
        static void sendUD(int port,int v, int d)
        {
                Buren[port].Write.WriteLine("UD {0} {1} {2}", MijnPoort, v, d);
        }

        // Make a connection to a port
        static void MakeConnection(int port)
        {
            lock (locker)
            {
                // Check if the port is already connected
                if (Buren.ContainsKey(port))
                {
                    Console.WriteLine("Hier is al verbinding naar!");
                }
                // If not add the port to the neighbours

                else
                {
                    Buren.Add(port, new Connection(port));
                }
                // Lock the locker-object and initialize the distances

                initialiseDistance(port);
                SendAllDValues(port);

                Console.WriteLine("Verbonden: {0}", port);
            }
        }

        //Bij een net nieuw verbinding, zet de distance naar 1 en ndis van port naar zichzelf naar 0 
        static public void initialiseDistance(int port)
        {
            updateNdis(port, port, 0);
            setDValue(port, 1);
        }
        //Update ndisu dictionary
        static public void updateNdis(int neighbourport, int v, int distance)
        {
            
            //Make the key that belongs to the distance value of the neighbour to v and update it.
            int[] key = new int[2];
            key[0] = neighbourport; key[1] = v;
            //  int oldDistance = ndisu[key];
            lock (locker)
            {
                if (!ndisu.ContainsKey(key))
                {
                    ndisu.Add(key, distance);
                    Recompute(v);
                }
                //If the ndis is changed, do a recompute
                else if (ndisu[key] != distance)
                {
                    ndisu[key] = distance;
                    Recompute(v);
                }
            }
        }

        // R - case
        static void printRoutingTable()
        {
            // Make a list to sort it by port number
            var list = Nbu.Keys.ToList();
            list.Sort();

            foreach (var key in list)
            {
                // If the distance is 0, it isn't a preferred neighbour, but a local host
                // Print the port number distance(0) and localhost
                if (D[key] == 0) Console.WriteLine("{0} {1} local", key, D[key]);
                else
                {
                    // Find the preferred neighbour of the key
                    int neighbour = Nbu[key];
                    // Print the portnumber, distance and preferred neighbour
                    Console.WriteLine("{0} {1} {2}", key, D[key], neighbour);
                }
            }
        }

        // B - case
        static public void sendMessage(string message)
        {
            // Split the message for the port number
            string[] splitMessage = message.Split(' ');
            try
            {
                // Try to parse the string to an int
                int portnr = int.Parse(splitMessage[0]);
                
                // Check the preferred neighbour of that port
                int prefneighbour = Nbu[portnr];
                // Send the preferred neighbour a message
                Buren[prefneighbour].Write.WriteLine(message);

                Console.WriteLine("Bericht voor {0} doorgestuurd naar {1}", portnr, prefneighbour);


            }
            catch { Console.WriteLine("Poort {0} is niet bekend", splitMessage[0]); }
        }


        // D - case
        static void cutConnection(string inputPort)
        {
            try
            {
                int portnr = int.Parse(inputPort);
                Buren[portnr].Write.WriteLine("DISCONNECT {0}", MijnPoort);
                removeConnection(portnr);
                passOnAndRecomputeAll(portnr);
                Console.WriteLine("Verbroken: {0}", portnr);
            }
            catch { Console.WriteLine("Poort {0} is niet bekend", inputPort); }
        }
        //Remove all traces of the connection
        static public void removeConnection(int portnr)
        {
            Buren.Remove(portnr);
            Nbu.Remove(portnr);
            //remove all occurences of 1102 from ndisu
            removeFromNdisu(portnr);
        }
        //Send a high d value to neighbours, and do a recompute for all ports in Nbu 
        static public void passOnAndRecomputeAll(int portnr)
        {
            SendDValueToNeighbours(portnr, 20);
            Recompute(portnr);
            RecomputeAll();
        }
        //Remove all values from ndisu where a given port is the first element of the key 
        static public void removeFromNdisu(int port)
        {
            List<int[]> temp = new List<int[]>();
            foreach (KeyValuePair<int[], int> kvp in ndisu)
            {
                if (firstPortEqualsV(kvp.Key, port))
                {
                    temp.Add(kvp.Key);
                }
            }
            foreach (int[] key in temp)
            {
                ndisu.Remove(key);
            }
        }

        //Recompute all keys in Nbu
        static public void RecomputeAll()
        {
            List<int> temp = new List<int>();
            foreach (KeyValuePair<int, int> kvp in Nbu)
            {
                temp.Add(kvp.Key);
            }
            foreach (int k in temp)
                Recompute(k);
        }
    }
}
