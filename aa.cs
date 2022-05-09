using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using TanJi.Models;
using Microsoft.Extensions.Configuration;
using System.Data;
using System.Text;
using System.Text.Json;
using MySql.Data.MySqlClient;

namespace TanJi.Controllers;

[ApiController]
[Route("draft")]
public class DraftArticleController : ControllerBase
{
    private readonly ILogger<DraftArticleController> _logger;
    private readonly IConfiguration _configuration;
    private SqlFunction sqlFunction;
    private bool isDevelopment;
    private int user_Id;

    public DraftArticleController(IConfiguration configuration, ILogger<DraftArticleController> logger)
    {
        _configuration = configuration;
        sqlFunction = new SqlFunction(_configuration);
        isDevelopment = _configuration.GetValue<bool>("DevelopmentModel");
        _logger = logger;
        user_Id = 1; // user_Id 之後從 token 取出
    }

    /// <summary>
    /// 查詢文章草稿GET:draft/article/list
    /// </summary>
    /// <param name="requestDraftArticleListDto"></param>
    /// <returns></returns>
    /// TODO: user_Id從token取得
    [HttpGet("article/list")]
    public ResponseFormat<IdListObject> GetDraftArticleList([FromQuery] RequestDraftArticleListDto requestDraftArticleListDto)
    {
        ResponseFormat<IdListObject> result = new ResponseFormat<IdListObject>();
        IdListObject idListObj = new IdListObject();
        try
        {
            string sql = $@"
            SELECT `Id`
            FROM `TanJiCenter`.`ArticleTemp`
            WHERE User_Id = @User_Id
            AND `Archive` = 0";
            List<MySqlParameter> parameters = new List<MySqlParameter>();
            parameters.Add(new MySqlParameter("@User_Id", MySqlDbType.Int32) { Value = user_Id });

            //加入條件搜尋
            StringBuilder strCondition = new StringBuilder();
            if (!string.IsNullOrWhiteSpace(requestDraftArticleListDto.KeyWord))
            {
                strCondition.Append($@" 
                AND (`Title` LIKE CONCAT('%', @KeyWord, '%')
                    OR `Description` LIKE CONCAT('%', @KeyWord, '%'))");
                parameters.Add(new MySqlParameter("@KeyWord", MySqlDbType.VarChar, 200) { Value = requestDraftArticleListDto.KeyWord });
            }
            if (requestDraftArticleListDto.Project_Id != 0)
            {
                strCondition.Append($" AND `Projects` REGEXP CONCAT( @Project_Id, '[[:punct:]]')");
                parameters.Add(new MySqlParameter("@Project_Id", MySqlDbType.Int32) { Value = requestDraftArticleListDto.Project_Id });
            }
            if (requestDraftArticleListDto.PrivacyType_Id != 0)
            {
                strCondition.Append($" AND `PrivacyType_Id` = @PrivacyType_Id");
                parameters.Add(new MySqlParameter("@PrivacyType_Id", MySqlDbType.Int32) { Value = requestDraftArticleListDto.PrivacyType_Id });
            }
            if (!requestDraftArticleListDto.StartTime.Equals(DateTime.MinValue))
            {
                strCondition.Append($" AND `PublishTime` >= @StartTime");
                parameters.Add(new MySqlParameter("@StartTime", MySqlDbType.DateTime) { Value = requestDraftArticleListDto.StartTime });
            }
            if (!requestDraftArticleListDto.EndTime.Equals(DateTime.MinValue))
            {
                strCondition.Append($" AND `PublishTime` < @EndTime");
                parameters.Add(new MySqlParameter("@EndTime", MySqlDbType.DateTime) { Value = requestDraftArticleListDto.EndTime });
            }
            if (!requestDraftArticleListDto.UpdateStartTime.Equals(DateTime.MinValue))
            {
                strCondition.Append($" AND `UpdateTime` >= @UpdateStartTime");
                parameters.Add(new MySqlParameter("@UpdateStartTime", MySqlDbType.DateTime) { Value = requestDraftArticleListDto.UpdateStartTime });
            }
            if (!requestDraftArticleListDto.UpdateEndTime.Equals(DateTime.MinValue))
            {
                strCondition.Append($" AND `UpdateTime` < @UpdateEndTime");
                parameters.Add(new MySqlParameter("@UpdateEndTime", MySqlDbType.DateTime) { Value = requestDraftArticleListDto.UpdateEndTime });
            }
            if (!string.IsNullOrWhiteSpace(requestDraftArticleListDto.Order))
            {
                Order order = JsonSerializer.Deserialize<Order>(requestDraftArticleListDto.Order);
                if (order.Direction == 0)
                {
                    strCondition.Append($" ORDER BY `{ order.Sort }` ASC");
                }
                else
                {
                    strCondition.Append($" ORDER BY `{ order.Sort }` DESC");
                }
            }
            else
            {
                strCondition.Append($" ORDER BY `Id` ASC");
            }

            if (strCondition.Length > 0)
            {
                sql = sql + strCondition;
            }

            DataTable dtData = sqlFunction.GetData(sql, parameters.ToArray());
            List<int> idArray = new List<int>();
            for (int i = 0; i < dtData.Rows.Count; i++)
            {
                idArray.Add(dtData.Rows[i].Field<int>("Id"));
            }
            idListObj.Id = idArray.Distinct().ToArray();
        }
        catch (Exception e)
        {
            _logger.LogError($" \n  |ErrorMessage:{e}");
            result.StatusCode = (int)Error.Code.DATABASE_ERROR;
            result.Message = e.Message;
            return result;
        }

        result.StatusCode = (int)Error.Code.SUCCESS;
        result.Message = Error.Message[Error.Code.SUCCESS];
        result.Data = idListObj;
        return result;
    }

    /// <summary>
    /// 查詢文章草稿GET:draft/article/overview
    /// </summary>
    /// <param name="requestOverviewDto">[1]</param>
    /// <returns></returns>
    /// TODO: user_Id從token取得
    [HttpGet("article/overview")]
    public ResponseFormat<List<DraftArticleOverviewDto>> GetDraftArticleOverview([FromQuery] RequestOverviewDto requestOverviewDto)
    {
        ResponseFormat<List<DraftArticleOverviewDto>> result = new ResponseFormat<List<DraftArticleOverviewDto>>();
        List<DraftArticleOverviewDto> draftArticle = new List<DraftArticleOverviewDto>();
        try
        {
            if (!ModelState.IsValid)
            {
                if (isDevelopment)
                {
                    string message = string.Join(" ; ", ModelState.Values
                                    .SelectMany(v => v.Errors)
                                    .Select(e => e.ErrorMessage));
                    result.StatusCode = (int)Error.Code.BAD_REQUEST;
                    result.Message = Error.Message[Error.Code.BAD_REQUEST];
                }
                else
                {
                    result.StatusCode = (int)Error.Code.BAD_REQUEST;
                    result.Message = Error.Message[Error.Code.BAD_REQUEST];
                }
                return result;
            }
            if (!(requestOverviewDto.Id.StartsWith('[') && requestOverviewDto.Id.EndsWith(']') && (requestOverviewDto.Id.Length > 2)))
            {
                result.StatusCode = (int)Error.Code.BAD_REQUEST;
                result.Message = Error.Message[Error.Code.BAD_REQUEST];
                return result;
            }
            string ids = requestOverviewDto.Id.Substring(1, requestOverviewDto.Id.Length - 2);
            List<DraftArticleOverview> draft = new List<DraftArticleOverview>();

            string sql = $@"
            SELECT `Id`
                ,`Title`
                ,`Description`
                ,`User_Id`
                ,`Picture_Id`
                ,`Projects`
                ,`Tags`
                ,`PrivacyType_Id`
                ,`PublishTime`
                ,`UpdateTime`
            FROM `TanJiCenter`.`ArticleTemp`
            WHERE `Id` IN ( { ids } ) ORDER BY FIELD (`Id`,{ ids })
            AND `User_Id` = @User_Id
            AND `Archive` = 0";
            MySqlParameter[] parameters = new[]
            {
                new MySqlParameter("@User_Id", MySqlDbType.Int32) { Value = user_Id }
            };
            DataTable dtData = sqlFunction.GetData(sql, parameters);
            draft = sqlFunction.DataTableToList<DraftArticleOverview>(dtData);
            System.Console.WriteLine(draft);

            string picturePathPrefix = _configuration.GetValue<string>("PicturePathPrefix");

            for (int i = 0; i < draft.Count; i++)
            {
                draftArticle.Add(
                    new DraftArticleOverviewDto
                    {
                        Id = draft[i].Id,
                        Title = draft[i].Title,
                        Image = $"{ picturePathPrefix }{ draft[i].Picture_Id }/{ user_Id }.png",
                        Description = draft[i].Description,
                        User_Id = draft[i].User_Id,
                        Project_Ids = JsonSerializer.Deserialize<List<int>>(draft[i].Projects),
                        Tag_Ids = JsonSerializer.Deserialize<List<int>>(draft[i].Tags),
                        PrivacyType_Id = draft[i].PrivacyType_Id,
                        PublishTime = draft[i].PublishTime,
                        UpdateTime = draft[i].UpdateTime
                    });
            }

        }
        catch (Exception e)
        {
            _logger.LogError($" \n  |ErrorMessage:{e}");
            result.StatusCode = (int)Error.Code.DATABASE_ERROR;
            result.Message = e.Message;
            return result;
        }

        result.StatusCode = (int)Error.Code.SUCCESS;
        result.Message = Error.Message[Error.Code.SUCCESS];
        result.Data = draftArticle;
        return result;
    }

    /// <summary>
    /// 查詢文章草稿GET:draft/article/detail/:id
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    [HttpGet("article/detail/{id}")]
    public ResponseFormat<DraftArticleDetailDto> GetDraftArticleDetail(int id)
    {
        ResponseFormat<DraftArticleDetailDto> result = new ResponseFormat<DraftArticleDetailDto>();
        DraftArticleDetailDto draftArticle = new DraftArticleDetailDto();
        try
        {
            string sql = $@"
            SELECT `Id`
                ,`Title`
                ,`Description`
                ,`Content`
                ,`User_Id`
                ,`Picture_Id`
                ,`Projects`
                ,`Tags`
                ,`PrivacyType_Id`
                ,`PublishTime`
                ,`CreateTime`
                ,`UpdateTime`
            FROM `TanJiCenter`.`ArticleTemp`
            WHERE `Id` = @Id
            AND `User_Id` = @User_Id
            AND `Archive` = 0";

            MySqlParameter[] parameters = new[]
            {
                new MySqlParameter("@Id", MySqlDbType.Int32) { Value = id },
                new MySqlParameter("@User_Id", MySqlDbType.Int32) { Value = user_Id }
            };

            DataTable dtData = sqlFunction.GetData(sql, parameters);
            if (dtData.Rows.Count == 0)
            {
                result.StatusCode = (int)Error.Code.RESULT_NOT_FOUND;
                result.Message = Error.Message[Error.Code.RESULT_NOT_FOUND];
                return result;
            }
            DraftArticleDetail draft = new DraftArticleDetail();
            draft = sqlFunction.DataTableToList<DraftArticleDetail>(dtData)[0];
            string picturePathPrefix = _configuration.GetValue<string>("PicturePathPrefix");

            draftArticle.Id = draft.Id;
            draftArticle.Title = draft.Title;
            draftArticle.Image = $"{ picturePathPrefix }{ draft.Picture_Id }/{ user_Id }.png";
            draftArticle.Description = draft.Description;
            draftArticle.Content = draft.Content;
            draftArticle.User_Id = draft.User_Id;
            draftArticle.Project_Ids = JsonSerializer.Deserialize<List<int>>(draft.Projects);
            draftArticle.Tag_Ids = JsonSerializer.Deserialize<List<int>>(draft.Tags);
            draftArticle.PrivacyType_Id = draft.PrivacyType_Id;
            draftArticle.PublishTime = draft.PublishTime;
            draftArticle.CreateTime = draft.CreateTime;
            draftArticle.UpdateTime = draft.UpdateTime;

        }
        catch (Exception e)
        {
            _logger.LogError($" \n  |ErrorMessage:{e}");
            result.StatusCode = (int)Error.Code.DATABASE_ERROR;
            result.Message = e.Message;
            return result;
        }

        result.StatusCode = (int)Error.Code.SUCCESS;
        result.Message = Error.Message[Error.Code.SUCCESS];
        result.Data = draftArticle;
        return result;
    }

    /// <summary>
    /// 新增文章草稿Post:draft/article
    /// </summary>
    /// <param name="draftArticlePostDto"></param>
    /// <returns></returns>
    [HttpPost("article")]
    public ResponseFormat<string> PostDraftArticle(DraftArticlePostDto draftArticlePostDto)
    {
        ResponseFormat<string> result = new ResponseFormat<string>();

        try
        {
            if (!draftArticlePostDto.PublishTime.Equals(DateTime.MinValue))
            {
                if (DateTime.Compare(draftArticlePostDto.PublishTime, DateTime.UtcNow) < 0)
                {
                    result.StatusCode = (int)Error.Code.PUBLISHTIME_CANNOT_BE_MODIFY;
                    result.Message = Error.Message[Error.Code.PUBLISHTIME_CANNOT_BE_MODIFY];
                    return result;
                }
            }
            else
            {
                draftArticlePostDto.PublishTime = DateTime.UtcNow;
            }
            if (!IsPictureExist(draftArticlePostDto.Picture_Id))
            {
                result.StatusCode = (int)Error.Code.PICTURE_NOT_FOUND;
                result.Message = Error.Message[Error.Code.PICTURE_NOT_FOUND];
                return result;
            }
            if (!IsValidPrivacyType(draftArticlePostDto.PrivacyType_Id))
            {
                result.StatusCode = (int)Error.Code.BAD_REQUEST;
                result.Message = Error.Message[Error.Code.BAD_REQUEST];
                return result;
            }

            foreach (int i in draftArticlePostDto.Tag_Ids)
            {
                if (!IsValidTag(i))
                {
                    result.StatusCode = (int)Error.Code.BAD_REQUEST;
                    result.Message = Error.Message[Error.Code.BAD_REQUEST];
                    return result;
                }
            }

            foreach (int i in draftArticlePostDto.Project_Ids)
            {
                if (!IsValidProject(i))
                {
                    result.StatusCode = (int)Error.Code.BAD_REQUEST;
                    result.Message = Error.Message[Error.Code.BAD_REQUEST];
                    return result;
                }
            }
            string sql = $@"
            INSERT INTO `TanJiCenter`.`ArticleTemp`
                (`Title`
                ,`Description`
                ,`Content`
                ,`User_Id`
                ,`Picture_Id`
                ,`PublishTime`
                ,`PrivacyType_Id`
                ,`Projects`
                ,`Tags`
                ,`Admin_Id`)
            VALUES 
                (@Title
                ,@Description
                ,@Content
                ,@User_Id
                ,@Picture_Id
                ,@PublishTime
                ,@PrivacyType_Id
                ,@Projects
                ,@Tags
                ,@Admin_Id);";
            MySqlParameter[] parameters = new[]
            {
                new MySqlParameter("@Title", MySqlDbType.VarChar, 200) { Value = draftArticlePostDto.Title },
                new MySqlParameter("@Description", MySqlDbType.VarChar, 200) { Value = draftArticlePostDto.Description },
                new MySqlParameter("@Content", MySqlDbType.LongText) { Value = draftArticlePostDto.Content },
                new MySqlParameter("@User_Id", MySqlDbType.Int32) { Value = user_Id },
                new MySqlParameter("@Picture_Id", MySqlDbType.Int32) { Value = draftArticlePostDto.Picture_Id },
                new MySqlParameter("@PublishTime", MySqlDbType.DateTime) { Value = draftArticlePostDto.PublishTime },
                new MySqlParameter("@PrivacyType_Id", MySqlDbType.Int32) { Value = draftArticlePostDto.PrivacyType_Id },
                new MySqlParameter("@Projects", MySqlDbType.VarChar, 100) { Value = JsonSerializer.Serialize(draftArticlePostDto.Project_Ids) },
                new MySqlParameter("@Tags", MySqlDbType.VarChar, 100) { Value = JsonSerializer.Serialize(draftArticlePostDto.Tag_Ids) },
                new MySqlParameter("@Admin_Id", MySqlDbType.Int32) { Value = user_Id }
            };
            sqlFunction.ExecuteSql(sql, parameters);
        }
        catch (Exception e)
        {
            _logger.LogError($" \n  |ErrorMessage:{e}");
            result.StatusCode = (int)Error.Code.DATABASE_ERROR;
            result.Message = e.Message;
            return result;
        }

        result.StatusCode = (int)Error.Code.SUCCESS;
        result.Message = Error.Message[Error.Code.SUCCESS];
        return result;
    }

    /// <summary>
    /// 修改文章草稿Put:draft/article/:id
    /// </summary>
    /// <param name="id"></param>
    /// <param name="draftArticlePutDto"></param>
    /// <returns></returns>
    [HttpPut("article/{id}")]
    public ResponseFormat<string> PutDraftArticle(int id, DraftArticlePutDto draftArticlePutDto)
    {
        ResponseFormat<string> result = new ResponseFormat<string>();
        try
        {
            if (id != draftArticlePutDto.Id)
            {
                result.StatusCode = (int)Error.Code.BAD_REQUEST;
                result.Message = Error.Message[Error.Code.BAD_REQUEST];
                return result;
            }
            if (!IsValidArticleDraft(id))
            {
                result.StatusCode = (int)Error.Code.BAD_REQUEST;
                result.Message = Error.Message[Error.Code.BAD_REQUEST];
                return result;
            }
            if (!IsPictureExist(draftArticlePutDto.Picture_Id))
            {
                result.StatusCode = (int)Error.Code.PICTURE_NOT_FOUND;
                result.Message = Error.Message[Error.Code.PICTURE_NOT_FOUND];
                return result;
            }
            if (!draftArticlePutDto.PublishTime.Equals(DateTime.MinValue))
            {
                if (DateTime.Compare(draftArticlePutDto.PublishTime, DateTime.UtcNow) < 0)
                {
                    result.StatusCode = (int)Error.Code.PUBLISHTIME_CANNOT_BE_MODIFY;
                    result.Message = Error.Message[Error.Code.PUBLISHTIME_CANNOT_BE_MODIFY];
                    return result;
                }
            }
            else
            {
                draftArticlePutDto.PublishTime = DateTime.UtcNow;
            }

            foreach (int i in draftArticlePutDto.Tag_Ids)
            {
                if (!IsValidTag(i))
                {
                    result.StatusCode = (int)Error.Code.BAD_REQUEST;
                    result.Message = Error.Message[Error.Code.BAD_REQUEST];
                    return result;
                }
            }

            foreach (int i in draftArticlePutDto.Project_Ids)
            {
                if (!IsValidProject(i))
                {
                    result.StatusCode = (int)Error.Code.BAD_REQUEST;
                    result.Message = Error.Message[Error.Code.BAD_REQUEST];
                    return result;
                }
            }

            if (!IsValidPrivacyType(draftArticlePutDto.PrivacyType_Id))
            {
                result.StatusCode = (int)Error.Code.BAD_REQUEST;
                result.Message = Error.Message[Error.Code.BAD_REQUEST];
                return result;
            }

            string sql = $@"
            UPDATE `TanJiCenter`.`ArticleTemp`
            SET `Title` = @Title
                , `Description` = @Description
                , `Content` = @Content
                , `Picture_Id` = @Picture_Id
                , `PublishTime` = @PublishTime
                , `PrivacyType_Id` = @PrivacyType_Id
                , `Projects` = @Projects
                , `Tags` = @Tags 
                , `Admin_Id` = @Admin_Id
                , `UpdateTime` = utc_timestamp()
            WHERE `Id` = @Id";
            MySqlParameter[] parameters = new[]
            {
                new MySqlParameter("@Id", MySqlDbType.Int32) { Value = draftArticlePutDto.Id },
                new MySqlParameter("@Title", MySqlDbType.VarChar, 200) { Value = draftArticlePutDto.Title },
                new MySqlParameter("@Description", MySqlDbType.VarChar, 200) { Value = draftArticlePutDto.Description },
                new MySqlParameter("@Content", MySqlDbType.LongText) { Value = draftArticlePutDto.Content },
                new MySqlParameter("@Picture_Id", MySqlDbType.Int32) { Value = draftArticlePutDto.Picture_Id },
                new MySqlParameter("@PublishTime", MySqlDbType.DateTime) { Value = draftArticlePutDto.PublishTime },
                new MySqlParameter("@PrivacyType_Id", MySqlDbType.Int32) { Value = draftArticlePutDto.PrivacyType_Id },
                new MySqlParameter("@Projects", MySqlDbType.VarChar, 100) { Value = JsonSerializer.Serialize(draftArticlePutDto.Project_Ids) },
                new MySqlParameter("@Tags", MySqlDbType.VarChar, 100) { Value = JsonSerializer.Serialize(draftArticlePutDto.Tag_Ids) },
                new MySqlParameter("@Admin_Id", MySqlDbType.Int32) { Value = user_Id }
            };
            sqlFunction.ExecuteSql(sql, parameters);
        }
        catch (Exception e)
        {
            _logger.LogError($" \n  |ErrorMessage:{e}");
            result.StatusCode = (int)Error.Code.DATABASE_ERROR;
            result.Message = e.Message;
            return result;
        }

        result.StatusCode = (int)Error.Code.SUCCESS;
        result.Message = Error.Message[Error.Code.SUCCESS];
        return result;
    }

    /// <summary>
    /// 封存文章草稿 DELETE:draft/article/:id
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    [HttpDelete("article/{id}")]
    public ResponseFormat<string> DeleteDraftArticle(int id)
    {
        ResponseFormat<string> result = new ResponseFormat<string>();
        try
        {
            string sql = @"
            UPDATE `TanjiCenter`.`ArticleTemp`
            SET `Archive` = IF(`Archive` = 1, 0, 1)
            WHERE `Id` = @Id";

            MySqlParameter[] parameters = new[]
            {
                new MySqlParameter("@Id", MySqlDbType.Int32) { Value = id }
            };
            sqlFunction.ExecuteSql(sql, parameters);
        }
        catch (Exception e)
        {
            _logger.LogError($" \n  |ErrorMessage:{e}");
            result.StatusCode = (int)Error.Code.DATABASE_ERROR;
            result.Message = e.Message;
            return result;
        }

        result.StatusCode = (int)Error.Code.SUCCESS;
        result.Message = Error.Message[Error.Code.SUCCESS];
        return result;
    }

    /// <summary>
    /// 檢查 ArticleDraft 是否合理
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    /// //TODO: user_Id從token取得
    private bool IsValidArticleDraft(int id)
    {
        bool boolFlag = false;

        string sql = @"
        SELECT `Id`
        FROM `TanjiCenter`.`ArticleTemp`
        WHERE `Id` = @Id
        AND `Archive` = 0
        AND `User_Id` = @User_Id";
        MySqlParameter[] parameters = new[]
        {
            new MySqlParameter("@Id", MySqlDbType.Int32) { Value = id },
            new MySqlParameter("@User_Id", MySqlDbType.Int32) { Value = user_Id }
        };

        DataTable dtData = sqlFunction.GetData(sql, parameters);

        if (dtData.Rows.Count == 1)
        {
            boolFlag = true;
        }

        return boolFlag;
    }

    /// <summary>
    /// 檢查 Picture 是否存在
    /// </summary>
    /// <param name="pictureId"></param>
    /// <returns></returns>
    //TODO: user_Id從token取得
    private bool IsPictureExist(int pictureId)
    {
        bool boolFlag = false;

        string sql = @"
        SELECT A.`Id`
        FROM `TanjiCenter`.`User-Picture-Relation` AS A
        INNER JOIN `TanjiCenter`.`Picture` AS B
        ON A.Picture_Id = B.Id
        WHERE A.`User_Id` = @User_Id
        AND A.`Picture_Id` = @Picture_Id
        AND A.`Archive` = 0
        AND B.IsProject = 0";

        MySqlParameter[] parameters = new[]
        {
            new MySqlParameter("@User_Id", MySqlDbType.Int32) { Value = user_Id },
            new MySqlParameter("@Picture_Id", MySqlDbType.Int32) { Value = pictureId }
        };

        DataTable dtData = sqlFunction.GetData(sql, parameters);

        if (dtData.Rows.Count == 1)
        {
            boolFlag = true;
        }
        return boolFlag;
    }

    /// <summary>
    /// 檢查 Tag 是否合理
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    private bool IsValidTag(int id)
    {
        bool boolFlag = false;

        string sql = @"
        SELECT `Id`
        FROM `TanjiCenter`.`Tag`
        WHERE `Id` = @Id
        AND `Archive` = 0";
        MySqlParameter[] parameters = new[]
        {
            new MySqlParameter("@Id", MySqlDbType.Int32) { Value = id },
        };

        DataTable dtData = sqlFunction.GetData(sql, parameters);

        if (dtData.Rows.Count == 1)
        {
            boolFlag = true;
        }

        return boolFlag;
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
        SELECT `Id`
        FROM `TanjiCenter`.`Project`
        WHERE `Id` = @Id
        AND `Archive` = 0";
        MySqlParameter[] parameters = new[]
        {
            new MySqlParameter("@Id", MySqlDbType.Int32) { Value = id },
        };

        DataTable dtData = sqlFunction.GetData(sql, parameters);

        if (dtData.Rows.Count == 1)
        {
            boolFlag = true;
        }

        return boolFlag;
    }

    /// <summary>
    /// 檢查 PrivacyType 是否合理
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    private bool IsValidPrivacyType(int id)
    {
        bool boolFlag = false;

        string sql = @"
        SELECT `Id`
        FROM `TanjiCenter`.`PrivacyType`
        WHERE `Id` = @Id
        AND `Archive` = 0";
        MySqlParameter[] parameters = new[]
        {
            new MySqlParameter("@Id", MySqlDbType.Int32) { Value = id },
        };

        DataTable dtData = sqlFunction.GetData(sql, parameters);

        if (dtData.Rows.Count == 1)
        {
            boolFlag = true;
        }

        return boolFlag;
    }

}