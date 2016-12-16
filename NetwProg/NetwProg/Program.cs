using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NetwProg
{
    class Program
    {
        static public int MijnPoort;

        static public Dictionary<int, Connection> Buren = new Dictionary<int, Connection>();
        static public Dictionary<int, int> distances = new Dictionary<int, int>();
        static public Dictionary<int, int> Du = new Dictionary<int, int>();
        static public Dictionary<int, int> Nbu = new Dictionary<int, int>();
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
            if (v == MijnPoort) { Du[v] = 0; Nbu[v] = MijnPoort; }
            else { Nbu[v] = getBestNToV(v); }
        }
        static int getBestNToV(int v)
        {
            //Alle afstanden in ndisu die naar v gaan
            var matches = ndisu.Where(kvp => secondPortEqualsV(kvp.Key, v));
            //zet het minimum op eerste waarde
            var min = matches.First();
            //verkrijg het keyvaluepair met minimum afstand 
            foreach (KeyValuePair<int[], int> kvp in matches)
            {
                if (kvp.Value < min.Value) min = kvp;
            }
            //return de port van de buur met de laagste afstand tot v 
            return min.Key[0];
        }
        static bool secondPortEqualsV(int[] ndisuKey,int v)
        {
            return (ndisuKey[1] == v);
        }
    }
}
