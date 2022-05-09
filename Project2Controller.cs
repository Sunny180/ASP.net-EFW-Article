using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.AspNetCore.Http; //IHttpContextAccessor
using Microsoft.Extensions.Configuration; //IConfiguration
using System.Collections.Generic; //IEnumerable
using SMTW_Management.Models;
using System.Linq; //SelectMany
using System; //Exception
using System.Data; //DataTable //SqlDbType
using System.Text; //StringBuilder
using System.Text.Json; //JsonSerializer

namespace SMTW_Management.Controllers
{
    [Authorize]
    [ApiController]
    [Route("project2")]
    public class Project2Controller : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private SqlFunction sqlFunction;
        private bool isDevelopment;
        private int admin_Id;
        public Project2Controller(IConfiguration configuration, IHttpContextAccessor httpContextAccessor)
        {
            _configuration = configuration;
            sqlFunction = new SqlFunction(_configuration);
            isDevelopment = _configuration.GetValue<bool>("DevelopmentModel");
            admin_Id = (int)httpContextAccessor.HttpContext.Items["User_Id"];
        }

        /// <summary>
        /// 查詢專案GET:project2/list
        /// </summary>
        /// <param name="requestProject2ListDto"></param>
        /// <returns></returns>
        [HttpGet("list")]
        public ResponseFormat<IdListObject> GetProjectList([FromQuery] RequestProject2ListDto requestProject2ListDto)
        {
            ResponseFormat<IdListObject> result = new ResponseFormat<IdListObject>();
            IdListObject idListObj = new IdListObject();
            try
            {
                if (!ModelState.IsValid)
                {
                    if (isDevelopment)
                    {
                        string message = string.Join(" ; ", ModelState.Values
                                        .SelectMany(v => v.Errors)
                                        .Select(e => e.ErrorMessage));
                        result.StatusCode = -3;
                        result.Message = message;
                    }
                    else
                    {
                        result.StatusCode = -3;
                        result.Message = Error.ErrorMessage[-3];
                    }
                    return result;
                }
                string sql = $@"
                SELECT Project.[Id]
                FROM [SMTW].[dbo].[Project] AS Project";

                //加入條件搜尋
                StringBuilder strCondition = new StringBuilder();
                if (requestProject2ListDto.Catalog_Id != null)
                {
                    strCondition.Append($@"
                        INNER JOIN [SMTW].[dbo].[Project-Catalog-Relation] AS ProjectCatalog
                        ON Project.[Id] = ProjectCatalog.[Project_Id]
                        WHERE ProjectCatalog.[Catalog_Id] = { requestProject2ListDto.Catalog_Id }
                        AND ProjectCatalog.[Archive] = 0");
                }
                if (requestProject2ListDto.User_Id != null)
                {
                    strCondition.Append($" AND Project.[User_Id] = { requestProject2ListDto.User_Id }");
                }
                if (!string.IsNullOrWhiteSpace(requestProject2ListDto.Title))
                {
                    strCondition.Append($" AND Project.[Title] like N'%{ requestProject2ListDto.Title }%'");
                }
                if (!requestProject2ListDto.StartTime.Equals(DateTimeOffset.MinValue))
                {
                    strCondition.Append($" AND Project.[PublishTime] >= '{ requestProject2ListDto.StartTime }'");
                }
                if (!requestProject2ListDto.EndTime.Equals(DateTimeOffset.MinValue))
                {
                    strCondition.Append($" AND Project.[PublishTime] < '{ requestProject2ListDto.EndTime }'");
                }
                if (requestProject2ListDto.Archive != null)
                {
                    strCondition.Append($" AND Project.[Archive] = { requestProject2ListDto.Archive }");
                }
                if (!string.IsNullOrWhiteSpace(requestProject2ListDto.Order))
                {
                    Order order = JsonSerializer.Deserialize<Order>(requestProject2ListDto.Order);
                    if (order.Direction == 0)
                    {
                        strCondition.Append($" Order By Project.[{ order.Sort }] ASC");
                    }
                    else
                    {
                        strCondition.Append($" Order By Project.[{ order.Sort }] DESC");
                    }
                }
                else
                {
                    strCondition.Append(" Order By Project.[Id] ASC");
                }

                if (strCondition.Length > 0)
                {
                    strCondition.Replace(" AND", " WHERE", 0, 5);
                    sql = sql + strCondition;
                }
                DataTable dtData = sqlFunction.GetData(sql);
                List<int> idArray = new List<int>();
                for (int i = 0; i < dtData.Rows.Count; i++)
                {
                    idArray.Add(dtData.Rows[i].Field<int>("Id"));
                }
                idListObj.Id = idArray.Distinct().ToArray();
            }

            catch (Exception e)
            {
                result.StatusCode = -1000;
                result.Message = e.Message;
                return result;
            }

            result.StatusCode = 0;
            result.Message = Error.ErrorMessage[0];
            result.Data = idListObj;
            return result;
        }

        /// <summary>
        /// 查詢專案GET:project2/overview
        /// </summary>
        /// <param name="requestOverviewDto">[1]</param>
        /// <returns></returns>
        [HttpGet("overview")]
        public ResponseFormat<List<Project2OverviewDto>> GetProjectOverview([FromQuery] RequestOverviewDto requestOverviewDto)
        {
            ResponseFormat<List<Project2OverviewDto>> result = new ResponseFormat<List<Project2OverviewDto>>();
            List<Project2OverviewDto> project = new List<Project2OverviewDto>();
            try
            {
                if (!ModelState.IsValid)
                {
                    if (isDevelopment)
                    {
                        string message = string.Join(" ; ", ModelState.Values
                                        .SelectMany(v => v.Errors)
                                        .Select(e => e.ErrorMessage));
                        result.StatusCode = -3;
                        result.Message = message;
                    }
                    else
                    {
                        result.StatusCode = -3;
                        result.Message = Error.ErrorMessage[-3];
                    }
                    return result;
                }
                if (!(requestOverviewDto.Id.StartsWith('[') && requestOverviewDto.Id.EndsWith(']') && (requestOverviewDto.Id.Length > 3)))
                {
                    result.StatusCode = -3;
                    result.Message = Error.ErrorMessage[-3];
                    return result;
                }
                string ids = requestOverviewDto.Id.Substring(1, requestOverviewDto.Id.Length - 2);

                string sql = $@"
                SELECT [Id]
                    ,[Title]
                    ,[User_Id]
                    ,[PublishTime]
                    ,[Admin_Id]
                    ,[CreateTime]
                    ,[UpdateTime]
                    ,[Archive]
                FROM [SMTW].[dbo].[Project]";

                //加入條件搜尋
                StringBuilder strCondition = new StringBuilder();
                if (!string.IsNullOrWhiteSpace(requestOverviewDto.Id))
                {
                    strCondition.Append(" AND [Id] IN (" + ids + ") order by charindex(','+rtrim(cast([Id] as varchar(10)))+',','," + ids + ",')");
                }
                if (strCondition.Length > 0)
                {
                    strCondition.Replace(" AND", " WHERE", 0, 5);
                    sql = sql + strCondition;
                }

                DataTable dtData = sqlFunction.GetData(sql);
                project = sqlFunction.DataTableToList<Project2OverviewDto>(dtData);
            }
            catch (Exception e)
            {
                result.StatusCode = -1000;
                result.Message = e.Message;
                return result;
            }

            result.StatusCode = 0;
            result.Message = Error.ErrorMessage[0];
            result.Data = project;
            return result;
        }

        /// <summary>
        /// 查詢專案GET:project2/detail/:id
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpGet("detail/{id}")]
        public ResponseFormat<Project2DetailDto> GetProject(int id)
        {
            ResponseFormat<Project2DetailDto> result = new ResponseFormat<Project2DetailDto>();
            Project2DetailDto project = new Project2DetailDto();
            try
            {
                string sql = $@"
                SELECT [Id]
                    ,[Title]
                    ,[Description]
                    ,[Content]
                    ,[User_Id]
                    ,[PublishTime]
                    ,[Admin_Id]
                    ,[CreateTime]
                    ,[UpdateTime]
                    ,[Archive]
                FROM [SMTW].[dbo].[Project]
                WHERE [Id] = { id }";

                DataTable dtData = sqlFunction.GetData(sql);
                if (dtData.Rows.Count != 1)
                {
                    result.StatusCode = -4;
                    result.Message = Error.ErrorMessage[-4];
                    return result;
                }
                project = sqlFunction.DataTableToList<Project2DetailDto>(dtData)[0];

                string sql2 = $@"
                SELECT ProjectCatalog.[Catalog_Id]
                FROM [SMTW].[dbo].[Project] AS Project
                INNER JOIN [SMTW].[dbo].[Project-Catalog-Relation] AS ProjectCatalog
                ON Project.[Id] = ProjectCatalog.[Project_Id]
                WHERE Project.[Id] = { id }
                AND ProjectCatalog.[Archive] = 0";

                DataTable dtData2 = sqlFunction.GetData(sql2);
                List<int> catalogs = new List<int>();
                for (int i = 0; i < dtData2.Rows.Count; i++)
                {
                    catalogs.Add(dtData2.Rows[i].Field<int>("Catalog_Id"));
                }
                project.Catalogs = catalogs;
            }
            catch (Exception e)
            {
                result.StatusCode = -1000;
                result.Message = e.Message;
                return result;
            }

            result.StatusCode = 0;
            result.Message = Error.ErrorMessage[0];
            result.Data = project;
            return result;
        }

