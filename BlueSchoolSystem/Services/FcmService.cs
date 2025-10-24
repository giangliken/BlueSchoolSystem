using FirebaseAdmin;
using FirebaseAdmin.Messaging;
using Google.Apis.Auth.OAuth2;
using System.Text;

namespace BlueSchoolSystem.Services
{
    public static class FcmService
    {
        private static FirebaseApp _app;
        private static readonly object _lock = new();

        // ==== Init FirebaseApp an toàn, path JSON chuẩn ====
        private static void InitFirebase()
        {
            if (_app != null) return;
            lock (_lock)
            {
                if (_app != null) return;

                // Ưu tiên ENV nếu có, fallback sang file trong /bin/Config
                var credPath = Environment.GetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS");
                GoogleCredential cred = !string.IsNullOrWhiteSpace(credPath)
                    ? GoogleCredential.FromFile(credPath)
                    : GoogleCredential.FromFile(Path.Combine(AppContext.BaseDirectory, "Config", "bluenet-e6525-76bafbf60398.json"));

                // Khai báo ProjectId cho chắc kèo (tránh mismatch)
                _app = FirebaseApp.Create(new AppOptions
                {
                    Credential = cred,
                    ProjectId = "bluenet-e6525"
                });
            }
        }

        // ==== Helpers ====
        private static string CleanToken(string t)
        {
            if (string.IsNullOrWhiteSpace(t)) return string.Empty;
            t = t.Trim().Trim('"');
            t = t.Replace("\r", "").Replace("\n", "");
            return t;
        }

        private static bool LooksLikeFcmToken(string t)
        {
            return !string.IsNullOrWhiteSpace(t)
                   && t.Length >= 80 && t.Length <= 4096
                   && !t.Any(char.IsWhiteSpace);
        }

        // ==== Gửi 1 token: validate (dryRun) + gửi thật, trả kết quả chi tiết ====
        public static async Task<(bool ok, string? errorCode, string? errorMessage)> SendNotificationAsync(
            string fcmToken,
            string title,
            string body,
            Dictionary<string, string>? data = null,
            bool validateOnly = false)
        {
            InitFirebase();

            var token = CleanToken(fcmToken);
            if (!LooksLikeFcmToken(token))
                return (false, "InvalidFormat", "Token không đúng định dạng FCM hoặc bị cắt thiếu.");

            var message = new Message
            {
                Token = token,
                Notification = new Notification { Title = title, Body = body },
                Data = (data ?? new Dictionary<string, string>())
                    .Append(new KeyValuePair<string, string>("click_action", "FLUTTER_NOTIFICATION_CLICK"))
                    .ToDictionary(k => k.Key, v => v.Value)
            };

            try
            {
                // 1) Validate trước (không gửi)
                await FirebaseMessaging.DefaultInstance.SendAsync(message, dryRun: true);

                if (validateOnly) return (true, null, null);

                // 2) Ok thì gửi thật
                var resp = await FirebaseMessaging.DefaultInstance.SendAsync(message);
                Console.WriteLine("FCM sent: " + resp);
                return (true, null, null);
            }
            catch (FirebaseMessagingException ex)
            {
                var code = ex.MessagingErrorCode.ToString();
                var msg = ex.Message;

                // In sâu body nếu có
                try
                {
                    var http = ex.HttpResponse;
                    if (http != null)
                    {
                        var bodyText = await http.Content.ReadAsStringAsync();
                        Console.WriteLine("FCM http body: " + bodyText);
                        msg = $"{msg} | {bodyText}";
                    }
                }
                catch { /* ignore */ }

                Console.WriteLine($"FCM fail: {code} - {msg}");
                return (false, code, msg);
            }
        }

        // ==== Gửi nhiều token (gửi cả lớp): trả về list token lỗi để gỡ khỏi DB ====
        public static async Task<(int success, int failure, List<string> badTokens)> SendToManyAsync(
            IEnumerable<string> tokens,
            string title,
            string body,
            Dictionary<string, string>? data = null)
        {
            InitFirebase();

            var cleaned = tokens
                .Select(CleanToken)
                .Where(LooksLikeFcmToken)
                .Distinct()
                .ToList();

            if (cleaned.Count == 0)
                return (0, 0, new List<string>());

            var msg = new MulticastMessage
            {
                Tokens = cleaned,
                Notification = new Notification { Title = title, Body = body },
                Data = (data ?? new Dictionary<string, string>())
            };

            var resp = await FirebaseMessaging.DefaultInstance.SendEachForMulticastAsync(msg);

            var bad = new List<string>();
            for (int i = 0; i < resp.Responses.Count; i++)
            {
                var r = resp.Responses[i];
                if (!r.IsSuccess)
                {
                    var ex = r.Exception as FirebaseMessagingException;
                    var code = ex?.MessagingErrorCode;

                    // Token hỏng/mismatch project → loại khỏi DB
                    if (code == MessagingErrorCode.Unregistered || code == MessagingErrorCode.InvalidArgument)
                    {
                        bad.Add(cleaned[i]);
                    }

                    Console.WriteLine($"[FCM] Fail {cleaned[i]}: {code} - {ex?.Message}");
                }
            }

            return (resp.SuccessCount, resp.FailureCount, bad);
        }

        // ==== Gửi theo topic (nếu ông dùng topic cho lớp) ====
        public static async Task<string> SendToTopicAsync(string topic, string title, string body, Dictionary<string, string>? data = null)
        {
            InitFirebase();
            var msg = new Message
            {
                Topic = topic, // ví dụ: "lop_LHP035"
                Notification = new Notification { Title = title, Body = body },
                Data = data ?? new Dictionary<string, string>()
            };
            return await FirebaseMessaging.DefaultInstance.SendAsync(msg);
        }
    }
}
