﻿using EntityLayer.Entities;
using Estate.UI.Areas.User.Models;
using Estate.UI.Areas.Admin.Models;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Estate.UI.Areas.User.Services;

namespace Estate.UI.Areas.User.Controllers
{
    [Area("User")]
   
    public class UserController : Controller
    {
        private UserManager<UserAdmin> _userManager;
        private SignInManager<UserAdmin> _signInManager;
        private RoleManager<IdentityRole> _roleManager;

        private readonly RabbitMQHelper _rabbitMQHelper;
        private readonly PasswordResetRequestHandler _passwordResetRequestHandler;

        public UserController(UserManager<UserAdmin> userManager, SignInManager<UserAdmin> signInManager, RoleManager<IdentityRole> roleManager, RabbitMQHelper rabbitMQHelper, PasswordResetRequestHandler passwordResetRequestHandler)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _roleManager = roleManager;
            _rabbitMQHelper = rabbitMQHelper;
            _passwordResetRequestHandler = new PasswordResetRequestHandler(_rabbitMQHelper);
        }
        public IActionResult Index()
        {

            return View();
        }

        public IActionResult ResetPassword()
        {
            return View(new ResetPasswordModel());
        }

        [HttpPost]
        public async Task<IActionResult> ResetPassword(ResetPasswordModel model)
        {
            UserAdmin user = await _userManager.FindByEmailAsync(model.Email);

            if (user!=null)
            {
                string resettoken = await _userManager.GeneratePasswordResetTokenAsync(user);

                string passwordresetlink = Url.Action("UpdatePassword", "User", new { userId = user.Id, token = resettoken },HttpContext.Request.Scheme);
                _rabbitMQHelper.SendPasswordResetRequest(model.Email, passwordresetlink);

                _passwordResetRequestHandler.StartHandling();
                //MailHelper.ResetPassword.PasswordSendMail(passwordresetlink); bu gönderme işlemi artık rabbitmqhelper ile yapılıyor

                ViewBag.state = true;
            }
            else
            {

                ViewBag.state = false;
            }
            return View(model);
        }

        public IActionResult UpdatePassword(string userId, string token)
        {
            TempData["userId"] = userId;
            TempData["token"] = token;
            return View();
        }
        [HttpPost]

        public async Task<IActionResult> UpdatePassword([Bind("NewPassword")] ResetPasswordModel model)
        {
            string token = TempData["token"].ToString();
            string userId = TempData["userId"].ToString();

            UserAdmin user = await _userManager.FindByIdAsync(userId);

            if (user != null)
            {
                IdentityResult result = await _userManager.ResetPasswordAsync(user, token, model.NewPassword);

                if (result.Succeeded)
                {
                    await _userManager.UpdateSecurityStampAsync(user);

                    TempData["Success"] = "Başarıyla güncellenmiştir";
                    
                }
            }
            else
            {
                ModelState.AddModelError("", "Böyle bir kullanıcı bulunamadı");
            }
            return View();
        }

        public IActionResult Profile()
        {
            var user = _userManager.FindByNameAsync(User.Identity.Name).Result;
            RegisterModel registerModel=new RegisterModel()
            {
                Email=user.Email,
                UserName=user.UserName,
                FullName=user.FullName
            };


            

                return View(registerModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Profile(RegisterModel model)
        {
            ModelState.Remove("Password");
            ModelState.Remove("RePassword");

            if (ModelState.IsValid)
            {
                UserAdmin user = await _userManager.FindByNameAsync(User.Identity.Name);

                user.FullName = model.FullName;
                user.UserName = model.UserName;
                user.Email = model.Email;

                IdentityResult result = await _userManager.UpdateAsync(user);

                if (result.Succeeded)
                {
                    await _userManager.UpdateSecurityStampAsync(user);

                    await _signInManager.SignOutAsync();
                    await _signInManager.SignInAsync(user,true);

                    ViewBag.success = "Kullanıcı bilgileri başarıyla güncellendi";


                }

                else
                {
                    foreach (var item in result.Errors)
                    {
                        ModelState.AddModelError("", item.Description);
                    }
                }
            }
            return View(model);
        }
        public IActionResult ChangePassword()
        {
            return View(new PasswordModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(PasswordModel model)
        {
            if (ModelState.IsValid)
            {
                UserAdmin user = await _userManager.FindByNameAsync(User.Identity.Name);

                bool exist = await _userManager.CheckPasswordAsync(user, model.OldPassword);

                if (exist)
                {
                    IdentityResult result = await _userManager.ChangePasswordAsync(user, model.OldPassword, model.NewPassword);

                    if (result.Succeeded)
                    {
                        await _userManager.UpdateSecurityStampAsync(user);
                        await _signInManager.SignOutAsync();
                        await _signInManager.PasswordSignInAsync(user, model.NewPassword,true,true);

                        ViewBag.success = "Kullanıcı Şifresi başarıyla güncellendi";

                    }
                    else
                    {
                        foreach (var item in result.Errors)
                        {
                            ModelState.AddModelError("", item.Description);
                        }
                    }
                }
                else
                {
                    
                        ModelState.AddModelError("", "Bir hata oluştu");
                    
                }


            }
                return View();
        }
        public IActionResult Login()
        {
            return View(new Estate.UI.Areas.User.Models.LoginModel());
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task <IActionResult> Login(Estate.UI.Areas.User.Models.LoginModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);

            }

            var user =  await _userManager.FindByNameAsync(model.Username);

            if (user == null)
            {
                ModelState.AddModelError("", "Böyle bir kullanıcı yok");
            }

            if (await _userManager.IsLockedOutAsync(user))
            {
                ModelState.AddModelError("", "Kullanıcı bir süreliğine kilitlendi");
            }

            var result = await _signInManager.PasswordSignInAsync(user, model.Password, false, true);

            if (result.Succeeded)
            {
                await _userManager.ResetAccessFailedCountAsync(user);

                HttpContext.Session.SetString("Id", user.Id);
                HttpContext.Session.SetString("FullName", user.FullName);
                return RedirectToAction("Index");
            }
            else
            {
                await _userManager.AccessFailedAsync(user);

                int fail = await _userManager.GetAccessFailedCountAsync(user);
                if (fail==3)
                {
                    await _userManager.SetLockoutEndDateAsync(user, new DateTimeOffset(DateTime.Now.AddMinutes(2)));
                }
                else
                {
                    ModelState.AddModelError("", "Hatalı eposta veya şifre");
                }
            }
            return View();
        }

        public IActionResult Register()
        {
            return View(new RegisterModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task <IActionResult> Register(RegisterModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);

            }

            UserAdmin user = new UserAdmin()
            {
                UserName = model.UserName,
                Email = model.Email,
                FullName = model.FullName,
                Status=true

            };

            IdentityRole role = new IdentityRole()
            {
                Name = "User"
            };

            await _roleManager.CreateAsync(role);

             var result= await _userManager.CreateAsync(user,model.Password);

            var resultt = await _userManager.AddToRoleAsync(user, role.Name);

            if (result.Succeeded || resultt.Succeeded)
            {
                return RedirectToAction("Login");
            }
            else
            {
                result.Errors.ToList().ForEach(x => ModelState.AddModelError("", x.Description));
                return View(model);
            }
            
        }
        public async Task<IActionResult> LogOut()
        {
            await _signInManager.SignOutAsync();
            HttpContext.Session.Remove("FullName");
            return RedirectToAction("Login");
        }
    }
}
