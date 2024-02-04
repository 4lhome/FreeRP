namespace FreeRP.Net.Server.Data
{
    public class UriService(FrpSettings frpSettings)
    {
        private readonly FrpSettings _frpSettings = frpSettings;

        public string Combine(string u1, string u2)
        {
            if (u2.StartsWith('/'))
                u2 = u2[1..];

            if (u1.EndsWith('/'))
                return u1 + u2;

            return $"{u1}/{u2}";
        }

        public Uri? GetUri(string uri)
        {
            Uri? result = null;

            if (uri.StartsWith("file"))
            {
                if (uri.StartsWith("file:///") == false && uri.StartsWith("file://"))
                    result = new Uri(uri.Replace("file://", "file:///"));
                else
                    result = new Uri(uri);
            }
            else if (uri.StartsWith("db"))
            {
                if (uri.StartsWith("db:///") == false && uri.StartsWith("db://"))
                    result = new Uri(uri.Replace("db://", "db:///"));
                else
                    result = new Uri(uri);
            }
            else if (uri.StartsWith("public"))
            {
                if (uri.StartsWith("public:///") == false && uri.StartsWith("public://"))
                    result = new Uri(uri.Replace("public://", "public:///"));
                else
                    result = new Uri(uri);
            }
            else if (uri.StartsWith("temp"))
            {
                if (uri.StartsWith("temp:///") == false && uri.StartsWith("temp://"))
                    result = new Uri(uri.Replace("temp://", "temp:///"));
                else
                    result = new Uri(uri);
            }

            if (result is not null)
            {
                if (result.Segments.Length > 1 && result.Segments.Last().EndsWith('/'))
                    return new Uri(result.OriginalString[..^1]);
                else
                    return result;
            }

            return null;
        }

        public string GetPath(Uri uri)
        {
            var path = uri.AbsolutePath.Replace("/", "\\");
            if (path.StartsWith('\\'))
                path = path[1..];

            switch (uri.Scheme)
            {
                case "file":
                    return Path.Combine(_frpSettings.ContentRootPath, path);
                case "db":
                    return Path.Combine(_frpSettings.DatabasesRootPath, path);
                case "public":
                    return Path.Combine(_frpSettings.PublicRootPath, path);
                case "temp":
                    return Path.Combine(_frpSettings.TempRootPath, path);
                default:
                    break;
            }

            return path;
        }
    }
}
