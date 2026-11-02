# Weekly Stand-up Meeting — Week 09

**วันที่ประชุม:** 2026-09-08 | **Sprint:** Sprint 1
-------------------------------------------------------

## รายงานความคืบหน้าประจาสัปดาห์ (3 คาถามหลัก)

| สมาชิก (Domain) | อาทิตย์ที่ผ่านมาทาอะไรมาบ้าง (Done) | อาทิตย์นี้จะทาอะไร (Plan) |
ปัญหา/อุปสรรคที่พบ (Blockers) |
|---|---|---|---|
| นายวุฒิภัทร นวมนิ่ม (Programmer) | เขียนโค้ดเดินซ้าย-ขวาเสร็จ และทดสอบ Import Sprite เข้า
MonoGame | ทาระบบกระโดดและตรวจการชนกับพื้น (Collision) | ยังคานวณ Gravity ไม่สมูท
ตัวละครตกทะลุพื้นเป็นบางจังหวะ |
| อัตถนิรัต แปงใจดี (Designer) | ร่าง Layout Tilemap ด่าน 1 ใน Tiled ขนาด 32x32 |
จัดวาง Collision Layer ให้ตรงกับ Tilemap | รอขนาด Tile Size ที่โปรแกรมเมอร์ต้องการยืนยัน
|
| นายวุฒิภัทร นวมนิ่ม (Programmer) | วางโครงสร้าง ScreenManager และหน้า Title Screen |
เชื่อมต่อระบบเปลี่ยน State ระหว่าง Title Screen และ Gameplay | โค้ด MonoGame บน
macOS มีปัญหาเรื่อง Font Rendering |
-------------------------------------------------

## Action Items & Blockers Resolution

- [ ] [ช่วยนายวุฒิภัทร นวมนิ่มแก้สมการ Gravity และ Jump Physics] [status:: doing]
  [owner:: นายวุฒิภัทร นวมนิ่ม] [due:: 2026-09-10]
- [ ] [ยืนยันขนาด Tile Size ให้สมหญิง] [status:: done] [owner:: อัตถนิรัต แปงใจดี]
  [due:: 2026-09-08]
- [ ] [ทดสอบ Cross-platform Font บน Windows/macOS] [status:: todo]
  [owner:: นายวุฒิภัทร นวมนิ่ม] [due:: 2026-09-12]

---

## Related Documents

- [[docs/agile/sprint-plan-01|Sprint 1 Plan]]
- [[docs/agile/02-sprint-backlog|Sprint Backlog]]
