// See https://aka.ms/new-console-template for more information
using CsvManagement;
using System.Collections;
using System.Text.RegularExpressions;

var samples = Arguments
    .Validate(args)
    .Generate();

foreach(var sample in samples)
    Console.WriteLine(sample);


class Arguments
{
    int seed = 0;
    public string DistributionName { get; set; } = "P";
    public int NumberOfSamples { get; set; }
    public int SamplesFromD1 { get; set; } = 5;
    public int SamplesFromD2 { get; set; } = 1;

    public int Seed { get => seed; set { seed = value; Random = new System.Random(value); } }
    public Random Random { get; private set; } = new System.Random(0);

    public static Arguments Validate(string[] args)
    {
        return new Arguments { DistributionName = args[0], NumberOfSamples = int.Parse(args[1]), Seed = DateTime.Now.Millisecond + 1000*DateTime.Now.Second };
    }

    public Generator Generate()
    {
        return new Generator(this);
    }
}

class Generator : IEnumerable<string>
{
    public Generator(Arguments args)
    {
        this.Arguments = args;

        Parse($"Distributions\\{args.DistributionName}.csv", out Dictionary<int, int> d1, out Dictionary<int, int> d2);

        Distribution1 = d1;
        Distribution2 = d2;
    }

    public Dictionary<int, int> Distribution1 { get; } = new Dictionary<int, int>();
    public Dictionary<int, int> Distribution2 { get; } = new Dictionary<int, int>();
    public Arguments Arguments { get; set; }

    static void Parse(string file, out Dictionary<int, int> Distribution1, out Dictionary<int, int> Distribution2)
    {

        /*
         * Header
         * 
         * Distribution Name
         * |
         * |       0-N columns
         * |       |
         * |       |       Separator
         * |       |       |
         * |       |       |   0-M columns
         * |       |       |   |
         * V |-----------| V |---|
         * P,0,1,2,3,4,5,6,-,0,1,2
         * 
         */

        Csv result = CsvManagement.Csv.Parse(file, settings:new DefaultSettings { HasHeaderRow = false}).Result;

        int
            indexOfDash = 0;
        
        for(int i = 1; i < result.Size.Width; i++)
        {
            if (result[i, 0] == "-")
            {
                indexOfDash = i;
                break;
            }
        }

        int
            d1Start = 1,
            d1Length = indexOfDash - 1,
            d2Start = indexOfDash + 1,
            d2Length = result.Size.Width - d2Start;

        Distribution1 = GetDistribution(result, d1Start, d1Length);
        Distribution2 = GetDistribution(result, d2Start, d2Length);
    }

    static Dictionary<int,int> GetDistribution(Csv csv, int startIndex, int length)
    {
        Dictionary<int, int>
            result = new Dictionary<int, int>();

        Regex reg = new Regex(@"[0-9]+");

        for(int row = 1; row < 10; row++)
        {
            for(int col = 0; col < length; col++)
            {
                int
                    realCol = col + startIndex;
                string 
                    entry = csv[realCol, row]!;

                if (reg.IsMatch(entry))
                {
                    int 
                        index = int.Parse(csv[0, row]!) + 10 * int.Parse(csv[realCol, 0]!);

                    result[index] = int.Parse(entry);
                }
            }
        }

        return result;
    }

    public IEnumerator<string> GetEnumerator()
    {
        Random 
            rand = new Random();
        List<int>
            D1 = GenerateList(Distribution1),
            D2 = GenerateList(Distribution2);

        for(int s = 0; s < Arguments.NumberOfSamples; s++)
        {
            yield return string.Join(" ", Sample(Arguments, D1, D2).Select(x =>$"{x}".PadLeft(2)));
        }
    }
    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    static List<int> GenerateList(Dictionary<int, int> distribution)
    {
        return distribution
            .SelectMany(x => 
                Enumerable.Repeat(x.Key, x.Value + 1))
            .ToList();
    }

    static IEnumerable<int> Sample(Arguments args, List<int> D1, List<int> D2)
    {
        return Sample(args.Random, args.SamplesFromD1, D1)
            .OrderBy(x => x)
            .Concat(Sample(args.Random, args.SamplesFromD2, D2).OrderBy(x => x));
    }

    static IEnumerable<int> Sample(Random rand, int nSamples, List<int> D)
    {
        List<int> tD = D.ToList();

        for(int i = 0; i < nSamples; i++)
        {
            int index = rand.Next(int.MaxValue) % tD.Count;

            var value = tD[index];

            yield return value;

            tD.RemoveAll(x => x == value);
        }
    }
}
