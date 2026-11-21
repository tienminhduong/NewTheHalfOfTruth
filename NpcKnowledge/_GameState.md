# Game state có thẩm quyền (Unity)

Vật chứng, tiến độ quest và tương tác chỉ đến từ Unity. LLM chỉ được nói về chúng,
không được tạo, đưa, mở khóa hay xác nhận chúng. File này ghi lại những gì Unity
đang thực sự cung cấp, để điều kiện tiết lộ trong các file NPC khớp với code.

## Dữ liệu Unity gửi cho LLM

`LLMService.CallLLMAsync` gửi `{npcName, npcSystem, question, summary, history, stage}`.
Không có danh sách vật chứng đã thu thập.

`stage` do `LLMService.GetStage(npcName)` tính:

| NPC | Điều kiện | stage |
|---|---|---|
| mọi NPC khác Johny | luôn | 1 |
| Johny | scene đang mở là `Road` | 1 |
| Johny | scene khác `Road`, chưa có `EvidenceImage` | 2 |
| Johny | scene khác `Road`, đã có `EvidenceImage` | 3 |

Hệ quả: điều kiện `có: <EvidenceKey>` trong các file NPC hiện chỉ được LLM đánh giá
mềm theo lời người chơi. Muốn kiểm tra cứng thì cần gửi thêm danh sách evidence key
đã thu thập (từ `SavedDataService.GetEvidenceKeys()`) trong request, và cập nhật
`LLMService`, struct `LLMRequest` và backend cùng lúc.

## Evidence key trong `EvidenceDatabase.asset`

| Key | Tên | Liên quan fact |
|---|---|---|
| `Window` | Cửa sổ | F-T-07, F-T-12 |
| `DanieDead` | Thi thể của Danie | F-T-08, F-E-02 |
| `ChrisDead` | Thi thể của Chris | F-T-10, F-T-11 |
| `Bed` | Chiếc giường đôi | F-E-04 |
| `Book` | Một quyển sách | F-PPL-03 |
| `Paper` | Tờ giấy (danh sách nơi định đi) | F-PPL-03 |
| `CouplePicture` | Hình ảnh trong phòng ("Chris, Danie, và ai vậy?") | F-PPL-01; người thứ ba chưa xác định |
| `ImageEvidence` | Tấm hình Johny đánh rơi (nhà Chris và chiếc xe) | F-T-05, F-T-06, F-J-06 |
| `EvidenceImage` | Hình ảnh Johny chụp lúc 9h (ảnh ngoại phạm) | F-T-04 |
| `ProgramList` | Danh sách chương trình truyền hình | F-T-02 |
| `Guide` | Hướng dẫn điều khiển | — |

## NPC trong `NPCDatabase.asset`

`James`, `Michael`, `Wills`, `Daniel`, `Thomas`, `Brad`, `Johny`, `Jack`, `Edward`.
**`Margaret` chưa có**: cần thêm entry và GameObject `ChatNpc` tên `Margaret` trong scene nhà Johny.

## Chưa đồng bộ, cần designer quyết định

| # | Vấn đề | Đề xuất |
|---|---|---|
| 1 | `ProgramList` ghi "19:00 - Chương trình bóng đá / 21:00 - Thế giới động vật", không có giờ kết thúc trận. Theo đó trận bóng có thể chiếu đến 21:00, làm mâu thuẫn ảnh 20:58 không còn. | Sửa mô tả thành "19:00 - Bóng đá (kết thúc 20:30)\n20:30 - Bản tin\n21:00 - Thế giới động vật", hoặc thêm vật chứng tờ báo ghi trận kết thúc 20:30. |
| 2 | `CouplePicture` hỏi "Chris, Danie, và ai vậy?" nhưng chưa có fact nào nói người thứ ba là ai. | Chốt người thứ ba (ví dụ Brad) và thêm fact. |
| 3 | Vật chứng trong phòng Johny chưa có key: ảnh theo dõi, tấm ảnh ghi lời Chris và Danie, đôi găng tay, tờ báo, ảnh gia đình, bài báo cũ. | Thêm vào `EvidenceDatabase` và cập nhật cột "Nguồn chứng cứ" trong `_CaseFacts.md`. |
| 4 | Khẩu súng không có evidence key riêng. | Tùy chọn: thêm key `Gun`. |
| 5 | `EvidenceImage` mô tả "chụp lúc 9h", trong khi fact là đồng hồ trong ảnh chỉ 20:58. | Đổi thành "Tấm ảnh Johny chụp cạnh đồng hồ chỉ 20:58, TV đang chiếu bóng đá". |
