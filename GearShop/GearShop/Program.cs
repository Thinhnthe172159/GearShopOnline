using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using GearShop.Data;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllersWithViews();
builder.Services.AddControllers();
builder.Services.AddDbContext<ApplicationDbContext>(options => options.UseSqlServer(builder.Configuration.GetConnectionString("Default")));

builder.Services.AddDefaultIdentity<IdentityUser>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequiredLength = 6;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = true; 
    options.Password.RequireLowercase = true;
})
           .AddRoles<IdentityRole>()
           .AddEntityFrameworkStores<ApplicationDbContext>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapRazorPages();
//using (var scope = app.Services.CreateScope())
//{
//    var services = scope.ServiceProvider;
//    var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
//    DataSeeder.SeedRoles(roleManager);
//}
app.Run();

public static class DataSeeder
{
    public static void SeedRoles(RoleManager<IdentityRole> roleManager)
    {
        if (!roleManager.RoleExistsAsync("Admin").Result)
        {
            var role = new IdentityRole { Name = "Admin", NormalizedName = "ADMIN" };
            roleManager.CreateAsync(role).Wait();
        }

        if (!roleManager.RoleExistsAsync("Staff").Result)
        {
            var role = new IdentityRole { Name = "Staff", NormalizedName = "STAFF" };
            roleManager.CreateAsync(role).Wait();
        }

        if (!roleManager.RoleExistsAsync("Customer").Result)
        {
            var role = new IdentityRole { Name = "Customer", NormalizedName = "CUSTOMER" };
            roleManager.CreateAsync(role).Wait();
        }
    }
}
