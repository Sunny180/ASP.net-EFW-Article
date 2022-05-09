using System.Text.Json;
using SMTW_Management.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Linq;
using System.Security.Claims;
using Microsoft.Extensions.Configuration;
using System.Text;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;
using System.Data;
using Microsoft.Data.SqlClient;

namespace SMTW_Management
{
    public class AuthorizeAttribute : TypeFilterAttribute
    {
        public AuthorizeAttribute(params string[] claim) : base(typeof(AuthorizeFilter))
        {
            Arguments = new object[] { claim };
        }
    }

    public class AuthorizeFilter : IAuthorizationFilter
    {
        private readonly IConfiguration _configuration;
        readonly string[] _claim;
        private SqlFunction sqlFunction;

        public AuthorizeFilter(IConfiguration configuration, params string[] claim)
        {
            _configuration = configuration;
            _claim = claim;
            sqlFunction = new SqlFunction(_configuration);
        }

        public void OnAuthorization(AuthorizationFilterContext context)
        {
            string account = (string)context.HttpContext.Request.Headers["Account"];
            string password = (string)context.HttpContext.Request.Headers["Password"];
            string token = (string)context.HttpContext.Request.Headers["Authorization"].FirstOrDefault()?.Split(" ").Last();
            ResponseFormat<string> result = new ResponseFormat<string>();

            if (account != null && password != null)
            {
                User user = IsUserExist(account, password);
                if (user != null)
                {
                    context.HttpContext.Items["User_Id"] = user.Id;
                    context.HttpContext.Items["PowerType_Id"] = user.PowerType_Id;
                    context.HttpContext.Items["Name"] = user.Name;
                    context.HttpContext.Items["Account"] = user.Account;
                }
                else
                {
                    result.StatusCode = -2;
                    result.Message = Error.ErrorMessage[-2];
                    context.Result = new JsonResult(result);
                }
            }
            else
            {
                // 確認token是否存在
                if (token == null)
                {
                    result.StatusCode = -8;
                    result.Message = Error.ErrorMessage[-8];
                    context.Result = new JsonResult(result);
                    return;
                }

                try
                {
                    // 驗證token是否合法
                    JwtSecurityTokenHandler tokenHandler = new JwtSecurityTokenHandler();

                    byte[] signKey = Encoding.UTF8.GetBytes(_configuration.GetValue<string>("JwtSettings:SignKey"));

                    TokenValidationParameters tokenValidationParameters = new TokenValidationParameters
                    {
                        // 一般我們都會驗證 Issuer
                        ValidateIssuer = true,
                        ValidIssuer = _configuration.GetValue<string>("JwtSettings:Issuer"),
                        // 通常不太需要驗證 Audience
                        ValidateAudience = false,
                        // 一般我們都會驗證 Token 的有效期間
                        ValidateLifetime = true,
                        // 如果 Token 中包含 key 才需要驗證，一般都只有簽章而已
                        ValidateIssuerSigningKey = false,
                        IssuerSigningKey = new SymmetricSecurityKey(signKey)
                    };

                    SecurityToken securityToken;

                    ClaimsPrincipal principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out securityToken);

                    // 驗證是否重複登入
                    string sql = $@"
                    SELECT [User_Id]
                    FROM [SMTW].[dbo].[Token]
                    WHERE [Token] = @Token";

                    SqlParameter[] sqlParameters = new[]
                    {
                        new SqlParameter("@Token",token),
                    };

                    DataTable dtData = sqlFunction.GetData(sql, sqlParameters);

                    if (dtData.Rows.Count != 1)
                    {
                        result.StatusCode = -9;
                        result.Message = Error.ErrorMessage[-9];
                        context.Result = new JsonResult(result);
                        return;
                    }

                    // 將UserInfo資料塞入HttpContext.Items
                    string userInfoStr = principal.Claims.FirstOrDefault(p => p.Type == "UserInfo").Value;
                    UserInfo userInfo = JsonSerializer.Deserialize<UserInfo>(userInfoStr);
                    context.HttpContext.Items["User_Id"] = userInfo.Id;
                    context.HttpContext.Items["PowerType_Id"] = userInfo.PowerType_Id;
                    context.HttpContext.Items["Name"] = userInfo.Name;
                    context.HttpContext.Items["Account"] = userInfo.Account;

                }
                catch (SecurityTokenInvalidSignatureException)
                {
                    result.StatusCode = -1;
                    result.Message = Error.ErrorMessage[-1];
                    context.Result = new JsonResult(result);
                }
                catch (SecurityTokenExpiredException)
                {
                    result.StatusCode = -10;
                    result.Message = Error.ErrorMessage[-10];
                    context.Result = new JsonResult(result);
                }
            }
        }

        private User IsUserExist(string account, string password)
        {
            User user = new User();
            string sql = $@"
            SELECT [Id]
            ,[Name]
            ,[Account]
            ,[Password]
            ,[PowerType_Id]
            ,[Admin_Id]
            ,[CreateTime]
            ,[UpdateTime]
            ,[Archive]
            FROM [SMTW].[dbo].[User]
            WHERE [Account] = @Account AND [Password] = @Password";

            SqlParameter[] sqlParameters = new[]
            {
                new SqlParameter("@Account",account),
                new SqlParameter("@Password",password),
            };

            DataTable dtData = sqlFunction.GetData(sql, sqlParameters);

            if (dtData.Rows.Count == 1)
            {
                user = sqlFunction.DataTableToList<User>(dtData)[0];
                return user;
            }

            return null;
        }
    }
}