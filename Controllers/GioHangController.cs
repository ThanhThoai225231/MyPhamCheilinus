using AspNetCoreHero.ToastNotification.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using MyPhamCheilinus.Controllers;
using MyPhamCheilinus.Helpper;
using MyPhamCheilinus.Infrastructure;
using MyPhamCheilinus.Models;
using MyPhamCheilinus.ModelViews;
using MyPhamCheilinus.Services;
using Org.BouncyCastle.Asn1.Pkcs;
using System.Data;
using System.Net;
using System.Web.Helpers;


namespace MyPhamCpuheilinus.Controllers
{
    public class GioHangController : Controller
    {
        private readonly _2023MyPhamContext _context;
        private readonly PaypalClient _paypalClient;
        private readonly IMailService _mailService;
        public GioHang? GioHang { get; set; }
        public KhachHang? KhachHang { get; set; }
        _2023MyPhamContext db = new _2023MyPhamContext();
        public INotyfService _notifyService { get; }
        private readonly ILogger<GioHangController> _logger;
        private readonly IVnPayService _vnPayservice;

        public GioHangController(ILogger<GioHangController> logger, INotyfService notifyService, IVnPayService vnPayService, IMailService mailService, _2023MyPhamContext context, PaypalClient paypalClient)
        {
            _logger = logger;
            _notifyService = notifyService;
            _vnPayservice = vnPayService;
            _mailService = mailService;
            _context = context;
            _paypalClient = paypalClient;
        }

        [Authorize(Roles = "Customer, Admin, Employee")]

        [HttpPost]
        public IActionResult AddGioHang1(string maSanPham)
        {
            SanPham? sanpham = db.SanPhams.FirstOrDefault(p => p.MaSanPham == maSanPham);
            if (sanpham.Slkho == 0)
            {
                _notifyService.Error("Sản phẩm đã hết hàng");
                return RedirectToAction("SanPhamTheoDanhMuc");
            }

            if (sanpham != null)
            {
                GioHang = HttpContext.Session.GetJson<GioHang>("giohang") ?? new GioHang();
                GioHang.AddItem(sanpham, 1);
                HttpContext.Session.SetJson("giohang", GioHang);
                TempData["ThongBao"] = "Sản phẩm đã được thêm vào giỏ hàng";
            }
            return View("GioHang", GioHang);
        }
        [HttpPost]
        public IActionResult AddGioHang(string maSanPham, int soLuong)
        {
            SanPham? sanpham = db.SanPhams.FirstOrDefault(p => p.MaSanPham == maSanPham);
            if (sanpham == null)
            {
                // Xử lý khi không tìm thấy sản phẩm
                return NotFound();
            }

            if (sanpham.Slkho == 0)
            {
                _notifyService.Error("Sản phẩm đã hết hàng");
                return RedirectToAction("SanPhamTheoDanhMuc");
            }

            if ( soLuong > sanpham.Slkho)
            {
                _notifyService.Error("Số lượng không lớn hơn số lượng trong kho");
                return RedirectToAction("SanPhamTheoDanhMuc");
            }

            GioHang = HttpContext.Session.GetJson<GioHang>("giohang") ?? new GioHang();
            GioHang.AddItem(sanpham, soLuong);
            HttpContext.Session.SetJson("giohang", GioHang);
            TempData["ThongBao"] = "Sản phẩm đã được thêm vào giỏ hàng";

            return View("GioHang", GioHang);
        }

