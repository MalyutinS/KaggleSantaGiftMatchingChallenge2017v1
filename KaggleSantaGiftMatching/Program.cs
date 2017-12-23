using System;
using System.Collections.Generic;
using ILOG.Concert;
using ILOG.CPLEX;
using System.IO;

namespace KaggleSantaGiftMatching
{
    class Program
    {


        public static double MinFlow = -101 / 2000000.0; // 

        static void Main()
        {

            var cm = new int[1000000, 10];
            using (var reader = new StreamReader("child_wishlist.csv"))
            {
                var childId = 0;
                while (!reader.EndOfStream)
                {
                    var line = reader.ReadLine();
                    var values = line.Split(',');

                    for (byte i = 1; i <= 10; i++)
                    {
                        cm[childId, i - 1] = int.Parse(values[i]);
                    }
                    childId++;
                }
            }
            Console.WriteLine("{0}: child_wishlist.csv - OK", DateTime.Now);



            var gm = new int[1000, 1000];
            using (var reader = new StreamReader("gift_goodkids.csv"))
            {
                var giftId = 0;
                while (!reader.EndOfStream)
                {
                    var line = reader.ReadLine();
                    var values = line.Split(',');

                    for (short i = 1; i <= 1000; i++)
                    {
                        gm[giftId, i - 1] = int.Parse(values[i]);
                    }
                    giftId++;
                }
            }
            Console.WriteLine("{0}: gift_goodkids.csv - OK", DateTime.Now);



            var edgeMap = new Dictionary<int, double>();

            for (int i = 0; i < 1000000; i++)
            {
                for (int k = 0; k < 10; k++)
                {
                    var j = cm[i, k];
                    var edge1 = i*1000 + j;
                    edgeMap[edge1] = (200 * (10 - k) - 1) / 2000000.0; // !!!!!!!!!!!
                }
            }

            for (int j = 0; j < 1000; j++)
            {
                for (int k = 0; k < 1000; k++)
                {
                    var i = gm[j, k];
                    var edge1 = i * 1000 + j;
                    if (edgeMap.ContainsKey(edge1))
                    {
                        edgeMap[edge1] += (2 * (1000 - k) + 1) / 2000000.0; // !!!!!!!!!!!!!!!!
                    }
                    else
                    {
                        edgeMap[edge1] = (2 * (1000 - k) - 100) / 2000000.0; //!!!!!!!!!!!!!!!!
                    }
                }
            }

            for (int i = 0; i < 4000; i++)
            {
                for (int j = 0; j < 1000; j++)
                {
                    var edge = i * 1000 + j;
                    if (!edgeMap.ContainsKey(edge))
                    {
                        edgeMap[edge] = MinFlow; // !!!!!!!!!!!!!!
                    }
                }
            }

            var totalArcs = edgeMap.Count;

            Console.WriteLine("{1}: edgeMap - ok ({0})", totalArcs, DateTime.Now);

            // to define objective
            var costs = new double[totalArcs];
            // to define constraints
            var edge2Ind = new Dictionary<int, int>(totalArcs);
            var i2J = new List<List<short>>(10000000);
            var j2I = new List<List<int>>(1000);
            // to restore solution from CPLEX output
            var ind2Edge = new int[totalArcs];

            for (int i=0; i < 1000000; i++)
            {
                i2J.Add(new List<short>());
            }
            for (int j = 0; j < 1000; j++)
            {
                j2I.Add(new List<int>());
            }

            int t = 0;
            foreach (var edge in edgeMap)
            {
                int i = edge.Key / 1000;
                short j = (short)(edge.Key % 1000);

                costs[t] = edge.Value;
                edge2Ind[edge.Key] = t;
                i2J[i].Add(j);
                j2I[j].Add(i);
                ind2Edge[t] = edge.Key;

                t++;
            }
            edgeMap = null;

            Console.WriteLine("{1}: cost - ok ({0})", costs.Length, DateTime.Now);
            Console.WriteLine("{1}: edge2ind - ok ({0})", edge2Ind.Count, DateTime.Now);
            Console.WriteLine("{1}: i2j - ok ({0})", i2J.Count, DateTime.Now);
            Console.WriteLine("{1}: j2i - ok ({0})", j2I.Count, DateTime.Now);
            Console.WriteLine("{1}: ind2edge - ok ({0})", ind2Edge.Length, DateTime.Now);
            

            Console.WriteLine("{0}: start cplex", DateTime.Now);

            Cplex cplex = null;
            try
            {
                cplex = new Cplex();
                // 2h time limit in seconds
                cplex.SetParam(Cplex.DoubleParam.TiLim, 2 * 60 * 60); 
                // mip tolerances (absolute and relative) that I set to 0. 
                // If I don't set them, then I get a solution 0.00000071 worse
                cplex.SetParam(Cplex.Param.MIP.Tolerances.MIPGap, 0); 
                cplex.SetParam(Cplex.Param.MIP.Tolerances.AbsMIPGap, 0); 

                INumVar[][] var = new INumVar[1][];
                IRange[][] rng = new IRange[3][];

                var[0] = cplex.BoolVarArray(totalArcs + 1000000 - 4000);
                //var[1] = cplex.IntVarArray(1000, 0, 1000);
                //var[2] = cplex.IntVarArray(1, 0, 1000000 - 4000);

                DescribeModel(cplex, costs, var, rng, edge2Ind, i2J, j2I);
                

                Console.WriteLine("{0}: start solve", DateTime.Now);

                if (cplex.Solve())
                {
                    Console.WriteLine("{0}: Solved !", DateTime.Now);
                    Console.WriteLine("Solution status = " + cplex.GetStatus());
                    Console.WriteLine("Solution value  = " + cplex.ObjValue);
                }
                else
                {
                    Console.WriteLine("Solution status = " + cplex.GetStatus());
                    Console.WriteLine("Solution value  = " + cplex.ObjValue);
                }

                double[] outX = cplex.GetValues(var[0]);

                var child2Gift = new short[1000000];
                var giftCount = new int[1000];
                for (int i = 0; i < 1000000; i++)
                {
                    giftCount[i] = -1;
                }
                for (int j = 0; j < 1000; j++)
                {
                    giftCount[j] = 1000;
                }

                var total = 0;
                for (int k = 0; k < totalArcs; k++)
                {
                    if (outX[k] > 0.9 && outX[k] < 1.1)
                    {
                        var edge = ind2Edge[k];
                        int i = edge / 1000;
                        short j = (short)(edge % 1000);
                        child2Gift[i] = j;
                        giftCount[j]--;
                        total++;
                    }
                }
                Console.WriteLine("{1}: Total number of childs with gifts after MIP: {0}", total, DateTime.Now);


                for (int i = 0; i < 1000000; i++)
                {
                    if (child2Gift[i] == -1)
                    {
                        Console.WriteLine("no gift for {0}", i);
                        if (i < 4000)
                        {
                            Console.WriteLine("shouldn't happen {0}", i);
                        }
                        else
                        {
                            //find first available gift
                            for (short j = 0; j < 1000; j++)
                            {
                                if (giftCount[j] > 0)
                                {
                                    Console.WriteLine("gift {0} assigned to {1}", j, i);
                                    child2Gift[i] = j;
                                    giftCount[j]--;
                                    break;
                                }
                            }
                        }
                    }
                }

                

                Console.WriteLine("{0}: write solution to csv", DateTime.Now);
                

                using (var w = new StreamWriter("my.csv"))
                {
                    w.WriteLine("ChildId,GiftId");
                    w.Flush();
                    for (int i = 0; i < 1000000; i++)
                    {
                        if (child2Gift[i] >= 0)
                        {
                            var line = string.Format("{0},{1}", i, child2Gift[i]);
                            w.WriteLine(line);
                            w.Flush();
                        }
                        else
                        {
                            Console.WriteLine("Error: child {0} has no gift", i);
                        }

                    }
                }

            }
            catch (ILOG.Concert.Exception e)
            {
                Console.WriteLine("Concert exception caught '" + e + "' caught");

            }
            finally
            {
                if (cplex != null)
                {
                    cplex.End();
                }
            }
        }


        

        
        public static void DescribeModel(Cplex cplex,
            double[] costs,
            INumVar[][] var,
            IRange[][] rng,
            Dictionary<int, int> edge2Ind,
            List< List<short>> i2J,
            List< List<int>> j2I)
        {
            IIntVar[] x = (IIntVar[])var[0];

            int totalArcs = costs.Length;

            // objective
            INumExpr[] prodsObj = new INumExpr[costs.Length + 1000000 - 4000];
            for (int i = 0; i < totalArcs; i++)
            {
                prodsObj[i] = cplex.Prod(x[i], costs[i]);
            }
            for (int i = totalArcs; i < totalArcs + 1000000 - 4000; i++)
            {
                prodsObj[i] = cplex.Prod(x[i], MinFlow); 
            }
            cplex.AddMaximize(cplex.Sum(prodsObj));
            Console.WriteLine("{0}: cplex Objective - ok", DateTime.Now);
            costs = null;
            


            // flow to childs
            rng[0] = new IRange[1000000];
            for (int i = 0; i < 1000000; i++)
            {
                var prods = new List<INumExpr>();
                foreach (var j in i2J[i])
                {
                    prods.Add(x[edge2Ind[i*1000+j]]);
                }
                if (i >= 4000)
                {
                    prods.Add(x[totalArcs + i - 4000]);
                }
                //prods.Add(x[edgeMap.Count + i]);

                rng[0][i] = cplex.AddEq(cplex.Sum(prods.ToArray()), 1);
            }
            i2J = null;
            Console.WriteLine("{0}: constraint to ensure each child has a gift - OK", DateTime.Now);



            // flow from gifts
            rng[1] = new IRange[1000];
            for (int j = 0; j < 1000; j++)
            {
                var prods = new List<INumExpr>();
                foreach (var i in j2I[j])
                {
                    prods.Add(x[edge2Ind[i * 1000 + j]]);
                }
                //prods.Add(cplex.Prod(y[j], -1));
                //rng[1][j] = cplex.AddEq(cplex.Sum(prods.ToArray()), 0);
                rng[1][j] = cplex.AddLe(cplex.Sum(prods.ToArray()), 1000);
            }
            j2I = null;
            Console.WriteLine("{0}: constraint to ensure we do not use more than 1000 of each gift - OK", DateTime.Now);
            

            // twins
            rng[2] = new IRange[2000*1000];
            int r = 0;
            foreach(var edge1 in edge2Ind)
            {
                int i = edge1.Key / 1000;
                if (i < 4000 && i % 2 == 0)
                {
                    short j = (short)(edge1.Key % 1000);
                    var m = edge2Ind[(i + 1) * 1000 + j];
                    rng[2][r] = cplex.AddEq(cplex.Sum(x[edge1.Value], cplex.Prod(x[m], -1)), 0);
                    r++;
                }
            }
            edge2Ind = null;
            Console.WriteLine("{0}: twins constraint", DateTime.Now);



            // export
            //cplex.ExportModel("milp-workers.lp");
        }
    }
}