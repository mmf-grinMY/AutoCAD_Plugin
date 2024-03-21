namespace Plugins
{
    static class SessionDispatcher
    {
        static Session session;
        public static void StartSession(Session s) => session = s;
        public static Session Current => session;
        public static void Run()
        {
            var model = new View.DrawInfoViewModel(session);
            var window = new View.DrawInfoWindow() { DataContext = model };
            window.Closed += model.HandleOperationCancel;
            window.ShowDialog();
        }
    }
}