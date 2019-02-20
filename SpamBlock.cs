////////////////////////////////////////////////
// © https://github.com/badhitman - Telegram @fakegov 
////////////////////////////////////////////////
using MultiTool.LibraryHmacHttp;
using MultiTool.LibraryLog;
using MultiTool;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.Serialization;
using System.Text;
using System.Text.RegularExpressions;
using TelegramBot.TelegramMetadata;
using TelegramBot.TelegramMetadata.AvailableTypes;
using TelegramBot.TelegramMetadata.GettingUpdates;

namespace SpamBlockTelegram
{
    public enum ChatTypes { Private, Group, Supergroup, Channel, Error }

    /// <summary>
    /// Типы данных сообщения
    /// </summary>
    public enum TelegramDataTypes
    {
        /// <summary>
        /// Признак того что к сообщению прикреплена локация
        /// </summary>
        Location,

        /// <summary>
        /// Признак того что в сообщении есть локация
        /// </summary>
        LocationText,

        /// <summary>
        /// Признак того что к сообщению прикреплено фото
        /// </summary>
        Photo,

        /// <summary>
        /// Признак того что к сообщению прикреплено видео
        /// </summary>
        Video,

        /// <summary>
        /// Признак того что к сообщению прикреплено аудио
        /// </summary>
        Audio,

        /// <summary>
        /// Признак того что к сообщению прикреплен документ
        /// </summary>
        Document,

        /// <summary>
        /// Признак того что у сообщения назначено описание медиа-данных
        /// </summary>
        Caption,

        /// <summary>
        /// Признак того что в сообщении есть текст
        /// </summary>
        Text,

        /// <summary>
        /// Признак того данное сообщение-уведомление говорит о том что в групу добавлен новый учасник
        /// </summary>
        NewChatMembers
    }

    [DataContract]
    public class SpamBlock
    {
        public MethodsTelegramClass telegram_client;
        public LogClass Log = new LogClass() { MinimalLogStatusWrite = LogStatusEnum.Alarm };

        ////////////////////////////////////////////////////////
        [DataMember]
        public List<string> block_strings = new List<string>();
        [DataMember]
        public List<string> block_regexes = new List<string>();
        [DataMember]
        public List<string> alert_strings = new List<string>();
        [DataMember]
        public List<string> alert_regexes = new List<string>();

        /// <summary>
        /// Разрешение автоматически сразу удалять сообщения, в которых будет обнаружены данные из Block списков.
        /// Сообщения администраторов не удаляются, но уведомления рассылаются
        /// </summary>
        public bool AutoDelete = true;

        /// <summary>
        /// Состав значимых данных в сообщении
        /// </summary>
        public List<TelegramDataTypes> CompositionTypes;

        ////////////////////////////////////////////////////////
        [DataMember]
        private string bot_admin_username = "";
        /// <summary>
        /// Username администратора бота (может управлять ботом в реиме чата с ботом)
        /// </summary>
        public string BotAdminUsername
        {
            get
            {
                return bot_admin_username;
            }
            set
            {
                bot_admin_username = value.Trim();
                //
                if (bot_admin_username.IndexOf("@") == 0)
                    bot_admin_username = bot_admin_username.Substring(1);
            }
        }

        /// <summary>
        /// Адрес HTTP сервиса для формирования ответа на входящее сообщение
        /// </summary>
        public string WebhookAddress { get; private set; }
        string hmac_key;
        string hmac_secret;
        public void SetWebhook(string Url, string hmac_api_auth_key, string hmac_api_auth_secret)
        {
            Log.WriteLine("Вызов метода установки Webhook");
            WebhookAddress = Url;
            hmac_key = hmac_api_auth_key;
            hmac_secret = hmac_api_auth_secret;
        }

        /// <summary>
        /// Полное/подробное наименование отправителя
        /// </summary>
        public string FullNameSender { get { return " [id:" + IncUpdate.message.from.id + "] [username:" + IncUpdate.message.from.username + "] [first name:" + IncUpdate.message.from.first_name + "] [last name:" + IncUpdate.message.from.last_name + "]"; } }

        /// <summary>
        /// Признак того: является ли отправитель текущего сообщения администратором или не является
        /// </summary>
        public bool IsAdminSender { get { return ChatAdmins.Exists(x => x.user.id == IncUpdate.message.from.id) || (!string.IsNullOrEmpty(bot_admin_username) && IncUpdate.message.from.username == bot_admin_username); } }

        /// <summary>
        /// Тип контекста/переписки (частный, группа, супергруппа, группа ...)
        /// </summary>
        public ChatTypes ChatType;

        public SpamBlock(string telegram_api_key_bot)
        {
            telegram_client = new MethodsTelegramClass(telegram_api_key_bot);
            if (telegram_client.Me is null)
                Log.WriteLine("API bot-key invalid", LogStatusEnum.Alarm);
            else
                Log.WriteLine("SpamBlock-Bot is ready", LogStatusEnum.Happi);
        }

