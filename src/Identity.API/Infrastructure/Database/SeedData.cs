using Bogus;
using Identity.API.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Identity.API.Infrastructure.Database
{
    public static class SeedData
    {
        public static async Task InitializeAsync(IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();

            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher<ApplicationUser>>();

            
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

            var studentRole = await roleManager.FindByNameAsync("Student");
            if (studentRole == null) return;

            bool hasStudents = await dbContext.UserRoles.AnyAsync(ur => ur.RoleId == studentRole.Id);
            if (hasStudents) return; // Nếu đã seed rồi thì bỏ qua

            Console.WriteLine("Bắt đầu Seed 20.812 tài khoản sinh viên...");

            // Băm mật khẩu chung cho tất cả sinh viên MỘT LẦN DUY NHẤT để tối ưu hiệu năng
            var defaultPasswordHash = passwordHasher.HashPassword(null!, "Test1234@");

            int studentIdCounter = 23000000;

            var faker = new Faker<ApplicationUser>("vi")
                .RuleFor(u => u.Id, f => Guid.NewGuid().ToString()) // Identity mặc định dùng string cho Id
                .RuleFor(u => u.UserName, f => $"SV{studentIdCounter++}")
                .RuleFor(u => u.NormalizedUserName, (f, u) => u.UserName!.ToUpper()) // Bắt buộc phải có để Login
                .RuleFor(u => u.Email, (f, u) => $"{u.UserName}@student.edu.vn")
                .RuleFor(u => u.NormalizedEmail, (f, u) => u.Email!.ToUpper())       // Bắt buộc phải có để Login
                .RuleFor(u => u.FullName, f => $"{f.Name.LastName()} {f.Name.FirstName()}")
                .RuleFor(u => u.IsActive, true)
                .RuleFor(u => u.EmailConfirmed, true)
                .RuleFor(u => u.PasswordHash, defaultPasswordHash)
                .RuleFor(u => u.SecurityStamp, f => Guid.NewGuid().ToString())       // Bắt buộc của Identity
                .RuleFor(u => u.ConcurrencyStamp, f => Guid.NewGuid().ToString());   // Bắt buộc của Identity

            int targetCount = 22000;
            int batchSize = 5000;

            for (int i = 0; i < targetCount; i += batchSize)
            {
                int countToGenerate = Math.Min(batchSize, targetCount - i);
                var students = faker.Generate(countToGenerate);

                // Tạo danh sách ánh xạ Role cho đợt sinh viên này
                var userRoles = students.Select(u => new IdentityUserRole<string>
                {
                    UserId = u.Id,
                    RoleId = studentRole.Id
                }).ToList();

                // Insert trực tiếp bằng EF Core
                await dbContext.Users.AddRangeAsync(students);
                await dbContext.UserRoles.AddRangeAsync(userRoles);
                
                await dbContext.SaveChangesAsync();

                Console.WriteLine($"Đã seed {i + countToGenerate}/{targetCount} sinh viên...");
            }

            Console.WriteLine("Hoàn tất Seed Data cho IdentityDb.");
        }
    }
}