        [HttpPost]
        public IActionResult UpdateQuantity(string maSanPham, int soLuong)
        {
            // Lấy giỏ hàng từ Session
            GioHang gioHang = HttpContext.Session.GetJson<GioHang>("giohang") ?? new GioHang();

            // Tìm dòng sản phẩm trong giỏ hàng
            GioHangLine line = gioHang.Lines.FirstOrDefault(l => l.SanPham.MaSanPham == maSanPham);

            if (line != null)
            {
                // Nếu số lượng mới nhập vào là 0, loại bỏ sản phẩm khỏi giỏ hàng
                if (soLuong == 0)
                {
                    gioHang.Lines.Remove(line);
                }
                else
                {
                    // Cập nhật số lượng sản phẩm trong giỏ hàng
                    gioHang.UpdateQuantity(maSanPham, soLuong);
                }

                // Lưu giỏ hàng đã được cập nhật vào Session
                HttpContext.Session.SetJson("giohang", gioHang);
            }

            // Trả về trang xem giỏ hàng
            return RedirectToAction("ViewGioHang", "GioHang");
        }


        [HttpPost]

        public IActionResult Remove_1_FromGioHang(string maSanPham)
        {
            GioHang gioHang = HttpContext.Session.GetJson<GioHang>("giohang") ?? new GioHang();

            // Tìm kiếm sản phẩm trong giỏ hàng
            GioHangLine lineToRemove = gioHang.Lines.FirstOrDefault(line => line.SanPham.MaSanPham == maSanPham);

            // Nếu sản phẩm được tìm thấy và số lượng lớn hơn 0, giảm số lượng đi 1
            if (lineToRemove != null && lineToRemove.SoLuong > 0)
            {
                gioHang.AddItem(lineToRemove.SanPham, -1);

                // Nếu số lượng sau khi giảm bằng 0, xóa sản phẩm khỏi giỏ hàng
                if (lineToRemove.SoLuong == 0)
                {
                    gioHang.Lines.Remove(lineToRemove);
                }

                var tongTien = gioHang.ComputeTotalValues();
                ViewBag.TongTien = tongTien;
            }

            HttpContext.Session.SetJson("giohang", gioHang);

            return View("GioHang", gioHang);
        }



        public IActionResult RemoveFromGioHang(string maSanPham)
        {
            SanPham? sanpham = db.SanPhams.FirstOrDefault(p => p.MaSanPham == maSanPham);
            if (sanpham != null)
            {
                GioHang = HttpContext.Session.GetJson<GioHang>("giohang");
                GioHang.RemoveLine(sanpham);
                HttpContext.Session.SetJson("giohang", GioHang);
            }
            return View("GioHang", GioHang);
        }

