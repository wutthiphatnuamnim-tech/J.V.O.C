---
type: gdd-core-loop
version: 0.1
date: [7/7/69]
---
# [The Tower Line Battle] — Core Loop & Gameplay

## Core Loop

```mermaid
flowchart LR
	A([เริ่มต้น]) --> B[เรียกตัวละครมาป้องกันฐานและโจมตีศัตรู]
	B --> C{ป้อมเราแตก}
	C --> |Yes| A
	C --> |No| D
	D{ป้อมศัตรูแตก} --> |Yes|E[ได้รับรางวัล]
	D --> |No| B
	E --> F
	F([จบ])
```

## Core Mechanics

1. [Mechanic หลักที่ 1 — การเรียกตัวละครมาป้องกันฐาน]
2. [Mechanic หลักที่ 2 - ระบบค่าcostในการเรียกใช้]
3. [Mechanic หลักที่ 3 - บริหารทรัพยากร]

## Controls

| Key | Action |
| Mouse1 | Accept-all UI |
| Space | ChangeTeamslot |
| press 1 | Sp skill 1 |
| press 2 | Sp skill 2 |
| press 3 | Sp skill 3 |
| press 4 | Sp skill 4 |

## Win / Lose Condition

- **ชนะเมื่อ:** [ป้อมศัตรูแตก]
- **แพ้เมื่อ:** [ป้อมผู้เล่นแตก]
