namespace CsvManagement
{
    public class CsvVectors
    {
        public CsvVectors(Csv Parent, Dimension dimension)
        {
            this.Parent = Parent;
            this.Dimension = dimension;
        }

        public Csv Parent { get; }
        public Dimension Dimension { get; }

        public CsvVector this[int index]
        {
            get { return new CsvVector(Dimension, Parent, index); }
        }
    }
}
