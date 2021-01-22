using System;
using System.IO;
using System.Threading.Tasks;

namespace Seikilos.LargeTextFileIndexerLib
{
    public class LargeFileIndexer
    {
        /// <summary>
        /// Convenience method to read and write to disk
        /// </summary>
        /// <param name="fileToRead">File to read</param>
        /// <param name="indexFileToWrite">Binary index of input file. Will be overwritten if it exists</param>
        public Task IndexAsync(string fileToRead, string indexFileToWrite)
        {
            if (File.Exists(fileToRead) == false)
            {
                throw new FileNotFoundException(fileToRead);
            }

            using (var inputStream = File.Open(fileToRead, FileMode.Open))
            using (var outputStream = File.Open(indexFileToWrite, FileMode.Create))
            {
                return this.IndexAsync(inputStream, outputStream);
            }
            
          
        }


        /// <summary>
        /// Performs a byte indexing of the input stream.
        /// </summary>
        /// <param name="inputStream">Arbitrary stream read byte by byte</param>
        /// <param name="indexOutputStream">Binary output stream. Ensure to dispose it after use</param>
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
                        // Improvement: Read buffered, not only single bytes to make use of caching
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
