using System;
using System.Text;
using System.Threading.Tasks;
using MeteringFramework;

namespace MeteringDemo
{
    class Program
    {
        static async Task Main(string[] args)
        {
            await Measurement1();
            Console.WriteLine("------------------------------------------------");
            await Measurement2(false);
            Console.WriteLine("------------------------------------------------");
            await Measurement3();
            Console.WriteLine("------------------------------------------------");
            await Measurement4();
        }

        private static async Task Measurement1()
        {
            int a = int.MinValue;
            long b = long.MinValue;
            short c = short.MinValue;
            byte d = byte.MinValue;

            Console.WriteLine("Inc-dec average times:");

            var result = await new Race(new Action[]
            {
                ()=>{ d++; d--; d--; d++; },
                ()=>{ c++; c--; c--; c++; },
                ()=>{ a++; a--; a--; a++; },
                ()=>{ b++; b--; b--; b++; },
            }, 10, 1000000, progress => { Console.Write(progress.Message); Console.Write("     \r"); })
                .RunAsync().ConfigureAwait(false);

            Console.Write("                                                     \r");
            Console.WriteLine($"  byte:  {result[0].AverageTime:hh\\:mm\\:ss\\.ffff}  {result[0].Percent:F2}");
            Console.WriteLine($"  short: {result[1].AverageTime:hh\\:mm\\:ss\\.ffff}  {result[1].Percent:F2}");
            Console.WriteLine($"  int:   {result[2].AverageTime:hh\\:mm\\:ss\\.ffff}  {result[2].Percent:F2}");
            Console.WriteLine($"  long:  {result[3].AverageTime:hh\\:mm\\:ss\\.ffff}  {result[3].Percent:F2}");
        }

        private static async Task Measurement2(bool parallel)
        {
            var a = int.MinValue;
            var b = long.MinValue;

            Console.WriteLine("Inc average times:");

            var result = await new Duel(
                racer1: () => { a++; },
                racer2: () => { b++; },
                turns: 10,
                count: 10000000,
                progress: p => { Console.Write(p.Message); Console.Write("     \r"); },
                parallel
            ).RunAsync().ConfigureAwait(false);

            Console.Write("                                                     \r");
            Console.WriteLine("  int:   {0:hh\\:mm\\:ss\\.ffff}  {1:F2}", result[0].AverageTime, result[0].Percent);
            Console.WriteLine("  long:  {0:hh\\:mm\\:ss\\.ffff}  {1:F2}", result[1].AverageTime, result[1].Percent);
        }

        private static async Task Measurement3()
        {
            var aa = new AA();
            var bb = new BB();
            var cc = new CC();
            var a = int.MinValue;
            var b = int.MinValue;
            var c = int.MinValue;
            var d = int.MinValue;

            Console.WriteLine("Method call times:");

            var result = await new Race(
                racers: new Action[]
                {
                    () => { a = aa.A(a); },
                    () => { b = aa.B(b); },
                    () => { c = bb.B(c); },
                    () => { d = cc.B(d); },
                },
                turns: 10,
                count: 10000000,
                progress: p => { Console.Write(p.Message); Console.Write("     \r"); }
            ).RunAsync().ConfigureAwait(false);

            Console.Write("                                                     \r");
            Console.WriteLine("  Pure method:        {0:hh\\:mm\\:ss\\.ffff}  {1:F2}", result[0].AverageTime, result[0].Percent);
            Console.WriteLine("  Virtual method:     {0:hh\\:mm\\:ss\\.ffff}  {1:F2}", result[1].AverageTime, result[1].Percent);
            Console.WriteLine("  Overridden method:  {0:hh\\:mm\\:ss\\.ffff}  {1:F2}", result[2].AverageTime, result[2].Percent);
            Console.WriteLine("  Overridden2 method: {0:hh\\:mm\\:ss\\.ffff}  {1:F2}", result[3].AverageTime, result[3].Percent);
        }
        private class AA
        {
            public int A(int x) { return x++; }
            public virtual int B(int x) { return x++; }
        }
        private class BB : AA
        {
            public override int B(int x) { return x++; }
        }
        private class CC : BB
        {
            public override int B(int x) { return x++; }
        }

        private static async Task Measurement4()
        {
            var a = new string('a', 256);
            var b = new string('b', 256);
            var c = new string('c', 256);
            var d = new string('d', 256);
            string s1, s2, s3, s4, s5;

            Console.WriteLine("String concatenation times:");

            var result = await new Race(
                racers: new Action[]
                {
                    () =>
                    {
                        var sb = new StringBuilder();
                        sb.Append(a).Append(",").Append(b).Append(",").Append(c).Append(",").Append(d);
                        s1 = sb.ToString();
                    },
                    () => { s2 = string.Concat(a, ",", b, ",", c, ",", d); },
                    () => { s3 = a + "," + b + "," + c + "," + d; },
                    () => { s4 = string.Format("{0},{1},{2},{3}", a, b, c, d); },
                    () => { s5 = $"{a},{b},{c},{d}"; },
                },
                turns: 10,
                count: 100000,
                progress: p => { Console.Write(p.Message); Console.Write("     \r"); }
            ).RunAsync().ConfigureAwait(false);

            Console.Write("                                                     \r");
            Console.WriteLine("  StringBuilder       :  {0:hh\\:mm\\:ss\\.ffff}  {1:F2}", result[0].AverageTime, result[0].Percent);
            Console.WriteLine("  + operator          :  {0:hh\\:mm\\:ss\\.ffff}  {1:F2}", result[1].AverageTime, result[1].Percent);
            Console.WriteLine("  string.Concat       :  {0:hh\\:mm\\:ss\\.ffff}  {1:F2}", result[2].AverageTime, result[2].Percent);
            Console.WriteLine("  string.Format       :  {0:hh\\:mm\\:ss\\.ffff}  {1:F2}", result[3].AverageTime, result[3].Percent);
            Console.WriteLine("  string interpolation:  {0:hh\\:mm\\:ss\\.ffff}  {1:F2}", result[4].AverageTime, result[4].Percent);
        }
    }
}
