namespace RtsServer.App.FileSystem
{
    public class FileManager
    {
        protected string path;
        protected string format;

        public FileManager()
        {
            string location = System.Reflection.Assembly.GetExecutingAssembly().Location;
            string? path = Path.GetDirectoryName(location);
            if (path == null) throw new Exception("Не удалось получить путь до проекта");
            this.path = path;
            format = "txt";
        }
    }
}