        public List<ChatMemberClass> ChatAdmins;

        /// <summary>
        /// Список совпадений
        /// </summary>
        public List<SpamMatch> ScanSpamMatches;

        public TelegramCommandClass TelegramCommand { get; private set; }

        [DataMember]
        public Update IncUpdate;

        /// <summary>
        /// Отчёт по результатам сканирования
        /// </summary>
        public string MatchesReport
        {
            get
            {
                string ret_val = IsAdminSender ? "Сообщение не удалено!" + Environment.NewLine : "";

                foreach (SpamMatch ssm in ScanSpamMatches.OrderBy(x => x.LevelMatch.ToString("g") + x.PlaceMatch.ToString("g") + x.TypeMatch.ToString("g")))
                {
                    ret_val += ssm.PlaceMatch.ToString("g") + " -> " + ssm.LevelMatch.ToString("g") + ". " + ssm.TypeMatch.ToString("g") + " -> " + ssm.find_data + Environment.NewLine;
                }

                if (string.IsNullOrEmpty(ret_val))
                    ret_val = "<в сообщении ни чего подозрительного не найдено>";
                else
                    ret_val = ret_val.Trim();

                ret_val += Environment.NewLine + Environment.NewLine + "Отправитель: " + FullNameSender + Environment.NewLine;

                if (!string.IsNullOrEmpty(IncUpdate.message.text))
                    ret_val += Environment.NewLine + "Сообщение: " + Environment.NewLine + "~ ~ ~ ~ ~ ~ ~ ~" + Environment.NewLine + IncUpdate.message.text + Environment.NewLine + "~ ~ ~ ~ ~ ~ ~ ~" + Environment.NewLine;

                if (!string.IsNullOrEmpty(IncUpdate.message.caption))
                    ret_val += Environment.NewLine + "Заголовок: " + Environment.NewLine + "~ ~ ~ ~ ~ ~ ~ ~" + Environment.NewLine + IncUpdate.message.caption + Environment.NewLine + "~ ~ ~ ~ ~ ~ ~ ~" + Environment.NewLine;

                return ret_val.Trim();
            }
        }

        /// <summary>
        /// Сканировать входящий Telegram.Update
        /// </summary>
        public void UpdateNew()
        {
            if (IncUpdate is null)
                return;
            ScanSpamMatches = new List<SpamMatch>();
            ChatAdmins = new List<ChatMemberClass>();
            CompositionTypes = new List<TelegramDataTypes>();

            #region Определение типа чата
            ChatType = ChatTypes.Error;
            foreach (ChatTypes c in Enum.GetValues(typeof(ChatTypes)))
            {
                if (c.ToString("g").ToLower() == IncUpdate.message.chat.type.ToLower())
                {
                    ChatType = c;
                    break;
                }
            }
            #endregion

            ////////////////////////////////////////////////////////
            #region Если сообщение из группы или супергруппы, то определяем администраторов чата
            if ((ChatType == ChatTypes.Group || ChatType == ChatTypes.Supergroup) && ChatAdmins.Count == 0)
            {
                ChatMemberClass[] chat_admins = telegram_client.getChatAdministrators(IncUpdate.message.chat.id.ToString());
                if (!(chat_admins is null))
                    ChatAdmins.AddRange(chat_admins.Where(x => !x.user.is_bot));
            }
            #endregion

            #region Определение состава данных сообщения
            if (!(IncUpdate.message is null))
            {
                if (!string.IsNullOrEmpty(IncUpdate.message.text))
                {
                    CompositionTypes.Add(TelegramDataTypes.Text);
                    if(Regex.IsMatch(IncUpdate.message.text, @"^\s*\d{1,2}\.\d{1,6}\s*,\s*d{1,2}\.\d{1,6}\s*$"))
                        CompositionTypes.Add(TelegramDataTypes.LocationText);
                }
                if (!string.IsNullOrEmpty(IncUpdate.message.caption))
                    CompositionTypes.Add(TelegramDataTypes.Caption);

                if (!(IncUpdate.message.new_chat_members is null))
                    CompositionTypes.Add(TelegramDataTypes.NewChatMembers);

                if (!(IncUpdate.message.location is null))
                    CompositionTypes.Add(TelegramDataTypes.Location);

                if (!(IncUpdate.message.photo is null))
                    CompositionTypes.Add(TelegramDataTypes.Photo);

                if (!(IncUpdate.message.document is null))
                    CompositionTypes.Add(TelegramDataTypes.Document);

                if (!(IncUpdate.message.video is null))
                    CompositionTypes.Add(TelegramDataTypes.Video);
            }

            #endregion

            if (ChatType == ChatTypes.Private && !string.IsNullOrEmpty(IncUpdate.message.text))
            {
                TelegramCommand = new TelegramCommandClass(IncUpdate.message.text);
            }
            else
            if (ChatType == ChatTypes.Group || ChatType == ChatTypes.Supergroup)
            {
                #region Если это группа/чат
                Log.WriteLine("Сканирование сообщения на спам");

                SpamScanner scanMessage = new SpamScanner(this);
                ScanSpamMatches = scanMessage.scan_matches;
                #endregion
            }
        }

