# ⚡ CHẠY LỀNHGit NHANH NHẤT

## ⚠️ Vấn Đề: Git Chưa Cài

Bạn chưa cài Git trên máy. Tôi đã tạo script tự động cho bạn.

---

## 📋 HƯỚNG DẪN (3 Bước)

### **BƯỚC 1: Cài Git**

1. Vào: **https://git-scm.com/download/win**
2. Tải bản **64-bit** (khuyên nhất)
3. Chạy installer:
   - Nhấp "Next" cho tất cả
   - ✅ Tích "Git Bash Here"
   - Hoàn thành cài đặt

✅ **Git đã cài xong**

---

### **BƯỚC 2: Tạo GitHub Token (Bắt Buộc)**

1. Vào: **https://github.com/settings/tokens**
2. Nhấp "Generate new token (classic)"
3. Cấu hình:
   - **Note:** `doanNET`
   - **Expiration:** `90 days`
   - **Scopes:** Tích `repo`, `admin:repo_hook`, `user`
4. Nhấp "Generate token"
5. **COPY TOKEN NGAY** (sẽ không thấy lại)
6. Giữ tab này mở (dùng sau)

✅ **Token đã có**

---

### **BƯỚC 3: Chạy Script**

1. **Mở File Explorer**
2. **Vào:** `d:\DATA\dotNet-duAn\doAn_dotNET\DoAnNet`
3. **Nhấp đôi:** `git_setup_and_push.bat`
4. **Console sẽ chạy từng bước:**
   - Nó sẽ yêu cầu GitHub credentials
   - **Username:** `tranvonghoclaptrinh`
   - **Password:** Dán token (CTRL+V)
5. **Xem kết quả:**
   - Nếu thành công: "Push completed!"
   - Nếu lỗi: xem phần **Troubleshooting**

✅ **Đã push lên GitHub!**

---

## ✅ Kiểm Tra Kết Quả

1. Vào: **https://github.com/tranvonghoclaptrinh/doanNET**
2. Refresh (F5)
3. Sẽ thấy:
   - ✅ Folder `QL_TrungTamNgoaiNgu/`
   - ✅ File `README.md`
   - ✅ Các SQL script
   - ✅ Commit message

---

## 🐛 Troubleshooting

### ❌ "Git not installed" (Script báo lỗi)

**Giải pháp:**
1. Cài lại Git từ https://git-scm.com
2. Chọn **"Git from the command line and also from 3rd-party software"** khi cài
3. Restart máy
4. Chạy lại script

### ❌ "fatal: not a git repository"

**Giải pháp:**
- Xóa folder `.git` (nếu có)
- Chạy lại script

### ❌ "Authentication failed" (Token sai)

**Giải pháp:**
1. Tạo token mới tại https://github.com/settings/tokens
2. Copy token **ngay** (không chờ)
3. Chạy lại script
4. Khi hỏi password, dán token mới

### ❌ "remote origin already exists"

**Giải pháp:**
```bash
git remote remove origin
# Rồi chạy script lại
```

---

## 📊 Quy Trình Trong Script

Script sẽ tự động:

1. ✅ Thêm "# doanNET" vào README.md
2. ✅ `git init` - khởi tạo repo
3. ✅ `git add .` - thêm tất cả file
4. ✅ `git config user` - cấu hình người dùng
5. ✅ `git commit -m "first commit"` - commit đầu tiên
6. ✅ `git branch -M main` - đổi branch thành main
7. ✅ `git remote add origin ...` - thêm GitHub remote
8. ✅ `git push -u origin main` - push lên GitHub (yêu cầu token)

---

## 💡 Nếu Vẫn Có Vấn Đề

**Chạy thủ công trong Git Bash:**

1. Chuột phải folder `d:\DATA\dotNet-duAn\doAn_dotNET\DoAnNet`
2. Chọn "Git Bash Here"
3. Copy-paste từng lệnh:

```bash
git config user.name "Tran Huu Vong"
git config user.email "tranhuuvong23092006@gmail.com"
git init
git add .
git commit -m "first commit"
git branch -M main
git remote add origin https://github.com/tranvonghoclaptrinh/doanNET.git
git push -u origin main
```

4. Khi hỏi password: Dán token

---

## ✨ Xong!

Sau khi chạy xong, bạn sẽ có:
- ✅ Repository on GitHub: https://github.com/tranvonghoclaptrinh/doanNET
- ✅ Toàn bộ code đã push lên
- ✅ README.md hiển thị trên GitHub

**Thành công! 🎉**

---

**Email hỗ trợ:** tranhuuvong23092006@gmail.com
