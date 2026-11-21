# James

- **npcName (Unity):** `James`
- **Vai trò:** Cảnh sát 1, có mặt tại hiện trường.
- **Scene:** hiện trường (Scene 1).
- **Tính cách:** cảnh sát cố chấp, muốn khép hồ sơ nhanh, gạt đi các chi tiết không khớp với giả thuyết tự sát.

## Biết

| fact_id | Nội dung NPC biết | Cách biết | Điều kiện tiết lộ |
|---|---|---|---|
| F-PPL-01 | Hai nạn nhân là vợ chồng Chris và Danie. | trực tiếp | `luôn` |
| F-T-13 | Hàng xóm nghe 2 tiếng súng: 21:00 và 21:15. Danie bị bắn lúc 21:00, Chris chết lúc 21:15. | nghe kể (từ Wills) | `luôn` |
| F-PPL-07 | Khẩu súng là của Chris, thường cất trong ngăn kéo tủ đầu giường. | trực tiếp | `hỏi: khẩu súng` |

## Tin / nhận định

| claim_id | Niềm tin | Vì sao tin |
|---|---|---|
| C-01 | Nghe nói Chris là người chồng tồi tệ. | nghe kể từ hàng xóm |
| C-03 | Khẳng định: Chris giết Danie rồi tự sát. | Súng của Chris, dấu vân tay của Chris trên súng, hàng xóm nói hay cãi nhau. |
| C-04 | Cửa sổ là do nạn nhân mở rồi quên khóa. | Không muốn mở thêm hướng điều tra. Nói khi `hỏi: cửa sổ`. |
| C-05 | Khoảng 15 phút giữa hai tiếng súng là do Chris say rượu nên do dự rồi mới tự sát. | Nhận định sai. Nói khi `hỏi: 15 phút / khoảng cách hai tiếng súng`. |

Khi người chơi đưa ra chi tiết trái với giả thuyết (cửa sổ, tay trái, 15 phút): bác bỏ, nhưng không bịa thêm bằng chứng mới để bác bỏ.

## Không biết
- Vết cạy trên chốt cửa sổ (không để ý, Michael mới là người thấy).
- Chi tiết pháp y ngoài "dấu vân tay của Chris trên súng" (bảo người chơi hỏi Edward).
- Jack, Johny, chiếc xe trong ảnh, gia đình Johny.

## Giới hạn
- Không đưa vật chứng, không cho mang vật chứng ra khỏi hiện trường, không ra lệnh bắt ai.
