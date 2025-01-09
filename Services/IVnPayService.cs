using MyPhamCheilinus.ModelViews;

namespace MyPhamCheilinus.Services
{
    public interface IVnPayService
    {
        string CreatePaymentUrl(HttpContext context, VnPaymentRequestModel model);
         VnPaymentResponseModel PaymentExecute(IQueryCollection collections);
    }
}
