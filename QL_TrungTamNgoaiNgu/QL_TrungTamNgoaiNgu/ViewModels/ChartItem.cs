using System.Globalization;

namespace QL_TrungTamNgoaiNgu.ViewModels
{
    public sealed class ChartItem : BaseViewModel
    {
        public ChartItem(string label, decimal value, decimal maxValue)
        {
            Label = label;
            Value = value;
            BarWidth = maxValue <= 0 ? 0 : (double)(value / maxValue) * 360;
        }

        public string Label { get; }
        public decimal Value { get; }
        public double BarWidth { get; }
        public string DisplayValue => Value.ToString("#,0", CultureInfo.InvariantCulture);
    }
}
