using System.Text;
using MailKit;
using MailKit.Net.Imap;
using MailKit.Search;
using SendMail.Data;
using SendMail.Models;

namespace SendMail.Services
{
    public class SweepMail
    {

        public async Task<List<ResponseEmailTrack>> FetchReply()
        {
            // สร้าง IMAP instance
            using ImapClient client = new ImapClient();

            // เชื่อมต่อเข้าไปที่ imap protocol ของ gmail แล้วเปิด SSL ด้วย
            await client.ConnectAsync("imap.gmail.com", 993, MailKit.Security.SecureSocketOptions.SslOnConnect);

            // ยืนยันตัวตนด้วย email และ app password
            await client.AuthenticateAsync(App.SENDER_EMAIL, App.SENDER_PASSWORD);
            
            // เลือก email folder ชื่อ index (หน้าหลัก)
            var inbox = client.Inbox;

            // เปิดโฟลเดอร์
            await inbox.OpenAsync(FolderAccess.ReadOnly);

            // ค้นหาอีเมลที่ subject มีคำว่า Re:, re:, ตอบกลับ: และจะต้องมีคำว่า ขอตรวจสอบยอดเงินลูกค้า
            var results = await inbox.SearchAsync(
                SearchQuery.SubjectContains("Re:")
                .Or(SearchQuery.SubjectContains("re:"))
                .Or(SearchQuery.SubjectContains("ตอบกลับ:"))
                .And(SearchQuery.SubjectContains("ขอตรวจสอบยอดเงินลูกค้า"))
            );;

            // สร้าง email track instance สำหรับจัดเก็บผลลัพธ์
            List<ResponseEmailTrack> emailTracks = new List<ResponseEmailTrack>();

            // จัดการข้อมากจากใหม่ไปเก่า
            for (int i = results.Count - 1; i >= 0; i--)
            {
                // ดึง id ของ email ออกมา
                var uid = results[i]; 

                // ดึง email ด้วย id
                var message = inbox.GetMessage(uid);

                // ดึง textBody โดยเราจะลบ ช่องว่างที่เกิดจาก \r ออกและแยก string ออกจากกันด้วย \n
                // โดยเราดึงเอา index ที่ 0 เพราะจะเป็นข้อความที่การตอบกลับ นอกนั้นอาจเป็นข้อความขยะ
                string replyMessage = message.TextBody.Replace("\r", "").Split("\n")[0];
                emailTracks.Add(new ResponseEmailTrack()
                {
                    TrackId = this.GetTrackId(message.Subject),
                    Description = replyMessage,
                    Amount = this.FilterAmountFromText(replyMessage)
                });
            }

            // ยกเลิกการเชื่อมต่อกับ imap gmail
            await client.DisconnectAsync(true);

            return emailTracks;
        }

        private string GetTrackId(string subject)
        {
            // เราต้องระบุตำแหน่งของ [] ให้ได้ก่อนเพราะ Track ID จะถูกเก็บอยุ๋ระหว่างนี้
            // จัดเก็บตำแหน่งเริ่มต้นของ [
            int? start = null;

            // จัดเก็บตำแหน่งสิ้นสุดของ ]
            int? end = null;

            // ค้นหาตำแหน่งของ start และ end
            for (int index = 0; index < subject.Length; index++)
            {
                if (subject[index].Equals('[')) start = index;
                if (subject[index].Equals(']')) end = index;

                // ถ้าทั้งคู่ไม่ใช่ null แสดงว่าเราได้ตำแหน่งของทั้ง 2 แล้ว ไม่จำเป็นต้อง loop อีกต่อไป ก็สั่ง break ไปเลย
                if (start != null && end != null) break;
            }

            // ถ้าหลุด loop แล้วแต่อันใดอันหนึ่งยังเป็น null อยู่แสดงว่า subject นั้นผิด
            if (start == null && end == null) throw new Exception("subject is invalid.");

            // อาจมีการต่อ string จำนวนมาก, เพื่อประหยัด memory เราจึงใช้ String Builder
            StringBuilder trackBuilder = new StringBuilder();
            for (int index = (int) start; index <= end; index++)
            {
                // หยิบเฉพาะตัวเลขเพราะ track id เป็นตัวเลขเท่านั้น
                // ถ้าเป็น ตัวอักษร ที่อยู่ระหว่าง 0 - 9 แสดงว่าตัวอักษรนั้นคือตัวเลข
                bool digitCondition = (subject[index] >= '0' && subject[index] <= '9');
                if (digitCondition) trackBuilder.Append(subject[index]);
            }

            // แปลง StringBuilder ให้เป็น string แล้วส่งคืน
            return trackBuilder.ToString();
        }

        public decimal FilterAmountFromText(string message)
        {
            if (message.Length == 0) return 0;

            string amountText = string.Empty;
            for (int messageIndex = 0; messageIndex < message.Length; messageIndex++)
            {
                char character = message[messageIndex];

                // เงื่อนไข ถ้าตัวอักษรอยู่ระหว่าง 0 - 9
                bool numberCondition = (character >= '0' && character <= '9');

                // เงื่อนไข ถ้าตัวอักษรเป็น '.'
                bool dotCondition = (
                    (character == '.' || character == ',')
                    && (message[messageIndex - 1] >= '0' && message[messageIndex - 1] <= '9') // ตำแหน่งก่อนหน้า '.' เป็นตัวเลข
                    && messageIndex + 1 < message.Length // index ถัดไปจะต้องน้อยกว่าขนาดของ message
                    && (message[messageIndex + 1] >= '0' && message[messageIndex + 1] <= '9') // ตำแหน่งถัดไปจะต้องเป็นตัวอักษร
                );

                bool nextIsNumberOrDotCondition = (
                    messageIndex + 1 < message.Length 
                    && (
                            (message[messageIndex + 1] >= '0' && message[messageIndex + 1] <= '9') 
                            || message[messageIndex + 1] == '.' 
                            || message[messageIndex + 1] == ','
                        )
                );

                
                if (numberCondition) // ถ้าอยู๋ระหว่าง 0 - 9
                {
                    // ถ้าหากตำแหน่งของตัวเลข เท่ากับ ตัวสุดท้ายของ message ให้เพิ่มลงไปใน amountText เลย
                    if (messageIndex == message.Length - 1)
                    {
                        amountText += character;
                    }
                    else 
                    {
                        // ถ้า ตัวอักษร ไม่ใช่ '.' ให้้ทำการเพิ่มตัวอักษรนั้นลงไปใน amountText
                        if (message[messageIndex + 1] != '.')
                        {   
                            amountText += character;
                        }
                        else 
                        {
                            // ถ้า ตัวอักษร ตำแหน่งถัดไปต่อจาก '.' เป็นตัวเลข ให้เพิ่ม text ลงไปใน amountText
                            if ((messageIndex + 2) <= message.Length && message[messageIndex + 2] >= '0' && message[messageIndex + 2] <= '9') amountText += character;
                        }
                    }

                    if (!nextIsNumberOrDotCondition) break;
                }
                else if (dotCondition) // ถ้าเป็น '.'
                {
                    amountText += character; // เพิ่มตัวอักษรลง amountText ไปเลย
                }
            }

            // ถ้า amountText เป็นค่าว่าง ให้ส่งคืน 0, ถ้าไม่ใช่ให้แปลงเป็น decimal และส่งคืน
            return (string.IsNullOrEmpty(amountText)) ? 0 : Convert.ToDecimal(amountText);
        }
    }
}