        /// <summary>
        /// 新增專案POST:project2
        /// </summary>
        /// <param name="projectPost"></param>
        /// <returns></returns>
        [HttpPost]
        public ResponseFormat<string> PostProject(Project2PostDto projectPost)
        {
            ResponseFormat<string> result = new ResponseFormat<string>();
            try
            {
                if (!ModelState.IsValid)
                {
                    if (isDevelopment)
                    {
                        string message = string.Join(" ; ", ModelState.Values
                                        .SelectMany(v => v.Errors)
                                        .Select(e => e.ErrorMessage));
                        result.StatusCode = -3;
                        result.Message = message;
                    }
                    else
                    {
                        result.StatusCode = -3;
                        result.Message = Error.ErrorMessage[-3];
                    }
                    return result;
                }
                if (!projectPost.PublishTime.Equals(DateTimeOffset.MinValue))
                {
                    if (DateTimeOffset.Compare(projectPost.PublishTime, DateTimeOffset.UtcNow) < 0)
                    {
                        result.StatusCode = -23;
                        result.Message = Error.ErrorMessage[-23];
                        return result;
                    }
                }
                else
                {
                    projectPost.PublishTime = DateTimeOffset.UtcNow;
                }

                string sql = $@"
                INSERT INTO [SMTW].[dbo].[Project] 
                    ([Title]
                    , [Description]
                    , [Content]
                    , [User_Id] 
                    , [PublishTime] 
                    , [Picture_Id]
                    , [Admin_Id]) 
                VALUES 
                    (@Title
                    , @Description
                    , @Content
                    , @User_Id
                    , @PublishTime
                    , @Picture_Id
                    , @Admin_Id);
                SELECT SCOPE_IDENTITY();";
                SqlParameter[] sqlParameters = new SqlParameter[]
                {
                    new SqlParameter("@Title", SqlDbType.NVarChar, 200) { Value = projectPost.Title },
                    new SqlParameter("@Description", SqlDbType.NVarChar, 200) { Value = projectPost.Description },
                    new SqlParameter("@Content", SqlDbType.NVarChar, -1) { Value = projectPost.Content },
                    new SqlParameter("@User_Id", SqlDbType.Int) { Value = projectPost.User_Id },
                    new SqlParameter("@PublishTime", SqlDbType.DateTimeOffset) { Value = projectPost.PublishTime },
                    new SqlParameter("@Picture_Id", SqlDbType.Int) { Value = 54 },
                    new SqlParameter("@Admin_Id", SqlDbType.Int) { Value = admin_Id }
                };
                int projectId = sqlFunction.GetId(sql, sqlParameters);
                for (int i = 0; i < projectPost.Catalogs.Count; i++)
                {
                    string sql2 = $@"
                    INSERT INTO [SMTW].[dbo].[Project-Catalog-Relation] 
                        ([Project_Id]
                        , [Catalog_Id]
                        , [Admin_Id]) 
                    VALUES 
                        (@Project_Id
                        , @Catalog_Id
                        , @Admin_Id)";
                    SqlParameter[] sqlParameters2 = new SqlParameter[]
                    {
                        new SqlParameter("@Project_Id", SqlDbType.Int) { Value = projectId },
                        new SqlParameter("@Catalog_Id", SqlDbType.Int) { Value = projectPost.Catalogs[i]},
                        new SqlParameter("@Admin_Id", SqlDbType.Int) { Value = admin_Id }
                    };
                    sqlFunction.ExecuteSql(sql2, sqlParameters2);
                }
            }
            catch (Exception e)
            {
                result.StatusCode = -1000;
                result.Message = e.Message;
                return result;
            }
            result.StatusCode = 0;
            result.Message = Error.ErrorMessage[0];
            return result;
        }

