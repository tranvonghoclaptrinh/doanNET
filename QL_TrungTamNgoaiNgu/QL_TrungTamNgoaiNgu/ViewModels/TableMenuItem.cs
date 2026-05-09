namespace QL_TrungTamNgoaiNgu.ViewModels
{
    public sealed class TableMenuItem : BaseViewModel
    {
        private int _rowCount;

        public TableMenuItem(string key, string title, string description)
        {
            Key = key;
            Title = title;
            Description = description;
        }

        public string Key { get; }
        public string Title { get; }
        public string Description { get; }

        public int RowCount
        {
            get => _rowCount;
            set => SetProperty(ref _rowCount, value);
        }
    }
}