        public IActionResult CheckOut( int maKM)
        {
            ViewBag.PaypalClientdId = _paypalClient.ClientId;
            ViewBag.MaKM = maKM;
			// Lấy AccountId từ session
			var taikhoanID = HttpContext.Session.GetString("AccountId");
            if (string.IsNullOrEmpty(taikhoanID))
            {
                // Nếu chưa đăng nhập, có thể chuyển hướng người dùng đến trang đăng nhập
                return RedirectToAction("Login", "Accounts");
            }
            int id;
			int.TryParse(taikhoanID, out id);
			var khachHang = _context.Accounts.SingleOrDefault(kh => kh.AccountId == id);

			var khuyenMai = _context.KhuyenMais.SingleOrDefault(km => km.MaKM == maKM && km.AccountId == khachHang.AccountId);
            double TongGiamGia = 0;
            if(khuyenMai == null)
            {
                maKM = 0;
                TongGiamGia = 0;
            }
            else
            {
                TongGiamGia = khuyenMai.GiaGiam;
            }

			if (khuyenMai == null && maKM != 0)
			{
				ViewBag.ErrorMessage = "Mã giảm giá không hợp lệ.";
				// Hoặc có thể sử dụng TempData nếu muốn giữ thông báo qua một redirect
				// TempData["ErrorMessage"] = "Mã giảm giá không hợp lệ.";
			}

			// Kiểm tra đăng nhập
			

            if (int.TryParse(taikhoanID, out var accountId))
            {
                // Truy vấn danh sách KhachHang thuộc Account có AccountId tương ứng
                var danhSachKhachHang = db.KhachHangs
                    .Where(kh => kh.AccountId == accountId)
                    .ToList();

                // Lấy thông tin giỏ hàng từ session
                var gioHang = HttpContext.Session.GetJson<MyPhamCheilinus.Models.GioHang>("giohang") ?? new MyPhamCheilinus.Models.GioHang();

                
                if (khuyenMai != null)
                {
                    gioHang.TienGiam = khuyenMai.GiaGiam;
                    HttpContext.Session.SetInt32("MaKM", khuyenMai.MaKM);
                }

                HttpContext.Session.SetString("TongGiamGia", TongGiamGia.ToString());

                gioHang.PhiVanChuyen = 10000;
                // Kết hợp danh sách KhachHang và thông tin giỏ hàng
                var model = new Tuple<List<MyPhamCheilinus.Models.KhachHang>, MyPhamCheilinus.Models.GioHang>(danhSachKhachHang, gioHang);

                // Trả về View với model chứa cả danh sách KhachHang và thông tin giỏ hàng
                return View(model);
            }

            // Nếu có lỗi xử lý, có thể chuyển hướng người dùng đến trang lỗi hoặc trang mặc định
            return RedirectToAction("Login", "Accounts");
        }
        public IActionResult ViewGioHang()
        {
            var gioHang = HttpContext.Session.GetJson<GioHang>("giohang") ?? new GioHang();
            return View(gioHang);
        }
        private int GenerateUniqueCustomerCode()
        {
            Random random = new Random();
            int randomNumber = random.Next(10000, 99999); // Sinh ra số ngẫu nhiên từ 10,000 đến 99,999

            return randomNumber;
        }
        private string GenerateUniqueOrderCode()
        {
            Random random = new Random();
            string customerCode;

            // Lặp cho đến khi tạo ra một mã đơn hàng không trùng lặp trong CSDL
            do
            {
                int randomNumber = random.Next(10000, 99999); // Sinh ra số ngẫu nhiên từ 10,000 đến 99,999
                customerCode = "DH" + randomNumber.ToString();
            }
            while (OrderCodeExists(customerCode)); // Kiểm tra xem mã đơn hàng đã tồn tại trong CSDL chưa

            return customerCode;
        }

        private bool OrderCodeExists(string orderCode)
        {
            // Truy vấn CSDL để kiểm tra xem mã đơn hàng đã tồn tại trong CSDL chưa
            // Giả sử bạn sử dụng Entity Framework Core
            bool exists = db.DonHangs.Any(o => o.MaDonHang == orderCode);

            return exists;
        }

        private int GeneratePromoCode()
        {
            Random random = new Random();
            int promoCode = random.Next(1000, 9999); // Sinh ra số ngẫu nhiên từ 1000 đến 9999
            return promoCode;
        }

        private bool IsPromoCodeExists(int promoCode)
        {
            // Thực hiện truy vấn đến cơ sở dữ liệu để kiểm tra xem mã khuyến mãi đã tồn tại chưa
            var existingPromoCode = _context.KhuyenMais.FirstOrDefault(p => p.MaKM == promoCode);

            // Nếu mã khuyến mãi đã tồn tại trong CSDL, trả về true
            // Ngược lại, trả về false
            return existingPromoCode != null;
        }



        //   public ActionResult ThanhToan(string maKhachHang, string hoTen, string soDienThoai, string diaChi, string email)
        //   {
        //       var taikhoanID = HttpContext.Session.GetString("AccountId");
        //       var khachhang = new KhachHang
        //       {
        //           //MaKhachHang = GenerateUniqueCustomerCode(),
        //           TenKhachHang = hoTen,
        //           DiaChi = diaChi,
        //           SoDienThoai = soDienThoai,
        //           AccountId= Convert.ToInt32(taikhoanID)
        //       };
        //       db.KhachHangs.Add(khachhang);
        //       db.SaveChanges();

