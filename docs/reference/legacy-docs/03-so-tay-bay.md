# Sổ tay BẪY — 194 mục đã biết

> **Đọc trước khi sửa bất cứ hành vi nào có vẻ "sai".**
> Phần lớn những chỗ kỳ quặc trong ezInvoice là *cố ý* hoặc *đã được PMS/nhà cung cấp thích nghi*.
> Sửa cho "đúng chuẩn" = làm hỏng production.

Mỗi mục cần một **quyết định** trước khi viết lại: `GIỮ NGUYÊN` / `SỬA` / `CẦN HỎI`.
Cột quyết định để trống — đây là việc của tech lead + PO, không phải của AI.

Nguồn: khảo sát tự động toàn bộ mã nguồn, 2026-09-04. Mọi dẫn chiếu `file:line` theo commit `f77fa2e`.

---

## ☠️ 12 bẫy chết người — đọc trước nếu chỉ có 10 phút

Xếp theo **thiệt hại nếu làm sai**, không theo thứ tự trong tài liệu.

| Hạng | Bẫy | Thiệt hại nếu làm sai | Chi tiết |
|---:|---|---|---|
| 1 | **FAST nhồi chuỗi JSON vào cột `Username`**, và chọn cặp cột theo `isInvoiceMTT` tại runtime | Khách hàng FAST gửi hóa đơn MTT bằng credential GTGT → **hóa đơn bị từ chối hàng loạt** | [`02`](02-bang-credential.md) §3.4 |
| 2 | **`GetFastInfoToken` là code chết** nhưng đọc cùng 6 cột theo cách **không tương thích** với hàm thật | Migrate sai **toàn bộ** dữ liệu FAST | [`02`](02-bang-credential.md) §3.4 |
| 3 | **`static bool isSerialMTT`** trong `PrepareDataToThirdParty` dùng chung mọi request đồng thời | Khách sạn A phát hành MTT → khách sạn B bị coi là MTT → **sai loại hóa đơn** | D01 |
| 4 | **`staticTempVar.com`** — `CompanyViewModel` static toàn cục bị ghi đè khi bất kỳ ai login Minvoice | **Rò rỉ token giữa các khách hàng** | C-series |
| 5 | **`sid` của Sun = `ezInvoiceId` rút gọn 32 ký tự** — là idempotency key chống trùng khi timeout | **Phát hành hóa đơn đôi** | [`02`](02-bang-credential.md) §3.5 |
| 6 | **Bỏ qua kiểm tra chứng chỉ SSL + ép TLS 1.2** (`InitiateSSL.cs`), phạm vi toàn tiến trình | Validate đúng chuẩn → **không kết nối được** tới một số NCC, không ai đoán ra nguyên nhân | A-series |
| 7 | **Luôn trả HTTP 200**; khi exception → **200 + body RỖNG** | PMS đã sống chung nhiều năm; trả 500 + JSON → **PMS xử lý khác đi** | A01 |
| 8 | **Trộn `DateTime.Now` và `DateTime.UtcNow`** khắp hệ thống; Viettel dùng epoch sau `ToUniversalTime()` | Hệ mới chạy UTC → **sai ngày hóa đơn**, lệch 7 giờ | A-series, D-series |
| 9 | **Quy tắc làm tròn** (banker's vs away-from-zero, `decimal` 128-bit, 3 mức precision) | **Lệch tiền trên hóa đơn thuế** | D-series |
| 10 | **Gửi JSON bằng `Encoding.Unicode` (UTF-16)** sang ezInvoiceAdmin | Đổi sang UTF-8 "cho đúng" → **hỏng tích hợp** | A09 |
| 11 | **Migration đã bị sửa tay 2 lần sau khi apply**; 2 bảng không có migration nào | Dựng DB từ migration → **thiếu bảng, sai schema** | C-series |
| 12 | **`[ThirdPartyAuthorize]` đã bị comment** → 33 endpoint không có authorization | Bật xác thực thật → **chặn toàn bộ PMS đang chạy** | A-series |

---

## Mục lục

- [A. Kiến trúc, khởi động & pipeline HTTP](#a-kiến-trúc-khởi-động--pipeline-http) — 28 mục
- [B. Bề mặt API & hợp đồng với PMS](#b-bề-mặt-api--hợp-đồng-với-pms) — 26 mục
- [C. Mô hình dữ liệu & Database](#c-mô-hình-dữ-liệu--database) — 15 mục
- [D. Logic nghiệp vụ & mapping dữ liệu](#d-logic-nghiệp-vụ--mapping-dữ-liệu) — 22 mục
- [E. Tích hợp VNPT & Viettel](#e-tích-hợp-vnpt--viettel) — 29 mục
- [F. Tích hợp MISA & Minvoice](#f-tích-hợp-misa--minvoice) — 29 mục
- [G. Tích hợp Cyberbill, EasyInvoice, Fast, Sun](#g-tích-hợp-cyberbill-easyinvoice-fast-sun) — 26 mục
- [H. Cấu hình, vận hành & triển khai](#h-cấu-hình-vận-hành--triển-khai) — 19 mục

---


## A. Kiến trúc, khởi động & pipeline HTTP

| # | Bẫy | Quyết định |
|---|---|---|
| A01 | `WebApiCustomMiddleware` NUỐT toàn bộ exception: khi _next ném lỗi, dòng copy `memStream` ra body gốc (WebApiCustomMiddleware.cs:41) nằm trong try nên không chạy, và Response.Body không được khôi phục trong finally → client nhận HTTP 200 với BODY RỖNG. Đây là hành vi quan sát được trên production, đừng 'sửa' mà không thông báo cho đội PMS. | |
| A02 | `WebApiCustomMiddleware` buffer TOÀN BỘ response vào MemoryStream cho MỌI request. Các endpoint trả PDF base64 (DownloadPDF, DownloadPDFInvoiceDraft, GetXMLInvoice) sẽ chiếm gấp đôi RAM, và toàn bộ nội dung đó còn bị `LogContext.PushProperty("ResponseContent", ...)` giữ thêm một bản nữa (WebApiCustomMiddleware.cs:34-40). Có class `SubStringLogPDF.first100Characters` để cắt bớt nhưng chỉ dùng ở tầng LoggingModel, không dùng ở middleware. | |
| A03 | `LogContext.PushProperty(...)` được gọi mà KHÔNG dispose IDisposable trả về (WebApiCustomMiddleware.cs:23,27,31,40) → property tích luỹ trong ambient stack của Serilog. | |
| A04 | Thứ tự middleware bất thường: 2 middleware tự viết đặt TRƯỚC UseRouting/UseAuthConfiguration, còn UseDeveloperExceptionPage đặt SAU chúng nên không bao giờ có tác dụng. Khi viết lại đừng 'sắp xếp lại cho đúng chuẩn' nếu chưa xác nhận PMS chịu được. | |
| A05 | Serilog thực tế KHÔNG có sink ở mọi môi trường trong repo (config Serilog nằm ở appsettings.json nhưng Program.cs:13-19 chỉ đọc appsettings.{Env}.json). Phải kiểm tra file cấu hình TRÊN SERVER trước khi kết luận. | |
| A06 | `Host.CreateDefaultBuilder(args).UseSerilog();` (Program.cs:31-32) là no-op — ILogger<T> qua DI KHÔNG dùng Serilog. | |
| A07 | Khối try/catch bao trọn Program.cs (dòng 9-97): lỗi khởi động (sai connection string, sai config) KHÔNG làm process crash mà chỉ ghi Log.Fatal rồi thoát bình thường → IIS/orchestrator không phát hiện được là app hỏng. | |
| A08 | `CompanyAppService.GetById` gọi HTTP đồng bộ trong luồng async: `Task.WhenAll(task)` rồi `.Result` (CompanyAppService.cs:35-36) và `client.PostAsync(...).GetAwaiter().GetResult()` (dòng 67) → nguy cơ deadlock/thread-pool starvation dưới tải. | |
| A09 | `CompanyAppService` gửi body JSON với `Encoding.Unicode` (UTF-16) nhưng khai content-type application/json (CompanyAppService.cs:66) → phía ezInvoiceAdmin phải chấp nhận charset=utf-16. Đổi sang UTF-8 khi viết lại có thể làm hỏng tích hợp — phải test kỹ. | |
| A10 | `new HttpClient()` được tạo mới ở khắp nơi mà không dùng IHttpClientFactory (CompanyAppService.cs:56, LoggingContext.cs:209, MinvoiceAppService.cs:44...) → socket exhaustion / TIME_WAIT. | |
| A11 | `staticTempVar.com` là biến STATIC toàn tiến trình chứa CompanyViewModel, bị ghi đè khi bất kỳ khách sạn nào login Minvoice (MinvoiceAppService.cs:66-67, CompanyViewModel.cs:23-27) → nguy cơ lẫn token giữa các tenant. | |
| A12 | `MisaRepository.tokenMisaByCompanies` là `List<T>` STATIC, không khoá, bị Add/đọc đồng thời từ nhiều request (MisaRepository.cs:14, 40-63) → race condition. Cache này chứa cả Password ở dạng plain text trong RAM và làm key so sánh. | |
| A13 | LỖI MÚI GIỜ trong cache MISA: khi TẠO mới dùng `DateTime.Now.AddDays(7)` (MisaRepository.cs:57) nhưng khi KIỂM TRA so với `DateTime.UtcNow` (dòng 72) và khi GIA HẠN lại dùng `DateTime.UtcNow.AddDays(7)` (dòng 80). VN là UTC+7 nên token mới tạo sống lâu hơn 7 giờ so với token gia hạn — hành vi không nhất quán. | |
| A14 | Toàn hệ thống trộn lẫn `DateTime.UtcNow` (LoggingContext, PmsController SetTime) và `DateTime.Now` (Minvoice/Misa expiry, MisaRepository.cs:57, MinvoiceAppService.cs:67). Không có chuẩn múi giờ thống nhất — phải quyết định rõ khi viết lại và đối chiếu với dữ liệu log cũ. | |
| A15 | `ServicePointManager.ServerCertificateValidationCallback += ... => true` dùng `+=` (InitiateSSL.cs:11) → delegate tích luỹ vô hạn theo số lần gọi, và ảnh hưởng TOÀN BỘ tiến trình chứ không chỉ request hiện tại (kể cả kết nối tới ezInvoiceAdmin và SQL). | |
| A16 | `ezInvoiceContext` bật NoTracking + tắt AutoDetectChanges (ezInvoiceContext.cs:21-22) trong khi `PublishDomainEvents` lại dựa vào ChangeTracker (dòng 71-77) → mâu thuẫn nội tại. | |
| A17 | `PublishDomainEvents` chạy `Task.WhenAll` các handler SONG SONG trên cùng một DbContext (ezInvoiceContext.cs:82-87) — DbContext không thread-safe. | |
| A18 | `EventStoreSqlRepository.Store` gọi `SaveChanges()` ĐỒNG BỘ bên trong luồng async, và `SqlEventStore.Save` là void (không await được) → ghi event store là blocking I/O ẩn. | |
| A19 | Đăng ký DI TRÙNG: MediatR auto-scan (Program.cs:55) + đăng ký tay (NativeInjectorBootStrapper.cs:55-63) cùng ánh xạ INotificationHandler<T> → CustomerEventHandler; notification có thể chạy 2 lần. | |
| A20 | `AddMediatR(AppDomain.CurrentDomain.GetAssemblies())` phụ thuộc vào việc assembly đã được nạp TÌNH CỜ bởi các lời gọi phía trên — đảo thứ tự các dòng Program.cs:39-58 có thể làm mất handler mà không báo lỗi. | |
| A21 | CORS `AllowAnyOrigin` + `AllowAnyHeader` + `AllowAnyMethod` (Program.cs:75-80) cấu hình inline, không qua AddCors — mở hoàn toàn. | |
| A22 | Secret nằm PLAIN TEXT trong repo: connection string SQL kèm mật khẩu (appsettings.json:3, appsettings.Testing.json, appsettings.Staging.json), JWT SecretKey (appsettings.json:15), mật khẩu VNPT trong sql/AddCompany.sql. Khi migrate phải xoay (rotate) toàn bộ. | |
| A23 | Swagger doc để tên/liên hệ của tác giả template (SwaggerConfig.cs:21-22) — đang public trên môi trường thật, cần dọn. | |
| A24 | Route trùng tên method: hai action cùng tên `GetXMLInvoice` với 2 route khác nhau (PmsController.cs:1548 và 1603) — dễ nhầm khi map lại. | |
| A25 | `INVOICE_ACTION` nhảy số (không có 13) — đừng 'chuẩn hoá' lại thành liên tục. | |
| A26 | Project Viettel loại trừ file khỏi build: `<Compile Remove="LoggingHandler.cs" />` (ezInvoice.Infra.ThirdParty.Viettel.csproj) — có file .cs nhưng KHÔNG được biên dịch; đọc code mà không đọc csproj sẽ port nhầm code chết. | |
| A27 | Có 12 project trong solution nhưng chỉ `ezInvoice.Services.Api` được deploy; `ezInvoice.UI.Web` (MVC + Identity UI + Razor) và `Front` (Vue) là di sản, đừng port. | |
| A28 | Dockerfile chỉ COPY duy nhất csproj của Services.Api rồi `dotnet restore` — sẽ FAIL nếu build thật; đây là bằng chứng Dockerfile chưa từng được dùng. | |


## B. Bề mặt API & hợp đồng với PMS

| # | Bẫy | Quyết định |
|---|---|---|
| B01 | MOI endpoint PMS deu tra HTTP 200 — ke ca loi he thong, loi NCC, loi cau hinh. Client TUYET DOI khong duoc dua vao HTTP status. Neu viet lai ma tra 4xx/5xx dung chuan REST se pha vo toan bo client PMS hien co. | |
| B02 | Ngoai le duy nhat cua quy tac 200: [ApiController] tu dong tra HTTP 400 voi ValidationProblemDetails (RFC7807) khi JSON sai cu phap hoac model binding that bai — dinh dang HOAN TOAN KHAC ResponseModel. Khong co InvalidModelStateResponseFactory tuy bien (da grep, khong co). | |
| B03 | Exception thoat khoi controller o moi truong Production => HTTP 200 + body RONG (WebApiCustomMiddleware nuot exception va khong copy stream). Trong Development thi lai ra trang HTML developer exception page. Hai moi truong hanh xu KHAC NHAU. | |
| B04 | WebApiCustomMiddleware thay context.Response.Body bang MemoryStream nhung KHONG khoi phuc lai originalBody trong finally (WebApiCustomMiddleware.cs:33-45). Voi response lon (PDF base64) toan bo body bi giu trong RAM. Endpoint DownloadPDF/Preview vi the ton bo nho theo kich thuoc PDF x so request dong thoi. | |
| B05 | PrepareDataToThirdParty dung static field => race condition thuc su giua cac request dong thoi (chi tiet o tacit_knowledge). Day la ly do co the giai thich cac loi 'thinh thoang phat hanh nham loai hoa don' trong qua khu. | |
| B06 | Thu tu goi BAT BUOC trong moi endpoint: LogClientInfo() -> LoggingContext.Init().Set*() -> await LoggingContext.Start() -> GetCompany -> SetCompany(site) -> PrepareData -> SendToThirdParty. Buoc SetCompany(site) PHAI dung truoc SendToThirdParty vi tang convert VNPT doc company tu log context (VnptConvertRequest.cs:948). Doi thu tu = mat du lieu am tham. | |
| B07 | LoggingContext.SetLoggingId nem InvalidOperationException neu goi lan 2 trong cung ExecutionContext, nhung TAT CA exception deu bi nuot bang `catch { }` (LoggingContext.cs:46). Moi loi logging deu im lang — bao gom ca loi ket noi Elastic. | |
| B08 | LoggingContext.SaveToElastic tao `new HttpClient()` moi cho MOI lan ghi log va khong dispose dung cach (using block nhung van la new moi lan) => nguy co can kiet socket (TIME_WAIT) duoi tai cao. LoggingContext.cs:171. | |
| B09 | Timeout: KHONG co timeout thong nhat. MISA dat `client.Timeout = Timeout.InfiniteTimeSpan` o 9 cho (MisaRepository.cs:233, 421, 556, 593, 629, 663, 737, 774, 885) va 90 giay o 1 cho (:386). Minvoice dat 5 phut (Minvoice/Config/APIServices.cs:54). => Request MISA co the treo vo han, PMS phai tu dat timeout phia minh. | |
| B10 | Nhan dien timeout phai duyet CA CHUOI InnerException tim TimeoutException, vi tu .NET 5 HttpClient nem TaskCanceledException BOC TimeoutException (ExecuteAndTranslateException.cs:33-51). Bat rieng TimeoutException la KHONG DU. Ngoai ra huy do phia goi ngat ket noi thi khong co InnerException nen khong bi nham. | |
| B11 | Duong dan tra ve loi cua NCC (Status 5000) van kem theo Data la PmsInvoiceResponse mot phan (co ezInvoiceId, Sun_sid...) chu khong phai null — vd SunConvertResponse.cs:218. Client phai doc Data ngay ca khi that bai de biet ezInvoiceId nao that bai. | |
| B12 | Encoding: CompanyAppService gui body sang ezInvoiceAdmin bang Encoding.Unicode (UTF-16), khong phai UTF-8 (CompanyAppService.cs:69). Cac cho khac dung UTF-8. Khong duoc 'chuan hoa' ma khong test voi ezInvoiceAdmin. | |
| B13 | Timezone: toan bo log dung DateTime.UtcNow, nhung cac truong ngay trong request PMS (InvoiceDate, transdate, ArrivalDate, DepartureDate, AdditionalReferenceDate) la STRING khong co dinh dang duoc khai bao o dau. Dinh dang thuc te nam rai rac trong tung ConvertRequest cua tung NCC. | |
| B14 | Culture: SunConvertResponse.cs va cac converter khac dung `using System.Globalization` va CultureInfo khong nhat quan; VnptConvertRequest chuyen decimal sang chuoi bang `.ToString()` khong tham so (VnptConvertRequest.cs:1022-1025) => phu thuoc CurrentCulture cua may chu. Neu may chu doi locale (vi-VN dung dau phay thap phan) toan bo so tien gui sang VNPT se sai. | |
| B15 | SUN tra chuoi bi ma hoa HTML entity (vd '&#x75;&#x6E;...' = 'undefined') — co ham DecodeHtmlEntities (SunConvertResponse.cs:250-254) nhung KHONG duoc goi o dau ca. Quirk da biet nhung chua xu ly. | |
| B16 | CORS mo hoan toan: AllowAnyHeader + AllowAnyMethod + AllowAnyOrigin (Program.cs:74-79). Ket hop voi viec khong xac thuc => bat ky trang web nao cung goi duoc API nay. | |
| B17 | GET /server (InfoController.cs) lo MachineName/LocalIP/LocalPort khong can xac thuc. | |
| B18 | Endpoint CustomerController KHONG co [Route] o cap class nen route la tuyet doi tu goc: /customer-management (khong co prefix /api). Rat de sot khi viet lai. | |
| B19 | Chuoi thong bao loi tieng Viet la mot phan cua hop dong (PMS co the so khop chuoi). Co 2 bien the chinh ta cua cung mot thong bao va co loi chinh ta co dinh ('Don vi cung cap' thieu dau, 'Nha cung cap khong phan hoi' -> trong code viet 'Nha cung cap khong phan hoi' nhung Errors.cs:29 ghi 'Nha cung cap khong phan hoi' voi loi typo 'cung cap' -> 'cung cap'). Phai copy nguyen van, khong duoc sua chinh ta. | |
| B20 | MISA tra Data la chuoi JSON escape KEP, phai `.Replace("\\\"", "\"")` truoc khi deserialize (MisaConvertResponse.cs:66, :95, :299). Bo buoc nay se that bai am tham. | |
| B21 | ConvertSendInvoiceCQTUSBTokenResponse co code CHET sau lenh return (MisaConvertResponse.cs:573-578) — khoi kiem tra temp.Count khong bao gio chay. Khong duoc 'khoi phuc' khi viet lai. | |
| B22 | Endpoint SendInvoiceCQT va GetStatusInvoice DUNG CHUNG mot model request (SendInvoiceCQT) mac du y nghia nghiep vu khac nhau; GetStatusInvoice bo qua ezInvoiceIds/InvoiceType nhung dung TypeEinvoice. | |
| B23 | CreateAndPublishMultiInvoice nhan MANG JSON o cap cao nhat (khong boc trong object). Neu mang rong thi request.FirstOrDefault() tra null va co `?.` nhung request.Select(_ => _.Invoice...) tai PmsController.cs:931 se NRE neu phan tu co Invoice null. | |
| B24 | Khong co retry, khong co circuit breaker, khong co queue. Moi request PMS = 1 lan goi thang NCC. Neu NCC cham, thread pool cua ezInvoice bi giu (nhat la voi MISA InfiniteTimeSpan). | |
| B25 | Khong co idempotency key o tang HTTP. Neu PMS retry mot request CreateAndPublishInvoice da thanh cong nhung mat response, hoa don SE bi phat hanh 2 lan tru khi PMS tu gui lai dung ezInvoiceId cu (va NCC do co ho tro chong trung theo fkey/RefID). VNPT bao ERR:13/ERR:22, MISA bao DuplicateInvoiceRefID (duoc coi la thanh cong). | |
| B26 | Mat khau ket noi CSDL, mat khau Serilog sink, SecretKey JWT deu hard-code trong appsettings.json cua repo (appsettings.json:3, :17, :59). Khi viet lai phai chuyen sang secret manager va PHAI xoay vong toan bo credential nay. | |


## C. Mô hình dữ liệu & Database

| # | Bẫy | Quyết định |
|---|---|---|
| C01 | STATIC MUTABLE STATE #1 — PrepareDataToThirdParty có `private static bool isSerialMTT` và `private static bool haveDeleteInvoiceDraft` ở cấp CLASS TĨNH, dùng chung cho MỌI request đồng thời (PrepareDataToThirdParty.cs:12-13). Các hàm SearchById (dòng 43), FormSerials (dòng 104), CreateAndPublishInvoiceXML (dòng 79), HealthCheck (dòng 112), Login (dòng 116), MinvoiceRequest (dòng 108) KHÔNG tính lại mà DÙNG LẠI giá trị của request TRƯỚC ĐÓ (comment trong code thừa nhận: 'cần kiểm tra, trao đổi lại do body không truyền lên Ký hiệu(Serial)'). Đây là race condition thật: khách sạn A gọi Create hóa đơn MTT xong, khách sạn B gọi FormSerials sẽ được coi là MTT. Viết lại phải quyết định: giữ nguyên bug hay sửa (sửa sẽ đổi hành vi production). | |
| C02 | STATIC MUTABLE STATE #2 — `staticTempVar.com` là một CompanyViewModel static toàn cục (CompanyViewModel.cs:23-27), bị MinvoiceAppService ghi vào (MinvoiceAppService.cs:66-67). Đây là rò rỉ dữ liệu đa khách hàng tiềm tàng (token của khách sạn này lưu vào biến dùng chung). | |
| C03 | TRỘN DateTime.Now VÀ DateTime.UtcNow — PmsController.GetCompany gán MinvoiceExpired = DateTime.UtcNow (PmsController.cs:1776) nhưng MinvoiceAppService so sánh `company.MinvoiceExpired < DateTime.Now` (MinvoiceAppService.cs:39, 103). Ở VN (UTC+7) UtcNow luôn nhỏ hơn Now 7 tiếng => LUÔN coi là hết hạn => LUÔN login lại. Hệ thống mới chạy ở timezone khác (hoặc UTC) sẽ có hành vi KHÁC. LoggingContext lại dùng DateTime.UtcNow (LoggingContext.cs:125,148,167) còn Misa dùng DateTime.Now (MisaConvertRequest.cs:1946). Phải khảo sát từng chỗ. | |
| C04 | ENCODING — CompanyAppService gửi request tới ezInvoice Admin bằng `new StringContent(json, System.Text.Encoding.Unicode, "application/json")` tức UTF-16LE, KHÔNG phải UTF-8 (CompanyAppService.cs:66). Server phía Admin đang chấp nhận điều này. Đổi sang UTF-8 'cho đúng chuẩn' có thể làm hỏng tích hợp. | |
| C05 | GLOBAL SIDE EFFECT — CompanyAppService.Post set `ServicePointManager.Expect100Continue = true` và `ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12` ở mức TOÀN PROCESS mỗi lần gọi (CompanyAppService.cs:58-59), ảnh hưởng mọi HTTP client khác trong ứng dụng. Tương tự MinvoiceAppService bỏ qua toàn bộ kiểm tra SSL bằng DangerousAcceptAnyServerCertificateValidator (MinvoiceAppService.cs:43) và hàm checkssl luôn return true (dòng 28-31). | |
| C06 | SYNC-OVER-ASYNC — CompanyAppService.GetById gọi `Task.WhenAll(task)` rồi `.Result` (CompanyAppService.cs:35-36) và bên trong lại `client.PostAsync(...).GetAwaiter().GetResult()` (dòng 67). Đây là điểm deadlock/thread-starvation kinh điển, nằm ngay trên đường đi của MỌI request PMS. `new HttpClient()` được tạo mới mỗi lần gọi (dòng 56) => cạn kiệt socket. | |
| C07 | NUỐT LỖI — Nếu gọi ezInvoice Admin thất bại, CompanyAppService trả về ezINVBaseResponse với Data = null (CompanyAppService.cs:84-91), và PmsController chỉ kiểm tra `site != null` rồi trả EzInvoiceAdminResponse. Không phân biệt được 'công ty không tồn tại' với 'hệ thống Admin đang chết'. Toàn bộ LoggingContext cũng bọc trong `catch { }` rỗng (LoggingContext.cs:47,130,153,171,200). | |
| C08 | KHÔNG CÓ RÀNG BUỘC TOÀN VẸN — không FK, không unique, không index nào ngoài PK trên cả 5 bảng nghiệp vụ. ExtraDataMaps.CompanyId là uniqueidentifier NOT NULL nhưng không trỏ đâu cả. Dữ liệu production nhiều khả năng có bản ghi mồ côi / trùng lặp => import vào schema mới có ràng buộc sẽ FAIL hàng loạt. | |
| C09 | THỨ TỰ KHÔNG XÁC ĐỊNH — InvoiceActionRepository.GetAllActions() và InvoiceSystemRepository.GetAllSystems() gọi DbSet.ToListAsync() KHÔNG ORDER BY (InvoiceActionRepository.cs:31-34, InvoiceSystemRepository.cs:31-34); việc sắp xếp theo OrderNumber làm ở FRONTEND (LogSync.js:254,261). Tương tự, danh sách ExtraDataMap từ Admin không được sắp xếp nhưng Misa lại lấy 'FirstOrDefault khác rỗng' (MisaConvertRequest.cs:1634) => kết quả phụ thuộc thứ tự trả về của SQL Server. | |
| C10 | GIỚI HẠN ĐỘ DÀI CỘT quá chật cho dữ liệu thực: PartnerUrl/PartnerUrl2/Username/Password/Username2/Password2 đều nvarchar(100). Nhưng với FAST, cột Username/Username2 đang chứa một chuỗi JSON {clientC, proxyC, user, unitC} (FastConvertRequest.cs:947-948) — gần như chắc chắn vượt 100 ký tự, nghĩa là hoặc DB production đã được ALTER tay, hoặc dữ liệu đang bị cắt cụt. PHẢI kiểm tra schema production thật. | |
| C11 | MẬT KHẨU LƯU DẠNG PLAINTEXT trong cột nvarchar(100) (Companies.Password, Password2) và trong file sql/AddCompany.sql commit vào git. Nếu hệ thống mới mã hóa, phải viết bước giải mã/nạp lại; nếu không mã hóa, phải ý thức đây là nợ bảo mật đang tồn tại. | |
| C12 | IEventStoreRepository.Store gọi SaveChanges() ĐỒNG BỘ và NGOÀI transaction của ezInvoiceContext (EventStoreSQLRepository.cs:25-29) trong khi ezInvoiceContext.Commit() publish event TRƯỚC khi SaveChangesAsync (ezInvoiceContext.cs:57-61). Nghĩa là StoredEvent có thể được ghi thành công trong khi transaction nghiệp vụ rollback => event store không nhất quán với dữ liệu. | |
| C13 | Hai file appsettings.json khác nhau khai báo hai DATABASE KHÁC NHAU: src/ezInvoice.Services.Api/appsettings.json trỏ tới 'ezInvoice_dev' trên 10.19.192.56, còn src/ezInvoice.Infra.Data/appsettings.json trỏ tới 'ezInvoice' trên 192.168.1.126. File thứ hai là cái mà EF Tools dùng khi chạy Add-Migration/Update-Database => rất dễ chạy migration nhầm database. | |
| C14 | Serilog sink MSSqlServer bật autoCreateSqlTable=true và trỏ vào database ezInvoiceAdmin_TEST (appsettings.json:59-63) — tức ứng dụng ezInvoice TỰ TẠO BẢNG trong database của hệ thống Admin. Một dependency ngầm giữa 2 hệ thống mà không ai khai báo. | |
| C15 | Không có project test nào cho tầng dữ liệu (ezInvoice.sln không có test project cho Domain/Infra.Data). Mọi hành vi mô tả ở trên chỉ được xác nhận bằng đọc code, không có test bảo vệ khi viết lại. | |


## D. Logic nghiệp vụ & mapping dữ liệu

| # | Bẫy | Quyết định |
|---|---|---|
| D01 | Math.Round trong .NET mặc định làm tròn về SỐ CHẴN (Banker's rounding). Chỉ 3 vị trí ở MISA dùng MidpointRounding.AwayFromZero (MisaConvertRequest.cs:1751-1752, 1757-1758, 2164); các vị trí còn lại (MisaConvertRequest.cs:1755-1756 DiscountAmount, 1775-1776 CustomField3Detail, ViettelConvertRequest.cs:166 unitPrice) vẫn dùng mặc định. Port sang Python/JS/Java (mặc định HALF_UP hoặc HALF_EVEN khác nhau) sẽ lệch tiền ở đúng những vị trí này. | |
| D02 | Kiểu decimal của C# (128-bit, 28-29 chữ số có nghĩa, lưu thập phân chính xác) KHÔNG tương đương double/float. Toàn bộ tiền dùng decimal. Riêng SUN ép sang double khi đọc tiền bằng chữ: NumberReader.NumberToText((double)(AfterTaxAmount*multiplier)) tại SunConvertRequest.cs:157 — có thể mất chính xác với số rất lớn. | |
| D03 | Định dạng số khi ToString() PHỤ THUỘC CULTURE của process. Rất nhiều chỗ gọi .ToString() trần trên decimal mà không truyền InvariantCulture (VnptConvertRequest.cs:758-771, 896-903, 1126-1135; SunConvertRequest.cs; EasyInvoiceConvertRequest.cs:868-875). Nếu server chạy culture vi-VN thì dấu thập phân là ',' → XML/JSON gửi sang NCC sai. Chỉ các chỗ ParseExact mới truyền CultureInfo.InvariantCulture. | |
| D04 | DateTime.ParseExact(invoice.InvoiceDate, "dd/MM/yyyy", null) với tham số null provider — dùng CurrentCulture. Xuất hiện hàng chục lần trong MisaConvertRequest.cs (135, 233, 312, 447, 613, 1273, 1325, 1429, 1480, 1686, 2012). Sẽ ném FormatException nếu culture khác hoặc chuỗi ngày sai định dạng. | |
| D05 | Viettel dùng .ToUniversalTime() trước khi tính epoch (ViettelConvertRequest.cs:92-95, 317-326) → kết quả PHỤ THUỘC TIMEZONE CỦA SERVER. Server ở UTC+7 sẽ ra epoch lệch 7 giờ so với server UTC. Đây là bẫy nghiêm trọng khi container hóa. | |
| D06 | So sánh GUID không nhất quán: VnptConvertRequest.IsNotSendServiceSummary có .ToUpper() (dòng 41) nhưng VnptMTTConvertRequest.IsNotSendServiceSummary KHÔNG có (dòng 35). Cùng một danh sách GUID viết HOA → nhánh MTT sẽ KHÔNG khớp nếu Company.Id trả về chữ thường. | |
| D07 | Rất nhiều NullReferenceException tiềm tàng do dùng .Value trực tiếp: invoice.AfterTaxAmount.Value * invoice.ExchangeRate.Value (MisaConvertRequest.cs:163, 261, 340, 475, 641, 1301, 1458, 1703; FastConvertRequest.cs:56, 150, 167, 185, 208, 653); invoice.TaxRate.Value, invoice.ExchangeRate.Value (MisaConvertRequest.cs:1905-1910); item.Quantity.Value (MisaConvertRequest.cs:1794). Nếu PMS không gửi ExchangeRate hoặc AfterTaxAmount là crash. | |
| D08 | Int32.Parse(pmsInvoice.PaymentMethod) ở Cyberbill (CyberbillConvertRequest.cs:148) — PaymentMethod ở các NCC khác là chuỗi mô tả ('Tiền mặt', 'Chuyển khoản'). Cyberbill bắt buộc phải là số. | |
| D09 | Thứ tự khóa trong Dictionary quyết định tính đúng đắn của payload FAST: structure.master là danh sách KEY, invoices[].master là danh sách VALUE — hai danh sách phải cùng thứ tự. C# Dictionary<string,object> giữ thứ tự chèn trong thực tế nhưng KHÔNG ĐẢM BẢO theo đặc tả. Ngôn ngữ khác (Go map, Java HashMap) sẽ xáo trộn thứ tự → hóa đơn sai toàn bộ. Xem FastConvertRequest.cs:481-565, 553-560; bản multi dùng HashSet<string> còn nguy hiểm hơn (FastConvertRequest.cs:637-643, 724). | |
| D10 | Biến static không thread-safe: PrepareDataToThirdParty.isSerialMTT/haveDeleteInvoiceDraft (PrepareDataToThirdParty.cs:12-13) và staticTempVar.com (CompanyViewModel.cs:23-27). Dưới tải cao, request của công ty A có thể dùng cờ MTT/token của công ty B. | |
| D11 | masterData.Add("TaxAmount0"/"TaxAmount5"/"TaxAmount10") của FAST dùng .Add chứ .NET sẽ ném ArgumentException nếu TaxSummarys có 2 dòng cùng TaxRate (FastConvertRequest.cs:506-521, 689-704, 799-814). | |
| D12 | VNPT nhánh V4 MapInvoice ép (int) thuế suất còn nhánh Adjust/Replace không ép — thuế suất lẻ sẽ khác nhau giữa phát hành và điều chỉnh (VnptConvertRequest.cs:852, 901 vs 760, 809). | |
| D13 | VNPT MTT ConvertDetail đặt Amount = item.BeforeTaxAmount (không phải AfterTaxAmount) trong khi VNPT V4 ConvertDetail đặt Amount = item.AfterTaxAmount (VnptMTTConvertRequest.cs:358 vs VnptConvertRequest.cs:903). Có thể là bug đã tồn tại. | |
| D14 | OptionUserDefined.AmountDecimalDigits của MISA mặc định "" ở nhánh MTT nhưng "0" ở nhánh HSM/Preview/vé (MisaConvertRequest.cs:350, 485, 651, 826 vs 173, 996, 1171, 1312, 1468, 1556, 1712). Khác biệt này ảnh hưởng trực tiếp đến việc MISA làm tròn tiền. | |
| D15 | MISA CheckIn/CheckOut ở nhánh MTT-HSM KHÔNG đổi định dạng ngày (nối thẳng stringValue + " 00:00:00 AM") trong khi các nhánh khác gọi ConvertDateFormat đổi dd/MM/yyyy → MM/dd/yyyy (MisaConvertRequest.cs:582-586, 757-761 vs 396-404, 702-710, 1363-1371). | |
| D16 | MISA CreateAndPublishInvoice: nếu InvoiceType==0 và TypeEinvoice==INVOICE_NORMAL_USB_TOKEN(2) và không phải MTT thì KHÔNG nhánh nào chạy → misaResponse = null → ConvertCreateAndPublishInvoiceResponse nhận null (MisaAppService.cs:383-451). Cần xác nhận đây là trường hợp không xảy ra hay là lỗ hổng. | |
| D17 | Thứ tự bắt buộc: với VNPT V5, PHẢI gọi DeleteInvoice trước CreateAndPublish khi HaveDeleteInvoiceDraft (VnptV5AppService.cs:376-380, 547-551). Với MISA biên lai, PHẢI gọi GetInvoiceSerial trước để lấy InvoiceType/InvoiceSeries (MisaAppService.cs:466-486). Đảo thứ tự sẽ lỗi. | |
| D18 | XmlHelperObject.Serialize dùng Newtonsoft DeserializeXmlNode — mảng 1 phần tử và mảng nhiều phần tử tạo ra cấu trúc XML KHÁC NHAU, và giá trị null có thể bị bỏ qua hoặc thành thẻ rỗng. Hành vi này rất khó tái tạo ở thư viện khác (XmlHelper.cs:39-52). | |
| D19 | VNPT ErrorResponse: nếu vnptResult chứa ':' thì int.TryParse(split(':')[1]) để lấy mã lỗi — biến statusError được gán nhưng câu lệnh if rỗng và statusError KHÔNG được dùng (VnptConvertResponse.cs:650-652). Dead code gây hiểu nhầm. | |
| D20 | MinvoiceAppService bypass toàn bộ kiểm tra SSL (DangerousAcceptAnyServerCertificateValidator, MinvoiceAppService.cs:43) — rủi ro bảo mật đang tồn tại, cần quyết định có giữ khi viết lại không. | |
| D21 | Chuỗi kết nối DB và mật khẩu đang được commit thẳng trong appsettings.json (D:\Code\25.ezInvoice\src\ezInvoice.Services.Api\appsettings.json) — không được sao chép nguyên xi sang hệ thống mới. | |
| D22 | Toàn bộ khối dựng body của MISA bị copy-paste 7-9 lần với khác biệt nhỏ (IsInvoiceCalculatingMachine, InvoiceName, AmountDecimalDigits, ReferenceType). Viết lại bằng cách gộp thành 1 hàm chung sẽ VÔ TÌNH ĐỒNG NHẤT các khác biệt này và đổi hành vi. Phải lập bảng so sánh 9 biến thể trước khi refactor. | |


## E. Tích hợp VNPT & Viettel

| # | Bẫy | Quyết định |
|---|---|---|
| E01 | Thứ tự thẻ XML gửi VNPT V5/MTT là bắt buộc (NBan trước NMua trước DSHHDVu trước TToan) — mọi cấu trúc dữ liệu không bảo toàn thứ tự (dict/map/JSON object sắp xếp lại) sẽ phá hỏng payload. | |
| E02 | Null xử lý khác nhau giữa 2 luồng: VNPT V1 (XmlSerializer) BỎ thẻ khi null; VNPT V5/MTT (JSON->XML) tạo THẺ RỖNG khi null. Có chỗ code cố tình gán null (TLCKhau, NBan) để đạt hiệu ứng mong muốn. | |
| E03 | Payload VNPT V1 chứa khai báo `<?xml version="1.0" encoding="utf-16"?>` (do StringWriter là UTF-16) dù dữ liệu thực tế đi trong SOAP UTF-8 — VNPT chấp nhận suốt nhiều năm; đổi thành utf-8 là thay đổi hành vi chưa được kiểm chứng. | |
| E04 | Múi giờ: mọi epoch-ms gửi Viettel phụ thuộc múi giờ tiến trình (ToUniversalTime trên DateTime Kind=Unspecified). Deploy trên máy UTC sẽ lệch ngày hóa đơn 1 ngày. Bắt buộc chạy UTC+7 hoặc cộng bù tường minh. | |
| E05 | Định dạng số: không hàm nào chỉ định CultureInfo. Chạy trên máy/locale vi-VN sẽ sinh dấu phẩy thập phân và làm hỏng payload. Phải cố định Invariant. Đồng thời phải giữ nguyên số chữ số thập phân do PMS gửi (100.00 != 100). | |
| E06 | Làm tròn: chỗ duy nhất là Math.Round(Price, PricePrecision) của Viettel, dùng banker's rounding (ToEven) của .NET. Ngôn ngữ khác mặc định HALF_UP sẽ lệch. | |
| E07 | Không có retry và không set timeout: WCF mặc định 60 giây, HttpClient mặc định 100 giây. Nếu hệ thống mới đặt timeout dài hơn/ngắn hơn sẽ đổi hành vi trong tình huống VNPT chậm (rủi ro phát hành trùng hóa đơn). | |
| E08 | Viettel gọi Login() TRƯỚC MỖI REQUEST và exception lúc login bị nuốt hoàn toàn (catch rỗng) — hệ thống chỉ báo 'Vui lòng kiểm tra tài khoản!' mà mất nguyên nhân gốc. | |
| E09 | Viettel dùng .Result (sync-over-async) trong ViettelRepository.SendToViettel và Login — nguy cơ chặn thread pool; đồng thời `new HttpClient` mỗi lần gọi gây cạn socket. | |
| E10 | Trường `_token` là field instance của ViettelRepository đăng ký Scoped: an toàn trong 1 request nhưng KHÔNG dùng lại giữa request; đừng nhầm là cache. | |
| E11 | Trường static `isSerialMTT` trong PrepareDataToThirdParty có race condition giữa các request đồng thời. | |
| E12 | Không được gộp 2 API Viettel cancelTransactionInvoice và searchInvoiceByTransactionUuid vào luồng JSON — chúng bắt buộc form-url-encoded. | |
| E13 | Company.PartnerUrl phải kết thúc bằng '/': VNPT nối thẳng 'publishservice.asmx'; Viettel nối thẳng 'auth/login', 'services/...'. Thiếu '/' là 404 im lặng. | |
| E14 | Binding VNPT dùng Security.Mode = Transport => chỉ chạy HTTPS. PartnerUrl http sẽ ném lỗi cấu hình WCF. | |
| E15 | Với cùng một nghiệp vụ, tên tham số tài khoản VNPT khác nhau (username/password vs userName/userPass vs username/pass vs Account/ACpass) — copy-paste sai sinh ERR:1 rất khó lần. | |
| E16 | Chuỗi thành công của VNPT có thể chứa cả 'ERR:' (vd 'ERR:6#\|\|ERR:15#\|\|OK:#..._129'). Dùng phép kiểm 'chứa ERR:' cho PublishInvFkey sẽ báo lỗi nhầm cho hóa đơn đã phát hành. | |
| E17 | Bóc ezInvoiceId từ response multi dựa vào Substring(len-36) (độ dài GUID) — nếu ezInvoiceId đổi định dạng (không còn 36 ký tự) sẽ hỏng. | |
| E18 | GetMCCQThueByFkeys/GetCertInfo trả Base64 XML; nếu tưởng là chuỗi thường sẽ parse rác. GetCertInfo còn phải regex lấy <SerialNumber>. | |
| E19 | Hóa đơn Biên lai (INV_RECEIPT = 2) đi WSDL PublishService_TLYT riêng, luồng hủy phải unConfirmPayment trước cancelInv, và không hỗ trợ DELETE / DOWNLOAD_PDF_INV_DRAFT. | |
| E20 | Sửa hóa đơn nháp phải sinh ezInvoiceId (GUID) MỚI sau khi xóa, nếu tái dùng fkey cũ VNPT trả ERR:13 (trùng fkey). | |
| E21 | Giới hạn phía VNPT (theo bảng lỗi): tối đa 200 fkey khi ký (ERR:10 nhóm SignInvoice), tối đa 100 hóa đơn khi lấy trạng thái (ERR:10 nhóm GetStatusInvoice), tối đa 500 hóa đơn/lô và thông điệp gửi thuế <= 1MB (ERR:10/ERR:12 nhóm SendIncoieCQT). | |
| E22 | MaxReceivedMessageSize/MaxBufferSize phải mở tối đa vì PDF trả về dạng Base64 trong SOAP; giới hạn mặc định 64KB sẽ đứt. | |
| E23 | Một số trường bị comment 'tạm bỏ do truyền sang báo ERR3' (Product.Remark, Product.Extra2, Product.Discount, Product.DiscountAmount, Invoice.ServicechargeAmount) — KHÔNG được 'khôi phục cho đầy đủ' khi viết lại. | |
| E24 | Bảng dịch lỗi tra theo khóa chuỗi nghiệp vụ, trong đó SEND_INVOICE_CQT = 'SendIncoieCQT' viết sai chính tả; sửa chính tả sẽ làm mất khớp bảng lỗi. | |
| E25 | Danh sách 9 GUID khách sạn hard-code (không gửi ServiceSummary) bị lặp ở 2 file và có 1 GUID trùng nhau (F97FF80D... ghi chú cho cả TTC Đà Lạt và Nova Phan Thiết) => thực chất chỉ có 8 GUID hiệu lực. | |
| E26 | Bản MTT của IsNotSendServiceSummary thiếu .ToUpper() nên điều kiện không bao giờ đúng — phải quyết định giữ nguyên hay sửa, và ghi rõ vào tài liệu bàn giao. | |
| E27 | Log cắt 100 ký tự cho PDF/Preview: không thể dùng log cũ để đối chiếu byte-by-byte nội dung file. | |
| E28 | Newtonsoft mặc định gửi cả field null trong JSON Viettel; bỏ null có thể đổi hành vi Viettel (chưa từng kiểm chứng). | |
| E29 | VnptRepository (Partner=1) dùng lẫn proxy BusinessService_V5 và PublishService/PortalService v1 — không thể giả định 1 bộ contract duy nhất cho 'VNPT V1'. | |


## F. Tích hợp MISA & Minvoice

| # | Bẫy | Quyết định |
|---|---|---|
| F01 | MISA: KHONG duoc dung `response.EnsureSuccessStatusCode()` hay tuong duong. Misa tra loi nghiep vu kem HTTP 4xx/5xx - phai doc body truoc, chi dung StatusCode lam fallback khi body rong (MisaRepository.cs:128-136). | |
| F02 | MISA: Truong `Data` trong response la CHUOI JSON LONG (double-encoded). Mot so endpoint can `.Replace("\\\"","\"")` truoc khi parse, mot so KHONG. FormSerial can them `.Replace("\"[","[").Replace("]\"","]")`. Ap dung dong nhat se lam vo mot nua so luong (doi chieu MisaConvertResponse.cs:66 vs :254 vs :403). | |
| F03 | MISA: Wrapper JSON key khac nhau giua cac luong - MTT dung `OrgInvoiceData`, HSM/MTT-HSM/Ve dung `OriginalInvoiceData` (MisaInvoiceModel.cs:61 vs :70). | |
| F04 | MISA: Header `CompanyTaxcode` (login) va `CompanyTaxCode` (nghiep vu) khac chu hoa/thuong; endpoint token bien lai truyen GUID CompanyId vao header MST (MisaRepository.cs:339). Dung 'sua cho dep'. | |
| F05 | MISA: Preview BAT BUOC goi LoginWithDetail truoc roi moi goi PreviewInvoice - neu khong, loi dang nhap se hien ra thanh loi parse response (MisaAppService.cs:672-686). | |
| F06 | MISA: Cac API phat hanh dung `Timeout.InfiniteTimeSpan` CO CHU DICH (hoa don may tinh tien/ky HSM co the mat rat lau). Rieng GetInvoiceFormSerial phai cat o 90 giay vi Misa hay treo. Dat timeout chung se hong mot trong hai. | |
| F07 | MISA: Lam tron BAT BUOC dung MidpointRounding.AwayFromZero. Math.Round mac dinh .NET la banker's rounding; JavaScript Math.round lam tron .5 len (khac voi so am); Python round() cung la banker's. Da co 2 bug production vi chuyen nay (febc885, 1dcadf1). | |
| F08 | MISA: Thu tu goi BAT BUOC cua luong USB Token: createinvoice -> xmlhash -> (client ky) -> invoice-calculating/tax -> invoicepublishing. Bo qua hoac dao thu tu -> hoa don khong duoc CQT chap nhan. | |
| F09 | MISA bien lai (INV_RECEIPT): BAT BUOC goi GetInvoiceSerial va tim dung TemplateCode == Invoice.Serial TRUOC khi phat hanh; khong tim thay thi phai dung han (nem Exception), khong duoc gui bua (MisaConvertResponse.cs:290-313). | |
| F10 | MISA: `Quantity` trong Originalinvoicedetail (bien lai) la kieu INT va bi ep `(int)item.Quantity.Value` -> mat phan thap phan cua so luong (MisaConvertRequest.cs:1794). Con OriginalInvoiceDetail (hoa don/ve) la decimal. | |
| F11 | MISA: OrgInvSeries khi dieu chinh/thay the phai cat ky tu dau NEU do dai bang 7. Neu do dai khac 7 thi giu nguyen. Logic dieu kien do-dai nay rat de bi bo sot (MisaConvertRequest.cs:2174-2177). | |
| F12 | MISA: Cache token la `static List<...>` khong khoa - race condition duoi tai cao. Ngoai ra co lech mui gio: tao moi dung DateTime.Now.AddDays(7) nhung refresh va so sanh dung DateTime.UtcNow -> token dau tien song lau hon 7 gio (MisaRepository.cs:57 vs :72,:80). | |
| F13 | MINVOICE: Header Authorization thuc te la `Bear <token>` chu khong phai `Bearer`. Da hoat dong on dinh nhieu nam. Doi thanh Bearer PHAI xac minh voi NCC truoc (MinvoiceAppService.cs:63). | |
| F14 | MINVOICE: ma_dvcs bi hard-code "VP" cho moi giao dich nghiep vu (MinvoiceAppService.cs:48). Chi HealthCheck moi dung Username2. | |
| F15 | MINVOICE: URL base ghep khac nhau o 2 cho - __Login dung `{PartnerUrl}/api/...` (co dau /) con APIServices dung `{PartnerUrl}api...` (khong co dau /). Nghia la PartnerUrl luu trong DB PHAI ket thuc bang '/' de APIServices chay dung, va __Login se tao thanh '//api' (van chay). Chuan hoa URL se lam vo mot trong hai (MinvoiceAppService.cs:45 vs Models/UrlApi.cs:5). | |
| F16 | MINVOICE: chi Login/Create/CreateAndPublish co timeout 5 phut + bat timeout. Replace/Adjust/Cancel/Sign/GetPdf/GetInvoices/SendEmail tao HttpClient rieng KHONG set timeout (mac dinh 100s) va KHONG bat TaskCanceledException (APIServices.cs:559, :664, :734, :787, :839, :874, :923, :953). | |
| F17 | MINVOICE: Endpoint dieu chinh (DieuChinh) nhan body la OBJECT PHANG, khong boc trong {data:[...]} nhu Save/ThayThe (APIServices.cs:694-699 tra ve JsonConvert.SerializeObject(master) truc tiep). | |
| F18 | MINVOICE: Detail cua dieu chinh dung ten truong `stt` (khong phai `stt_rec0`) va `ma_thue` kieu STRING format "#0.#####" (khong phai int) - MinvoiceModels.cs:269,:279 vs :241,:251. | |
| F19 | MINVOICE: Ham lay PDF doc CUNG MOT response 2 lan (ReadAsStringAsync roi ReadAsByteArrayAsync). Trong .NET dieu nay chay duoc do content da buffer; nhieu HTTP client khac se nem loi 'body already consumed' (APIServices.cs:845-846). | |
| F20 | MINVOICE: Nhan dien 'khong ton tai hoa don' bang cach tim chuoi tieng Anh `"There is no row at position 0."` trong response - phu thuoc vao thong bao exception noi bo cua NCC (APIServices.cs:847). | |
| F21 | MINVOICE: 2 GUID cong ty hard-code trong code quyet dinh cach tinh tong phi dich vu (APIServices.cs:401). Danh sach nay da tang dan qua 2 commit - co kha nang se con them. | |
| F22 | MINVOICE: Cac Exception validate ném tu getMasterData KHONG duoc bat trong Create() -> noi thang len tang tren. Neu ben moi bat het thanh 'loi khong xac dinh' se mat thong bao huong dan nguoi dung (APIServices.cs:381-386). | |
| F23 | MOI NHA CUNG CAP: `ServicePointManager.ServerCertificateValidationCallback += ...` (cong don, khong go). Muc dich la BO QUA kiem tra chung chi cho server NCC (chung chi tu ky/het han/chuoi khong day du) + ep TLS 1.2 (server NCC khong ho tro TLS 1.3 hoac negotiate that bai). Ben moi PHAI co co che tuong duong (vd Node: rejectUnauthorized:false + minVersion TLSv1.2; Java: TrustManager tuy bien) NHUNG NEN gioi han theo host thay vi toan cuc, va PHAI dung phep gan mot lan chu khong cong don (Helper/InitiateSSL.cs:11-12). | |
| F24 | LUU Y BAO MAT khi viet lai: credential Minvoice/Misa bien lai di qua QUERY STRING (`user/token?...&password=...` - MisaRepository.cs:333) nen se nam trong access log cua NCC va cua proxy. Day la yeu cau cua NCC, khong sua duoc, nhung phai biet de khong log lai o phia minh. | |
| F25 | MisaWebApp: ApiClient nem Exception khi status khac 2xx -> mat body loi cua Misa. Neu tiep tuc phat trien luong nay PHAI sua truoc, neu khong toan bo bang 68 ma loi se vo dung (MisaWebApp/Http/ApiClient.cs:41-44). | |
| F26 | MisaWebApp: bang ma ItemType/InventoryItemType KHAC bang cua Misa cu (0/2/3/4/6 vs 1/2/3/4). Copy nham -> sinh dong hoa don sai tinh chat (MisaWebApp/Models/MisaRequestModel.cs:298-305 vs Misa/Models/MisaInvoiceModel.cs:393). | |
| F27 | MisaWebApp: khoa cache token KHONG chua Password (`{CompanyId}_{TaxNumber}_{Username}_{PartnerUrl}`) - doi mat khau se khong lam moi token trong 7 ngay (MisaWebApp/Auth/MisaAuthService.cs:19-20). | |
| F28 | LoggingContext dung AsyncLocal + ghi truc tiep vao Elasticsearch bang fire-and-forget (`_ = SaveToElastic()`), moi exception deu bi nuot bang `catch {}`. Log co the mat am tham (Helper/LoggingContext.cs:174-218). | |
| F29 | Cac ham Convert* cua Misa deu co `catch (Exception) { throw; }` - khong lam gi, chi lam mat stack trace goc trong mot so truong hop. Day la nhieu, khong phai logic. | |


## G. Tích hợp Cyberbill, EasyInvoice, Fast, Sun

| # | Bẫy | Quyết định |
|---|---|---|
| G01 | Sun: 'PartnerUrl' và 'PartnerUrl2' KHÔNG cùng nghĩa với các NCC khác. Sun: login + hầu hết API dùng PartnerUrl2, riêng hủy hóa đơn dùng PartnerUrl. Fast: PartnerUrl = MTT, PartnerUrl2 = GTGT (SOAP). Cyberbill: chỉ dùng PartnerUrl. EasyInvoice: chỉ dùng PartnerUrl. Bằng chứng: SunRepository.cs:63/70/75/114, FastConvertRequest.cs:945, CyberbillRepository.cs:36, EasyInvoiceConvertRequest.cs:44. | |
| G02 | Fast: Username/Username2 chứa CHUỖI JSON {clientC, proxyC, user, unitC} chứ không phải tên đăng nhập; Password/Password2 mới là mật khẩu thật (FastConvertRequest.cs:942-958). Migrate dữ liệu cấu hình mà không biết điều này là hỏng toàn bộ Fast. | |
| G03 | Fast: TaxRate == 8 được đổ vào cột 'TaxAmount0' chứ không phải 'TaxAmount8' (FastConvertRequest.cs:508-511, :691-694, :801-804). Phải hỏi lại NCC/nghiệp vụ xem đây là thỏa thuận hay bug trước khi viết lại. | |
| G04 | Fast SOAP: ký tự phân tách 'þ' (U+00FE, Chr(254) của VB) trong hash GetKey (FastConvertRequest.cs:24, :915). Rất dễ mất khi copy giữa các encoding; body phải encode UTF-8. | |
| G05 | Fast: SendToFast gửi body JSON với Content-Type 'text/plain' (FastRepository.cs:90). Đổi sang application/json có thể bị NCC từ chối. | |
| G06 | Fast: thứ tự cột trong structure.master/detail phải khớp CHÍNH XÁC thứ tự giá trị. Ở luồng multi-invoice, allMasterKeys là HashSet gom key của mọi hóa đơn và giá trị được sắp theo thứ tự chèn của HashSet (FastConvertRequest.cs:637-643, :707-730). Đổi ngôn ngữ có kiểu tập hợp không giữ thứ tự chèn sẽ làm lệch toàn bộ dữ liệu mà API vẫn báo thành công. | |
| G07 | Sun: sid = ezInvoiceId.Replace("-","").Substring(0,32) ném ArgumentOutOfRangeException nếu ezInvoiceId không phải GUID chuẩn (SunConvertRequest.cs:142). Không có kiểm tra null/độ dài. | |
| G08 | Sun: ép kiểu (int?) làm mất phần thập phân của ServiceRate (c8) và SpecialTaxRate (c6) (SunConvertRequest.cs:158, :192). | |
| G09 | Sun: khi có ExtraDataMap + ExtraData thì HotelExtra bị bỏ qua HOÀN TOÀN (else-if tại SunConvertRequest.cs:174-189). Cấu hình map thiếu trường -> trường đó thành chuỗi rỗng chứ không fallback. | |
| G10 | Sun thay thế hóa đơn: AdjReason.GetValueOrDefault(3) trả null vì dictionary chỉ có key 1 và 2 -> trường 'rea' gửi sang Sun là null (SunConvertRequest.cs:552, :564, :599-603). | |
| G11 | Sun: điều chỉnh dùng JObject.FromObject(...).ToObject<SunAdjustInvoiceModel>() để 'nâng cấp' kiểu (SunConvertRequest.cs:538, :570) — round-trip qua JSON, mọi trường không serialize được sẽ âm thầm mất. | |
| G12 | Sun: dòng thuế TTĐB tạo bằng GroupBy(...).FirstOrDefault() rồi Add thẳng vào danh sách; nếu FirstOrDefault trả null (không xảy ra với GroupBy(e=>1) nhưng phụ thuộc chi tiết LINQ) sẽ chèn phần tử null vào items (SunConvertRequest.cs:413-432). | |
| G13 | Cyberbill: Int32.Parse(pmsInvoice.PaymentMethod) ném exception nếu PMS gửi chuỗi không phải số, và CyberbillAppService KHÔNG bọc ExecuteAndTranslateException nên lỗi này nổi lên tận controller (CyberbillConvertRequest.cs:148; CyberbillAppService.cs:78-84). | |
| G14 | Cyberbill: Login() nuốt mọi exception và trả chuỗi rỗng (CyberbillRepository.cs:46), sau đó SendToCyberbill trả chuỗi JSON '"Vui lòng kiểm tra tài khoản!"' (:132) — chuỗi này rồi bị JsonConvert.DeserializeObject<dynamic> ở ConvertResponse xử lý như đối tượng, sinh lỗi khó lần. | |
| G15 | Cyberbill: EndpointApi.CREATE_AND_PUBLISH và SEARCH_BY_ID là chuỗi RỖNG (Models\EndpointApi.cs:12, :19) -> POST thẳng vào base URL nếu ai đó gọi nhầm. | |
| G16 | EasyInvoice: SendToEasyInvoicePDF luôn đọc response thành byte và base64 hóa, kể cả khi NCC trả JSON lỗi (EasyInvoiceRepository.cs:75-76) -> PMS nhận 'PDF' là base64 của chuỗi lỗi. | |
| G17 | EasyInvoice: cả 3 hàm CreateDataInvoice/CreateDataAdjustInvoice/CreateDataReplaceInvoice lặp lại nguyên khối logic phí + extraData (EasyInvoiceConvertRequest.cs:368-443, :534-610, :701-777). Đã có tiền lệ sửa 1 chỗ quên 2 chỗ; khi viết lại phải gom thành 1 hàm. | |
| G18 | EasyInvoice: AmountInWords ở luồng tạo nhân với exchangeRate đã chuẩn hóa (0 -> 1) nhưng ở luồng điều chỉnh/thay thế lại nhân trực tiếp (pmsDataInvoice.ExchangeRate ?? 1) — không đồng nhất khi ExchangeRate = 0 (EasyInvoiceConvertRequest.cs:361 & :474 vs :643, :807). | |
| G19 | Helper: InitiateSSLTrust dùng '+=' nên mỗi lần gọi lại thêm 1 delegate vào callback toàn cục -> danh sách delegate phình vô hạn theo số request, và TLS validation bị tắt cho TOÀN BỘ tiến trình (Helper\InitiateSSL.cs:11-12). | |
| G20 | Helper: LoggingContext.SaveAsync gọi SaveToElastic bằng '_ = ...' (fire-and-forget, không await) và HttpClient được new/dispose mỗi lần (Helper\LoggingContext.cs:197, :209) -> nguy cơ cạn socket và mất log khi tiến trình dừng. | |
| G21 | Helper: mọi hàm log bọc trong catch{} rỗng (LoggingContext.cs:47, :130, :153, :171, :200) -> Elastic chết thì hệ thống vẫn chạy nhưng KHÔNG có bất kỳ cảnh báo nào. | |
| G22 | Helper: LoggingContext.Configuration là biến static public phải được gán thủ công ở Program.cs:21; quên gán thì log im lặng không hoạt động. | |
| G23 | Múi giờ: LoggingModel dùng DateTime.UtcNow (LoggingContext.cs:125, :148, :167) trong khi SetRequestPartnerTime/SetResponsePartnerTime của các repository dùng DateTime.Now (giờ máy chủ, +07) — cùng một document log có 2 hệ múi giờ (SunRepository.cs:53 & :95, CyberbillRepository.cs:95 & :134, FastRepository.cs:40 & :55, EasyInvoiceRepository.cs:20 & :38). Ngày nghiệp vụ (rdt, ngày hủy, ngày điều chỉnh) cũng dùng DateTime.Now. | |
| G24 | Culture: mọi ParseExact dùng CultureInfo.InvariantCulture với 'dd/MM/yyyy', nhưng các lệnh .ToString() số tiền (decimal.ToString(), ToString("#0.####"), ToString("G29")) KHÔNG truyền culture -> phụ thuộc culture của tiến trình; máy chủ đặt culture dùng dấu phẩy thập phân sẽ sinh payload sai (SunConvertRequest.cs:220-221/376-377, EasyInvoiceConvertRequest.cs:868-875, CyberbillConvertRequest.cs:188). | |
| G25 | Trong SunConvertRequest, các trường ánh xạ ExtraData được khai báo là 'private static string c0..c9' (SunConvertRequest.cs:16-25) — biến static dùng như hằng chuỗi; c2, c6, c7, c8, c9 khai báo nhưng không dùng trong GetDataCustomField. | |
| G26 | Thuế suất ngoài 0/5/8/10 (không chịu thuế, không kê khai, thuế nhà thầu) KHÔNG được đổ vào bảng tổng của EasyInvoice và Fast dù model có trường (EasyInvoiceConvertRequest.cs:480-506, FastConvertRequest.cs:506-521) — cần xác nhận nghiệp vụ trước khi viết lại. | |


## H. Cấu hình, vận hành & triển khai

| # | Bẫy | Quyết định |
|---|---|---|
| H01 | WebApiCustomMiddleware NUỐT exception: nó thay context.Response.Body bằng MemoryStream, gọi _next trong try, và nếu _next ném lỗi thì chỉ Log.Error rồi KHÔNG rethrow, KHÔNG copy MemoryStream về originalBody, KHÔNG khôi phục Response.Body. Kết quả: client nhận response rỗng thay vì lỗi. Xem src/ezInvoice.Services.Api/Configurations/WebApiCustomMiddleware.cs:16-48. | |
| H02 | Middleware này cũng đọc toàn bộ body request và body response vào chuỗi (ReadToEndAsync) cho MỌI request, kể cả response chứa PDF base64 - đây là lý do phải có Helper/SubStringLogPDF.cs và commit 58164dc 'Giảm ký tự khi log thông tin file PDF' (EAS-27827). Viết lại mà log nguyên body PDF sẽ nổ bộ nhớ/log. | |
| H03 | LoggingContext là static với AsyncLocal và SetLoggingId ném InvalidOperationException nếu correlation id đã set - nhưng tất cả đều bọc trong try{}catch{} RỖNG (LoggingContext.cs:27-47, :153, :171, :200). Mọi lỗi ghi log bị nuốt hoàn toàn, không có cảnh báo. Trong môi trường mới nếu dùng scoped context thay AsyncLocal phải kiểm tra kỹ luồng async/fire-and-forget. | |
| H04 | SaveToElastic được gọi kiểu fire-and-forget `_ = SaveToElastic();` (LoggingContext.cs:197) và tạo mới HttpClient trong using mỗi lần (LoggingContext.cs:209) -> vừa mất log âm thầm vừa có nguy cơ cạn socket (socket exhaustion) khi tải cao. | |
| H05 | ServicePointManager.ServerCertificateValidationCallback += (...) => true là TOÀN CỤC và dùng toán tử += nên mỗi lần gọi lại ĐĂNG KÝ THÊM một delegate; gọi 47 chỗ, mỗi request -> danh sách delegate phình vô hạn theo thời gian chạy (memory leak). Xem Helper/InitiateSSL.cs:11. | |
| H06 | Múi giờ: Viettel nhận invoiceIssuedDate là epoch milliseconds. Code hiện tại parse 'dd/MM/yyyy' rồi .ToUniversalTime() rồi trừ 1970 (ViettelConvertRequest.cs:~92-96). Điều này CHỈ đúng khi server chạy múi giờ GMT+7. Trước 26/01/2025 không có .ToUniversalTime() và hóa đơn hiển thị 07:00 thay vì 00:00 (commit 42421ef 'chỉnh lại thời gian mặc định từ 7 giờ về 0 giờ'). Deploy container UTC sẽ tái hiện lại đúng bug cũ. | |
| H07 | Trộn DateTime.Now và DateTime.UtcNow trong cùng một logic: cache token Misa set TokenExpireDate = DateTime.Now.AddDays(7) khi tạo mới (MisaRepository.cs:57) nhưng khi refresh lại dùng DateTime.UtcNow.AddDays(7) (MisaRepository.cs:80) và so sánh với DateTime.UtcNow (MisaRepository.cs:72). Trên server GMT+7 token đầu tiên sống 7 ngày 7 giờ, các token sau sống đúng 7 ngày. | |
| H08 | Điều kiện hết hạn token Misa TỪNG BỊ ĐẢO NGƯỢC: `TokenExpireDate > DateTime.UtcNow` (tức chỉ lấy token mới khi token CÒN hạn). Sửa ngày 19/03/2025 tại commit 8730f96. Đây là loại bug rất dễ lặp lại khi viết lại. | |
| H09 | Làm tròn: .NET Math.Round mặc định là banker's rounding (MidpointRounding.ToEven) nên 0.5 làm tròn xuống 0. Đã 2 lần phải sửa bằng cách thêm MidpointRounding.AwayFromZero - commit febc885 (16/01/2026, ES-282610, AmountOC/Amount của Misa) và 1dcadf1 (18/06/2026, ES-323605, UnitPrice của Misa). Ngôn ngữ mới có quy tắc làm tròn mặc định KHÁC (Python banker's, JS half-up) -> phải chỉ định tường minh AwayFromZero ở MỌI phép làm tròn tiền. | |
| H10 | Định dạng số cho Sun dùng dấu PHẨY làm dấu thập phân bằng cách .ToString("#0.#########").Replace('.', ',') - phụ thuộc CurrentCulture của thread. Nếu culture server là vi-VN thì ToString đã ra dấu phẩy và Replace không có tác dụng -> ra sai. Xem src/ezInvoice.Application/EventSourcedNormalizers/SunNormalizes/SunConvertRequest.cs (invoiceData.c9, c7). | |
| H11 | Code chuyển đổi bị COPY-PASTE hàng loạt: cùng một khối gán Amount0/Amount5/Amount8/Amount10 lặp ở ít nhất 6 vị trí trong EasyInvoiceConvertRequest.cs (dòng ~483, ~652, ~816...), khối BuyerCode lặp 6+ lần trong MisaConvertRequest.cs, khối IsTaxReduction43 lặp ~10 lần. Bug 'Amount = TaxAmount thay vì BeforeTaxAmount+TaxAmount' sống rất lâu và phải sửa 12 chỗ cùng lúc (commit 8bc48a7, 15/08/2026). Khi viết lại PHẢI gom về một hàm chung, và phải rà đủ mọi biến thể (Create / Adjust / Replace / Draft / Preview). | |
| H12 | Bộ lọc tra cứu log CÓ BUG THẬT: các nhánh lọc InvoiceTaxCode, InvoiceStatus, InvoiceSystem đều truyền model.InvoiceId làm giá trị Query thay vì trường tương ứng. Xem src/ezInvoice.Application/Services/InvoiceAppService.cs:97-120. Phân trang GetAllInvoiceLogs dùng From = model.PageIndex (không nhân PageSize) trong khi GetInvoiceLogs dùng (PageIndex-1)*PageSize (InvoiceAppService.cs:151 vs :238) - 2 API hành xử khác nhau. | |
| H13 | CompanyAppService.GetById gọi bất đồng bộ nhưng chặn luồng bằng .GetAwaiter().GetResult() bên trong (CompanyAppService.cs:67) và Task.WhenAll + .Result ở ngoài (CompanyAppService.cs:35-36) -> nguy cơ deadlock/thread starvation dưới tải. Viết lại nên dùng async xuyên suốt nhưng phải kiểm chứng lại hành vi timeout. | |
| H14 | Thứ tự middleware bắt buộc: LoggingMiddleware (sinh correlation id) PHẢI chạy TRƯỚC WebApiCustomMiddleware và trước mọi controller, nếu không LoggingContext.Init() trong controller sẽ tạo LoggingModel không có Id -> ghi vào Elastic với id rỗng. Program.cs:63-65. | |
| H15 | Swagger UI trỏ endpoint tương đối '../swagger/v2/swagger.json' (SwaggerConfig.cs:60) - chỉ hoạt động khi app được host dưới virtual directory con của IIS (ezInvoiceMsApi_TEST...). Chạy ở root sẽ 404. | |
| H16 | AddMediatR(AppDomain.CurrentDomain.GetAssemblies()) (Program.cs:55) quét các assembly ĐÃ NẠP tại thời điểm gọi - phụ thuộc thứ tự nạp assembly của .NET, có thể bỏ sót handler một cách không xác định. | |
| H17 | Tất cả API PMS trả HTTP 200 kể cả khi lỗi (return Ok(ExceptionResponse(ex))) - phải đọc body.Status/body.Code. Đừng 'sửa cho đúng REST' khi viết lại, PMS đang phụ thuộc điều này. Xem PmsController.cs:67-72 (mẫu lặp lại ở ~30 action). | |
| H18 | Trường TypeEinvoice, InvoiceType (0=hóa đơn điện tử, 1=vé điện tử, 2=biên lai điện tử) và FormNo được dùng để phân nhánh loại hóa đơn ở nhiều nơi; ngoài ra Misa dùng formNo_GTGT="1" và formNo_HDBH="2" (MisaConvertRequest.cs:33-34) - đây là mã nội bộ của ezInvoice chứ không phải mẫu số hóa đơn theo quy định thuế, rất dễ nhầm. | |
| H19 | Có 'điểm chết' cấu hình: PmsController có attribute [ThirdPartyAuthorize] bị comment (PmsController.cs:18) và AccountController/CustomerController dùng Identity nhưng không ai gọi. Nếu viết lại mà bật xác thực lên sẽ chặn hết PMS đang chạy - phải phối hợp với đội PMS trước. | |


---

**Tổng: 194 mục.**
