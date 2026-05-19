using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using WebUITopic5_Team4.Data;
using WebUITopic5_Team4.Models;
using WebUITopic5_Team4.Models.ViewModels;

namespace WebUITopic5_Team4.Controllers
{
    public class AccountController : Controller
    {
        private readonly ElectronicShopContext _context;

        public AccountController(ElectronicShopContext context)
        {
            _context = context;
        }

        // GET: /Account/Login
        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            if (User.Identity!.IsAuthenticated)
            {
                return RedirectToAction("Index", "Home");
            }
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        // POST: /Account/Login (AJAX)
        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
        {
            if (!ModelState.IsValid)
            {
                return Json(new { success = false, message = "Dữ liệu không hợp lệ." });
            }

            var account = await _context.Accounts
                .FirstOrDefaultAsync(a => (a.AccountId == model.UsernameOrEmail || a.Email == model.UsernameOrEmail) && a.Password == model.Password);

            if (account == null)
            {
                return Json(new { success = false, message = "Tên đăng nhập hoặc mật khẩu không chính xác." });
            }

            if (!account.IsActive)
            {
                return Json(new { success = false, message = "Tài khoản của bạn đã bị khóa." });
            }

            // Create claims
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, account.FullName),
                new Claim("AccountId", account.AccountId),
                new Claim("Email", account.Email)
            };

            var claimsIdentity = new ClaimsIdentity(claims, "CookieAuth");
            var authProperties = new AuthenticationProperties
            {
                IsPersistent = model.RememberMe
            };

            await HttpContext.SignInAsync("CookieAuth", new ClaimsPrincipal(claimsIdentity), authProperties);

            return Json(new { success = true, message = "Đăng nhập thành công!", returnUrl = returnUrl });
        }

        // POST: /Account/Logout
        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync("CookieAuth");
            return RedirectToAction("Index", "Home");
        }

        // GET: /Account/Register
        [HttpGet]
        public IActionResult Register()
        {
            if (User.Identity!.IsAuthenticated)
            {
                return RedirectToAction("Index", "Home");
            }
            return View();
        }

        // POST: /Account/SendRegisterOTP (AJAX)
        [HttpPost]
        public IActionResult SendRegisterOTP(string username, string email)
        {
            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(email))
            {
                return Json(new { success = false, message = "Thông tin không hợp lệ." });
            }

            // In a real app we send OTP. Here we just mock it for simplicity.
            HttpContext.Session.SetString("RegisterOTP", "123456");
            return Json(new { success = true, message = "Mã OTP đã được gửi đến email của bạn. (Mã demo: 123456)" });
        }

        // POST: /Account/Register (AJAX)
        [HttpPost]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return Json(new { success = false, message = "Dữ liệu đăng ký không hợp lệ." });
            }

            var sessionOtp = HttpContext.Session.GetString("RegisterOTP");
            if (sessionOtp == null || model.VerificationCode != sessionOtp)
            {
                return Json(new { success = false, message = "Mã xác thực OTP không chính xác hoặc đã hết hạn." });
            }

            var exist = await _context.Accounts.AnyAsync(a => a.AccountId == model.Username || a.Email == model.Email);
            if (exist)
            {
                return Json(new { success = false, message = "Tên đăng nhập hoặc email đã tồn tại trên hệ thống." });
            }

            var newAccount = new Account
            {
                AccountId = model.Username,
                Password = model.Password,
                FullName = model.FullName,
                Email = model.Email,
                IsActive = true,
                RoleId = 1 // Khách hàng
            };

            _context.Accounts.Add(newAccount);
            await _context.SaveChangesAsync();

            // Automatically sign in after register
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, newAccount.FullName),
                new Claim("AccountId", newAccount.AccountId),
                new Claim("Email", newAccount.Email)
            };

            var claimsIdentity = new ClaimsIdentity(claims, "CookieAuth");
            await HttpContext.SignInAsync("CookieAuth", new ClaimsPrincipal(claimsIdentity));

            return Json(new { success = true, message = "Đăng ký tài khoản thành công!" });
        }

        // GET: /Account/Profile
        [HttpGet]
        public async Task<IActionResult> Profile()
        {
            if (!User.Identity!.IsAuthenticated)
            {
                return RedirectToAction("Login");
            }

            var accountId = User.FindFirst("AccountId")?.Value;
            var account = await _context.Accounts.FindAsync(accountId);
            if (account == null)
            {
                return RedirectToAction("Logout");
            }

            return View(account);
        }

        // GET: /Account/EditProfile
        [HttpGet]
        public async Task<IActionResult> EditProfile()
        {
            if (!User.Identity!.IsAuthenticated)
            {
                return RedirectToAction("Login");
            }

            var accountId = User.FindFirst("AccountId")?.Value;
            var account = await _context.Accounts.FindAsync(accountId);
            if (account == null)
            {
                return RedirectToAction("Logout");
            }

            var model = new EditProfileViewModel
            {
                FullName = account.FullName,
                Email = account.Email,
                PhoneNumber = account.PhoneNumber,
                Address = account.Address
            };

            return View(model);
        }

        // POST: /Account/EditProfile
        [HttpPost]
        public async Task<IActionResult> EditProfile(EditProfileViewModel model)
        {
            if (!User.Identity!.IsAuthenticated)
            {
                return RedirectToAction("Login");
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var accountId = User.FindFirst("AccountId")?.Value;
            var account = await _context.Accounts.FindAsync(accountId);
            if (account == null)
            {
                return RedirectToAction("Logout");
            }

            account.FullName = model.FullName;
            account.Email = model.Email;
            account.PhoneNumber = model.PhoneNumber;
            account.Address = model.Address;

            _context.Accounts.Update(account);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Cập nhật hồ sơ thành công!";
            return RedirectToAction("Profile");
        }

        // GET: /Account/ChangePassword
        [HttpGet]
        public IActionResult ChangePassword()
        {
            if (!User.Identity!.IsAuthenticated)
            {
                return RedirectToAction("Login");
            }
            return View();
        }

        // POST: /Account/ChangePassword
        [HttpPost]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            if (!User.Identity!.IsAuthenticated)
            {
                return RedirectToAction("Login");
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var accountId = User.FindFirst("AccountId")?.Value;
            var account = await _context.Accounts.FindAsync(accountId);
            if (account == null)
            {
                return RedirectToAction("Logout");
            }

            if (account.Password != model.OldPassword)
            {
                ModelState.AddModelError("OldPassword", "Mật khẩu cũ không chính xác.");
                return View(model);
            }

            account.Password = model.NewPassword;
            _context.Accounts.Update(account);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Đổi mật khẩu thành công!";
            return RedirectToAction("Profile");
        }
    }
}
