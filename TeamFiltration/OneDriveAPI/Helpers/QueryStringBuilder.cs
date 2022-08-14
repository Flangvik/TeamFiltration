using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace KoenZomers.OneDrive.Api.Helpers
{
    /// <summary>
    /// Helper class for building querystrings
    /// </summary>
    public class QueryStringBuilder
    {
        /// <summary>
        /// Collection of parameters in the querystring
        /// </summary>
        private readonly Dictionary<string, string> _parameters = new Dictionary<string, string>();

        /// <summary>
        /// Boolean indicating if the querystring has any items in it
        /// </summary>
        public bool HasKeys
        {
            get
            {
                return _parameters.Count > 0;
            }
        }

        /// <summary>
        /// Character to start the querystring with. Defaults to not including it.
        /// </summary>
        public char? StartCharacter { get; set; }

        /// <summary>
        /// Character used to separate the items in the querystring. Defaults to &.
        /// </summary>
        public char SeperatorCharacter { get; set; }

        /// <summary>
        /// Character used to separate the keys from its value sin the querystring. Defaults to =.
        /// </summary>
        public char KeyValueJoinCharacter { get; set; }

        /// <summary>
        /// Allows retrieval or setting the value of items in the querystring
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public string this[string key]
        {
            get
            {
                return _parameters.ContainsKey(key) ? _parameters[key] : null;
            }
            set
            {
                _parameters[key] = value;
            }
        }

        /// <summary>
        /// Collection with keys of the parameters in this querystring
        /// </summary>
        public string[] Keys
        {
            get
            {
                return _parameters.Keys.ToArray();
            }
        }

        /// <summary>
        /// Instantiates a new empty querystring
        /// </summary>
        public QueryStringBuilder()
        {
            StartCharacter = new char?();
            SeperatorCharacter = '&';
            KeyValueJoinCharacter = '=';
        }

        /// <summary>
        /// Instantiates a new querystring and adds the provided key and value to it
        /// </summary>
        /// <param name="key">Key to add</param>
        /// <param name="value">Value to add</param>
        public QueryStringBuilder(string key, string value) : this()
        {
            this[key] = value;
        }

        /// <summary>
        /// Removes all items from the querystring
        /// </summary>
        public void Clear()
        {
            _parameters.Clear();
        }

        /// <summary>
        /// Returns a boolean indicating if a specific key exists in the querystring
        /// </summary>
        /// <param name="key">Name of the key to validate if it exists</param>
        /// <returns>Boolean indicating if the provided key exists in the querystring</returns>
        public bool ContainsKey(string key)
        {
            return _parameters.ContainsKey(key);
        }

        /// <summary>
        /// Adds an item to the querystring
        /// </summary>
        /// <param name="key">Key of the item to add</param>
        /// <param name="value">Value of the item to add</param>
        public void Add(string key, string value)
        {
            _parameters[key] = value;
        }

        /// <summary>
        /// Removes a specific key from the querystring
        /// </summary>
        /// <param name="key"></param>
        public void Remove(string key)
        {
            _parameters.Remove(key);
        }

        /// <summary>
        /// Outputs the entire querystring
        /// </summary>
        /// <returns>Querystring</returns>
        public override string ToString()
        {
            var stringBuilder = new StringBuilder();
            foreach (var keyValuePair in _parameters.Where(x => x.Value != null))
            {
                if (stringBuilder.Length == 0)
                {
                    var startCharacter = StartCharacter;
                    if ((startCharacter.HasValue ? startCharacter.GetValueOrDefault() : new int?()).HasValue)
                        stringBuilder.Append(StartCharacter);
                }
                if (stringBuilder.Length > 0)
                {
                    int num = stringBuilder[stringBuilder.Length - 1];
                    char? startCharacter = StartCharacter;
                    if ((num != startCharacter.GetValueOrDefault() ? 1 : (!startCharacter.HasValue ? 1 : 0)) != 0)
                        stringBuilder.Append(SeperatorCharacter);
                }
                stringBuilder.Append(keyValuePair.Key);
                stringBuilder.Append(KeyValueJoinCharacter);
                stringBuilder.Append(Uri.EscapeDataString(keyValuePair.Value));
            }
            return stringBuilder.ToString();
        }
    }
}
