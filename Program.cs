////////////////////////////////////////////////
// © https://github.com/badhitman - @fakegov 
////////////////////////////////////////////////
using MultiTool.LibraryLog;
using MultiTool.LibraryParserIncomingArguments;
using System;
using System.Collections.Generic;
using System.Linq;
using TelegramBot.TelegramMetadata.GettingUpdates;

namespace SpamBlockTelegram
{
    class Program
    {
        static LogClass Log = new LogClass() { MinimalLogStatusWrite = LogStatusEnum.Trace };
        //
        static SpamBlock anti_spam;
        static void Main(string[] args)
        {
            Log.WriteLine("Hello World!");
            List<IncomingArgsClass> arg_parser = new ParserArguments().Parse(args);
            if (!arg_parser.Exists(x => x.name_argument.ToLower() == "api_key_telegram_bot"))
            {
                Log.WriteLine("Отсутсвует API Telegram auth key", LogStatusEnum.Alarm);
                return;
            }

            anti_spam = new SpamBlock(arg_parser.First(x => x.name_argument.ToLower() == "api_key_telegram_bot").value_argument);

            if (anti_spam.telegram_client.Me is null)
            {
                Log.WriteLine("Telegram бот не запущен. Проверьте подключение к интернету и api_key_telegram_bot", LogStatusEnum.Alarm);
                return;
            }

            if (arg_parser.Exists(x => x.name_argument.ToLower() == "bot_admin_username"))
            {
                anti_spam.BotAdminUsername = arg_parser.First(x => x.name_argument.ToLower() == "bot_admin_username").value_argument;
                Log.WriteLine("bot_admin_username=" + anti_spam.BotAdminUsername, LogStatusEnum.Norma);
            }
            else
                Log.WriteLine("Отсутсвует bot_admin_username. Некому управлять ботом в режиме Online", LogStatusEnum.Notice);

            if (
                arg_parser.Exists(x => x.name_argument.ToLower() == "webhook_api_url") &&
                arg_parser.Exists(x => x.name_argument.ToLower() == "hmac_sign_key") &&
                arg_parser.Exists(x => x.name_argument.ToLower() == "hmac_sign_secret"))
            {
                anti_spam.SetWebhook(arg_parser.First(x => x.name_argument.ToLower() == "webhook_api_url").value_argument, arg_parser.First(x => x.name_argument.ToLower() == "hmac_sign_key").value_argument, arg_parser.First(x => x.name_argument.ToLower() == "hmac_sign_secret").value_argument);
                Log.WriteLine("Установлен Webhook/HMAC > " + anti_spam.WebhookAddress, LogStatusEnum.Norma);
            }

            foreach (IncomingArgsClass arg in arg_parser.Where(x => x.name_argument.ToLower() == "block_text"))
                anti_spam.block_strings.Add(arg.value_argument);

            foreach (IncomingArgsClass arg in arg_parser.Where(x => x.name_argument.ToLower() == "block_regex"))
                anti_spam.block_regexes.Add(arg.value_argument);

            foreach (IncomingArgsClass arg in arg_parser.Where(x => x.name_argument.ToLower() == "alert_text"))
                anti_spam.alert_strings.Add(arg.value_argument);

            foreach (IncomingArgsClass arg in arg_parser.Where(x => x.name_argument.ToLower() == "alert_regex"))
                anti_spam.alert_regexes.Add(arg.value_argument);

            Check_telegram();
            Log.WriteLine("Завершение работы", LogStatusEnum.Trace);
            Environment.Exit(1);
        }

        private static void Check_telegram()
        {
            foreach (Update u in anti_spam.telegram_client.getUpdates())
            {
                DoUpdate(u);
            }
            anti_spam.telegram_client.getUpdates(1);
        }

        private static void DoUpdate(Update u)
        {
            anti_spam.telegram_client.offset = u.update_id;
            //
            Log.WriteLine("tg[" + u.message.chat.type + "]:> " + u.message.text?.Trim() + Environment.NewLine, LogStatusEnum.Notice);
            //
            anti_spam.UpdateNew(u);
            if (!(anti_spam.TelegramCommand is null) && anti_spam.TelegramCommand.Command != Commands.NULL)
            {

            }
        }
    }
}