        //       var gioHang = HttpContext.Session.GetJson<GioHang>("giohang");
        //       var donHang = new DonHang
        //       {
        //           MaDonHang = GenerateUniqueOrderCode(),
        //           NgayDatHang = DateTime.Now,
        //           TongTien = gioHang.ComputeTotalValues(),
        //           TrangThaiDonHang = 1,
        //           MaKhachHang = khachhang.MaKhachHang,
        //           ThanhToan = true,
        //           VanChuyen = 1,
        //           PhiVanChuyen = 10000
        //       };
        //       db.DonHangs.Add(donHang);
        //       db.SaveChanges();
        //       foreach (var line in gioHang.Lines)
        //       {
        //           var chiTietDonHang = new ChiTietDonHang
        //           {
        //               MaDonHang = donHang.MaDonHang,
        //               MaSanPham = line.SanPham.MaSanPham,
        //               SoLuong = line.SoLuong,
        //               GiaBan = line.SanPham.Gia
        //           };
        //           db.ChiTietDonHangs.Add(chiTietDonHang);
        //           RemoveFromGioHang(line.SanPham.MaSanPham);
        //       }

        //       db.SaveChanges();

        //       foreach (var line in gioHang.Lines)
        //       {
        //           ;
        //           RemoveFromGioHang(line.SanPham.MaSanPham);
        //       }
        //;

        //       gioHang.Clear();


        //       return View("Success");
        //   }






