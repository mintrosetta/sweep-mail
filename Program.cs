using SendMail.Services;

public class Program
{
    public static async Task Main(string[] args) 
    {

        SweepMail sweepMail = new SweepMail();
        var emailTracks = await sweepMail.FetchReply();

        foreach (var emailTrack in emailTracks)
        {
            Console.WriteLine($"Track ID: {emailTrack.TrackId} has reply message {emailTrack.Description} and system can detect amount from reply is {emailTrack.Amount} bath.");
        }

        MailService mailService = new MailService();
        string[] receives = new string[] { "mint.rosetta2001@gmail.com" };
        string subject = "ขอตรวจสอบยอดเงินลูกค้า [TICKET ID: 10]";
        string body = "ขอตรวจสอบยอดเงินของลูกค้าชื่อ นาย ภัทรพงค์ ชุมวงศ์จันทร์ เป็นจำนวนเงิน 700 บาท ถูกต้องหรือไม่ ?";
        await mailService.SendAsync(receives, subject, body);
    }
}