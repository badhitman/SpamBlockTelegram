////////////////////////////////////////////////
// © https://github.com/badhitman 
////////////////////////////////////////////////

using System.Runtime.Serialization;

namespace SpamBlockTelegram
{
    /// <summary>
    /// Место нахождения текста (текст сообщения или текст описания к вложению)
    /// </summary>
    [DataContract]
    public enum PlaceMatches
    {
        /// <summary>
        /// Область поиска: текст сообщения
        /// </summary>
        MessageText,

        /// <summary>
        /// Область поиска: описание едиа-объекта
        /// </summary>
        Caption
    }

    /// <summary>
    /// Тип вхождения (регулярное выражение или прсотой текст)
    /// </summary>
    [DataContract]
    public enum TypeMatches
    {
        /// <summary>
        /// Поиск по регулярному выражению
        /// </summary>
        Regex,

        /// <summary>
        /// Поиск по вхождению строки
        /// </summary>
        Text
    }

    /// <summary>
    /// Признак вхождения. К какой группе относится вхождение (block или alert)
    /// </summary>
    [DataContract]
    public enum LevelMatches
    {
        /// <summary>
        /// Поиск важных данных для уведомления администраторов
        /// </summary>
        Alert,

        /// <summary>
        /// Поиск запрещённых данных
        /// </summary>
        Block
    }

    [DataContract]
    public class SpamMatch
    {
        /// <summary>
        /// Место нахождения текста (текст сообщения или текст описания к вложению)
        /// </summary>
        [DataMember]
        public PlaceMatches PlaceMatch;

        /// <summary>
        /// Тип вхождения (регулярное выражение или прсотой текст)
        /// </summary>
        [DataMember]
        public TypeMatches TypeMatch;

        /// <summary>
        /// Признак вхождения. К какой группе относится вхождение (block или alert)
        /// </summary>
        [DataMember]
        public LevelMatches LevelMatch;

        /// <summary>
        /// Позиция в исходной строке, в которой находится первый символ захваченной подстроки.
        /// </summary>
        [DataMember]
        public int index;

        [DataMember]
        public string find_data;

        /// <summary>
        /// Получить клон/копию объекта
        /// </summary>
        public SpamMatch Clone() { return new SpamMatch() { index = this.index, LevelMatch = this.LevelMatch, PlaceMatch = this.PlaceMatch, TypeMatch = this.TypeMatch, find_data = this.find_data }; }
    }
}
