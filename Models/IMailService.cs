using MyPhamCheilinus.Models;

namespace MyPhamCheilinus.Models
{
    public interface IMailService
    {
        bool SendMail(MailData mailData);
    }
}
