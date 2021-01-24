using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Seikilos.LargeTextFileIndexerTests
{
    public static class TestUtils
    {
        public static string RandomString(Random random, int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            return new string(Enumerable.Repeat(chars, length)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }


        public static Stream MakeLargeString(Random rand, int lines)
        {
            var stringStream = new MemoryStream();
            var sw = new StreamWriter(stringStream);
            for (var i = 0; i < lines; ++i)
            {
                sw.Write(RandomString(rand, rand.Next(1, 4949)));
                if (i < lines - 1)
                {
                    if (rand.NextDouble() < 0.5)
                    {
                        sw.Write("\r\n");
                    }
                    else
                    {
                        sw.Write("\n");
                    }
                }
            }

            sw.Flush();

            stringStream.Position = 0;
            return stringStream;

        }
    }
}
