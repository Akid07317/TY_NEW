using System;
using System.Globalization;

namespace CampusRPG.Multiplayer
{
    public static class MultiplayerCommandLineUtility
    {
        public static bool HasFlag(string[] args, params string[] names)
        {
            if (args == null || names == null)
            {
                return false;
            }

            for (int i = 0; i < args.Length; i++)
            {
                string arg = args[i];

                if (string.IsNullOrEmpty(arg))
                {
                    continue;
                }

                for (int nameIndex = 0; nameIndex < names.Length; nameIndex++)
                {
                    if (arg.Equals(names[nameIndex], StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public static int ReadIntArgument(string[] args, int fallback, params string[] names)
        {
            if (args == null || names == null)
            {
                return fallback;
            }

            for (int i = 0; i < args.Length; i++)
            {
                string arg = args[i];

                if (string.IsNullOrEmpty(arg))
                {
                    continue;
                }

                for (int nameIndex = 0; nameIndex < names.Length; nameIndex++)
                {
                    string name = names[nameIndex];

                    if (arg.Equals(name, StringComparison.OrdinalIgnoreCase))
                    {
                        if (i + 1 < args.Length && int.TryParse(args[i + 1], out int splitValue))
                        {
                            return splitValue;
                        }

                        continue;
                    }

                    string prefix = name + "=";

                    if (arg.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
                        && int.TryParse(arg.Substring(prefix.Length), out int inlineValue))
                    {
                        return inlineValue;
                    }
                }
            }

            return fallback;
        }

        public static string ReadStringArgument(string[] args, string fallback, params string[] names)
        {
            if (args == null || names == null)
            {
                return fallback;
            }

            for (int i = 0; i < args.Length; i++)
            {
                string arg = args[i];

                if (string.IsNullOrEmpty(arg))
                {
                    continue;
                }

                for (int nameIndex = 0; nameIndex < names.Length; nameIndex++)
                {
                    string name = names[nameIndex];

                    if (arg.Equals(name, StringComparison.OrdinalIgnoreCase))
                    {
                        if (i + 1 < args.Length && !args[i + 1].StartsWith("-", StringComparison.Ordinal))
                        {
                            return args[i + 1];
                        }

                        continue;
                    }

                    string prefix = name + "=";

                    if (arg.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                    {
                        return arg.Substring(prefix.Length);
                    }
                }
            }

            return fallback;
        }

        public static float ReadFloatArgument(string[] args, float fallback, params string[] names)
        {
            if (args == null || names == null)
            {
                return fallback;
            }

            for (int i = 0; i < args.Length; i++)
            {
                string arg = args[i];

                if (string.IsNullOrEmpty(arg))
                {
                    continue;
                }

                for (int nameIndex = 0; nameIndex < names.Length; nameIndex++)
                {
                    string name = names[nameIndex];

                    if (arg.Equals(name, StringComparison.OrdinalIgnoreCase))
                    {
                        if (i + 1 < args.Length
                            && float.TryParse(
                                args[i + 1],
                                NumberStyles.Float,
                                CultureInfo.InvariantCulture,
                                out float splitValue))
                        {
                            return splitValue;
                        }

                        continue;
                    }

                    string prefix = name + "=";

                    if (arg.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
                        && float.TryParse(
                            arg.Substring(prefix.Length),
                            NumberStyles.Float,
                            CultureInfo.InvariantCulture,
                            out float inlineValue))
                    {
                        return inlineValue;
                    }
                }
            }

            return fallback;
        }

        public static bool ReadBoolArgument(string[] args, bool fallback, params string[] names)
        {
            if (args == null || names == null)
            {
                return fallback;
            }

            for (int i = 0; i < args.Length; i++)
            {
                string arg = args[i];

                if (string.IsNullOrEmpty(arg))
                {
                    continue;
                }

                for (int nameIndex = 0; nameIndex < names.Length; nameIndex++)
                {
                    string name = names[nameIndex];

                    if (arg.Equals(name, StringComparison.OrdinalIgnoreCase))
                    {
                        if (i + 1 < args.Length && TryParseBool(args[i + 1], out bool splitValue))
                        {
                            return splitValue;
                        }

                        return true;
                    }

                    string prefix = name + "=";

                    if (arg.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
                        && TryParseBool(arg.Substring(prefix.Length), out bool inlineValue))
                    {
                        return inlineValue;
                    }
                }
            }

            return fallback;
        }

        public static int Clamp(int value, int min, int max)
        {
            if (value < min)
            {
                return min;
            }

            return value > max ? max : value;
        }

        public static string NormalizeAddress(string value, string fallback, string defaultValue)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.IsNullOrWhiteSpace(fallback) ? defaultValue : fallback.Trim();
            }

            return value.Trim();
        }

        private static bool TryParseBool(string value, out bool result)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                result = false;
                return false;
            }

            switch (value.Trim().ToLowerInvariant())
            {
                case "1":
                case "true":
                case "yes":
                case "on":
                    result = true;
                    return true;
                case "0":
                case "false":
                case "no":
                case "off":
                    result = false;
                    return true;
                default:
                    return bool.TryParse(value, out result);
            }
        }
    }
}
