# WatchFlow AI – שלב 2: לוגיקה עסקית ו-API

## Endpoints חדשים (כולם דורשים Admin)

| Method | Route | תיאור |
|---|---|---|
| GET | `/api/customers/lookup?phone=050-1234567` | זיהוי לקוח לפי טלפון, בכל פורמט |
| GET | `/api/customers/{id}` | פרטי לקוח |
| GET | `/api/customers/{id}/appointments` | היסטוריית תורים של לקוח |
| POST | `/api/customers` | לקוח חדש (409 אם הטלפון קיים) |
| PUT | `/api/customers/{id}` | עדכון פרטי לקוח |
| GET | `/api/appointment-types` | סוגי התורים הפעילים |
| GET | `/api/appointments/availability?appointmentTypeId=1&fromDate=2027-03-07&customerType=Collector` | מועדים פנויים |
| GET | `/api/appointments?fromDate=&toDate=&status=&page=&pageSize=` | יומן תורים |
| GET | `/api/appointments/{id}` | פרטי תור |
| POST | `/api/appointments` | קביעת תור |
| POST | `/api/appointments/{id}/cancel` | ביטול תור |

כל זמן בתשובה מופיע פעמיים: `StartUtc` (מה שנשמר) ו-`StartLocal` (שעון העסק, עם offset). בבקשת קביעת תור `StartUtc` חייב להסתיים ב-`Z`.

## קביעת תור – הזרימה

1. **IdempotencyKey**: אם המפתח כבר שימש לאותו תור בדיוק, מוחזר התור הקיים (ניסיון חוזר של ה-AI לא יוצר כפילות). מפתח זהה לתור אחר מחזיר 409.
2. **משך דינמי**: `AppointmentDurationCalculator` = משך בסיס + כל הכללים שמתאימים לסוג הלקוח ולצורך בהכנה מיוחדת.
3. **בדיקת זמינות סופית**: `SlotFinder` מחשב את המועדים הפנויים לאותו יום (שעות פעילות, תורים קיימים כולל זמני הכנה, חסימות, הודעה מוקדמת מינימלית). השעה המבוקשת חייבת להיות אחת מהם.
4. **שמירה**: אם בקשה מקבילה הספיקה לתפוס את השעה, ה-exclusion constraint דוחה את ה-INSERT.
5. **התנגשות**: בכל מקרה של שעה לא פנויה מוחזר 409 עם 3 החלופות הקרובות ביותר:

```json
{
  "status": 409,
  "message": "השעה שנבחרה אינה פנויה. אלה המועדים הפנויים הקרובים.",
  "correlationId": "...",
  "alternatives": [
    { "startUtc": "2027-03-07T07:15:00Z", "startLocal": "2027-03-07T09:15:00+02:00", "durationMinutes": 30 }
  ]
}
```

## הגדרות (`appsettings.json` → `WatchFlow`)

| מפתח | ברירת מחדל | משמעות |
|---|---|---|
| `SlotIntervalMinutes` | 15 | כל כמה דקות מתחיל מועד אפשרי |
| `MinimumNoticeMinutes` | 60 | אי אפשר לקבוע תור שמתחיל בפחות מזה |
| `DefaultSearchDays` / `MaxSearchDays` | 7 / 14 | טווח חיפוש זמינות |
| `AlternativesCount` | 3 | מספר החלופות ב-409 |

## בדיקות

| קובץ | מה נבדק |
|---|---|
| `Scheduling/AppointmentDurationCalculatorTests` | משך דינמי וכללים |
| `Scheduling/SlotFinderTests` | שעות פעילות, הפסקה, תורים קיימים, buffers, חסימות, כמה משאבים, שעון קיץ |
| `Scheduling/SchedulingIntegrationTests` | תרחישים A, B, C מול PostgreSQL: לקוח חדש קובע תור, זיהוי לקוח קיים, שתי הזמנות מקבילות לאותה שעה, idempotency, ביטול, עבר, יום סגור |

## לא בשלב הזה

- הרשאה לשירות ה-AI (role ייעודי וטוקן שירות) – שלב 2.5, יחד עם שירות ה-Python.
- מסכי ניהול להגדרות העסק (שעות, חסימות, סוגי תורים) – כרגע דרך ה-seed.
- בדיקות HTTP מלאות (`WebApplicationFactory`) – יתווספו כשיהיו endpoints שה-AI קורא להם.
