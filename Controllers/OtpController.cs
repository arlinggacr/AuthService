using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Mail;
using AuthService.DataContext;
using AuthService.Models;
using AuthService.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;

namespace AuthService.Controllers
{
    [ApiController]
    [Route("api")]
    public class AuthController : ControllerBase
    {
        private readonly OtpService _otpService;
        private readonly KeycloakService _keycloakService;
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;

        public AuthController(
            OtpService otpService,
            KeycloakService keycloakService,
            ApplicationDbContext applicationDbContext,
            IConfiguration configuration
        )
        {
            _otpService = otpService;
            _keycloakService = keycloakService;
            _context = applicationDbContext;
            _configuration = configuration;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto loginDto)
        {
            try
            {
                var accessToken = await _keycloakService.LoginAsync(
                    loginDto.Username,
                    loginDto.Password
                );
                if (accessToken == null)
                {
                    return Unauthorized("Invalid credentials.");
                }

                return Ok(new { AccessToken = accessToken });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during login: {ex}");
                return StatusCode(
                    (int)HttpStatusCode.InternalServerError,
                    "An error occurred during login."
                );
            }
        }

        // [Authorize(Policy = "AdminPolicy")]
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDto registerDto)
        {
            try
            {
                var existingUser = await _context
                    .User
                    .FirstOrDefaultAsync(u => u.Email == registerDto.Email);
                if (existingUser != null)
                {
                    return BadRequest("User already exists.");
                }

                var user = new User
                {
                    Username = registerDto.Username,
                    Email = registerDto.Email,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(registerDto.Password),
                    CreatedAt = DateTime.UtcNow
                };

                _context.User.Add(user);
                await _context.SaveChangesAsync();

                var accessToken = HttpContext.Items["AccessToken"] as string;
                Console.WriteLine(accessToken);

                var keycloakUserCreated = await _keycloakService.CreateUserAsync(
                    accessToken,
                    registerDto.Username,
                    registerDto.Email,
                    registerDto.Password
                );

                if (!keycloakUserCreated)
                {
                    _context.User.Remove(user);
                    await _context.SaveChangesAsync();
                    return BadRequest("Failed to create user in Keycloak.");
                }

                return Ok("Registration successful.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during registration: {ex}");
                return StatusCode(
                    (int)HttpStatusCode.InternalServerError,
                    "An error occurred during registration."
                );
            }
        }

        // [Authorize(Policy = "AdminPolicy")]
        [HttpPost("users")]
        public async Task<IActionResult> GetUsers()
        {
            try
            {
                var users = await _keycloakService.GetUsersAsync();
                if (users == null)
                {
                    return NotFound(new { message = "No users found." });
                }

                return Ok(users);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error while retrieving users: {ex.Message}");
                return StatusCode(
                    (int)HttpStatusCode.InternalServerError,
                    "An error occurred while retrieving users."
                );
            }
        }

        [HttpPost("send-otp")]
        public async Task<IActionResult> SendOtp([FromBody] EmailDto emailDto)
        {
            try
            {
                var otp = _otpService.GenerateOtp(emailDto.Email);
                await SendOtpAsync(emailDto.Email, otp);

                return Ok("OTP sent successfully");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error sending OTP: {ex.Message}");
                return StatusCode(
                    (int)HttpStatusCode.InternalServerError,
                    "An error occurred while sending OTP."
                );
            }
        }

        [HttpPost("verify-otp")]
        public async Task<IActionResult> VerifyOtp([FromBody] OtpDto otpDto)
        {
            try
            {
                var otpRecord = await _context
                    .OtpRecord
                    .FirstOrDefaultAsync(
                        o => o.Email == otpDto.Email && o.OtpCode == otpDto.Otp && !o.IsUsed
                    );

                if (otpRecord == null || otpRecord.ExpiredAt < DateTime.UtcNow)
                {
                    return BadRequest("Invalid or expired OTP.");
                }

                // Mark OTP as used
                otpRecord.IsUsed = true;
                await _context.SaveChangesAsync();

                // Mark the user as verified
                var user = await _context.User.FirstOrDefaultAsync(u => u.Email == otpDto.Email);
                if (user != null)
                {
                    user.IsVerified = true;
                    await _context.SaveChangesAsync();
                }

                return Ok("OTP verified successfully.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error verifying OTP: {ex.Message}");
                return StatusCode(
                    (int)HttpStatusCode.InternalServerError,
                    "An error occurred while verifying OTP."
                );
            }
        }

        private async Task SendOtpAsync(string email, string otp)
        {
            try
            {
                var smtpSettings = _configuration.GetSection("Email");

                // Ensure required settings are available
                var fromAddress =
                    smtpSettings["From"]
                    ?? throw new ArgumentNullException("From address is not configured.");
                var displayName =
                    smtpSettings["DisplayName"]
                    ?? throw new ArgumentNullException("Display name is not configured.");
                var smtpServer =
                    smtpSettings["SmtpServer"]
                    ?? throw new ArgumentNullException("SMTP server is not configured.");
                var smtpPortString =
                    smtpSettings["SmtpPort"]
                    ?? throw new ArgumentNullException("SMTP port is not configured.");
                var username =
                    smtpSettings["Username"]
                    ?? throw new ArgumentNullException("SMTP username is not configured.");
                var password =
                    smtpSettings["Password"]
                    ?? throw new ArgumentNullException("SMTP password is not configured.");
                var enableSslString =
                    smtpSettings["EnableSsl"]
                    ?? throw new ArgumentNullException("SMTP EnableSsl is not configured.");

                // Convert SMTP port and EnableSsl values
                if (!int.TryParse(smtpPortString, out var smtpPort))
                {
                    throw new ArgumentException("Invalid SMTP port value.");
                }
                if (!bool.TryParse(enableSslString, out var enableSsl))
                {
                    throw new ArgumentException("Invalid EnableSsl value.");
                }

                var message = new MailMessage
                {
                    From = new MailAddress(fromAddress, displayName),
                    Subject = "Your OTP Code",
                    Body = $"Your OTP code is {otp}",
                    IsBodyHtml = true
                };
                message.To.Add(new MailAddress(email));

                using (var smtpClient = new SmtpClient(smtpServer, smtpPort))
                {
                    smtpClient.Credentials = new NetworkCredential(username, password);
                    smtpClient.EnableSsl = enableSsl;

                    await smtpClient.SendMailAsync(message);
                }
            }
            catch (SmtpException smtpEx)
            {
                // Log SMTP-specific errors
                Console.WriteLine($"SMTP error sending email: {smtpEx.Message}");
                throw;
            }
            catch (Exception ex)
            {
                // Log general errors
                Console.WriteLine($"Error sending email: {ex.Message}");
                throw;
            }
        }
    }
}
