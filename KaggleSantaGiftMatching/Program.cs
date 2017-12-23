using System;
using System.Collections.Generic;
using ILOG.Concert;
using ILOG.CPLEX;
using System.IO;

namespace KaggleSantaGiftMatching
{
    class Program
    {


        public static double MIN_FLOW = -101 / 2000000.0; // 2.0505

        static void Main(string[] args)
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

            var edgeMap = new Dictionary<Tuple<int, int>, double>();


            for (int i = 0; i < 1000000; i++)
            {
                for (int k = 0; k < 10; k++)
                {
                    var j = cm[i, k];
                    var edge1 = new Tuple<int, int>(j, i);
                    edgeMap[edge1] = (200 * (10 - k) - 1) / 2000000.0; /// !!!!!!!!!!!
                }
            }

            for (int j = 0; j < 1000; j++)
            {
                for (int k = 0; k < 1000; k++)
                {
                    var i = gm[j, k];
                    var edge1 = new Tuple<int, int>(j, i);
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
                    var edge = new Tuple<int, int>(j, i);
                    if (!edgeMap.ContainsKey(edge))
                    {
                        edgeMap[edge] = MIN_FLOW; /// !!!!!!!!!!!!!!
                    }
                }
            }




            Console.WriteLine("{1}: edgeMap - OK ({0})", edgeMap.Count, DateTime.Now);

            // new dict helper
            var edge2ind = new Dictionary<Tuple<int, int>, int>();
            var ind2edge = new Dictionary<int, Tuple<int, int>>();
            var i2j = new Dictionary<int, List<int>>();
            var j2i = new Dictionary<int, List<int>>();

            int t = 0;
            foreach (var edge in edgeMap)
            {
                edge2ind[edge.Key] = t;
                ind2edge[t] = edge.Key;
                if (!i2j.ContainsKey(edge.Key.Item2))
                {
                    i2j[edge.Key.Item2] = new List<int>();
                }
                i2j[edge.Key.Item2].Add(edge.Key.Item1);
                if (!j2i.ContainsKey(edge.Key.Item1))
                {
                    j2i[edge.Key.Item1] = new List<int>();
                }
                j2i[edge.Key.Item1].Add(edge.Key.Item2);
                t++;
            }

            Console.WriteLine("{1}: edge2ind - ok ({0})", edge2ind.Count, DateTime.Now);
            Console.WriteLine("i2jCost - ok ({0})", i2j.Count);
            Console.WriteLine("j2iCost - ok ({0})", j2i.Count);

            // total arcs edgeMap.Count + 1000000 + 1000 + 1

            Console.WriteLine("{0}: start cplex", DateTime.Now);

