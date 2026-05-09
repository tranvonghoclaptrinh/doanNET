using QL_TrungTamNgoaiNgu.Models;
using System;
using System.Collections.Concurrent;
using System.Data.Entity;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;

namespace QL_TrungTamNgoaiNgu.Services
{
    public sealed class AuthService
    {
        private static readonly ConcurrentDictionary<string, int> FailedAttempts = new ConcurrentDictionary<string, int>();

        public async Task<UserSession> LoginAsync(string email, string password, string roleGroup)
        {
            using (var db = new HeThongQuanLyTrungTamNgoaiNguEntities1())
            {
                var normalizedEmail = (email ?? string.Empty).Trim().ToLower();
                var user = await db.NguoiDungs
                    .AsNoTracking()
                    .Where(item => item.IsActive && item.Email.ToLower() == normalizedEmail)
                    .Select(item => new AuthUserResult
                    {
                        MaNguoiDung = item.MaNguoiDung,
                        HoTen = item.HoTen,
                        Email = item.Email,
                        SoDienThoai = item.SoDienThoai,
                        MuoiMatKhau = item.MuoiMatKhau,
                        MatKhau = item.MatKhau,
                        VaiTro = string.Empty
                    })
                    .FirstOrDefaultAsync();

                if (user == null)
                {
                    throw new InvalidOperationException("Email hoac mat khau khong dung.");
                }

                var key = normalizedEmail;
                var enteredPassword = password ?? string.Empty;
                var storedPasswordHash = (user.MatKhau ?? string.Empty).Trim();
                var passwordHash = await db.Database.SqlQuery<string>(
                    "SELECT dbo.fn_HashMatKhau(@Salt, @MatKhau)",
                    new SqlParameter("@Salt", (user.MuoiMatKhau ?? string.Empty).Trim()),
                    new SqlParameter("@MatKhau", enteredPassword))
                    .SingleAsync();

                if (!PasswordMatches(enteredPassword, storedPasswordHash, passwordHash))
                {
                    var attempts = FailedAttempts.AddOrUpdate(key, 1, (_, current) => current + 1);
                    if (attempts >= 3)
                    {
                        throw new InvalidOperationException("Truy cap bat thuong: tai khoan da nhap sai mat khau qua 3 lan.");
                    }

                    throw new InvalidOperationException("Email hoac mat khau khong dung.");
                }

                FailedAttempts.TryRemove(key, out _);

                var roles = await db.NguoiDungs
                    .Where(item => item.MaNguoiDung == user.MaNguoiDung)
                    .SelectMany(item => item.VaiTroes)
                    .Select(role => role.TenVaiTro)
                    .ToListAsync();

                if (!roles.Any(role => UserSession.RoleMatches(role, roleGroup)))
                {
                    throw new InvalidOperationException($"Tai khoan nay khong thuoc vai tro {roleGroup}.");
                }

                return new UserSession(user.MaNguoiDung, user.HoTen, user.Email, user.SoDienThoai, roles);
            }
        }

        public async Task ResetPasswordAsync(string email, string soDienThoai, string newPassword)
        {
            using (var db = new HeThongQuanLyTrungTamNgoaiNguEntities1())
            {
                var normalizedEmail = (email ?? string.Empty).Trim().ToLower();
                var normalizedPhone = (soDienThoai ?? string.Empty).Trim();

                var user = await db.NguoiDungs.FirstOrDefaultAsync(item =>
                    item.Email.ToLower() == normalizedEmail && item.SoDienThoai == normalizedPhone);

                if (user == null)
                {
                    throw new InvalidOperationException("Email hoac so dien thoai khong dung.");
                }

                var salt = Guid.NewGuid().ToString();
                var passwordHash = await db.Database.SqlQuery<string>(
                    "SELECT dbo.fn_HashMatKhau(@Salt, @MatKhau)",
                    new SqlParameter("@Salt", salt),
                    new SqlParameter("@MatKhau", newPassword ?? string.Empty))
                    .SingleAsync();

                user.MuoiMatKhau = salt;
                user.MatKhau = passwordHash;
                user.OTP = null;
                user.ThoiGianOTP = null;
                await db.SaveChangesAsync();
            }
        }

        private static bool PasswordMatches(string enteredPassword, string storedPasswordHash, string calculatedPasswordHash)
        {
            if (string.Equals(storedPasswordHash, calculatedPasswordHash?.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            return IsSha256Hex(storedPasswordHash)
                && string.Equals(storedPasswordHash, enteredPassword?.Trim(), StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsSha256Hex(string value)
        {
            if (string.IsNullOrWhiteSpace(value) || value.Length != 64)
            {
                return false;
            }

            return value.All(Uri.IsHexDigit);
        }
    }
}
