////////////////////////////////////////////////
// © https://github.com/badhitman - Telegram @fakegov 
////////////////////////////////////////////////
using System;
using System.Text.RegularExpressions;

namespace SpamBlockTelegram
{
    public enum Commands
    {
        /// <summary>
        /// Команда отсутсвует
        /// </summary>
        NULL,

        /// <summary>
        /// /Start - Telegram bot
        /// </summary>
        Start,

        /// <summary>
        /// /Settings - Telegram bot
        /// </summary>
        Settings,

        /// <summary>
        /// /Help - Telegram bot
        /// </summary>
        Help,

        /// <summary>
        /// Получить временную/одноразовую ссылку для авторизации на сайте
        /// </summary>
        Web,

        /// <summary>
        /// Команда поступила, но не опознана
        /// </summary>
        NewCommand
    }

    public class TelegramCommandClass
    {
        public string OriginalParsedString { get; private set; }
        public string CommandAsString { get; private set; }
        public Commands Command = Commands.NULL;
        public string option;

        public TelegramCommandClass(string string_commands)
        {
            OriginalParsedString = string_commands;
            Regex regex = new Regex(@"^/?(\w+[\w\d_]+)[\s_]*(.*)$", RegexOptions.IgnoreCase);
            Match match = regex.Match(string_commands);

            if (match.Success)
            {
                CommandAsString = match.Groups[1].Value;
                foreach (Commands c in Enum.GetValues(typeof(Commands)))
                {
                    if (match.Groups[1].Value.ToLower() == c.ToString("g").ToLower())
                    {
                        Command = c;
                        option = match.Groups[2].Value.Trim();
                        break;
                    }
                }
            }
        }
    }
}
