using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Blunatic.Parsing
{
    public static class Hex
    {
        private static char[] _valToChar = { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', 'a', 'b', 'c', 'd', 'e', 'f' };
        private static Dictionary<char, byte> _charToVal = new Dictionary<char, byte>
        {
            { '0', _initChar('0') },
            { '1', _initChar('1') },
            { '2', _initChar('2') },
            { '3', _initChar('3') },
            { '4', _initChar('4') },
            { '5', _initChar('5') },
            { '6', _initChar('6') },
            { '7', _initChar('7') },
            { '8', _initChar('8') },
            { '9', _initChar('9') },
            { 'a', _initChar('a') },
            { 'b', _initChar('b') },
            { 'c', _initChar('c') },
            { 'd', _initChar('d') },
            { 'e', _initChar('e') },
            { 'f', _initChar('f') },
        };

        private static byte _initChar(char c)
        {
            return (byte)Array.IndexOf(_valToChar, c);
        }

        public static string GetString(params byte[] bytes)
        {
            string returnValue = string.Empty;

            foreach (byte b in bytes)
            {
                returnValue += $"{_valToChar[b / 16]}{_valToChar[b % 16]}";
            }

            return returnValue;
        }
        public static byte[] GetBytes(string input)
        {
            input = input.ToUpper();
            byte[] returnItems = new byte[input.Length / 2];
            for (int i = 0; i < returnItems.Length; i++)
            {
                returnItems[i] = (byte)(_charToVal[input[i * 2]] * 16 + _charToVal[input[i * 2 + 1]]);
            }
            return returnItems;
        }

        public static string GetString(System.Drawing.Color color)
        {
            return GetString(color.R, color.G, color.B);
        }
    }
}
