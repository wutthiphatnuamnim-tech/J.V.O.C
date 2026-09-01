# Product Backlog

**Version:** 1.0 | **Last Updated:** 2026-09-01

> รวม User Story ทั้งหมดของโปรเจกต์ — ยังไม่ได้แปลว่าต้องทำใน Sprint นี้ทั้งหมด
> โปรเจกต์นี้แบ่งงานตลอดเทอมเป็น **3 Sprint** (Sprint 1-3) — Sprint ไหนหยิบ Story ไปทำ ให้ใส่เลข Sprint นั้น (1-3) ลงคอลัมน์ `Sprint`

## Must Have (MVP)

| # | User Story                                                                      | Acceptance Criteria                                                                                            | Estimate (SP) | Sprint |
| - | ------------------------------------------------------------------------------- | -------------------------------------------------------------------------------------------------------------- | ------------- | ------ |
| 1 | As a player, I want to Fight, so that I Need Enemy to fight                     | ศัตรูโผล่ขึ้นมาและตัวละครสามารถโจมตีศัตรูฝั่งตรงข้ามได้ | 5             | 1      |
| 2 | As a player, I want to Fight, so that I Need System to make me to fight         | คลิกโจมตีแล้วตัวละครพุ่งไปโจมตี                                                 | 3             | 1      |
| 3 | As a player, I want to Stand , so that I Need Background stage to make me stand | มี Background แสดงขึ้นมา เพื่อไม่ให้แสดงว่าตัวละครยืนลอย            | 6             | 2      |
| 4 | As a player, I want More Character , so that To make me strong                  | มีตัวละครหลากหลาย                                                                             | 8             | 1      |

## Should Have

| # | User Story                                                                             | Acceptance Criteria                                                                                 | Estimate (SP) | Sprint |
| - | -------------------------------------------------------------------------------------- | --------------------------------------------------------------------------------------------------- | ------------- | ------ |
| 1 | As a player, I want to Fight, so that I Need Animation sprite sheet to see my Movement | คลิกโจมตีแล้วตัวละครพุ่งไปโจมตีแสดง Animation                    | 1             | 2      |
| 2 | As a player, I want to see my Hp bars, so that I know how close I am to game over      | หลอกเลือดแสดงบนจอตลอดเวลา ลดลงทันทีที่โดนโจมตี         | 3             | 1      |
|   | As a player, I want to Stand , so that I Need Background stage to make me stand        | มี Background แสดงขึ้นมา เพื่อไม่ให้แสดงว่าตัวละครยืนลอย | 6             | 2      |
| 3 | As a player, I want to see UI, so that I can interact in game                          | สามารถกดเพื่อแสดง action                                                           | 7             | 2      |
| 4 | As a player, I want to see my Ultimate skill, so that To make me feel impact           | สามารถใช้สกิล ulitimate ได้                                                         | 15            | 2      |
| 5 | As a player, I want to hear SFX, so that To make me feel impressive                    | สามารถได้ยินเสียง SFX ได้                                                       | 3             | 3      |

## Nice to Have

| # | User Story                                                                             | Acceptance Criteria                                                                                                                                      | Estimate (SP) | Sprint |
| - | -------------------------------------------------------------------------------------- | -------------------------------------------------------------------------------------------------------------------------------------------------------- | ------------- | ------ |
| 1 | As a designer, I want to store data, so that I can to continue previous play          | ปรับค่า spawn rate ในไฟล์ data แล้วรันเกมใหม่ ค่าที่เปลี่ยนมีผลทันทีโดยไม่ต้อง build ใหม่ | 3             | 3      |
| 2 | As a player, I want to see Fx particle, so that to make me feel immersive atmosphere | เห็น FX แสดงหลังจากตัวละครโจมตีศัตรู9                                                                                    | 9             | 3      |
| 3 | As a player, I want to add more skill, so that I can attack in various ways.         | มีการโจมตีที่หลากหลายให้ผู้เล่นเลือกโจมตี                                                                       | 7             | 3      |

## MoSCoW Legend

- **Must Have** — จำเป็นต่อ core gameplay loop เกมเล่นไม่ได้ถ้าขาด (MVP)
- **Should Have** — เพิ่มคุณภาพเกม แต่เกมเล่นได้โดยไม่มีก็ได้
- **Nice to Have** — ทำถ้ามีเวลาเหลือ

## Links

- [[docs/gdd/00-concept|GDD Concept]]
- [[docs/agile/02-sprint-backlog|Sprint Backlog]]