        [HttpPost]
        public IActionResult SaveAddress(string hoTen, string diaChi, string soDienThoai, string email)
        {
            try
            {
                var taikhoanID = HttpContext.Session.GetString("AccountId");

                // Kiểm tra và chuyển đổi taikhoanID thành int
                if (!int.TryParse(taikhoanID, out int accountId))
                {
                    return Json(new { success = false, message = "AccountId không hợp lệ" });
                }

                // Tạo đối tượng KhachHang
                var khachhang = new KhachHang
                {
                    //MaKhachHang = GenerateUniqueCustomerCode(), // Cần triển khai hàm này
                    TenKhachHang = hoTen,
                    DiaChi = diaChi,
                    SoDienThoai = soDienThoai,
                    AccountId = accountId
                };

                // Lưu đối tượng vào cơ sở dữ liệu
                db.KhachHangs.Add(khachhang);
                db.SaveChanges();

                return RedirectToAction("CheckOut");
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi: " + ex.Message });
            }
        }
        [HttpPost]
        public ActionResult ThanhToan(int selectedKhachHang, string payment = "COD")
        {
            try
            {
                HttpContext.Session.SetInt32("SelectedCustomerId", selectedKhachHang);

                double TongGiamGia = double.Parse(HttpContext.Session.GetString("TongGiamGia"));
                var maKM = HttpContext.Session.GetInt32("MaKM");

                var khuyenMai1 = _context.KhuyenMais.SingleOrDefault(km => km.MaKM == maKM);
                if (khuyenMai1 != null)
                {
                    _context.KhuyenMais.Remove(khuyenMai1);
                    _context.SaveChanges();
                }
                // Lấy thông tin của KhachHang đã chọn
                var selectedKhachHangObject = db.KhachHangs.Find(selectedKhachHang);

                if (payment == "Thanh toán VnPay")
                {
                    var gioHang = HttpContext.Session.GetJson<GioHang>("giohang");

                    var vnPayModel = new VnPaymentRequestModel
                    {
                        Amount = gioHang.ComputeTotalValues() + gioHang.PhiVanChuyen - TongGiamGia,
                        CreatedDate = DateTime.Now,
                        Description = $"{selectedKhachHangObject.TenKhachHang} {selectedKhachHangObject.SoDienThoai}",
                        FullName = selectedKhachHangObject.TenKhachHang,
                        OrderId = GenerateUniqueOrderCode(),
                        //CustomerId = selectedKhachHangObject.MaKhachHang.ToString()

                    };
                    return Redirect(_vnPayservice.CreatePaymentUrl(HttpContext, vnPayModel));
                }

                if (selectedKhachHangObject != null)
                {
                    // Thực hiện các bước thanh toán với selectedKhachHangObject
                    var gioHang = HttpContext.Session.GetJson<GioHang>("giohang");
                    var donHang = new DonHang
                    {
                        MaDonHang = GenerateUniqueOrderCode(),
                        NgayDatHang = DateTime.Now,
                        TongTien = gioHang.ComputeTotalValues(),
                        TrangThaiDonHang = 1,
                        MaKhachHang = selectedKhachHangObject.MaKhachHang,
                        ThanhToan = true,
                        VanChuyen = 1,
                        PhiVanChuyen = 10000,
                        TienGiam = TongGiamGia,
                        TongThanhToan = gioHang.ComputeTotalValues() + 10000 - TongGiamGia
                    };

                    db.DonHangs.Add(donHang);
                    db.SaveChanges();

                    foreach (var line in gioHang.Lines)
                    {
                        var sanPham = db.SanPhams.Find(line.SanPham.MaSanPham);

                        if (sanPham != null)
                        {
                            // Cập nhật số lượng tồn kho và lượt mua của sản phẩm
                            sanPham.Slkho -= line.SoLuong;
                            sanPham.LuotMua += line.SoLuong;
                            db.Update(sanPham);

                            // Tìm ChiTietLoHang có HSD gần nhất và MaSanPham khớp
                            var chiTietLoHangs = db.ChiTietLoHangs
                            .Where(ct => ct.MaSanPham == sanPham.MaSanPham && ct.HSDSP.Value.Date >= DateTime.Now)
                            .OrderBy(ct => ct.HSDSP)
                            .ToList();

                            int remainingQuantity = line.SoLuong;

                            foreach (var loHang in chiTietLoHangs)
                            {
                                int availableQuantity = (int)(loHang.SoLuong - loHang.DaBan);

                                if (availableQuantity >= remainingQuantity)
                                {
                                    loHang.DaBan += remainingQuantity;
                                    db.Update(loHang);
                                    remainingQuantity = 0;
                                    break;
                                }
                                else
                                {
                                    loHang.DaBan = loHang.SoLuong; // bán hết lô hàng này
                                    db.Update(loHang);
                                    remainingQuantity -= availableQuantity;
                                }
                            }

                            db.SaveChanges();
                        }
                        db.SaveChanges();
                    }

                    // Lưu các thay đổi vào cơ sở dữ liệu
             


                    var fullName = HttpContext.Session.GetString("AccountId");

                    int accountId;
                    int.TryParse(fullName, out accountId);

                    var user = _context.Accounts.FirstOrDefault(a => a.AccountId == accountId);

                    if (gioHang.TongTienThanhToan > 500000 && gioHang.TongTienThanhToan < 1000000)
                    {
                        var khuyenMai = new KhuyenMai
                        {
                            MaKM = GeneratePromoCode(),
                            TenKM = "Giảm giá 20.000 VNĐ cho đơn hàng",
                            GiaGiam = 20000,
                            NgayBD = DateTime.Now,
                            NgayKT = DateTime.Now.AddDays(30),
                            AccountId = user.AccountId


                        };
                        _context.KhuyenMais.Add(khuyenMai);
                        _context.SaveChanges();

                        var mailData2 = new MailData
                        {
                            ReceiverName = user.FullName, // Thay "Tên khách hàng" bằng tên thực sự của khách hàng
                            ReceiverEmail = user.AccountEmail, // Thay "email@example.com" bằng địa chỉ email thực sự của khách hàng
                            Title = "Thông báo khuyến mãi",
                            Body = "Chào " + user.FullName + ",\n\n"
        + "Chúng tôi rất vui thông báo đến bạn về mã khuyến mãi đặc biệt:\n\n"
        + "Mã khuyến mãi: " + khuyenMai.MaKM + "\n"
        + "Tên khuyến mãi: " + khuyenMai.TenKM + "\n"
        + "Giá giảm: " + khuyenMai.GiaGiam.ToString("N0") + "" + "VNĐ" + "\n"
        + "Thời gian bắt đầu: " + khuyenMai.NgayBD.ToString("dd/MM/yyyy") + "\n"
        + "Thời gian kết thúc: " + khuyenMai.NgayKT.ToString("dd/MM/yyyy") + "\n\n"
        + "Hãy sử dụng mã này để nhận ưu đãi đặc biệt khi mua hàng tiếp theo tại cửa hàng của chúng tôi.\n\n"
        + "Chúc bạn mua sắm vui vẻ và tiết kiệm!"
                        };
                        // Gửi email
                        _mailService.SendMail(mailData2);
                    }


                    if (gioHang.TongTienThanhToan > 1000000 && gioHang.TongTienThanhToan < 2000000)
                    {
                        var khuyenMai = new KhuyenMai
                        {
                            MaKM = GeneratePromoCode(),
                            TenKM = "Giảm giá 50.000 VNĐ cho đơn hàng",
                            GiaGiam = 50000,
                            NgayBD = DateTime.Now,
                            NgayKT = DateTime.Now.AddDays(30),
                            AccountId = user.AccountId


                        };
                        _context.KhuyenMais.Add(khuyenMai);
                        _context.SaveChanges();

                        var mailData2 = new MailData
                        {
                            ReceiverName = user.FullName, // Thay "Tên khách hàng" bằng tên thực sự của khách hàng
                            ReceiverEmail = user.AccountEmail, // Thay "email@example.com" bằng địa chỉ email thực sự của khách hàng
                            Title = "Thông báo khuyến mãi",
                            Body = "Chào " + user.FullName + ",\n\n"
        + "Chúng tôi rất vui thông báo đến bạn về mã khuyến mãi đặc biệt:\n\n"
        + "Mã khuyến mãi: " + khuyenMai.MaKM + "\n"
        + "Tên khuyến mãi: " + khuyenMai.TenKM + "\n"
        + "Giá giảm: " + khuyenMai.GiaGiam.ToString("N0") + "" + "VNĐ" + "\n"
        + "Thời gian bắt đầu: " + khuyenMai.NgayBD.ToString("dd/MM/yyyy") + "\n"
        + "Thời gian kết thúc: " + khuyenMai.NgayKT.ToString("dd/MM/yyyy") + "\n\n"
        + "Hãy sử dụng mã này để nhận ưu đãi đặc biệt khi mua hàng tiếp theo tại cửa hàng của chúng tôi.\n\n"
        + "Chúc bạn mua sắm vui vẻ và tiết kiệm!"
                        };
                        // Gửi email
                        _mailService.SendMail(mailData2);
                    }



                    foreach (var line in gioHang.Lines)
                    {
                        // Lấy thông tin sản phẩm từ cơ sở dữ liệu
                        var sanPham = db.SanPhams.Find(line.SanPham.MaSanPham);

                        if (sanPham != null)
                        {
                            // Trừ số lượng đã mua từ số lượng tồn kho
                            sanPham.Slkho -= line.SoLuong;


                            // Cập nhật giá trị mới của số lượng tồn kho trong cơ sở dữ liệu
                            db.Update(sanPham);

                        }
                    }
                    foreach (var line in gioHang.Lines)
                    {
                        var chiTietDonHang = new ChiTietDonHang
                        {
                            MaDonHang = donHang.MaDonHang,
                            MaSanPham = line.SanPham.MaSanPham,
                            SoLuong = line.SoLuong,
                            GiaBan = line.SanPham.Gia
                        };
                        db.ChiTietDonHangs.Add(chiTietDonHang);
                        RemoveFromGioHang(line.SanPham.MaSanPham);
                    }


                    db.SaveChanges();

                    
                       

                    var mailData1 = new MailData
                    {
                        ReceiverName = selectedKhachHangObject.TenKhachHang, // Thay "Tên khách hàng" bằng tên thực sự của khách hàng
                        ReceiverEmail = user.AccountEmail, // Thay "email@example.com" bằng địa chỉ email thực sự của khách hàng
                        Title = "Thông báo đặt hàng thành công",
                        Body = "Chào " + selectedKhachHangObject.TenKhachHang + ",\n\n"
            + "Đơn hàng của bạn đã được xác nhận thành công. Dưới đây là chi tiết đơn hàng:\n\n"
            + "Mã đơn hàng: " + donHang.MaDonHang + "\n"
            + "Ngày đặt hàng: " + DateTime.Now.ToString("dd/MM/yyyy") + "\n"
            + "Danh sách sản phẩm:\n"
                    };

                    foreach (var line in gioHang.Lines)
                    {
                        mailData1.Body += "Tên sản phẩm: " + line.SanPham.TenSanPham + "\n"
                                         + "Số lượng: " + line.SoLuong + "\n"
                                         + "Giá bán: " + line.SanPham.Gia + "" + "VNĐ" + "\n";
                                         
                    }

                    var tienThanhToan = donHang.TongTien + donHang.PhiVanChuyen;

                    mailData1.Body += "Tổng tiền: " + donHang.TongTien + "" + "VNĐ" + "\n"
                            + "Phí vận chuyển: " + donHang.PhiVanChuyen + "" + "VNĐ" + "\n"
                             + "Tiền thanh toán: " + tienThanhToan  + "" + "VNĐ" + "\n"
                            + "Cảm ơn bạn đã mua hàng tại cửa hàng chúng tôi.";

                    // Gửi email
                    _mailService.SendMail(mailData1);

                    foreach (var line in gioHang.Lines)
                    {

                        RemoveFromGioHang(line.SanPham.MaSanPham);
                    }
     ;
                    gioHang.Clear();

                    // Có thể thêm các xử lý khác nếu cần
                    // ...

                    TempData["SuccessMessage"] = "Đã thanh toán thành công.";

                    return View("Success");
                }
                else
                {
                    TempData["ErrorMessage"] = "Không tìm thấy thông tin Khách Hàng đã chọn.";
                }
            }
            catch (Exception ex)
            {
                // Xử lý lỗi nếu có
                TempData["ErrorMessage"] = $"Đã xảy ra lỗi trong quá trình thanh toán: {ex.Message}";
            }

            // Chuyển hướng về CheckOut hoặc trang khác tùy thuộc vào yêu cầu của bạn
            return RedirectToAction("CheckOut");
        }

