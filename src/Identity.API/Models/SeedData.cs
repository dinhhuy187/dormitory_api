using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Identity.API.Models
{
    public static class SeedData
    {
        public static async Task InitializeAsync(IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();

            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            
            // 1. Tạo Roles nếu chưa có
            string[] roles = { "Student", "Manager", "SeniorManager", "Admin" };
            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(new IdentityRole { Name = role });
                }
            }

            // 2. Tạo tài khoản Admin mặc định
            string adminEmail = "admin@ktx.edu.vn";
            if (await userManager.FindByEmailAsync(adminEmail) == null)
            {
                var adminUser = new ApplicationUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    FullName = "System Administrator",
                    IsActive = true,
                    EmailConfirmed = true // Bỏ qua bước bắt xác thực email lúc ban đầu
                };

                // Tạo user với mật khẩu mạnh
                var result = await userManager.CreateAsync(adminUser, "Test1234@");

                if (result.Succeeded)
                {
                    // Phân quyền Admin cho tài khoản này
                    await userManager.AddToRoleAsync(adminUser, "Admin");
                }
            }
        }
    }
}
