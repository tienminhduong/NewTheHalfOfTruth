# NpcKnowledge

Thư mục này là knowledge base của vụ án Rainswept, được dựng từ `NPCKKnowledge.md`
(file nháp do game designer cập nhật). Khi `NPCKKnowledge.md` thay đổi, cập nhật lại
các file ở đây và giữ các quy tắc bên dưới.

## Cấu trúc

| File | Nội dung | Ai được dùng |
|---|---|---|
| `_CaseFacts.md` | **World truth**: sự thật của vụ án, dòng thời gian, và các lời đồn/nhận định sai đang lưu hành. | Designer, kiểm tra kịch bản, chấm kết luận. **Không nạp nguyên file vào prompt của NPC.** |
| `_CrossCheck.md` | Đối chiếu cùng một sự kiện qua lời khai của từng NPC, kèm lý do kịch bản cho mỗi khác biệt. | Designer, kiểm tra đồng bộ. |
| `_GameState.md` | Những gì Unity là nguồn có thẩm quyền: evidence key, `stage`, tên NPC, vật chứng còn thiếu. | Designer, lập trình viên. |
| `<npcName>.md` | Kiến thức, niềm tin, điều che giấu, điều không biết và điều kiện tiết lộ của một NPC. | Nguồn để dựng prompt/RAG cho NPC đó. |

Tên file NPC trùng với `npcName` trong `NPCDatabase.asset` (cũng là tên GameObject của `ChatNpc`).

## Quy tắc

### 1. Fact có định danh
Mỗi sự thật trong `_CaseFacts.md` có một `fact_id` cố định và đủ các trường:

- **Nội dung**: một câu, một sự kiện.
- **Nguồn chứng cứ**: evidence key trong Unity, lời khai NPC, hoặc `(chưa có)`.
- **Thời điểm**: mốc thời gian trong đêm xảy ra án (gọi là đêm N), hoặc `—`.
- **Ai biết**: NPC nào biết và biết bằng cách nào.
- **Độ tin cậy**: độ chắc chắn của nguồn chứng cứ (xem thang bên dưới).
- **Điều kiện tiết lộ**: khi nào người chơi có thể biết fact này.

Không đổi `fact_id` đã dùng; fact bị bỏ thì đánh dấu `(đã bỏ)` thay vì xóa.

Tiền tố: `F-` là sự thật, `C-` là lời đồn/nhận định (có thể đúng hoặc sai) mà NPC đang tin.

### 2. Thang độ tin cậy

| Mức | Ý nghĩa |
|---|---|
| A | Vật chứng hoặc kết quả pháp y. |
| B | Nhân chứng trực tiếp nghe/thấy. |
| C | Suy luận từ nhiều dữ kiện, chưa có vật chứng. |
| D | Nghe kể lại, tin đồn. |

### 3. Tách world truth khỏi kiến thức NPC
- NPC chỉ biết những fact được liệt kê trong file của chính NPC đó, kể cả khi hệ thống RAG có lưu những fact khác.
- Mỗi mục kiến thức trong file NPC ghi rõ **cách biết**: `trực tiếp`, `nghe kể`, `suy đoán`, hoặc `tin đồn`.
- Niềm tin sai được ghi ở mục **Tin/nhận định**, kèm lý do NPC tin như vậy. NPC nói niềm tin đó như sự thật của mình, không tự sửa theo world truth.
- Mục **Không biết** là ranh giới cứng: khi người chơi hỏi, NPC nói mình không biết hoặc đoán chung chung, không bịa thêm chi tiết.

### 4. Khác biệt giữa các lời khai phải có lý do
Hai NPC nói khác nhau về cùng một sự kiện chỉ khi có một trong các lý do sau, và lý do được ghi trong `_CrossCheck.md`:

- `nghe kể`: thông tin bị méo qua người kể.
- `nhận định sai`: NPC suy luận sai từ dữ kiện đúng.
- `cố tình nói dối/che giấu`: NPC có động cơ.
- `góc nhìn khác`: mỗi NPC chỉ thấy một phần sự việc.

Nếu khác biệt không có lý do nào ở trên thì đó là lỗi dữ liệu và phải sửa.

### 5. Unity là nguồn có thẩm quyền cho game state
- Vật chứng, tiến độ quest và tương tác chỉ đến từ Unity (`SavedDataService`, `EvidenceDatabase`, `stage` trong `LLMService`).
- NPC không được đưa vật phẩm, mở cửa, dẫn người chơi đi đâu, hay xác nhận người chơi đã có một vật chứng chỉ bằng lời nói. Nếu người chơi đòi xem vật gì, NPC chỉ mô tả hoặc nói "cứ tự xem" và để Unity xử lý.
- NPC không được tin lời người chơi nói "tôi có bằng chứng X" như sự thật. Hiện Unity chỉ gửi `stage` (xem `_GameState.md`), nên các điều kiện kiểu "khi người chơi có vật chứng X" chỉ được LLM đánh giá mềm dựa trên lời người chơi.

### 6. Điều kiện tiết lộ

| Ký hiệu | Ý nghĩa | Ai kiểm tra |
|---|---|---|
| `luôn` | Nói khi người chơi hỏi đúng chủ đề. | LLM |
| `hỏi: <chủ đề>` | Chỉ nói khi người chơi hỏi đúng chủ đề này. | LLM (mềm) |
| `nhắc: <thông tin>` | Chỉ nói khi người chơi đưa ra được thông tin này trong hội thoại. | LLM (mềm) |
| `stage >= N` | Chỉ mở khi Unity gửi `stage` từ N trở lên. | Unity (cứng) |
| `có: <EvidenceKey>` | Chỉ mở khi người chơi đã thu thập vật chứng này. | Unity (cứng), **chưa hỗ trợ**, xem `_GameState.md` |
| `không bao giờ` | NPC không bao giờ tự nói ra. | LLM |