        [Authorize]
        public IActionResult PaymentSuccess()
        {
            return View("Success");
        }

        [Authorize]
        public IActionResult PaymentFail()
        {
            return View();
        }

        [Authorize]
        public IActionResult PaymentCallBack()
        {
            try
            {
                var response = _vnPayservice.PaymentExecute(Request.Query);
                if (response == null || !response.Success || response.VnPayResponseCode != "00")
                {
                    TempData["Message"] = $"Lỗi thanh toán VNPay: {response.VnPayResponseCode}";
                    return RedirectToAction("PaymentFail");
                }
                var selectedKhachHang = HttpContext.Session.GetInt32("SelectedCustomerId");
                if (!selectedKhachHang.HasValue)
                {
                    TempData["ErrorMessage"] = "Không tìm thấy thông tin Khách Hàng đã chọn.";
                    return RedirectToAction("CheckOut");
                }

                var selectedKhachHangObject = db.KhachHangs.Find(selectedKhachHang);
                // Lưu thông tin đơn hàng vào cơ sở dữ liệu

                if (selectedKhachHangObject != null)
                {
                    // Thực hiện các bước thanh toán với selectedKhachHangObject
                    var gioHang = HttpContext.Session.GetJson<GioHang>("giohang");
                    var donHang = new DonHang
                    {
                        MaDonHang = response.OrderId,
                        NgayDatHang = DateTime.Now,
                        TongTien = response.Amount,
                        TrangThaiDonHang = 1, // Có thể là trạng thái khác tùy thuộc vào yêu cầu của bạn
                        MaKhachHang = selectedKhachHangObject.MaKhachHang, // Lấy từ response của VnPay hoặc từ session
                        ThanhToan = true, // Đánh dấu là đã thanh toán
                        VanChuyen = 1, // Giả sử vận chuyển có mã 1
                        PhiVanChuyen = 10000 // Phí vận chuyển (nếu có)
                    };

                    db.DonHangs.Add(donHang);
                    db.SaveChanges();
                    foreach (var line in gioHang.Lines)
                    {
                        // Lấy thông tin sản phẩm từ cơ sở dữ liệu
                        var sanPham = db.SanPhams.Find(line.SanPham.MaSanPham);

                        if (sanPham != null)
                        {
                            // Trừ số lượng đã mua từ số lượng tồn kho
                            sanPham.Slkho -= line.SoLuong;


                            // Cập nhật giá trị mới của số lượng tồn kho trong cơ sở dữ liệu
                            db.Update(sanPham);

                        }
                    }
                    db.SaveChanges();
                    foreach (var line in gioHang.Lines)
                    {
                        var chiTietDonHang = new ChiTietDonHang
                        {
                            MaDonHang = donHang.MaDonHang,
                            MaSanPham = line.SanPham.MaSanPham,
                            SoLuong = line.SoLuong,
                            GiaBan = line.SanPham.Gia
                        };
                        db.ChiTietDonHangs.Add(chiTietDonHang);
                        RemoveFromGioHang(line.SanPham.MaSanPham);

                    }
                    db.SaveChanges();


                    foreach (var line in gioHang.Lines)
                    {

                        RemoveFromGioHang(line.SanPham.MaSanPham);
                    }
                    gioHang.Clear();

                    TempData["Message"] = $"Thanh toán VNPay thành công!";
                    return RedirectToAction("PaymentSuccess");
                }
                else
                {
                    TempData["ErrorMessage"] = "Không tìm thấy thông tin Khách Hàng đã chọn.";
                    return RedirectToAction("CheckOut");
                }
            }
            catch (Exception ex)
            {
                // Xử lý lỗi nếu có
                TempData["Message"] = $"Đã xảy ra lỗi trong quá trình thanh toán: {ex.Message}";
                return RedirectToAction("PaymentFail");
            }
        }