        /// <summary>
        /// 修改專案PUT:project2/:id
        /// </summary>
        /// <param name="id"></param>
        /// <param name="projectPut"></param>
        /// <returns></returns>
        [HttpPut("{id}")]
        public ResponseFormat<string> PutProject(int id, Project2PutDto projectPut)
        {
            ResponseFormat<string> result = new ResponseFormat<string>();
            try
            {
                if (!ModelState.IsValid)
                {
                    if (isDevelopment)
                    {
                        string message = string.Join(" ; ", ModelState.Values
                                        .SelectMany(v => v.Errors)
                                        .Select(e => e.ErrorMessage));
                        result.StatusCode = -3;
                        result.Message = message;
                    }
                    else
                    {
                        result.StatusCode = -3;
                        result.Message = Error.ErrorMessage[-3];
                    }
                    return result;
                }
                if (id != projectPut.Id)
                {
                    result.StatusCode = -3;
                    result.Message = Error.ErrorMessage[-3];
                    return result;
                }
                if (!IsValidProject(id))
                {
                    result.StatusCode = -4;
                    result.Message = Error.ErrorMessage[-4];
                    return result;
                }
                if (!projectPut.PublishTime.Equals(DateTimeOffset.MinValue))
                {
                    if (DateTimeOffset.Compare(GetDBPublishTime(id), DateTimeOffset.UtcNow) < 0)
                    {
                        result.StatusCode = -23;
                        result.Message = Error.ErrorMessage[-23];
                        return result;
                    }
                    if (DateTimeOffset.Compare(projectPut.PublishTime, DateTimeOffset.UtcNow) < 0)
                    {
                        result.StatusCode = -23;
                        result.Message = Error.ErrorMessage[-23];
                        return result;
                    }
                }
                else
                {
                    projectPut.PublishTime = GetDBPublishTime(id);
                }
                string sql3 = @"
                UPDATE [SMTW].[dbo].[Project] 
                SET [User_Id] = @User_Id
                    ,[Title] = COALESCE(@Title,Title)
                    ,[Description] = COALESCE(@Description,Description)
                    ,[Content] = COALESCE(@Content,Content)
                    ,[PublishTime] = @PublishTime
                    ,[Picture_Id] = @Picture_Id
                    ,[Admin_Id] = @Admin_Id 
                    ,[UpdateTime] = GETUTCDATE()
                WHERE [Id] = @Id";
                SqlParameter[] sqlParameters3 = new SqlParameter[]
                {
                    new SqlParameter("@Id", SqlDbType.Int) { Value = id },
                    new SqlParameter("@Title",SqlDbType.NVarChar,200) {Value = (object)projectPut.Title?? DBNull.Value},
                    new SqlParameter("@Description", SqlDbType.NVarChar, 200) { Value = (object)projectPut.Description?? DBNull.Value },
                    new SqlParameter("@Content", SqlDbType.NVarChar, -1) { Value = (object)projectPut.Content?? DBNull.Value },
                    new SqlParameter("@User_Id", SqlDbType.Int) { Value = projectPut.User_Id },
                    new SqlParameter("@PublishTime", SqlDbType.DateTimeOffset) { Value = projectPut.PublishTime },
                    new SqlParameter("@Picture_Id", SqlDbType.Int) { Value = 54 },
                    new SqlParameter("@Admin_Id", SqlDbType.Int) { Value = admin_Id }
                };
                sqlFunction.ExecuteSql(sql3, sqlParameters3);
                if (projectPut.Catalogs != null)
                {
                    string sql4 = $@"
                    UPDATE [SMTW].[dbo].[Project-Catalog-Relation]
                    SET [Archive] = 1
                        , [UpdateTime] = GETUTCDATE()
                    WHERE [Project_Id] = @Id";
                    SqlParameter[] sqlParameters4 = new SqlParameter[]
                    {
                        new SqlParameter("@Id", SqlDbType.Int) { Value = id }
                    };
                    sqlFunction.ExecuteSql(sql4, sqlParameters4);

                    for (int i = 0; i < projectPut.Catalogs.Count; i++)
                    {
                        string sql5 = $@"
                        IF EXISTS 
                            (SELECT [Id] 
                            FROM [SMTW].[dbo].[Project-Catalog-Relation]
                            WHERE [Project_Id] = @Id
                            AND [Catalog_Id] = @Catalog_Id)
                        BEGIN
                            UPDATE [SMTW].[dbo].[Project-Catalog-Relation] 
                            SET [Archive] = 0
                                , [UpdateTime] = GETUTCDATE() 
                            WHERE [Project_Id] = @Id
                            AND [Catalog_Id] = @Catalog_Id 
                        END
                        ELSE
                        BEGIN
                            INSERT INTO [SMTW].[dbo].[Project-Catalog-Relation] 
                                ([Project_Id]
                                , [Catalog_Id]
                                , [Admin_Id])
                            VALUES 
                                (@Project_Id
                                , @Catalog_Id
                                , @Admin_Id) 
                        END";
                        SqlParameter[] sqlParameters5 = new SqlParameter[]
                        {
                            new SqlParameter("@Id", SqlDbType.Int) { Value = id },
                            new SqlParameter("@Project_Id", SqlDbType.Int) { Value = id },
                            new SqlParameter("@Catalog_Id", SqlDbType.Int) { Value = projectPut.Catalogs[i]},
                            new SqlParameter("@Admin_Id", SqlDbType.Int) { Value = admin_Id }
                        };
                        sqlFunction.ExecuteSql(sql5, sqlParameters5);
                    }
                }
            }
            catch (Exception e)
            {
                result.StatusCode = -1000;
                result.Message = e.Message;
                return result;
            }
            result.StatusCode = 0;
            result.Message = Error.ErrorMessage[0];
            return result;
        }

