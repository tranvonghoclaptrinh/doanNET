using Microsoft.Win32;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace QL_TrungTamNgoaiNgu.Services
{
    public sealed class CsvExportService : ICsvExportService
    {
        public async Task<string> ExportAsync(IEnumerable rows, string tableName)
        {
            var materializedRows = rows?.Cast<object>().ToList() ?? new List<object>();
            if (materializedRows.Count == 0)
            {
                throw new InvalidOperationException("Khong co du lieu de xuat CSV.");
            }

            var dialog = new SaveFileDialog
            {
                Title = "Xuat du lieu CSV",
                Filter = "CSV files (*.csv)|*.csv",
                FileName = $"{tableName}_{DateTime.Now:yyyyMMdd_HHmmss}.csv"
            };

            if (dialog.ShowDialog() != true)
            {
                return null;
            }

            var properties = GetScalarProperties(materializedRows[0].GetType()).ToList();
            var builder = new StringBuilder();

            builder.AppendLine(string.Join(",", properties.Select(property => Escape(property.Name))));

            foreach (var row in materializedRows)
            {
                var values = properties.Select(property => FormatValue(property.GetValue(row)));
                builder.AppendLine(string.Join(",", values));
            }

            await Task.Run(() => File.WriteAllText(dialog.FileName, builder.ToString(), Encoding.UTF8));
            return dialog.FileName;
        }

        private static IEnumerable<PropertyInfo> GetScalarProperties(Type rowType)
        {
            return rowType.GetProperties(BindingFlags.Instance | BindingFlags.Public)
                .Where(property => IsScalarType(property.PropertyType));
        }

        private static bool IsScalarType(Type type)
        {
            type = Nullable.GetUnderlyingType(type) ?? type;
            return type.IsPrimitive
                   || type.IsEnum
                   || type == typeof(string)
                   || type == typeof(decimal)
                   || type == typeof(DateTime)
                   || type == typeof(Guid);
        }

        private static string FormatValue(object value)
        {
            if (value == null)
            {
                return string.Empty;
            }

            if (value is DateTime dateTime)
            {
                return Escape(dateTime.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture));
            }

            return Escape(Convert.ToString(value, CultureInfo.InvariantCulture));
        }

        private static string Escape(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }

            return "\"" + value.Replace("\"", "\"\"") + "\"";
        }
    }
}
