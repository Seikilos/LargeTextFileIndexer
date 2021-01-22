using System.IO;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace Seikilos.LargeTextFileIndexerLib
{
    public class LargeFileIndexer
    {

        public Task IndexAsync(Stream inputStream, Stream indexOutputStream)
        {
            var indexPrevious = 0L;
            var indexNext = 0L;
            using (var streamReader = new BinaryReader(inputStream))
            {
                var binWriter = new BinaryWriter(indexOutputStream);

                while(true)
                {
                    try
                    {
                        var c = streamReader.ReadChar();
                        
                        if (c == '\n')
                        {
                            binWriter.Write(indexPrevious);
                            indexPrevious = indexNext+1;

                        }

                        ++indexNext;


                    }
                    catch (EndOfStreamException)
                    {
                        // Add index only if something has been found in the stream
                        if (indexNext != 0)
                        {
                            binWriter.Write(indexPrevious);
                        }

                        break;
                    }
                }


                binWriter.Flush();

                return Task.CompletedTask;
            }


        }
    }
}
