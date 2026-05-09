namespace QL_TrungTamNgoaiNgu.ViewModels
{
    public sealed class FormField : BaseViewModel
    {
        private string _value;

        public FormField(string name, string value, bool isReadOnly)
        {
            Name = name;
            Value = value;
            IsReadOnly = isReadOnly;
        }

        public string Name { get; }
        public bool IsReadOnly { get; }

        public string Value
        {
            get => _value;
            set => SetProperty(ref _value, value);
        }
    }
}
