using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using System.IO;
using System.Text;
using System.Diagnostics;

namespace simulator
{

    static class Program
    {
        [STAThread]
        static void Main(String[] args)
        {
            if (args.Length != 4 && args.Length !=5)
            {
                Console.WriteLine("Ant simulator (from ICFPC 2004)");
                Console.WriteLine("Usage:");
                Console.WriteLine("       simulator.exe <world> <ant1> <ant2> <randseed> [-nogui]");
                return;
            }

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            Map m = new Map(args[0]);

            Automaton redProgram = new Automaton(args[1]);
            Automaton blackProgram = new Automaton(args[2]);

            int seed = Int32.Parse(args[3]);

            bool noGui = args.Contains("-nogui");

            Simulator sim = new Simulator(m,redProgram,blackProgram,seed);

            /*
            // make dump
            FileStream fs = new FileStream(@"..\..\data\dump", FileMode.Create);
            StreamWriter wr = new StreamWriter(fs);

            wr.WriteLine("random seed: {0}",seed);
            for (int i = 0; i <= 1000; i++)
            {
                sim.dump(wr);
                sim.step();
            }
            wr.Close();
            */

            if (noGui) 
            {
                DateTime start = DateTime.Now;
                for (int i = 0; i < 100000; i++)
                    sim.step();
                sim.printScore();
                Console.WriteLine(DateTime.Now - start);
            }
            else
                Application.Run(new Form1(String.Join(" ", args), sim));
        }
    }
}
