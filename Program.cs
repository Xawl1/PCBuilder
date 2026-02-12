using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using PCBuilder.Data;
using PCBuilder.Services;
using PCBuilder.Models;

//Създаване на цялото приложение
//builder - обект, през него ние си конфигурираме нещата за сайта
//(базата, аутентикацията за логване и др такива)
var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews(); //казва това приложение изполва mvc (Controler + View)

builder.Services.AddDbContext<AppDbContext>(options =>
{
    //взима connection stringa за връзката към базата ни от appsetting.json-a
    var cs = builder.Configuration.GetConnectionString("DefaultConnection");
    //свързва с mysql, този ред казва използвай Mysql, aвтоматично ми разпознай версия на сървъра
    options.UseMySql(cs, ServerVersion.AutoDetect(cs));
});

builder.Services.AddScoped<UserService>();

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.AccessDeniedPath = "/Account/Login";
    });

builder.Services.AddAuthorization();

var app = builder.Build(); //създава самото приложение на база на това което имаме по дефоут при създаване на проекта




// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts(); // ако сме качили сайта да не се ползва през visual studito, а си е в интернет че ще ползва https за следващите 30 дни,
                   // това не е важно за момента, просто дефоутно ви го създава проекта
}

app.UseHttpsRedirection();  //-> Ако някой отвори сайта http://localhost:7122/ -> автоматично ще ви пренасочи към https://localhost:7122/
app.UseStaticFiles(); // за да може да ни зареждат файловете които не са динамично генерирани(класовете които ползваме)
app.UseRouting(); // приложението ни ще се ориентира с този ред и ще разбира към кой url и към controller да ти отиде

app.UseAuthentication();
app.UseAuthorization(); // това ни трябва за логин на един на един потребител и да може работин 


//регистрира/мапва всички статични файловр (css, js, избображения) според начина по който сме си натройли проекта 
app.MapControllerRoute( // дефиницията за route(маршрута) mvc котролерите ни 
    name: "default",
    pattern: "{controller=RentACar}/{action=Index}/{id?}")
    .WithStaticAssets(); // свързва ви route, идеята му е правилно да ви работят ресурсите(класовте които правим според конфигурацията)


app.Run(); //стартира приложението и започва да чака заяки 