        /// <summary>
        /// Сканировать входящий Telegram.Update
        /// </summary>
        public void UpdateNew(Update u)
        {
            IncUpdate = u;
            UpdateNew();

            Log.WriteLine((IsAdminSender ? "Administrator" : "User") + FullNameSender, IsAdminSender ? LogStatusEnum.Notice : LogStatusEnum.Norma);

            if (ChatType == ChatTypes.Group || ChatType == ChatTypes.Supergroup)
            {
                Regex rex = new Regex(@"^/\w[\w\d_]+@" + telegram_client.Me.username + "$", RegexOptions.IgnoreCase);
                if (CompositionTypes.Exists(x => x == TelegramDataTypes.Text) && rex.IsMatch(IncUpdate.message.text))
                {
                    telegram_client.sendMessage(IncUpdate.message.chat.id.ToString(), "Для общения с ботом напишите ему напрямую @" + telegram_client.Me.username, "", false, false, IncUpdate.message.chat.id);
                    return;
                }
                if (ScanSpamMatches.Count > 0)
                {
                    NotifyAdmins(MatchesReport);

                    if (AutoDelete && !IsAdminSender && ScanSpamMatches.Exists(x => x.LevelMatch == LevelMatches.Block))
                        telegram_client.deleteMessage(u.message.chat.id.ToString(), u.message.message_id);
                }
            }

            if (!string.IsNullOrEmpty(WebhookAddress) && !string.IsNullOrEmpty(hmac_key) && !string.IsNullOrEmpty(hmac_secret))
            {
                string postData = glob_tools.SerialiseJSON(this);
                string responsebody;
                //
                HttpWebRequest request;
                Stream requestStream;
                request = (HttpWebRequest)WebRequest.Create(WebhookAddress);
                request.ContentType = "application/json";
                request.Method = "POST";

                HmacHttpWebRequest hmacHttp = new HmacHttpWebRequest(hmac_key, hmac_secret);
                request = hmacHttp.SignRequest(request);

                requestStream = request.GetRequestStream();
                byte[] byteArray = Encoding.UTF8.GetBytes(postData);
                request.ContentLength = byteArray.Length;
                requestStream.Write(byteArray, 0, byteArray.Length);
                requestStream.Close();

                try
                {
                    using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                    {
                        if(!hmacHttp.VerifyResponse(response))
                        {
                            Log.WriteLine("Ответ API Webhook не подписан или подписан не действительной поджписью", LogStatusEnum.Alarm);
                            return;
                        }
                        Log.WriteLine("HMAC API Webhook подпись проверена", LogStatusEnum.Happi);
                        using (StreamReader reader = new StreamReader(response.GetResponseStream(), Encoding.UTF8, true))
                        {
                            if (response.StatusCode == HttpStatusCode.OK)
                            {
                                responsebody = reader.ReadToEnd();
                                if (string.IsNullOrEmpty(responsebody))
                                    Log.WriteLine("API HMAC JSON - вернул пустой ответ", LogStatusEnum.Alarm);
                                else
                                {
                                    try
                                    {
                                        ResultHmacResponseClass resultHmac = (ResultHmacResponseClass)glob_tools.DeSerialiseJSON(typeof(ResultHmacResponseClass), responsebody);
                                        Log.Write("API HMAC JSON - прочитан ");
                                        Log.WriteLine(resultHmac.status.ToString("g"), resultHmac.status == StatusResult.Ok ? LogStatusEnum.Happi : LogStatusEnum.Alarm);
                                        
                                    }
                                    catch (Exception e)
                                    {
                                        Log.WriteLine("API HMAC JSON - ошибка. " + e.Message, LogStatusEnum.Alarm);
                                    }
                                }
                            }
                            else
                            {
                                Log.WriteLine("API HTTP-CODE: " + response.StatusCode.ToString("g") + ". HTTP-DESC: " + response.StatusDescription + ".", LogStatusEnum.Alarm);
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    Log.WriteLine("API HMAC JSON - HTTP-ошибка. " + e.Message, LogStatusEnum.Alarm);
                }
            }
        }

        private void NotifyAdmins(string message)
        {
            foreach (ChatMemberClass member in ChatAdmins)
                telegram_client.sendMessage(member.user.id.ToString(), message);

            if (!string.IsNullOrEmpty(bot_admin_username))
                telegram_client.sendMessage("@" + bot_admin_username, message);
        }
    }
}