using QL_TrungTamNgoaiNgu.ViewModels;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;

namespace QL_TrungTamNgoaiNgu
{
    public partial class EntityFormWindow : Window
    {
        public EntityFormWindow(string title, IEnumerable<FormField> fields)
        {
            InitializeComponent();
            TitleTextBlock.Text = title;
            Fields = new ObservableCollection<FormField>(fields);
            FieldsItemsControl.ItemsSource = Fields;
        }

        public ObservableCollection<FormField> Fields { get; }
        public Dictionary<string, string> Values => Fields.ToDictionary(field => field.Name, field => field.Value);

        private void SaveButton_OnClick(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }

        private void CancelButton_OnClick(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
}
