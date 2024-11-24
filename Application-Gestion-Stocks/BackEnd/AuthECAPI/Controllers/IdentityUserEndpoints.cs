using AuthECAPI.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Web;

namespace AuthECAPI.Controllers
{
    // Modèles pour la gestion des utilisateurs
    public class UserRegistrationModel
    {
        public string Email { get; set; }
    }

    public class LoginModel
    {
        public string Email { get; set; }
        public string Password { get; set; }
    }

    public static class IdentityUserEndpoints
    {
        public static IEndpointRouteBuilder MapIdentityUserEndpoints(this IEndpointRouteBuilder app)
        {
            app.MapPost("/signup", PasswordReset);
            app.MapPost("/signin", SignIn);
            return app;
        }

        private static async Task<IResult> PasswordReset(
            UserManager<AppUser> userManager,
            [FromBody] UserRegistrationModel userRegistrationModel)
        {
            // Vérification si l'utilisateur existe
            var user = await userManager.FindByEmailAsync(userRegistrationModel.Email);
            if (user == null)
            {
                return Results.BadRequest(new { message = "Email not found." });
            }

            // Génération du token de réinitialisation
            var token = await userManager.GeneratePasswordResetTokenAsync(user);

            // Encodage de l'URL pour inclure le token et l'email
            var encodedToken = HttpUtility.UrlEncode(token);
            string baseUrl = Environment.GetEnvironmentVariable("FRONTEND_URL") ?? "http://localhost:4200";
            var resetLink = $"{baseUrl}/singup?token={encodedToken}&email={HttpUtility.UrlEncode(user.Email)}";

            // Placeholder pour le débogage ou implémenter un service d'envoi d'emails
            Console.WriteLine($"Password reset link: {resetLink}");

            return Results.Ok(new { message = "Password reset link sent." });
        }

        private static async Task<IResult> SignIn(
            UserManager<AppUser> userManager,
            [FromBody] LoginModel loginModel,
            IOptions<AppSettings> appSettings)
        {
            // Vérification des informations d'authentification
            var user = await userManager.FindByEmailAsync(loginModel.Email);
            if (user != null && await userManager.CheckPasswordAsync(user, loginModel.Password))
            {
                // Création de la clé de signature
                var signInKey = new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(appSettings.Value.JWTSecret)
                );

                // Configuration du token
                var tokenDescriptor = new SecurityTokenDescriptor
                {
                    Subject = new ClaimsIdentity(new[]
                    {
                        new Claim("UserID", user.Id.ToString())
                    }),
                    Expires = DateTime.UtcNow.AddDays(10),
                    SigningCredentials = new SigningCredentials(
                        signInKey,
                        SecurityAlgorithms.HmacSha256Signature
                    )
                };

                // Génération du token
                var tokenHandler = new JwtSecurityTokenHandler();
                var securityToken = tokenHandler.CreateToken(tokenDescriptor);
                var token = tokenHandler.WriteToken(securityToken);

                return Results.Ok(new { token });
            }
            else
            {
                return Results.BadRequest(new { message = "Username or password is incorrect." });
            }
        }
    }
}

