using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using PCBuilder.Models;
using PCBuilder.Data;
using PCBuilder.Services;
using System.Security.Claims; //Claims това като факти за потребителя(пример името на потребителя, роля) С тях си изграждаме 
//identity на логнат потребител

namespace PCBuilder.Controllers
{
    //когато създавате controlleri е важно след името на класа ключовата дума Controller, за да може MVC да го разпознава 
    //и да може като стартираме приложението да го имаме /Аccount/Login   /Account/Register
    public class AccountController : Controller
    {
        private readonly AppDbContext _context;

        private readonly UserService _userService;

        public AccountController(UserService userService)
        {
            _userService = userService;
        }

        //правим си гет 
        [HttpGet] //-> това е метод който извиква get заявка 
        public IActionResult Register() => View(new RegisterViewModel()); // IActionResult- типът на резултат: може да връща html stranica,
                                                                          // да редирктва към друга страничка, тескт и тн
                                                                          //View(new RegisterViewModel()) -> Връща htmla-a na RegisterViewModel

        [HttpPost] //и пост заявки за регистрацията
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid) //-> ModelState е резултата валидациите(Required, MinLength)
            {
                return View(model);  //този иф ви е ако има грешки да връщаме същата страничка за Регистрация но с грешките
                                     //return View(model);  -> връщане html на логиката ни но в грешките
            }

            if (_userService.UserExists(model.Username))
            {
                ModelState.AddModelError("", "Username already exist.");
                return View(model);
            }

            // Регистрация с хеширана парола
            _userService.Register(model.Username, model.Password);


            return RedirectToAction("Login"); // след успешна регистрация ще пренасочим към Логин Страницата
            //когато имаме Redirect http 302 -> което значи пренасочване
        }

        //правим си гет 
        [HttpGet]
        public IActionResult Login() => View(new LoginViewModel()); //както за регистрация същото

        [HttpPost] //и пост заявки за логина
        //async -> означава че този метод може да изчака някаква операция да се извърши без да блокира нишката
        //Task<IActionResult> -> да връщаме резултат но по асинхронен начин 
        public async Task<IActionResult> Login(LoginViewModel model) //
        {
            if (!ModelState.IsValid) //като горе във регистрация
            {
                return View(model);
            }


            // Валидация на потребител с хеширана парола
            var user = _userService.ValidateUser(model.Username, model.Password);

            //проверка дали има такъв потребител и дали паролата съвпада, ако не да ни върне грешката AddModelError
            if (user == null)
            {
                ModelState.AddModelError("", "Invalid username or password");
                return View(model);
            }


            //Claim e информация за потребителя, тук казваме че този потребител има Name= username,
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, model.Username),
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Role, user.Role),
                new Claim("UserId", user.Id.ToString())
            };

            //identity кой е потребителя, връзва identity-то към cookie authentication scheme
            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

            //Principal e целият потребителски обект, .net ще ни сложи Информацията в юзъра
            var principal = new ClaimsPrincipal(identity);

            //сега сървърът(нашето visual studio) ще каже логни този потребител
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

            // Redirect based on role
            if (user.Role == "Admin")
            {
                return RedirectToAction("Index", "Admin");
            }

            //след като потребителя се е логнал отиваме на home
            return RedirectToAction("Index", "Home");
        }

        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Index", "Home");
        }
    }
}
