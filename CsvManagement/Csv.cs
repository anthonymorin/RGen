using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CsvManagement
{
    public class Csv
    {
        Dictionary<(int col, int row), object?> data = new Dictionary<(int col, int row), object?>();
        Rectangle size = new Rectangle();
        public Csv() 
        {
            Row = new CsvVectors(this, Dimension.Row);
            Column = new CsvVectors(this, Dimension.Column);
            Headers = new Dictionary<string, int>();
        }

        public string? this[int column, int row]
        {
            get
            {
                if (data.TryGetValue((column, row), out object? value))
                {
                    return value == null ? null : $"{value}";
                }
                return null;
            }
            set 
            { 
                data[(column, row)] = value;

                size = new Rectangle(
                    Math.Min(size.Left, column),
                    Math.Min(size.Top, row),
                    Math.Max(size.Right, column + 1) - Math.Min(size.Left, column),
                    Math.Max(size.Bottom, row + 1) - Math.Min(size.Top, row));
            }
        }
        public string? this[string header, int row]
        {
            get 
            { 
                if (Headers.TryGetValue(header, out int column))
                    return this[column, row];
                throw new ArgumentException($"");
            }
            set
            {
                if (Headers.TryGetValue(header, out int column))
                    this[column, row] = value;
                throw new ArgumentException($"");
            }
        }

        public Dictionary<string, int> Headers { get; }

        public CsvVectors Row { get; }
        public CsvVectors Column { get; }
        public Rectangle Size { get => size; }

        public static async Task<Csv> Parse(string file, ICsvReader? Reader = null, ICsvSettings? settings = null)
        {
            return await (Reader ?? new CsvReader()).Read(file, settings ?? new DefaultSettings());
        }

        public async Task Write(string FileName, ICsvWriter? Writer = null, ICsvSettings? settings = null)
        {
            await (Writer ?? new CsvWriter()).Write(this, FileName, settings ?? new DefaultSettings());
        }        
    }

    public interface ICsvWriter
    {
        Task Write(Csv csv, string fileName, ICsvSettings settings);
    }

    public interface ICsvReader
    {
        Task<Csv> Read(string filename, ICsvSettings settings);
    }

    public class CsvReader : ICsvReader
    {
        public Task<Csv> Read(string fileName, ICsvSettings settings)
        {
            return Task.Run(() =>
            {
                using (var fs = File.OpenRead(fileName))
                using (StreamReader r = new StreamReader(fs, settings.Encoding))
                {
                    Csv result = new Csv();

                    if (settings.HasHeaderRow)
                    {
                        foreach (var kvp in ReadToEnd(r, settings).Split(settings.Delimiter, StringSplitOptions.None).Select((x, i) => (x, i)).ToDictionary(x => x.x, x => x.i))
                            result.Headers[UnwrapIfNecessary(kvp.Key,settings)!] = kvp.Value;
                    }

                    int lineNumber = 0;

                    while(r.Peek() >= 0)
                    {
                        foreach (var x in ReadToEnd(r, settings).Split(settings.Delimiter, StringSplitOptions.None).Select((x, i) => (x, i)))
                        {
                            result[x.i, lineNumber] = UnwrapIfNecessary(x.x, settings);
                        }
                            

                        lineNumber++;
                    }

                    return result;
                }
            });
        }

        static string ReadToEnd(StreamReader r, ICsvSettings settings)
        {
            int
                delIndex = 0;

            string
                lineDelimiter = settings.NewLine;

            byte[]
                delBytes = settings.Encoding.GetBytes(lineDelimiter);
            List<byte>
                line = new List<byte>();

            while(r.Peek() >= 0)
            {
                byte
                    cur = (byte)r.Read();
                line.Add(cur);

                if (cur == delBytes[delIndex])
                {
                    delIndex++;

                    if (delIndex == delBytes.Length)
                    {
                        line.RemoveRange(line.Count - delBytes.Length, delBytes.Length);
                        break;
                    }
                }
                else
                {
                    delIndex = 0;
                }
            }

            return settings.Encoding.GetString(line.ToArray());
        }

        private static string? UnwrapIfNecessary(string? value, ICsvSettings settings)
        {
            if (value == null)
                return value;

            if (settings.StringWrapping.HasFlag(StringWrap.IfString))
                return Unwrap(value, settings);

            if (settings.StringWrapping.HasFlag(StringWrap.IfContainsDelimiter) && value.Contains(settings.Delimiter))
                return Unwrap(value, settings);

            if (settings.StringWrapping.HasFlag(StringWrap.IfContainsNewLine) && value.Contains(settings.NewLine))
                return Unwrap(value, settings);

            return value;
        }

        private static string Unwrap(string value, ICsvSettings settings)
        {
            if (!value.StartsWith(settings.OpenQuote) && value.EndsWith(settings.CloseQuote))
            {
                if (settings.OpenQuote == settings.CloseQuote)
                    value = value.Replace("\\" + settings.OpenQuote, settings.OpenQuote);
                else
                    value = value.Replace("\\" + settings.OpenQuote, settings.OpenQuote).Replace("\\" + settings.CloseQuote, settings.CloseQuote);
                return settings.OpenQuote + value + settings.CloseQuote;
            }
            return value;
        }
    }

    public class CsvWriter : ICsvWriter
    {

        public Task Write(Csv csv, string fileName, ICsvSettings settings)
        {
            return Task.Run(() => 
            {
                using (var fs = File.OpenWrite(fileName))
                using (StreamWriter w = new StreamWriter(fs))
                {
                    //Write header row if needed
                    if (settings.HasHeaderRow) { }
                    {
                        IterateRowColumns(w, c => 
                        {
                            if (csv.Headers.FirstOrDefault(x => x.Value == c).Key is string header)
                            {
                                return header;
                            }

                            return $"Col_{c}";
                        }, csv, settings);
                    }

                    //Write rows
                    for (int r = Math.Min(csv.Size.Top, 0); r < csv.Size.Height; r++)
                    {
                        IterateRowColumns(w, c => csv[c, r], csv, settings);
                    }
                }
            });
        }

        private static void IterateRowColumns(StreamWriter w, Func<int, string?> ColumnAction, Csv csv, ICsvSettings settings)
        {
            List<string?> line = new List<string?>();

            int i = Math.Min(csv.Size.Left, 0);

            for (int c = i; c < csv.Size.Width; c++)
            {
                if (c > i)
                    w.Write(settings.Delimiter);

                string value = WrapIfNecessary(ColumnAction(c), settings) ?? "";

                w.Write(value);
            }


            w.Write(settings.NewLine);
        }

        private static string? WrapIfNecessary(string? value, ICsvSettings settings)
        {
            if (value == null)
                return value;

            if (settings.StringWrapping.HasFlag(StringWrap.IfString))
                return Wrap(value, settings);

            if (settings.StringWrapping.HasFlag(StringWrap.IfContainsDelimiter) && value.Contains(settings.Delimiter))
                return Wrap(value, settings);

            if (settings.StringWrapping.HasFlag(StringWrap.IfContainsNewLine) && value.Contains(settings.NewLine))
                return Wrap(value, settings);

            return value;
        }

        private static string Wrap(string value, ICsvSettings settings)
        {
            if (!value.StartsWith(settings.OpenQuote) && value.EndsWith(settings.CloseQuote))
            {
                if (settings.OpenQuote == settings.CloseQuote)
                    value = value.Replace(settings.OpenQuote, "\\" + settings.OpenQuote);
                else
                    value = value.Replace(settings.OpenQuote, "\\" + settings.OpenQuote).Replace(settings.CloseQuote, "\\" + settings.CloseQuote);
                return settings.OpenQuote + value + settings.CloseQuote;
            }
            return value;
        }
    }

    public interface ICsvSettings
    {
        string Delimiter { get; }
        string NewLine { get; }
        bool HasHeaderRow { get; }
        StringWrap StringWrapping { get; }
        string OpenQuote { get; }
        string CloseQuote { get; }
        Encoding Encoding { get; }
    }

    public class DefaultSettings : ICsvSettings
    {
        public string Delimiter { get; set; } = ",";

        public string NewLine { get; set; } = Environment.NewLine;
        public bool HasHeaderRow { get; set; } = false;
        public StringWrap StringWrapping { get; set; } = StringWrap.IfContainsCsvCharacter;
        public string OpenQuote { get; set; } = "\"";
        public string CloseQuote { get; set; } = "\"";
        public Encoding Encoding { get; set; } = Encoding.Default;
    }

    [Flags]
    public enum StringWrap
    {
        Never,
        IfContainsDelimiter = 1,
        IfContainsNewLine = 2,
        IfContainsCsvCharacter = IfContainsDelimiter | IfContainsNewLine,
        IfString = 4,
        Always = IfString | IfContainsCsvCharacter,
    }
}
