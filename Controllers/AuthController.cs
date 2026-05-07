using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Threading.Tasks;
using System.Collections.Generic;
using BAOCAO_369.Models;
using BAOCAO_369.Services;

namespace BAOCAO_369.Controllers
{
    public class AuthController : Controller
    {
        private readonly AuthService _authService;

        public AuthController(AuthService authService)
        {
            _authService = authService;
        }

        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            // Nếu đã đăng nhập thành công từ trước mà mở lại trang Login thì chuyển thẳng vào Báo Cáo
            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Index", "BaoCao");
            }
            
            ViewData["ReturnUrl"] = returnUrl;
            return View(new LoginViewModel());
        }

        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var nhanVien = await _authService.AuthenticateUser(model.Username, model.Password);

            if (nhanVien == null)
            {
                model.ErrorMessage = "Tên đăng nhập hoặc mật khẩu không chính xác.";
                return View(model);
            }

            // Khởi tạo thẻ hành lý (Claims) chứa thông tin của nhân viên
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, nhanVien.ID_NV.ToString()),
                new Claim(ClaimTypes.Name, nhanVien.USERNAME),
                new Claim("FullName", $"{nhanVien.FIRSTNAME} {nhanVien.LASTNAME}".Trim()),
                new Claim("Email", nhanVien.EMAIL ?? "")
            };

            // Nhét cấu trúc Tổ chức ID_DV vào Cookie để trích xuất gán vào bộ Filter sau này (Authorization Role rules)
            if (nhanVien.ID_DV.HasValue)
            {
                claims.Add(new Claim("IdDv", nhanVien.ID_DV.Value.ToString()));
            }

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

            var authProperties = new AuthenticationProperties
            {
                // Giữ trình duyệt tồn tại phiên nếu bấm Rember Password
                IsPersistent = model.RememberMe 
            };

            // Nạp phiên đăng nhập Cookie Session an toàn vào máy khách Browser
            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme, 
                new ClaimsPrincipal(claimsIdentity), 
                authProperties);

            // Kiểm tra link redirect an toàn trước khi đẩy đi
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }

            // Thành công quay về Dashboard chính BaoCao Mẫu 3
            return RedirectToAction("Index", "BaoCao");
        }

        [HttpGet]
        public async Task<IActionResult> Logout()
        {
            // Xóa Cookie
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login");
        }
    }
}
