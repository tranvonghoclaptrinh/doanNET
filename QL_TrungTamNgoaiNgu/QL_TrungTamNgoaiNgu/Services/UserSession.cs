using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace QL_TrungTamNgoaiNgu.Services
{
    public sealed class UserSession
    {
        public UserSession(int maNguoiDung, string hoTen, string email, string soDienThoai, IEnumerable<string> roles)
        {
            MaNguoiDung = maNguoiDung;
            HoTen = hoTen;
            Email = email;
            SoDienThoai = soDienThoai;
            Roles = roles?.ToList() ?? new List<string>();
        }

        public int MaNguoiDung { get; }
        public string HoTen { get; }
        public string Email { get; }
        public string SoDienThoai { get; }
        public IReadOnlyList<string> Roles { get; }

        public bool IsAdmin => HasRoleGroup("Admin");
        public bool IsStudent => HasRoleGroup("Student");
        public bool IsAccountant => HasRoleGroup("Accountant");
        public bool IsTeacher => HasRoleGroup("Teacher");

        public bool HasRoleGroup(string roleGroup)
        {
            return Roles.Any(role => RoleMatches(role, roleGroup));
        }

        public static bool RoleMatches(string databaseRole, string roleGroup)
        {
            var role = Normalize(databaseRole);
            var group = Normalize(roleGroup);

            if (group == "admin")
            {
                return role == "admin" || role == "quantrivien";
            }

            if (group == "student")
            {
                return role == "student" || role == "hocvien";
            }

            if (group == "accountant")
            {
                return role == "accounting" || role == "accountant" || role == "ketoan";
            }

            if (group == "teacher")
            {
                return role == "teacher" || role == "giangvien";
            }

            return role == group;
        }

        private static string Normalize(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            var normalized = value.Trim().Normalize(NormalizationForm.FormD);
            var builder = new StringBuilder();

            foreach (var character in normalized)
            {
                if (CharUnicodeInfo.GetUnicodeCategory(character) != UnicodeCategory.NonSpacingMark
                    && !char.IsWhiteSpace(character))
                {
                    builder.Append(char.ToLowerInvariant(character));
                }
            }

            return builder.ToString().Normalize(NormalizationForm.FormC);
        }
    }
}
