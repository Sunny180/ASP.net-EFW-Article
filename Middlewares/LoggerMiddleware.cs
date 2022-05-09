using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Data.SqlClient;

namespace SMTW_Management
{
    public class LoggerMiddleware
    {
        private readonly IConfiguration _configuration;
        private readonly RequestDelegate _next;

        public LoggerMiddleware(IConfiguration configuration, RequestDelegate next)
        {
            _configuration = configuration;
            _next = next;
        }

        public async Task Invoke(HttpContext context)
        {
            try
            {
                context.Request.EnableBuffering();

                // Request
                DateTime requestTime = DateTime.UtcNow;
                string method = context.Request.Method.ToString();
                string path = context.Request.Path.ToString();
                string query = context.Request.QueryString.ToString();
                string requestContent;
                context.Request.Body.Seek(0, SeekOrigin.Begin);
                using (StreamReader reader = new StreamReader(context.Request.Body, encoding: Encoding.UTF8, detectEncodingFromByteOrderMarks: false, bufferSize: 8192, leaveOpen: true))
                {
                    requestContent = await reader.ReadToEndAsync();
                    context.Request.Body.Seek(0, SeekOrigin.Begin);
                }

                // Response
                string responseContent;
                Stream originalBodyStream = context.Response.Body;
                using (MemoryStream fakeResponseBody = new MemoryStream())
                {
                    context.Response.Body = fakeResponseBody;

                    await _next(context);

                    fakeResponseBody.Seek(0, SeekOrigin.Begin);
                    using (StreamReader reader = new StreamReader(fakeResponseBody))
                    {
                        responseContent = await reader.ReadToEndAsync();
                        fakeResponseBody.Seek(0, SeekOrigin.Begin);
                        await fakeResponseBody.CopyToAsync(originalBodyStream);
                    }
                }
                DateTime responseTime = DateTime.UtcNow;

                SqlFunction sqlFunction = new SqlFunction(_configuration);

                string sql = $@"
                INSERT INTO [SMTW].[dbo].[SMTW_Management_Logger]
                    ([RequestTime]
                    ,[ResponseTime]
                    ,[Method]
                    ,[Path]
                    ,[RequestBody]
                    ,[ResponseBody]
                    ,[TimeTaken])
                VALUES
                    (@RequestTime
                    ,@ResponseTime
                    ,@Method
                    ,@Path
                    ,@RequestBody 
                    ,@ResponseBody
                    ,DATEDIFF(millisecond,@RequestTime,@ResponseTime)
                    )";

                SqlParameter[] sqlParameters = new[]
                {
                    new SqlParameter("@RequestTime",requestTime),
                    new SqlParameter("@ResponseTime",responseTime),
                    new SqlParameter("@Method",method),
                    new SqlParameter("@Path",path),
                    new SqlParameter("@RequestBody",query+requestContent),
                    new SqlParameter("@ResponseBody","")
                };

                sqlFunction.ExecuteSql(sql, sqlParameters);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }
    }
}