        /// <summary>
        /// 封存專案DELETE:project2/:id
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpDelete("{id}")]
        public ResponseFormat<string> DeleteProject(int id)
        {
            ResponseFormat<string> result = new ResponseFormat<string>();
            try
            {
                if (!IsValidProject(id))
                {
                    result.StatusCode = -4;
                    result.Message = Error.ErrorMessage[-4];
                    return result;
                }
                string sql = @"
                UPDATE [SMTW].[dbo].[Project]
                SET 
                    [Archive] = CASE
                        WHEN [Archive] = 0 THEN 1
                        ELSE 0
                        END
                    ,[Admin_Id] = @Admin_Id
                    ,[UpdateTime] = GETUTCDATE()
                WHERE [Id] = @Id

                DECLARE @Archive INT

                SELECT @Archive = [Archive] 
                FROM [SMTW].[dbo].[Project]
                WHERE [Id] = @Id

                IF EXISTS @Archive = 0
                BEGIN
                    UPDATE [SMTW].[dbo].[Project-Catalog-Relation] AS ProjectCatalog
                    SET [Archive] = Catalog.[Archive]
                        ,[Admin_Id] = @Admin_Id
                        ,[UpdateTime] = GETUTCDATE()
                    FROM [SMTW].[dbo].[Catalog] AS Catalog
                    WHERE Catalog.[Id] = ProjectCatalog.[Catalog_Id]
                    AND ProjectCatalog.[Project_Id] = @Id  
                END
                ELSE
                BEGIN
                    UPDATE [SMTW].[dbo].[Project-Catalog-Relation]
                    SET [Archive] = 1
                        ,[Admin_Id] = @Admin_Id
                        ,[UpdateTime] = GETUTCDATE()
                    WHERE [Project_Id] = @Id
                END";
                SqlParameter[] sqlParameters = new SqlParameter[]
                {
                    new SqlParameter("@Id", SqlDbType.Int) { Value = id },
                    new SqlParameter("@Admin_Id", SqlDbType.Int) { Value = admin_Id }
                };
                sqlFunction.ExecuteSql(sql, sqlParameters);
            }
            catch (Exception e)
            {
                result.StatusCode = -1000;
                result.Message = e.Message;
                return result;
            }
            result.StatusCode = 0;
            result.Message = Error.ErrorMessage[0];
            return result;
        }

        /// <summary>
        /// 檢查 Project 是否合理
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        private bool IsValidProject(int id)
        {
            bool boolFlag = false;
            string sql = @"
            SELECT [Id] 
            FROM [SMTW].[dbo].[Project] 
            WHERE [Id] = @Id";
            SqlParameter[] sqlParameters = new SqlParameter[]
            {
                new SqlParameter("@Id", SqlDbType.Int) { Value = id },
            };
            DataTable dtData = sqlFunction.GetData(sql, sqlParameters);
            if (dtData.Rows.Count == 1)
            {
                boolFlag = true;
            }
            return boolFlag;
        }

        /// <summary>
        /// 取得原本 DB 的 PublishTime
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        private DateTimeOffset GetDBPublishTime(int id)
        {
            string sql = @"
            SELECT [PublishTime] 
            FROM [SMTW].[dbo].[Project] 
            WHERE [Id] = @Id";
            SqlParameter[] sqlParameters = new SqlParameter[]
            {
                new SqlParameter("@Id", SqlDbType.Int) { Value = id },
            };
            DataTable dtData = sqlFunction.GetData(sql, sqlParameters);
            DateTimeOffset DBPublishTime = dtData.Rows[0].Field<DateTimeOffset>("PublishTime");
            return (DBPublishTime);
        }
    }
}