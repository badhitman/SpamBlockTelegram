////////////////////////////////////////////////
// © https://github.com/badhitman 
////////////////////////////////////////////////

using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace SpamBlockTelegram
{
    public class SpamScanner
    {
        public List<SpamMatch> scan_matches { get; private set; } = new List<SpamMatch>();
        private LevelMatches FindLevel;
        public SpamScanner(SpamBlock sender)
        {
            Regex rex;
            FindLevel = LevelMatches.Alert;
            foreach (string s in sender.alert_strings)
            {
                if (sender.CompositionTypes.Exists(x => x == TelegramDataTypes.Text))
                    scan_matches.AddRange(Find(s, sender.IncUpdate.message.text, PlaceMatches.MessageText));
                if (sender.CompositionTypes.Exists(x => x == TelegramDataTypes.Caption))
                    scan_matches.AddRange(Find(s, sender.IncUpdate.message.caption, PlaceMatches.Caption));
            }

            foreach (string r in sender.alert_regexes)
            {
                rex = new Regex(r, RegexOptions.IgnoreCase);
                if (sender.CompositionTypes.Exists(x => x == TelegramDataTypes.Text))
                    scan_matches.AddRange(Find(rex, sender.IncUpdate.message.text, PlaceMatches.MessageText));
                if (sender.CompositionTypes.Exists(x => x == TelegramDataTypes.Caption))
                    scan_matches.AddRange(Find(rex, sender.IncUpdate.message.caption, PlaceMatches.Caption));
            }

            //////////////////////////////////////////////////////
            FindLevel = LevelMatches.Block;
            foreach (string s in sender.block_strings)
            {
                if (sender.CompositionTypes.Exists(x => x == TelegramDataTypes.Text))
                    scan_matches.AddRange(Find(s, sender.IncUpdate.message.text, PlaceMatches.MessageText));
                if (sender.CompositionTypes.Exists(x => x == TelegramDataTypes.Caption))
                    scan_matches.AddRange(Find(s, sender.IncUpdate.message.caption, PlaceMatches.Caption));
            }

            foreach (string r in sender.block_regexes)
            {
                rex = new Regex(r, RegexOptions.IgnoreCase);
                if (sender.CompositionTypes.Exists(x => x == TelegramDataTypes.Text))
                    scan_matches.AddRange(Find(rex, sender.IncUpdate.message.text, PlaceMatches.MessageText));
                if (sender.CompositionTypes.Exists(x => x == TelegramDataTypes.Caption))
                    scan_matches.AddRange(Find(rex, sender.IncUpdate.message.caption, PlaceMatches.Caption));
            }
        }

        private List<SpamMatch> Find(string find_data, string text, PlaceMatches placeMatches)
        {
            List<SpamMatch> ret_val = new List<SpamMatch>();

            if (string.IsNullOrEmpty(find_data) || string.IsNullOrEmpty(text) || find_data.Length > text.Length)
                return ret_val;

            SpamMatch match = new SpamMatch() { LevelMatch = FindLevel, PlaceMatch = placeMatches, TypeMatch = TypeMatches.Text, find_data = find_data };
            int start_index = 0;
            while(text.IndexOf(find_data, start_index) >= 0)
            {
                match.index = text.IndexOf(find_data, start_index);
                ret_val.Add(match.Clone());
                start_index = match.index + 1;
            }

            return ret_val;
        }

        private List<SpamMatch> Find(Regex find_data, string text, PlaceMatches placeMatches)
        {
            List<SpamMatch> ret_val = new List<SpamMatch>();
            SpamMatch match = new SpamMatch() { LevelMatch = FindLevel, PlaceMatch = placeMatches, TypeMatch = TypeMatches.Regex, find_data = find_data.ToString() };
            foreach(Match m in find_data.Matches(text))
            {
                match.index = m.Index;
                ret_val.Add(match.Clone());
            }
            return ret_val;
        }
    }
}
