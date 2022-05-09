using Microsoft.Extensions.Configuration;
using System.Security.Claims;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System;
using SMTW_Management.Models;
using System.Text.Json;

namespace SMTW_Management
{
    public class JwtHelpers
    {
        private readonly IConfiguration Configuration;

        public JwtHelpers(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public string GenerateToken(int id, string name, string account, int powerType_Id)
        {
            string issuer = Configuration.GetValue<string>("JwtSettings:Issuer");
            string signKey = Configuration.GetValue<string>("JwtSettings:SignKey");
            int expireMinutes = Configuration.GetValue<int>("JwtSettings:ExpireMinutes");

            // 設定要加入到 JWT Token 中的聲明資訊(Claims)
            List<Claim> claims = new List<Claim>();

            // 在 RFC 7519 規格中(Section#4)，總共定義了 7 個預設的 Claims，我們應該只用的到兩種！
            //claims.Add(new Claim(JwtRegisteredClaimNames.Iss, issuer));
            claims.Add(new Claim(JwtRegisteredClaimNames.Sub, name)); // User.Identity.Name
            claims.Add(new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())); // JWT ID            
            //claims.Add(new Claim(JwtRegisteredClaimNames.Aud, "The Audience"));
            //claims.Add(new Claim(JwtRegisteredClaimNames.Exp, DateTimeOffset.UtcNow.AddMinutes(30).ToUnixTimeSeconds().ToString()));
            //claims.Add(new Claim(JwtRegisteredClaimNames.Nbf, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString())); // 必須為數字
            //claims.Add(new Claim(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString())); // 必須為數字


            // 你可以自行擴充 "roles" 加入登入者該有的角色
            //claims.Add(new Claim("roles", "Admin"));
            //claims.Add(new Claim("roles", "Users"));

            UserInfo userInfo = new UserInfo();
            userInfo.Id = id;
            userInfo.Name = name;
            userInfo.Account = account;
            userInfo.PowerType_Id = powerType_Id;

            string userInfoStr = JsonSerializer.Serialize(userInfo);
            claims.Add(new Claim("UserInfo", userInfoStr));

            ClaimsIdentity userClaimsIdentity = new ClaimsIdentity(claims);

            // 建立一組對稱式加密的金鑰，主要用於 JWT 簽章之用
            SymmetricSecurityKey securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signKey));

            // HmacSha256 有要求必須要大於 128 bits，所以 key 不能太短，至少要 16 字元以上
            // https://stackoverflow.com/questions/47279947/idx10603-the-algorithm-hs256-requires-the-securitykey-keysize-to-be-greater
            SigningCredentials signingCredentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256Signature);

            // 建立 SecurityTokenDescriptor
            SecurityTokenDescriptor tokenDescriptor = new SecurityTokenDescriptor
            {
                Issuer = issuer,
                //Audience = issuer, // 由於你的 API 受眾通常沒有區分特別對象，因此通常不太需要設定，也不太需要驗證
                //NotBefore = DateTime.Now, // 預設值就是 DateTime.Now
                //IssuedAt = DateTime.Now, // 預設值就是 DateTime.Now
                Subject = userClaimsIdentity,
                Expires = DateTime.Now.AddMinutes(expireMinutes),
                SigningCredentials = signingCredentials
            };

            // 產出所需要的 JWT securityToken 物件，並取得序列化後的 Token 結果(字串格式)
            JwtSecurityTokenHandler tokenHandler = new JwtSecurityTokenHandler();
            SecurityToken securityToken = tokenHandler.CreateToken(tokenDescriptor);
            string serializeToken = tokenHandler.WriteToken(securityToken);

            return serializeToken;
        }

    }
}