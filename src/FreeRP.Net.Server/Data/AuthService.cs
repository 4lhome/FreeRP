using FreeRP.GrpcService.Core;
using FreeRP.Net.Server.Data;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace FreeRP.Net.Server.Data
{
    public partial class AuthService
    {
        public User User { get; set; } = new();
        public List<Role> Roles { get; set; } = [];
        public bool IsAdmin { get; set; }

        private const string ClaimName = "name";
        private const string ClaimAdminName = "admin";
        private readonly FrpSettings _frpSettings;
        private readonly IFrpDataService _appData;

        private readonly JwtSecurityTokenHandler _tokenHandler;
        private readonly TokenValidationParameters _tokenValidations;
        private readonly SymmetricSecurityKey _tokenKey;

        public AuthService(IFrpDataService appData, FrpSettings frpSettings)
        {
            _appData = appData;
            _frpSettings = frpSettings;

            _tokenKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_frpSettings.JwtKey));
            _tokenHandler = new JwtSecurityTokenHandler();
            _tokenValidations = new TokenValidationParameters
            {
                ValidateIssuer = false,
                ValidateAudience = false,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = _tokenKey,
                ClockSkew = TimeSpan.FromMinutes(1)
            };
        }

        public void SetUser(ClaimsPrincipal claims)
        {
            Roles.Clear();
            var name = claims.FindFirst(x => x.Type == ClaimName);

            if (claims.FindFirst(x => x.Type == ClaimAdminName) is Claim c && c.Value == _frpSettings.Admin)
            {
                IsAdmin = true;
                User = new User();
                return;
            }

            if (name != null)
            {
                User = _appData.UserGetByEmail(name.Value);
                Roles.AddRange(_appData.UserGetRoles(User));
            }
        }

        public User? GetUserFromToken(string token)
        {
            try
            {
                var claims = _tokenHandler.ValidateToken(token, _tokenValidations, out _);
                var name = claims.FindFirst(x => x.Type == ClaimName);
                if (name != null)
                {
                    return _appData.UserGetByEmail(name.Value);
                }
            }
            catch (Exception)
            {
            }
            return null;
        }

        public string GenerateJwtToken(User user, bool isAdmin = false)
        {
            List<Claim> c = new()
            {
                new Claim(ClaimName, user.Email)
            };

            if (isAdmin)
                c.Add(new Claim(ClaimAdminName, _frpSettings.Admin));

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(c),
                Expires = DateTime.UtcNow.AddHours(_frpSettings.JwtExpireHours),
                SigningCredentials = new SigningCredentials(_tokenKey, SecurityAlgorithms.HmacSha256Signature)
            };

            var token = _tokenHandler.CreateToken(tokenDescriptor);
            return _tokenHandler.WriteToken(token);
        }

        public string GenerateJwtToken(User user, DateTime dt, bool isAdmin = false)
        {
            List<Claim> c = new()
            {
                new Claim(ClaimName, user.Email)
            };

            if (isAdmin)
                c.Add(new Claim(ClaimAdminName, _frpSettings.Admin));

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(c),
                Expires = dt,
                SigningCredentials = new SigningCredentials(_tokenKey, SecurityAlgorithms.HmacSha256Signature)
            };

            var token = _tokenHandler.CreateToken(tokenDescriptor);
            return _tokenHandler.WriteToken(token);
        }

        public static ErrorType ValidatePassword(string password, int passwortLength)
        {
            if (string.IsNullOrWhiteSpace(password) || password.Length < passwortLength)
            {
                return ErrorType.ErrorPasswordToShort;
            }

            if (RegexNumber().IsMatch(password) == false)
            {
                return ErrorType.ErrorPasswordNumber;
            }

            if (RegexUpperChar().IsMatch(password) == false)
            {
                return ErrorType.ErrorPasswordUpperChar;
            }

            if (RegexLowerChar().IsMatch(password) == false)
            {
                return ErrorType.ErrorPasswordLowerChar;
            }

            if (RegexSymbols().IsMatch(password) == false)
            {
                return ErrorType.ErrorPasswordSymbols;
            }

            return ErrorType.ErrorNone;
        }

        [GeneratedRegex("[0-9]{1,}")]
        private static partial Regex RegexNumber();

        [GeneratedRegex("[A-Z]{1,}")]
        private static partial Regex RegexUpperChar();

        [GeneratedRegex("[a-z]{1,}")]
        private static partial Regex RegexLowerChar();
        [GeneratedRegex("[!@#$%^&*()_+=\\[{\\]};:<>|./?,-]{1,}")]
        private static partial Regex RegexSymbols();
    }
}
