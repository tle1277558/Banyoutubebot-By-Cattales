using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using Google.Apis.YouTube.v3;
using Google.Apis.YouTube.v3.Data;
using System.Web; // สำหรับ HttpUtility


namespace banbotspam
{
    public partial class Form1 : Form
    {

        // เพิ่มฟิลด์สำหรับ Timer และตัวแปรที่เกี่ยวข้อง
        private System.Windows.Forms.Timer banbanTimer = new System.Windows.Forms.Timer();
        private int banbanIntervalMinutes = 5; // ค่าเริ่มต้น 5 นาที
        private DateTime lastRunTime;

        // เพิ่มส่วนประกอบ UI ที่จำเป็น
        private System.Windows.Forms.CheckBox checkBox1;
        private System.Windows.Forms.NumericUpDown numericInterval;
        private System.Windows.Forms.Label labelInterval;
        private System.Windows.Forms.Timer banbanLoopTimer = new System.Windows.Forms.Timer();
        private int banbanLoopIntervalMinutes = 5; // ค่าเริ่มต้น 5 นาที
        public Form1()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            Banbanban();
        }

        private async Task Banbanban(bool testFilteringMode = false)
        {
            // ล้าง RichTextBox ก่อนแสดงผลใหม่
            richTextBox1.Clear();

            try
            {
                SaveLastChannelId();

                richTextBox1.AppendText($"{(testFilteringMode ? "Test" : "Banbanban")} started at {DateTime.Now}\n");
                ScrollToBottom();

                var secrets = GoogleClientSecrets.FromFile(selectedSecretFile).Secrets;

                var credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
                    secrets,
                    new[] { YouTubeService.Scope.Youtube, YouTubeService.Scope.YoutubeForceSsl },
                    "user",
                    CancellationToken.None,
                    string.IsNullOrEmpty(tokenStorePath) ? null : new FileDataStore(tokenStorePath)
                );

                var youtubeService = new YouTubeService(new BaseClientService.Initializer()
                {
                    HttpClientInitializer = credential,
                    ApplicationName = "YouTube Comment Manager2"
                });

                string catTalesChannelId = textBox1.Text ; // Channel ID ของ "Cat Tales"
                var channelsRequest = youtubeService.Channels.List("snippet,contentDetails");
                channelsRequest.Id = catTalesChannelId;
                var channelsResponse = await channelsRequest.ExecuteAsync();

                if (channelsResponse.Items == null || channelsResponse.Items.Count == 0)
                {
                    richTextBox1.AppendText($"Channel with ID {catTalesChannelId} not found.\n");
                    ScrollToBottom();
                    return;
                }

                Channel catTalesChannel = channelsResponse.Items[0];
                richTextBox1.AppendText($"Channel: {catTalesChannel.Snippet.Title} (ID: {catTalesChannel.Id})\n");
                ScrollToBottom();

                string uploadsPlaylistId = catTalesChannel.ContentDetails.RelatedPlaylists.Uploads;
                if (string.IsNullOrEmpty(uploadsPlaylistId))
                {
                    richTextBox1.AppendText("Uploads Playlist ID not found for this channel.\n");
                    ScrollToBottom();
                    return;
                }

                string specificVideoId = null;
                if (!string.IsNullOrWhiteSpace(textBox2.Text))
                {
                    specificVideoId = ExtractVideoIdFromUrl(textBox2.Text);
                }

                string nextPageToken = null;
                int videoCount = 0;
                int maxVideos = Int32.Parse(textBox4.Text);

                int commentsModerated = 0;
                var moderatedComments = new List<string>();
                var maxComments = new List<string>();

                do
                {
                    var playlistItemsRequest = youtubeService.PlaylistItems.List("snippet");
                    playlistItemsRequest.PlaylistId = uploadsPlaylistId;
                    playlistItemsRequest.MaxResults = maxVideos;
                    playlistItemsRequest.PageToken = nextPageToken;

                    var playlistItemsResponse = await playlistItemsRequest.ExecuteAsync();

                    foreach (var playlistItem in playlistItemsResponse.Items)
                    {
                        if (videoCount >= maxVideos) break;

                        var videoId = specificVideoId ?? playlistItem.Snippet.ResourceId.VideoId;
                        richTextBox1.AppendText($"Processing video: {playlistItem.Snippet.Title} (ID: {videoId})\n");
                        ScrollToBottom();

                        string commentPageToken = null;
                        do
                        {
                            var commentRequest = youtubeService.CommentThreads.List("snippet");
                            commentRequest.VideoId = videoId;
                            commentRequest.MaxResults = 100;
                            commentRequest.PageToken = commentPageToken;

                            var commentResponse = await commentRequest.ExecuteAsync();

                            foreach (var commentThread in commentResponse.Items)
                            {
                                var commentText = commentThread.Snippet.TopLevelComment.Snippet.TextOriginal;
                                richTextBox1.AppendText($"Found comment: {commentText}\n");
                                ScrollToBottom();

                                if (ContainsMax(commentText))
                                {
                                    if (testFilteringMode)
                                    {
                                        maxComments.Add(commentText);
                                    }
                                    else
                                    {
                                        string commentId = commentThread.Snippet.TopLevelComment.Id;
                                        try
                                        {
                                            var commentCheckRequest = youtubeService.Comments.List("snippet");
                                            commentCheckRequest.Id = commentId;
                                            var commentCheckResponse = await commentCheckRequest.ExecuteAsync();

                                            if (commentCheckResponse.Items != null && commentCheckResponse.Items.Count > 0)
                                            {
                                                var moderationRequest = youtubeService.Comments.SetModerationStatus(commentId, CommentsResource.SetModerationStatusRequest.ModerationStatusEnum.HeldForReview);
                                                await moderationRequest.ExecuteAsync();
                                                richTextBox1.AppendText($"✅ Successfully set comment to 'heldForReview': {commentId}\n");
                                                moderatedComments.Add($"Comment: {commentText}\n  - Comment ID: {commentId}");
                                                commentsModerated++;
                                                ScrollToBottom();
                                            }
                                        }
                                        catch (Google.GoogleApiException ex)
                                        {
                                            richTextBox1.AppendText($"❌ Failed to set moderation status: {ex.Message}\n");
                                            ScrollToBottom();
                                        }
                                    }
                                }
                            }

                            commentPageToken = commentResponse.NextPageToken;
                            await Task.Delay(1000);
                        } while (commentPageToken != null);

                        if (specificVideoId != null) break; // หากเป็นวิดีโอเฉพาะ ให้หยุดหลังจากประมวลผลวิดีโอนั้น
                        videoCount++;
                    }

                    nextPageToken = playlistItemsResponse.NextPageToken;
                    if (videoCount >= maxVideos) break;
                } while (nextPageToken != null);

                if (testFilteringMode)
                {
                    if (maxComments.Count > 0)
                    {
                        richTextBox1.AppendText("\nFiltered Comments:\n");
                        ScrollToBottom();
                        foreach (var comment in maxComments)
                        {
                            richTextBox1.AppendText($"{comment}\n");
                            ScrollToBottom();
                        }
                    }
                    else
                    {
                        richTextBox1.AppendText("\nNo comments with 'max' found.\n");
                        ScrollToBottom();
                    }
                    richTextBox1.AppendText($"Test completed at {DateTime.Now}. Total comments with 'max' found: {maxComments.Count}\n");
                }
                else
                {
                    if (moderatedComments.Count > 0)
                    {
                        richTextBox1.AppendText("\nModerated Comments:\n");
                        ScrollToBottom();
                        foreach (var commentInfo in moderatedComments)
                        {
                            richTextBox1.AppendText($"{commentInfo}\n");
                            ScrollToBottom();
                        }
                        richTextBox1.AppendText("Note: Check YouTube Studio > Comments > Held for review to delete or approve these comments.\n");
                    }
                    else
                    {
                        richTextBox1.AppendText("\nNo comments with 'max' were moderated.\n");
                    }

                    richTextBox1.AppendText($"Banbanban completed at {DateTime.Now}.\n");
                    richTextBox1.AppendText($"Total comments set to 'heldForReview': {commentsModerated}\n");
                }
                ScrollToBottom();
            }
            catch (Exception ex)
            {
                richTextBox1.AppendText($"Error at {DateTime.Now}: {ex.Message}\n");
                ScrollToBottom();
            }
        }

        // แก้ไข button2_Click เป็น
        private async void button2_Click(object sender, EventArgs e)
        {
            await Banbanban(testFilteringMode: true);
        }


        // ฟังก์ชันช่วยเลื่อน RichTextBox ไปที่บรรทัดล่าสุด
        private void ScrollToBottom()
        {
            richTextBox1.SelectionStart = richTextBox1.Text.Length;
            richTextBox1.ScrollToCaret();
        }

        // ฟังก์ชันตรวจจับคำว่า "max" (ไม่สนใจตัวพิมพ์ใหญ่-เล็ก)
        private static bool ContainsMax(string text)
        {
            if (string.IsNullOrEmpty(text)) return false;

            // ดูข้อความต้นฉบับและแสดงข้อมูลตัวอักษรพิเศษเพื่อการวิเคราะห์
            // richTextBox1.AppendText($"Debug - Checking text: {text}\n");

            // 1. จับกรณีตัวอย่างที่ทราบแล้วโดยตรง (ตรงตามรูปแบบที่เจอได้ชัดเจน)
            if (text.Contains("𝙈А𝘟") || text.Contains("MА𝐗") ||
                text.Contains("𝘔А𝙓") || text.Contains("MА𝙓") ||
                text.Contains("мax") || text.Contains("mаx") ||
                text.Contains("мах") || text.Contains("max"))
            {
                // richTextBox1.AppendText("Debug - Detected known MAX pattern directly\n");
                return true;
            }

            // 2. แนวทางที่เข้มงวดมากขึ้น - แปลงข้อความแบบละเอียด
            string normalizedText = text.Normalize(NormalizationForm.FormKD);

            // 3. ตรวจสอบการมีอยู่ของอักขระพิเศษที่เกี่ยวข้องกับ M, A, X
            bool hasCyrillicA = normalizedText.Contains('\u0410') || normalizedText.Contains('\u0430'); // А, а
            bool hasCyrillicM = normalizedText.Contains('\u041C') || normalizedText.Contains('\u043C'); // М, м
            bool hasCyrillicX = normalizedText.Contains('\u0425') || normalizedText.Contains('\u0445'); // Х, х

            // 4. สร้างสตริงที่มีตัวอักษรทั้งหมดและทำการ normalize
            string lowercaseText = normalizedText.ToLower();

            // 5. แทนที่ตัวอักษรพิเศษด้วยตัวอักษรพื้นฐาน
            Dictionary<char, char> charMap = new Dictionary<char, char>
    {
        // Cyrillic และตัวอักษรที่คล้ายกับ M
        {'\u041C', 'm'}, {'\u043C', 'm'}, // М, м (Cyrillic)
        {'\u039C', 'm'}, {'\u03BC', 'm'}, // Μ, μ (Greek)
        {'\u217F', 'm'}, // ⅿ (Roman numeral)
        
        // Cyrillic และตัวอักษรที่คล้ายกับ A
        {'\u0410', 'a'}, {'\u0430', 'a'}, // А, а (Cyrillic)
        {'\u0391', 'a'}, {'\u03B1', 'a'}, // Α, α (Greek)
        {'\u0394', 'a'}, {'\u03B4', 'a'}, // Δ, δ (Greek Delta)
        {'@', 'a'}, {'4', 'a'}, {'∆', 'a'}, {'▲', 'a'}, {'△', 'a'},
        
        // Cyrillic และตัวอักษรที่คล้ายกับ X
        {'\u0425', 'x'}, {'\u0445', 'x'}, // Х, х (Cyrillic)
        {'\u03A7', 'x'}, {'\u03C7', 'x'}, // Χ, χ (Greek)
        {'×', 'x'}, {'✕', 'x'}, {'✖', 'x'}, {'✗', 'x'} // แก้ไขโดยลบ '×' ซ้ำ
    };

            // 6. แทนที่ทุกตัวอักษรพิเศษในแม็ป
            StringBuilder simplifiedText = new StringBuilder();
            foreach (char c in lowercaseText)
            {
                if (charMap.TryGetValue(c, out char mappedChar))
                {
                    simplifiedText.Append(mappedChar);
                }
                else
                {
                    // จัดการกับตัวอักษรที่อาจเป็น Mathematical Alphanumeric Symbols (e.g., 𝐌, 𝓜, etc.)
                    try
                    {
                        // ใช้ try-catch เพื่อป้องกัน IndexOutOfRangeException หรือข้อผิดพลาดอื่นๆ
                        int codePoint = char.ConvertToUtf32(lowercaseText, lowercaseText.IndexOf(c));
                        if (codePoint >= 0x1D400 && codePoint <= 0x1D7FF)
                        {
                            // ตรวจสอบว่าตัวอักษรนี้เป็น M, A หรือ X
                            char baseChar = GetBaseCharFromMathSymbol(codePoint);
                            if (baseChar != '\0')
                            {
                                simplifiedText.Append(baseChar);
                                continue;
                            }
                        }
                    }
                    catch (Exception)
                    {
                        // ถ้ามีข้อผิดพลาดในการแปลง codePoint ให้ใช้ตัวอักษรเดิม
                    }

                    // ใช้ตัวอักษรเดิมถ้าไม่ได้แทนที่
                    simplifiedText.Append(c);
                }
            }

            string finalProcessedText = simplifiedText.ToString();

            // 7. มีหลายวิธีในการตรวจสอบ "max" pattern

            // 7.1 ตรวจสอบแบบง่าย: มีตัวอักษร m, a, x ทั้งหมดในข้อความหรือไม่
            bool containsAllMAXChars = finalProcessedText.Contains('m') &&
                                       finalProcessedText.Contains('a') &&
                                       finalProcessedText.Contains('x');

            // 7.2 ตรวจสอบลำดับ: มีลำดับ m->a->x ในข้อความหรือไม่
            bool hasMAXSequence = false;
            int lastM = finalProcessedText.IndexOf('m');
            if (lastM >= 0)
            {
                int lastA = finalProcessedText.IndexOf('a', lastM);
                if (lastA > lastM)
                {
                    int lastX = finalProcessedText.IndexOf('x', lastA);
                    if (lastX > lastA)
                    {
                        hasMAXSequence = true;
                    }
                }
            }

            // 7.3 ใช้ Regex เพื่อตรวจสอบรูปแบบที่ซับซ้อนขึ้น (m...a...x)
            bool hasMAXRegexPattern = System.Text.RegularExpressions.Regex.IsMatch(
                finalProcessedText,
                @"m\W*a\W*x",  // m ตามด้วยอักขระที่ไม่ใช่ตัวอักษรหรือตัวเลข 0 ตัวขึ้นไป ตามด้วย a ตามด้วยอักขระที่ไม่ใช่ตัวอักษรหรือตัวเลข 0 ตัวขึ้นไป ตามด้วย x
                System.Text.RegularExpressions.RegexOptions.IgnoreCase
            );

            // 7.4 ตรวจสอบการมีอยู่ของตัวอักษร M, A, X เทียบกับตัวอักษรทั้งหมด (สัดส่วนสูง)
            int totalAlphaChars = finalProcessedText.Count(char.IsLetter);
            int maxChars = (finalProcessedText.Count(c => c == 'm') +
                           finalProcessedText.Count(c => c == 'a') +
                           finalProcessedText.Count(c => c == 'x'));
            bool highMAXRatio = totalAlphaChars > 0 && (double)maxChars / totalAlphaChars > 0.3;

            // 7.5 ตรวจสอบการมีอยู่ของ MAX ในชุดอักขระพิเศษที่ไม่สามารถแทนที่ได้
            bool hasSpecialMAXChars = (hasCyrillicM || hasCyrillicA || hasCyrillicX) &&
                                      (text.Contains('M') || text.Contains('m') ||
                                       text.Contains('A') || text.Contains('a') ||
                                       text.Contains('X') || text.Contains('x'));

            // 8. ตรวจสอบข้อความทั้งหมดด้วยการตัดอักขระพิเศษออกและดูว่ามี "max" หรือไม่
            string lettersOnly = new string(text.Where(char.IsLetter).ToArray()).ToLower();
            bool containsMAXSubstring = lettersOnly.Contains("max");

            // รวมผลการตรวจสอบทั้งหมด - ถ้ามีวิธีใดวิธีหนึ่งตรวจจับได้ ให้ถือว่าพบ MAX
            bool finalResult = containsAllMAXChars || hasMAXSequence || hasMAXRegexPattern ||
                               highMAXRatio || hasSpecialMAXChars || containsMAXSubstring;

            return finalResult;
        }

        // ฟังก์ชันช่วยในการแปลงสัญลักษณ์ทางคณิตศาสตร์เป็นตัวอักษรพื้นฐาน
        private static char GetBaseCharFromMathSymbol(int codePoint)
        {
            // Mathematical Alphanumeric Symbols (1D400–1D7FF)
            if (codePoint >= 0x1D400 && codePoint <= 0x1D433) // Math Bold Capital A-Z
                return (char)('A' + (codePoint - 0x1D400));
            if (codePoint >= 0x1D434 && codePoint <= 0x1D467) // Math Bold Small a-z
                return (char)('a' + (codePoint - 0x1D434));
            if (codePoint >= 0x1D468 && codePoint <= 0x1D49B) // Math Italic Capital A-Z
                return (char)('A' + (codePoint - 0x1D468));
            if (codePoint >= 0x1D49C && codePoint <= 0x1D4CF) // Math Italic Small a-z
                return (char)('a' + (codePoint - 0x1D49C));
            if (codePoint >= 0x1D4D0 && codePoint <= 0x1D503) // Math Bold Italic Capital A-Z
                return (char)('A' + (codePoint - 0x1D4D0));
            if (codePoint >= 0x1D504 && codePoint <= 0x1D537) // Math Bold Italic Small a-z
                return (char)('a' + (codePoint - 0x1D504));
            if (codePoint >= 0x1D538 && codePoint <= 0x1D56B) // Math Script Capital A-Z
                return (char)('A' + (codePoint - 0x1D538));
            if (codePoint >= 0x1D56C && codePoint <= 0x1D59F) // Math Script Small a-z
                return (char)('a' + (codePoint - 0x1D56C));

            // เพิ่มเติมกลุ่มอื่นๆ ตามต้องการ
            // ...

            // ไม่อยู่ในช่วงที่รู้จัก
            return '\0';
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            checkBox2.CheckedChanged -= new EventHandler(checkBox2_CheckedChanged);
            checkBox2.CheckedChanged += new EventHandler(checkBox2_CheckedChanged);

            textBox3.TextChanged -= new EventHandler(textBox3_TextChanged);
            textBox3.TextChanged += new EventHandler(textBox3_TextChanged);
            textBox1.Text = Properties.Settings.Default.LastChannelId;

        }

        private void SaveLastChannelId()
        {
            if (!string.IsNullOrWhiteSpace(textBox1.Text))
            {
                Properties.Settings.Default.LastChannelId = textBox1.Text;
                Properties.Settings.Default.Save();
            }
        }
        private async void button3_Click(object sender, EventArgs e)
        {
            richTextBox1.Clear();

            try
            {
                richTextBox1.AppendText($"Process started at {DateTime.Now}\n");
                ScrollToBottom();

                var secrets = GoogleClientSecrets.FromFile("secret.json").Secrets;

                var credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
                    secrets,
                    new[] {
                YouTubeService.Scope.Youtube,
                YouTubeService.Scope.YoutubeForceSsl,
                    },
                    "user",
                    CancellationToken.None,
                    new FileDataStore("YouTube.Comment.Auth.Store")
                );

                var youtubeService = new YouTubeService(new BaseClientService.Initializer()
                {
                    HttpClientInitializer = credential,
                    ApplicationName = "YouTube Comment Manager"
                });

                string commentId = "UgxiJVmmIz-WXDb1BtJ4AaABAg";

                richTextBox1.AppendText($"Attempting to mark comment as spam: {commentId}\n");
                ScrollToBottom();

                try
                {
                    // ลองขอข้อมูลความคิดเห็นก่อน
                    var commentRequest = youtubeService.Comments.List("snippet");
                    commentRequest.Id = commentId;
                    var commentResponse = await commentRequest.ExecuteAsync();

                    if (commentResponse.Items != null && commentResponse.Items.Count > 0)
                    {
                        richTextBox1.AppendText($"Found comment: {commentResponse.Items[0].Snippet.TextDisplay}\n");

                        // ทำเครื่องหมายเป็นสแปม
                        await youtubeService.Comments.MarkAsSpam(commentId).ExecuteAsync();
                        richTextBox1.AppendText("Comment marked as spam successfully.\n");
                    }
                    else
                    {
                        richTextBox1.AppendText("Comment not found. It may have been deleted already.\n");
                    }

                    ScrollToBottom();
                }
                catch (Google.GoogleApiException ex)
                {
                    richTextBox1.AppendText($"Failed to mark comment as spam: {ex.Message}\n");
                    if (ex.Error != null)
                    {
                        richTextBox1.AppendText($"Error Code: {ex.Error.Code}\n");
                        richTextBox1.AppendText($"Error Message: {ex.Error.Message}\n");
                        if (ex.Error.Errors != null && ex.Error.Errors.Count > 0)
                        {
                            foreach (var error in ex.Error.Errors)
                            {
                                richTextBox1.AppendText($"  - Domain: {error.Domain}\n");
                                richTextBox1.AppendText($"  - Reason: {error.Reason}\n");
                                richTextBox1.AppendText($"  - Message: {error.Message}\n");
                                richTextBox1.AppendText($"  - Location: {error.Location}\n");
                                richTextBox1.AppendText($"  - LocationType: {error.LocationType}\n");
                            }
                        }
                    }

                    ScrollToBottom();
                }

                richTextBox1.AppendText($"Process completed at {DateTime.Now}\n");
                ScrollToBottom();
            }
            catch (Exception ex)
            {
                richTextBox1.AppendText($"Error at {DateTime.Now}: {ex.Message}\n");
                ScrollToBottom();
            }
        }

        private async void button3_Click_1(object sender, EventArgs e)
        {
            // ล้าง RichTextBox ก่อนแสดงผลใหม่
            richTextBox1.Clear();

            try
            {
                richTextBox1.AppendText($"Moderation process started at {DateTime.Now}\n");
                ScrollToBottom();

                var secrets = GoogleClientSecrets.FromFile("secret.json").Secrets;

                // ขอการอนุญาตจากผู้ใช้ผ่าน OAuth 2.0 ใช้ทั้งสอง scope
                var credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
                    secrets,
                    new[] { YouTubeService.Scope.Youtube, YouTubeService.Scope.YoutubeForceSsl },
                    "user",
                    CancellationToken.None,
                    new FileDataStore("YouTube.Comment.Auth.Store")
                );

                // สร้าง YouTube Service
                var youtubeService = new YouTubeService(new BaseClientService.Initializer()
                {
                    HttpClientInitializer = credential,
                    ApplicationName = "YouTube Comment Manager"
                });

                // Comment ID ที่ต้องการจัดการ
                string commentIdToModerate = "UgxiJVmmIz-WXDb1BtJ4AaABAg";

                richTextBox1.AppendText($"Checking if comment with ID {commentIdToModerate} exists...\n");
                ScrollToBottom();

                // ตรวจสอบว่าคอมเมนต์ยังมีอยู่
                try
                {
                    var commentCheckRequest = youtubeService.Comments.List("snippet");
                    commentCheckRequest.Id = commentIdToModerate;
                    var commentCheckResponse = await commentCheckRequest.ExecuteAsync();

                    if (commentCheckResponse.Items != null && commentCheckResponse.Items.Count > 0)
                    {
                        richTextBox1.AppendText($"Found comment: {commentCheckResponse.Items[0].Snippet.TextDisplay}\n");
                        ScrollToBottom();

                        // ตั้งค่าสถานะเป็น heldForReview
                        richTextBox1.AppendText($"Attempting to set moderation status to 'heldForReview' for comment: {commentIdToModerate}\n");
                        ScrollToBottom();

                        // ใช้ Comments.SetModerationStatus อย่างถูกต้อง
                        var moderationRequest = youtubeService.Comments.SetModerationStatus(commentIdToModerate, CommentsResource.SetModerationStatusRequest.ModerationStatusEnum.HeldForReview);
                        await moderationRequest.ExecuteAsync();
                        richTextBox1.AppendText("Comment set to 'heldForReview' successfully.\n");
                        richTextBox1.AppendText("Note: Check YouTube Studio > Comments > Held for review to delete or approve this comment.\n");
                        ScrollToBottom();
                    }
                    else
                    {
                        richTextBox1.AppendText("Comment not found. It may have been deleted already.\n");
                        ScrollToBottom();
                    }
                }
                catch (Google.GoogleApiException ex)
                {
                    richTextBox1.AppendText($"Failed to check or set moderation status: {ex.Message}\n");
                    if (ex.Error != null)
                    {
                        richTextBox1.AppendText($"Error Code: {ex.Error.Code}\n");
                        richTextBox1.AppendText($"Error Message: {ex.Error.Message}\n");
                        if (ex.Error.Errors != null && ex.Error.Errors.Count > 0)
                        {
                            foreach (var error in ex.Error.Errors)
                            {
                                richTextBox1.AppendText($"  - Domain: {error.Domain}\n");
                                richTextBox1.AppendText($"  - Reason: {error.Reason}\n");
                                richTextBox1.AppendText($"  - Message: {error.Message}\n");
                                richTextBox1.AppendText($"  - Location: {error.Location}\n");
                                richTextBox1.AppendText($"  - LocationType: {error.LocationType}\n");
                            }
                        }
                    }
                    ScrollToBottom();
                }

                richTextBox1.AppendText($"Process completed at {DateTime.Now}\n");
                ScrollToBottom();
            }

            catch (Exception ex)
            {
                richTextBox1.AppendText($"Error at {DateTime.Now}: {ex.Message}\n");
                ScrollToBottom();
            }
        }

        private string selectedSecretFile = "secret.json"; // ไฟล์เริ่มต้น
        private string tokenStorePath = ""; // พาธที่เก็บ token

        private async void button4_Click(object sender, EventArgs e)
        {
            // เปิด Dialog ให้เลือกไฟล์ secret
            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*";
                openFileDialog.Title = "Select Secret File";
                openFileDialog.InitialDirectory = Directory.GetCurrentDirectory();

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    selectedSecretFile = openFileDialog.FileName;

                    // สร้างชื่อโฟลเดอร์จากชื่อไฟล์ (ไม่รวมนามสกุล)
                    string fileName = Path.GetFileNameWithoutExtension(selectedSecretFile);

                    // สร้างพาธไปยัง Desktop/alltoken/{ชื่อไฟล์}
                    string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                    string allTokenPath = Path.Combine(desktopPath, "alltoken");
                    tokenStorePath = Path.Combine(allTokenPath, fileName);

                    // ตรวจสอบและสร้างโฟลเดอร์ถ้ายังไม่มี
                    if (!Directory.Exists(allTokenPath))
                    {
                        Directory.CreateDirectory(allTokenPath);
                    }

                    if (!Directory.Exists(tokenStorePath))
                    {
                        Directory.CreateDirectory(tokenStorePath);
                    }

                    richTextBox1.Clear();
                    richTextBox1.AppendText($"Switched to secret file: {selectedSecretFile}\n");
                    richTextBox1.AppendText($"Token will be stored in: {tokenStorePath}\n");
                    ScrollToBottom();

                    MessageBox.Show($"Switched to secret file: {selectedSecretFile}.\nToken will be stored in: {tokenStorePath}\nClick 'Banbanban' to proceed.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
        }


        // เพิ่มฟังก์ชันสำหรับแยกรหัสวิดีโอ YouTube จาก URL
        private string ExtractVideoIdFromUrl(string url)
        {
            if (string.IsNullOrEmpty(url))
                return null;

            // ลองหลายรูปแบบของ URL YouTube
            // 1. youtube.com/watch?v=VIDEO_ID
            // 2. youtu.be/VIDEO_ID
            // 3. youtube.com/v/VIDEO_ID
            // 4. youtube.com/embed/VIDEO_ID

            try
            {
                // รองรับกรณีที่ URL อาจไม่ถูกต้อง
                Uri uri = new Uri(url);

                // รูปแบบ youtu.be/VIDEO_ID
                if (uri.Host == "youtu.be")
                {
                    return uri.AbsolutePath.TrimStart('/');
                }

                // รูปแบบอื่นๆ ของ YouTube
                if (uri.Host.Contains("youtube"))
                {
                    // รูปแบบ youtube.com/watch?v=VIDEO_ID
                    if (uri.AbsolutePath.StartsWith("/watch"))
                    {
                        var query = System.Web.HttpUtility.ParseQueryString(uri.Query);
                        return query["v"];
                    }

                    // รูปแบบ youtube.com/v/VIDEO_ID หรือ youtube.com/embed/VIDEO_ID
                    if (uri.AbsolutePath.StartsWith("/v/") || uri.AbsolutePath.StartsWith("/embed/"))
                    {
                        return uri.AbsolutePath.Split('/')[2];
                    }
                }
            }
            catch
            {
                // ถ้า URL ไม่ถูกต้อง ลองแยก VIDEO_ID โดยตรง
                if (url.Length == 11 && !url.Contains("/") && !url.Contains("?"))
                {
                    return url; // อาจจะเป็น VIDEO_ID โดยตรง
                }
            }

            return null;
        }
        private async Task ProcessVideoComments(YouTubeService youtubeService, string videoId)
        {
            int commentsModerated = 0;
            var moderatedComments = new List<string>();

            string commentPageToken = null;
            do
            {
                var commentRequest = youtubeService.CommentThreads.List("snippet");
                commentRequest.VideoId = videoId;
                commentRequest.MaxResults = 100;
                commentRequest.PageToken = commentPageToken;

                var commentResponse = await commentRequest.ExecuteAsync();

                foreach (var commentThread in commentResponse.Items)
                {
                    var commentText = commentThread.Snippet.TopLevelComment.Snippet.TextOriginal;
                    richTextBox1.AppendText($"Found comment: {commentText}\n");
                    ScrollToBottom();

                    if (ContainsMax(commentText))
                    {
                        string commentId = commentThread.Snippet.TopLevelComment.Id;
                        richTextBox1.AppendText($"Comment with 'max' found: {commentText}\n");
                        richTextBox1.AppendText($"Comment ID: {commentId}\n");

                        // ตรวจสอบว่าคอมเมนต์ยังมีอยู่
                        try
                        {
                            var commentCheckRequest = youtubeService.Comments.List("snippet");
                            commentCheckRequest.Id = commentId;
                            var commentCheckResponse = await commentCheckRequest.ExecuteAsync();

                            if (commentCheckResponse.Items != null && commentCheckResponse.Items.Count > 0)
                            {
                                // ตั้งสถานะเป็น heldForReview
                                richTextBox1.AppendText($"Attempting to set moderation status to 'heldForReview' for comment: {commentId}\n");
                                ScrollToBottom();

                                var moderationRequest = youtubeService.Comments.SetModerationStatus(commentId, CommentsResource.SetModerationStatusRequest.ModerationStatusEnum.HeldForReview);
                                await moderationRequest.ExecuteAsync();
                                richTextBox1.AppendText($"✅ Successfully set comment to 'heldForReview': {commentId}\n");
                                moderatedComments.Add($"Comment: {commentText}\n  - Comment ID: {commentId}");
                                commentsModerated++;
                                ScrollToBottom();
                            }
                            else
                            {
                                richTextBox1.AppendText($"Comment not found (possibly already deleted): {commentId}\n");
                                ScrollToBottom();
                            }
                        }
                        catch (Google.GoogleApiException ex)
                        {
                            richTextBox1.AppendText($"❌ Failed to set moderation status: {ex.Message}\n");
                            if (ex.Error != null)
                            {
                                richTextBox1.AppendText($"Error Code: {ex.Error.Code}\n");
                                richTextBox1.AppendText($"Error Message: {ex.Error.Message}\n");
                                if (ex.Error.Errors != null && ex.Error.Errors.Count > 0)
                                {
                                    foreach (var error in ex.Error.Errors)
                                    {
                                        richTextBox1.AppendText($"  - Domain: {error.Domain}\n");
                                        richTextBox1.AppendText($"  - Reason: {error.Reason}\n");
                                        richTextBox1.AppendText($"  - Message: {error.Message}\n");
                                        richTextBox1.AppendText($"  - Location: {error.Location}\n");
                                        richTextBox1.AppendText($"  - LocationType: {error.LocationType}\n");
                                    }
                                }
                            }
                            ScrollToBottom();
                        }
                    }
                }

                commentPageToken = commentResponse.NextPageToken;
                await Task.Delay(1000); // หน่วงเวลาเพื่อไม่ให้เกิน quota
            } while (commentPageToken != null);

            // แสดงคอมเมนต์ที่ถูกตั้งสถานะในก้อนเดียวกัน
            if (moderatedComments.Count > 0)
            {
                richTextBox1.AppendText("\nModerated Comments:\n");
                ScrollToBottom();
                foreach (var commentInfo in moderatedComments)
                {
                    richTextBox1.AppendText($"{commentInfo}\n");
                    ScrollToBottom();
                }
                richTextBox1.AppendText("Note: Check YouTube Studio > Comments > Held for review to delete or approve these comments.\n");
            }
            else
            {
                richTextBox1.AppendText("\nNo comments with 'max' were moderated.\n");
            }

            richTextBox1.AppendText($"Total comments set to 'heldForReview': {commentsModerated}\n");
            ScrollToBottom();
        }

        private void label1_Click(object sender, EventArgs e)
        {

        }

        private void checkBox2_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox2.Checked)
            {
                // Read interval from textbox3 if available
                if (!string.IsNullOrWhiteSpace(textBox3.Text) && int.TryParse(textBox3.Text, out int intervalMinutes))
                {
                    banbanLoopIntervalMinutes = intervalMinutes;
                }

                // Stop any existing timer before starting a new one
                StopBanbanLoop();

                // Start the loop
                StartBanbanLoop();
                richTextBox1.AppendText($"Banban loop activated. Will run every {banbanLoopIntervalMinutes} minutes.\n");
            }
            else
            {
                // Stop the loop
                StopBanbanLoop();
                richTextBox1.AppendText($"Banban loop deactivated.\n");
            }
            ScrollToBottom();
        }

        private void StartBanbanLoop()
        {
            // Remove any existing event handler before adding a new one
            banbanLoopTimer.Tick -= new EventHandler(banbanLoopTimer_Tick);
            banbanLoopTimer.Tick += new EventHandler(banbanLoopTimer_Tick);

            banbanLoopTimer.Interval = banbanLoopIntervalMinutes * 60 * 1000; // Convert minutes to milliseconds
            banbanLoopTimer.Start();

            richTextBox1.AppendText($"Next banban run scheduled at: {DateTime.Now.AddMinutes(banbanLoopIntervalMinutes)}\n");
            ScrollToBottom();
        
        }

        // เมธอดสำหรับหยุดการวนซ้ำ banban
        private void StopBanbanLoop()
        {
            banbanLoopTimer.Stop();
            banbanLoopTimer.Tick -= banbanLoopTimer_Tick;
        }

        private async void banbanLoopTimer_Tick(object sender, EventArgs e)
        {
            // หยุด timer ชั่วคราวระหว่างทำงาน
            banbanLoopTimer.Stop();

            richTextBox1.AppendText($"\n=== SCHEDULED BANBAN at {DateTime.Now} ===\n");
            ScrollToBottom();

            // เรียกใช้ Banbanban
            await Banbanban();

            // เริ่ม timer ใหม่หลังจากทำงานเสร็จ ถ้า checkbox ยังถูกติ๊กอยู่
            if (checkBox2.Checked)
            {
                banbanLoopTimer.Start();
                richTextBox1.AppendText($"\nNext banban run scheduled at: {DateTime.Now.AddMinutes(banbanLoopIntervalMinutes)}\n");
                ScrollToBottom();
            }
        }

        private void textBox3_TextChanged(object sender, EventArgs e)
        {
            if (int.TryParse(textBox3.Text, out int intervalMinutes) && intervalMinutes > 0)
            {
                banbanLoopIntervalMinutes = intervalMinutes;

                // อัพเดท interval ของ timer ถ้ากำลังทำงานอยู่
                if (checkBox2.Checked)
                {
                    banbanLoopTimer.Interval = banbanLoopIntervalMinutes * 60 * 1000;
                    richTextBox1.AppendText($"Banban loop interval changed to {banbanLoopIntervalMinutes} minutes.\n");
                    richTextBox1.AppendText($"Next run scheduled at: {DateTime.Now.AddMinutes(banbanLoopIntervalMinutes)}\n");
                    ScrollToBottom();
                }
            }
        }
    }
}

