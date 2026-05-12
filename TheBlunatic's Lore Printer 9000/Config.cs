using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LoreApp
{
    public interface IConfigParameter
    {
        string ID { get; }
        void ReturnToDefault();
        bool TrySetValue(string value);
        string GetFileRepresentation();
        ConsoleColor GetMostSuitableValueColor();
        Type GetPropertyType();
    }
    public class ConfigParameter<T> : IConfigParameter
    {
        public string ID { get; }
        public T Value { get; set; }
        public T DefaultValue { get; }

        public ConfigParameter(string id, T defaultValue)
        {
            ID = id;
            Value = defaultValue;
            DefaultValue = defaultValue;
        }

        public static implicit operator T(ConfigParameter<T> input)
        {
            return input.Value;
        }

        public void ReturnToDefault() => Value = DefaultValue;

        public Type GetPropertyType() => typeof(T);
        public bool TrySetValue(string value)
        {
            try
            {
                object newVal = Convert.ChangeType(value, typeof(T));
                Value = (T)newVal;
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
        public string GetFileRepresentation() => $"{ID} = {Value}";
        public override string ToString()
        {
            return Value.ToString();
        }
        public ConsoleColor GetMostSuitableValueColor()
        {
            if (typeof(T) == typeof(bool))
            {
                if (Value.ToString() == "false") return ConsoleColor.Red;
                return ConsoleColor.Green;
            }
            if (typeof(T) == typeof(string))
            {
                return ConsoleColor.White;
            }
            return ConsoleColor.Gray;
        }
    }

    public class Config
    {
        const string COMMENT_IDENTIFIER = "#";

        public Dictionary<string, IConfigParameter> ParameterDictionary;

        public ConfigParameter<string> InputFolder { get; set; } = new ConfigParameter<string>(InputFolderID, "put images here");
        const string InputFolderID = "Input Folder";
        public ConfigParameter<string> OutputFolder { get; set; } = new ConfigParameter<string>(OutputFolderID, "output will be sent here");
        const string OutputFolderID = "Output Folder";
        public ConfigParameter<bool> UseInstallLocationAsActiveDirectory { get; set; } = new ConfigParameter<bool>(UseInstallLocationAsActiveDirectoryID, true);
        const string UseInstallLocationAsActiveDirectoryID = "Use Install Location As Active Directory";

        public Config()
        {
            ParameterDictionary = new Dictionary<string, IConfigParameter>();

            void add(IConfigParameter configParameter)
            {
                ParameterDictionary.Add(configParameter.ID, configParameter);
            }

            add(InputFolder);
            add(OutputFolder);
            add(UseInstallLocationAsActiveDirectory);
        }

        static (string, string) FileToProperty(string line) => (line.Substring(0, line.IndexOf('=') - 1), line.Substring(line.IndexOf('=') + 2));

        public Config(string[] configFile) : this()
        {
            int expectedSuccesses = ParameterDictionary.Count;

            HashSet<string> successes = new HashSet<string>();

            foreach (string line in configFile)
            {
                if (line.StartsWith(COMMENT_IDENTIFIER)) continue;
                if (line.Trim().Length == 0) continue;

                string id, value;
                (id, value) = FileToProperty(line);

                successes.Add(id);

                if (ParameterDictionary.TryGetValue(id, out IConfigParameter configParameter))
                {
                    if (!configParameter.TrySetValue(value))
                    {
                        throw new ArgumentException($"Config failed to load parameter '{id}' to value '{value}'");
                    }
                }
                else
                {
                    throw new ArgumentException("Config file contained an unrecognised parameter id.");
                }
            }

            if (expectedSuccesses != successes.Count)
            {
                throw new ArgumentException("Config file did not contain");
            }
        }

        public string[] GetFileContents()
        {
            string[] contents = new string[ParameterDictionary.Count + 1];
            contents[0] = "# WARNING: Changing the values in this file could cause unexpected behaviour if they are invalid! These values are not validated until used.";
            int lineCount = 1;

            foreach (IConfigParameter p in ParameterDictionary.Values)
            {
                contents[lineCount++] = p.GetFileRepresentation();
            }

            return contents;
        }
    }
}