        #region Paypal payment
        [Authorize]
        [HttpPost("/GioHang/create-paypal-order")]
        public async Task<IActionResult> CreatePaypalOrder(CancellationToken cancellationToken)
        {
            var gioHang = HttpContext.Session.GetJson<GioHang>("giohang");
            // Thông tin đơn hàng gửi qua Paypal
            var tongTien = ((gioHang.ComputeTotalValues() + 10000) / 23500).ToString("0.00");

            // var tongTien = gioHang.ComputeTotalValues().ToString();

            var donViTienTe = "USD";
            var maDonHangThamChieu = "DH" + DateTime.Now.Ticks.ToString();

            try
            {
                var response = await _paypalClient.CreateOrder(tongTien, donViTienTe, maDonHangThamChieu);

                return Ok(response);
            }
            catch (Exception ex)
            {
                var error = new { ex.GetBaseException().Message };
                return BadRequest(error);
            }
        }

        [Authorize]
        [HttpPost("/GioHang/capture-paypal-order")]
        public async Task<IActionResult> CapturePaypalOrder(string orderID, CancellationToken cancellationToken)
        {
            try
            {
                var response = await _paypalClient.CaptureOrder(orderID);

                // Lưu database đơn hàng của mình

                return Ok(response);
            }
            catch (Exception ex)
            {
                var error = new { ex.GetBaseException().Message };
                return BadRequest(error);
            }
        }

        #endregion

    }
}