            Cplex cplex = null;
            try
            {
                cplex = new Cplex();
                cplex.SetParam(Cplex.DoubleParam.TiLim, 2 * 60 * 60); // in seconds

                // My solution is obtained at root node of CPLEX mip with default settings except for mip tolerances (absolute and relative) that I set to 0. If I don't set them, then I get a solution 1e-8 worse. !!!!!!!!!!!!!!
                cplex.SetParam(Cplex.Param.MIP.Tolerances.MIPGap, 0); // !!
                cplex.SetParam(Cplex.Param.MIP.Tolerances.AbsMIPGap, 0); // !!

                INumVar[][] var = new INumVar[3][];
                IRange[][] rng = new IRange[5][];

                var[0] = cplex.BoolVarArray(edgeMap.Count + 1000000 - 4000);
                var[1] = cplex.IntVarArray(1000, 0, 1000);
                var[2] = cplex.IntVarArray(1, 0, 1000000 - 4000);

                DescribeModel(cplex, edgeMap, var, rng, edge2ind, i2j, j2i);


                Console.WriteLine("{0}: start solve", DateTime.Now);

                var child2gift = new Dictionary<int, int>();
                var giftCount = new Dictionary<int, int>();
                for (int j = 0; j < 1000; j++)
                {
                    giftCount[j] = 1000;
                }


                if (cplex.Solve())
                {
                    Console.WriteLine("{0}: Solved !!!!!!!!!!!!!!!!!!!!!!!!!!!!", DateTime.Now);
                    Console.WriteLine("Solution status = " + cplex.GetStatus());
                    Console.WriteLine("Solution value  = " + cplex.ObjValue);


                }
                else
                {
                    Console.WriteLine("Solution status = " + cplex.GetStatus());
                    Console.WriteLine("Solution value  = " + cplex.ObjValue);
                }

                double[] outX = cplex.GetValues(var[0]);


                var total = 0;
                for (int k = 0; k < edgeMap.Count; k++)
                {
                    if (outX[k] > 0.9 && outX[k] < 1.1)
                    {
                        var edge = ind2edge[k];
                        int i = edge.Item2;
                        int j = edge.Item1;
                        child2gift[i] = j;
                        giftCount[j]--;
                        total++;
                    }
                }
                Console.WriteLine("Total: {0}", total);
                for (int i = 0; i < 1000000 - 4000; i++)
                //for (int i = 0; i < 1000000 ; i++)
                {
                    if (outX[edgeMap.Count + i] > 0.9 && outX[edgeMap.Count + i] < 1.1)
                    {
                        total++;
                    }
                }
                Console.WriteLine("Total: {0}", total);


                for (int i = 0; i < 1000000; i++)
                {
                    if (!child2gift.ContainsKey(i))
                    {
                        Console.WriteLine("no gift for {0}", i);
                        if (i < 4000)
                        {
                            Console.WriteLine("shouldn't happen {0}", i);
                            // twins
                            for (int j = 0; j < 1000; j++)
                            {
                                if (giftCount[j] > 1)
                                {
                                    child2gift[i] = j;
                                    child2gift[i + 1] = j;
                                    giftCount[j] -= 2;
                                    break;
                                }
                            }
                            if (!child2gift.ContainsKey(i))
                            {
                                Console.WriteLine("no gift for twin {0}", i);
                            }
                        }
                        else
                        {
                            //find first available gift
                            for (int j = 0; j < 1000; j++)
                            {
                                if (giftCount[j] > 0)
                                {
                                    Console.WriteLine("gift {0} assigned to {1}", j, i);
                                    child2gift[i] = j;
                                    giftCount[j]--;
                                    break;
                                }
                            }
                        }
                    }
                }


                Console.WriteLine("{0}: All gift assigned", DateTime.Now);

                for (int j = 0; j < 1000; j++)
                {
                    if (giftCount[j] > 0)
                    {
                        Console.WriteLine("{0}: {1}", j, giftCount[j]);
                    }
                }

                Console.WriteLine("{0}: dict to csv", DateTime.Now);
                // write to csv

                using (var w = new StreamWriter("my.csv"))
                {
                    w.WriteLine("ChildId,GiftId");
                    w.Flush();
                    for (int i = 0; i < 1000000; i++)
                    {
                        if (child2gift.ContainsKey(i))
                        {
                            var line = string.Format("{0},{1}", i, child2gift[i]);
                            w.WriteLine(line);
                            w.Flush();
                        }
                        else
                        {
                            Console.WriteLine("child {0} has no gift", i);
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



        // public static double Cost() { }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="cplex"></param>
        /// <param name="edgeMap"></param>
        /// <param name="var"></param>
        /// <param name="rng"></param>
        /// <param name="edge2ind"></param>
        /// <param name="i2j"></param>
        /// <param name="j2i"></param>
        public static void DescribeModel(Cplex cplex,
            Dictionary<Tuple<int, int>, double> edgeMap,
            INumVar[][] var,
            IRange[][] rng,
            Dictionary<Tuple<int, int>, int> edge2ind,
            Dictionary<int, List<int>> i2j,
            Dictionary<int, List<int>> j2i)
        {
            IIntVar[] x = (IIntVar[])var[0];
            IIntVar[] y = (IIntVar[])var[1];
            IIntVar[] z = (IIntVar[])var[2];


            // objective
            INumExpr[] prodsObj = new INumExpr[edgeMap.Count + 1000000 - 4000];
            //INumExpr[] prodsObj = new INumExpr[edgeMap.Count + 1000000];
            int k = 0;
            foreach (var edge in edgeMap)
            {
                prodsObj[k] = cplex.Prod(x[k], edge.Value);
                k++;
            }
            for (int i = 0; i < 1000000 - 4000; i++)
            //for (int i = 0; i < 1000000; i++)
            {
                prodsObj[k] = cplex.Prod(x[k], MIN_FLOW); // !!!!!!!!!
                k++;
            }
            cplex.AddMaximize(cplex.Sum(prodsObj));
            Console.WriteLine("{0}: cplex Objective - ok", DateTime.Now);



            // flow to childs
            rng[0] = new IRange[1000000];
            for (int i = 0; i < 1000000; i++)
            {
                var prods = new List<INumExpr>();
                foreach (var j in i2j[i])
                {
                    prods.Add(x[edge2ind[new Tuple<int, int>(j, i)]]);
                }
                if (i >= 4000)
                {
                    prods.Add(x[edgeMap.Count + i - 4000]);
                }
                //prods.Add(x[edgeMap.Count + i]);

                rng[0][i] = cplex.AddEq(cplex.Sum(prods.ToArray()), 1);
            }
            Console.WriteLine("{0}: cond1 - OK", DateTime.Now);



            // flow from gifts
            rng[1] = new IRange[1000];
            for (int j = 0; j < 1000; j++)
            {
                var prods = new List<INumExpr>();
                foreach (var i in j2i[j])
                {
                    prods.Add(x[edge2ind[new Tuple<int, int>(j, i)]]);
                }
                prods.Add(cplex.Prod(y[j], -1));
                rng[1][j] = cplex.AddEq(cplex.Sum(prods.ToArray()), 0);
                //rng[1][j] = cplex.AddLe(cplex.Sum(prods.ToArray()), 1000);
            }
            Console.WriteLine("{0}: cond2 - OK", DateTime.Now);

            //
            rng[2] = new IRange[1];
            var prods2 = new List<INumExpr>();
            //for (int i = 0; i < 1000000 ; i++)
            for (int i = 0; i < 1000000 - 4000; i++)
            {
                prods2.Add(x[edgeMap.Count + i]);
            }
            prods2.Add(cplex.Prod(z[0], -1));
            rng[2][0] = cplex.AddEq(cplex.Sum(prods2.ToArray()), 0);

            // 
            rng[3] = new IRange[1];
            var prods3 = new List<INumExpr>();
            for (int j = 0; j < 1000; j++)
            {
                prods3.Add(y[j]);
            }
            prods3.Add(z[0]);
            rng[3][0] = cplex.AddEq(cplex.Sum(prods3.ToArray()), 1000000);

            // twins

            var tmp = new List<IRange>();
            int r = 0;
            k = 0;
            foreach (var edge1 in edgeMap)
            {
                int i = edge1.Key.Item2;
                int j = edge1.Key.Item1;
                if (i < 4000 && i % 2 == 0)
                {
                    var edge2 = new Tuple<int, int>(j, i + 1);

                    var m = edge2ind[edge2];
                    //Console.WriteLine("{0}={1}", k, m);
                    tmp.Add(cplex.AddEq(cplex.Sum(x[k], cplex.Prod(x[m], -1)), 0));
                    //rng[4][r] = cplex.AddEq(cplex.Sum(x[k], cplex.Prod(x[m], -1)), 0);

                    r++;
                }
                k++;
            }

            rng[4] = tmp.ToArray();
            Console.WriteLine("{1}: twins r = {0}", r, DateTime.Now);



            // export
            cplex.ExportModel("milp-workers.lp");
        }
    }
}