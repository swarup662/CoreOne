using System;
using System.Collections.Concurrent;

namespace CoreOne.UI.Helper
{


    public static class TempFileStorage
    {
        private static readonly Dictionary<string, (byte[] Content, string FileName, string ContentType, string Module)> _files
            = new();

        public static string AddFile(byte[] content, string fileName, string contentType, string module)
        {
            var tempId = Guid.NewGuid().ToString();
            _files[tempId] = (content, fileName, contentType, module);
            return tempId;
        }

        public static bool TryGetFile(string tempId, out byte[] content, out string fileName, out string contentType, out string module)
        {
            if (_files.TryGetValue(tempId, out var val))
            {
                content = val.Content;
                fileName = val.FileName;
                contentType = val.ContentType;
                module = val.Module;
                return true;
            }

            content = null;
            fileName = null;
            contentType = null;
            module = null;
            return false;
        }

        public static void RemoveFile(string tempId)
        {
            _files.Remove(tempId);
        }
    }


}
