using System.Collections;
using System.Threading.Tasks;

namespace QL_TrungTamNgoaiNgu.Services
{
    public interface ICsvExportService
    {
        Task<string> ExportAsync(IEnumerable rows, string tableName);
    }
}
