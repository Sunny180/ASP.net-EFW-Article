using System.Collections.Generic;
using TanJi.Models;
public class Error
{
    public enum Code
    {
        SUCCESS = 0,
        TOKEN_INVALID = -1,
        PERMISSION_DENIED = -2,
        BAD_REQUEST = -3,
        RESULT_NOT_FOUND = -4,
        ACCOUNT_OR_PASSWORD_ERROR = -5,
        OLD_PASSWORD_ERROR = -6,
        CHECK_PASSWORD_ERROR = -7,
        TOKEN_NOT_FOUND = -8,
        REPEAT_LOGIN = -9,
        LOGIN_EXPIRED = -10,
        ARCHIVE_ACCOUNT = -11,
        MAILBOX_EXISTED = -14,
        ACCOUNT_EXISTED = -15,
        PICTURE_NOT_FOUND = -16,
        YOUTUBE_ID_NOT_FOUND = -17,
        COURSE_CANNOT_BE_MODIFY = -18,
        KEY_ERROR = -19,
        COURSE_CANNOT_BE_ACHIVED = -20,
        TAG_EXISTED = -21,
        TAG_CANNOT_BE_ACHIVED = -22,
        PUBLISHTIME_CANNOT_BE_MODIFY = -23,
        YOUTUBE_CHANNEL_ID_NOT_FOUND = -24,
        VIDEO_CANNOT_BE_ADD_IN_THE_CHANNEL = -25,
        CHANNEL_CANNOT_BE_ACHIVED = -26,
        CHANNEL_USED = -27,
        CHANNEL_EXISTED = -28,
        USER_NOT_EXISTED = -29,
        PICTURE_NAME_EXISTED = -30,
        DATABASE_ERROR = -1000,
        INTERNAL_SERVER_ERROR = -1001
    }

    public static Dictionary<Code, string> Message = new Dictionary<Code, string>()
    {
        {Code.SUCCESS, "成功"},
        {Code.TOKEN_INVALID, "token不合法"}, // 身分不合法
        {Code.PERMISSION_DENIED, "沒有權限"}, // 沒有權限
        {Code.BAD_REQUEST, "Request參數未填or有問題"}, // Request參數未填or有問題
        {Code.RESULT_NOT_FOUND, "查無結果"}, // 查無結果
        {Code.ACCOUNT_OR_PASSWORD_ERROR, "帳號or密碼錯誤"}, // 帳號or密碼錯誤
        {Code.OLD_PASSWORD_ERROR, "舊密碼錯誤"}, // 舊密碼錯誤
        {Code.CHECK_PASSWORD_ERROR, "確認密碼錯誤"}, // 確認密碼錯誤
        {Code.TOKEN_NOT_FOUND, "Token未存在"}, // Token未存在
        {Code.REPEAT_LOGIN, "重複登入"}, // 重複登入
        {Code.LOGIN_EXPIRED, "Login過期"}, // Login過期
        {Code.ARCHIVE_ACCOUNT, "帳號已封存"}, // 帳號已封存
        {Code.MAILBOX_EXISTED, "此信箱已註冊"}, // 此信箱已註冊
        {Code.ACCOUNT_EXISTED, "此帳號已註冊"}, // 此帳號已註冊
        {Code.PICTURE_NOT_FOUND, "圖片庫沒有此照片"}, //圖片庫沒有此照片
        {Code.YOUTUBE_ID_NOT_FOUND, "沒有此Youtube Id"}, //沒有此Youtube Id
        {Code.COURSE_CANNOT_BE_MODIFY, "此課程無法被修改"}, // 此課程無法被修改
        {Code.KEY_ERROR, "密鑰錯誤"}, //密鑰錯誤
        {Code.COURSE_CANNOT_BE_ACHIVED, "此課程無法被封存"}, // 此課程無法被封存
        {Code.TAG_EXISTED, "已有此Tag"}, // 已有此Tag
        {Code.TAG_CANNOT_BE_ACHIVED, "此Tag無法被封存"}, // 此Tag無法被封存
        {Code.PUBLISHTIME_CANNOT_BE_MODIFY, "無法修改發布時間"}, // 無法修改發布時間
        {Code.YOUTUBE_CHANNEL_ID_NOT_FOUND, "沒有此Youtube Channel Id"}, // 沒有此Youtube Channel Id
        {Code.VIDEO_CANNOT_BE_ADD_IN_THE_CHANNEL, "此Youtube Id無法新增於這個頻道"}, // 此Youtube Id無法新增於這個頻道
        {Code.CHANNEL_CANNOT_BE_ACHIVED, "此頻道無法被封存"}, // 此課程無法被封存
        {Code.CHANNEL_USED, "此頻道目前正在被使用"}, // 此頻道目前正在被使用
        {Code.CHANNEL_EXISTED, "此使用者已新增過此頻道"}, // 此使用者已新增過此頻道
        {Code.DATABASE_ERROR, "資料庫異常"}, // 資料庫異常
        {Code.INTERNAL_SERVER_ERROR, "HTTP 服務器發送請求異常"}, // HTTP 服務器發送請求異常
        {Code.USER_NOT_EXISTED, "該使用者不存在"}, // 該使用者不存在
        {Code.PICTURE_NAME_EXISTED, "圖片名稱已存在"} // 圖片名稱已存在
    };

}



