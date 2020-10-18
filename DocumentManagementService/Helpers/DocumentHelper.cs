namespace DocumentManagementService.Helpers
{
    using System.IO;

    /// <summary>
    /// The Document Helper static class
    /// </summary>
    public static class DocumentHelper
    {
        /// <summary>
        /// Returns byte array for provide stream content
        /// </summary>
        /// <param name="input">the input stream</param>
        /// <returns>byte array</returns>
        public static byte[] ReadContent(Stream input)
        {
            var buffer = new byte[16 * 1024];
            using var ms = new MemoryStream();
            int read;
            while ((read = input.Read(buffer, 0, buffer.Length)) > 0)
            {
                ms.Write(buffer, 0, read);
            }
            return ms.ToArray();
        }
    }
}
