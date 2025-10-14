using FirebaseAdmin;
using FirebaseAdmin.Messaging;
using Google.Apis.Auth.OAuth2;

namespace BlueSchoolSystem.Services
{
    public static class FcmService
    {
        private static bool _firebaseInitialized = false;
        private static readonly object _lock = new object();

        private static void InitFirebase()
        {
            if (!_firebaseInitialized)
            {
                lock (_lock)
                {
                    if (!_firebaseInitialized)
                    {
                        FirebaseApp.Create(new AppOptions()
                        {
                            Credential = GoogleCredential.FromFile("Config/bluenet-e6525-76bafbf60398.json"),
                        });
                        _firebaseInitialized = true;
                    }
                }
            }
        }

        public static async Task SendNotificationAsync(string fcmToken, string title, string body)
        {
            InitFirebase();

            var message = new Message()
            {
                Token = fcmToken,
                Notification = new Notification
                {
                    Title = title,
                    Body = body
                },
                Data = new Dictionary<string, string>
                {
                    { "click_action", "FLUTTER_NOTIFICATION_CLICK" },
                    { "customKey", "customValue" }
                }
            };

            string response = await FirebaseMessaging.DefaultInstance.SendAsync(message);
            Console.WriteLine("Successfully sent message: " + response);
        }
    }
}
