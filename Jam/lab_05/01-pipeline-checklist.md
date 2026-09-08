---
type: jam-pipeline
version: 0.1
date: [21/07/26]
team: [J.V.O.C]
---
# Pipeline Function Checklist — [Underwake]

> เกมทุกแนวต้องมี pipeline ขั้นต่ำนี้ก่อนถึงจะเรียกว่า "เล่นได้" — เติมชื่อผู้รับผิดชอบและติ๊กสถานะระหว่างลงมือทำจริง เพิ่มแถวได้ถ้าเกมของทีมต้องการ module อื่น

## Must-Have (ต้องมีก่อน Hour 24 — Playable Build)

| Module (ตาม MonoGame Game Loop)             | หน้าที่                                                        | สถานะ     | ผู้รับผิดชอบ                                                                                      |
| ---------------------------------------------- | --------------------------------------------------------------------- | -------------- | ------------------------------------------------------------------------------------------------------------- |
| `GameStateManager`                           | สลับ state (Menu / Playing / Pause / GameOver)                    | 🔲 Not Started | [วุฒิภัทร นวมนิ่ม]                                                                             |
| `InputManager`                               | รับ input keyboard/gamepad แบบรวมศูนย์                  | 🔲 Not Started | [วุฒิภัทร นวมนิ่ม]                                                                             |
| Core `Update()` logic ของกลไกหลัก | logic ของ core mechanic 1 อย่างที่เป็นหัวใจเกม | 🔲 Not Started | [วุฒิภัทร นวมนิ่ม]                                                                             |
| Collision / interaction พื้นฐาน         | ตรวจชน/ตรวจ trigger ระหว่าง entity                   | 🔲 Not Started | [วุฒิภัทร นวมนิ่ม]                                                                             |
| Background / Scene / Ui / Character (ART)      | วาดผ่าน Photoshop / aseprite / procreate / cilp studio         | 🔲 Not Started | [ชยพล นากิจ]<br />[อัตถนิรัต แปงใจดี]<br />[เอกการัณ กิ่งสักกลาง] |
| Win / Lose condition / setting                 | เงื่อนไขจบเกม/จบด่าน/ตั้งค่า                | 🔲 Not Started | [วุฒิภัทร นวมนิ่ม]                                                                             |
| Asset                                          | รวม asset                                                          | 🔲 Not Started | [วุฒิภัทร นวมนิ่ม]                                                                             |
| [module เพิ่มเติมของเกมนี้]  |                                                                       | 🔲 Not Started | [ชื่อ]                                                                                                    |

## Nice-to-Have (ทำถ้าเหลือเวลา — Hour 24–34)

| Module                                | หน้าที่                               | สถานะ     | ผู้รับผิดชอบ            |
| ------------------------------------- | -------------------------------------------- | -------------- | ----------------------------------- |
| `AudioManager` (SFX พื้นฐาน) | เสียงตอบสนอง action หลัก     | 🔲 Not Started | [ชยพล นากิจ]               |
| Gacha                                 | เพื่อรอรับตัวละครใหม่ๆ | 🔲 Not Started | [ชยพล นากิจ]               |
| SkillTree                             | unique ของเกม                          | 🔲 Not Started | [อัตถนิรัต แปงใจดี] |
| [feature รองอื่นๆ]            |                                              | 🔲 Not Started | [ชื่อ]                          |

## Cut-List (ตัดทิ้งก่อนถ้าเวลาไม่พอ — ห้ามเริ่มก่อน Must-Have เสร็จ)

- ❌ Save/Load
- ❌ Settings menu
- ❌ Leaderboard
- ❌ [feature สุ่มด่าน]

> เมื่อถึง **Feature Freeze (Hour 34)** ทุก module ในตารางนี้ต้องมีสถานะ ✅ Done หรือถูกย้ายไป Cut-List อย่างชัดเจน — ห้ามค้างเป็น 🔲/🟡
