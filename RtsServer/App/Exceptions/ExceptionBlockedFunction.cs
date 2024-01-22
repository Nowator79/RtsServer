namespace RtsServer.App.Exceptions
{
    internal class ExceptionBlockedFunction : Exception
    {
        public ExceptionBlockedFunction() : base("Нельзя использовать загружать данные в этот запросс")
        {
        }
    }
}
