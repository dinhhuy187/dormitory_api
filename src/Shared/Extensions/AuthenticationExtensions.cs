using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace Shared.Extensions
{
    public static class AuthenticationExtensions
    {
        public static IServiceCollection AddCustomJwtAuthentication(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            var secretKeyString = configuration["JWT_SECRET"];
            if (string.IsNullOrEmpty(secretKeyString))
            {
                throw new ArgumentNullException(nameof(secretKeyString), "JWT_SECRET is missing from environment variables or config.");
            }
            var secretKey = Encoding.UTF8.GetBytes(secretKeyString!);

            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
                .AddJwtBearer(options =>
                {
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey = new SymmetricSecurityKey(secretKey),
                        ValidateIssuer = true,
                        ValidIssuer = configuration["JWT_ISSUER"],
                        ValidateAudience = true,
                        ValidAudience = configuration["JWT_AUDIENCE"],
                        ValidateLifetime = true,
                        ClockSkew = TimeSpan.Zero
                    };
                });
            return services;
        }
    }
}
