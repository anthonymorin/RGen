namespace CsvManagement
{
    public class CsvVector
    {
        Dictionary<int, object?> data = new Dictionary<int, object?>();
        public CsvVector(Dimension dimension, int? VectorIndex = null) 
        {
            this.Dimension = dimension;
            this.VectorIndex = VectorIndex;
        }

        public CsvVector(Dimension dimension, Csv Parent, int? VectorIndex = null) : this(dimension, VectorIndex)
        {
            this.Parent = Parent;
        }
        
        public string? this[string header]
        {
            get => this[Parent.Headers[header]];
            set => this[Parent.Headers[header]] = value;
        }
        public string? this[int index]
        {
            get => GetGetter()(index);
            set => GetSetter()(index, value);
        }

        T GetAccessor<T>(T ColumnAccessor, T RowAccessor, T DefaultAccessor)
        {
            if (Parent != null && VectorIndex != null)
            {
                switch (Dimension)
                {
                    case Dimension.Column:
                        return ColumnAccessor;
                    case Dimension.Row:
                        return RowAccessor;
                }
            }
            return DefaultAccessor;
        }

        Action<int, string?> GetSetter()
        {
            return GetAccessor<Action<int, string?>>(
                (index, value) => Parent![(int)VectorIndex!, index] = value,
                (index, value) => Parent![index, (int)VectorIndex!] = value,
                (index, value) => this.data[index] = value);
        }

        Func<int, string?> GetGetter()
        {
            return GetAccessor<Func<int, string?>>(
                (index) => Parent![(int)VectorIndex!, index],
                (index) => Parent![index, (int)VectorIndex!],
                (index) => 
                {
                    if (this.data.TryGetValue(index, out object? value) && value != null)
                        return $"{value}";
                    return null;
                });
        }

        CsvVector? ValidateNextPrevious()
        {
            if (VectorIndex == null && Parent != null)
                return new CsvVector(Dimension, Parent, VectorIndex);
            if (VectorIndex == null)
                return new CsvVector(Dimension, VectorIndex);
            if (Parent == null)
                throw new ArgumentNullException(nameof(Parent));
            return null;
        }

        public CsvVector Next()
        {
            return ValidateNextPrevious() ?? new CsvVector(Dimension, Parent!, VectorIndex + 1);
        }

        public CsvVector Previous()
        {
            return ValidateNextPrevious() ?? new CsvVector(Dimension, Parent!, VectorIndex - 1);
        }

        public Csv? Parent { get; }
        public Dimension Dimension { get; }
        public int? VectorIndex { get; }

        public override bool Equals(object? obj)
        {
            return obj is CsvVector v && 
                v.VectorIndex == VectorIndex &&
                v.Parent == Parent &&
                v.Dimension == Dimension;
        }
        public override int GetHashCode()
        {
            return (Dimension.GetHashCode() << 16) ^
                ((VectorIndex?.GetHashCode() ?? 0) << 8) ^
                (Parent?.GetHashCode() ?? 0);
        }
    }
}
