# WatchStore API

Web API לניהול עסק סיטונאי לשעונים. בעל העסק (Admin) מנהל את קטלוג השעונים, המלאי, החנויות וההזמנות. חנויות לקוח (Store) נרשמות, צופות בקטלוג ומזמינות שעונים. המערכת בנויה ב-ASP.NET Core 8 בארכיטקטורת ארבע שכבות, מעל PostgreSQL ו-EF Core, עם JWT,‏ NLog ובדיקות xUnit + Moq.

## כתובת השרת

השרת פרוס ב-Render: https://watchstore-api-uuls.onrender.com

- Swagger: https://watchstore-api-uuls.onrender.com/swagger
- כתובת בסיס ל-API: `https://watchstore-api-uuls.onrender.com/api`

בכתובת הראשית (`/`) אין דף, ולכן היא מחזירה 404. בתוכנית החינמית של Render השרת נרדם אחרי כ-15 דקות בלי פעילות, והבקשה הראשונה אחרי זה יכולה לקחת 30 עד 60 שניות.

## המשאב המוגבל והתחרות עליו

**המשאב המוגבל הוא המלאי של שעון מסוים** (`Product.StockQuantity`). ב-seed יש לדגם `ROLEX-001` יחידה אחת בלבד.

**איפה נמצאת התחרות:** כששתי חנויות שולחות `POST /api/orders` על אותו שעון כמעט באותו רגע, שתי הבקשות קוראות `StockQuantity = 1` ושתיהן עוברות את בדיקת המלאי. בלי טיפול, שתיהן היו שומרות, ושתי החנויות היו מקבלות אישור על אותה יחידה.

**איך המערכת מגיבה (optimistic concurrency):**

1. ל-`Product` יש `RowVersion` (`uint` שממופה לעמודת המערכת `xmin` של PostgreSQL, מוגדר ב-`ProductConfiguration` עם `IsRowVersion()`). הערך מתעדכן אוטומטית בכל שינוי בשורה.
2. `OrderService.CreateAsync` טוען את המוצרים, בודק מלאי, מוריד אותו ויוצר את ההזמנה. הכול נשמר ב-`SaveChanges` **אחד**, ש-EF Core מריץ בטרנזקציה אחת.
3. ה-UPDATE של המוצר נשלח עם `WHERE Id = @id AND xmin = @original`. אם חנות אחרת הספיקה לשמור לפני כן, אף שורה לא מתעדכנת, EF זורק `DbUpdateConcurrencyException` והטרנזקציה מתבטלת, כך שההזמנה לא נוצרת.
4. ה-Repository מתרגם את החריגה ל-`ConcurrencyConflictException` (של ה-Core, כדי שה-Service לא יהיה תלוי ב-EF). ה-Service כותב לוג **Warning** וזורק `ConflictException`.
5. ה-middleware מחזיר **409 Conflict**:

```json
{ "status": 409, "message": "המלאי של המוצר כבר נתפס על ידי משתמש אחר. אנא נסה שוב.", "correlationId": "..." }
```

ההוכחה נמצאת ב-`WatchStore.Tests/Concurrency/ProductConcurrencyTests.cs`: שני `DbContext` נפרדים מול PostgreSQL אמיתי קוראים את אותו מוצר ושניהם משנים אותו. הראשון שומר, השני מקבל `DbUpdateConcurrencyException`, והמלאי בבסיס הנתונים יורד פעם אחת בלבד.

מלאי שאינו מספיק כבר בזמן הקריאה (למשל מלאי 1 והזמנה של 2) נדחה גם הוא ב-409, עם הודעה שמציינת את הכמות הזמינה.

## דרישות

- .NET 8 SDK
- **PostgreSQL** מקומי. Connection string: `Host=localhost;Port=5432;Database=watchstore;Username=postgres;Password=postgres`. אפשר להחליף אותו ב-`appsettings.Development.json` או ב-User Secrets.

## הרצה מקומית

```bash
# 1. מפתח ה-JWT נשמר ב-User Secrets ולא ב-repo (לפחות 32 תווים)
dotnet user-secrets set "Jwt:Key" "<מחרוזת-אקראית-ארוכה-לפחות-32-תווים>" --project WatchStore.API

# 2. הרצה. ה-migrations וה-seed רצים אוטומטית בעלייה, בלי התערבות ידנית בבסיס הנתונים
dotnet run --project WatchStore.API --launch-profile http
```

Swagger: http://localhost:5140/swagger. מתחברים דרך `POST /api/auth/login`, מעתיקים את ה-`token`, לוחצים **Authorize** ומדביקים.

בדיקות (כולל בדיקת ה-concurrency, שיוצרת ומוחקת בסיס נתונים זמני ב-PostgreSQL. השרת נלקח מ-`WATCHSTORE_TEST_POSTGRES`, וברירת המחדל היא localhost עם postgres/postgres):

```bash
dotnet test
```

## משתמשי דמו

| Role  | Username | Password    | חנות                     |
|-------|----------|-------------|--------------------------|
| Admin | `admin`  | `Admin123!` | —                        |
| Store | `store1` | `Store123!` | Time Center Tel Aviv (1) |
| Store | `store2` | `Store123!` | Jerusalem Watches (2)    |

הדגמת התחרות: מתחברים כ-`store1` וכ-`store2` ושולחים במקביל `POST /api/orders` עם `{"items":[{"productId":1,"quantity":1}]}`. אחת מקבלת 201 והשנייה 409.

## Endpoints

| Method | Route | הרשאה |
|---|---|---|
| POST | `/api/auth/register`, `/api/auth/login` | פתוח |
| GET | `/api/products?search=&brand=&categoryId=&minPrice=&maxPrice=&sortBy=name\|price\|stock\|createdAt&sortDirection=asc\|desc&page=1&pageSize=10` | מחובר |
| GET | `/api/products/{id}` | מחובר |
| POST / PUT / DELETE | `/api/products`, `/api/products/{id}` | Admin |
| GET | `/api/orders`, `/api/orders/{id}` | Admin רואה הכול, חנות רק את שלה |
| POST | `/api/orders` | Store |
| PATCH | `/api/orders/{id}/status` | Admin |
| GET | `/api/stores`, `/api/stores/{id}` | Admin |
| GET / POST / PUT / DELETE | `/api/categories`, `/api/tags` | קריאה: מחובר · כתיבה: Admin |

סטטוסים: 200, 201 (עם `Location`), 204, 400 (ולידציה או כלל עסקי), 401, 403, 404, 409. כל שגיאה מוחזרת כ-JSON אחיד עם `status`, `message` ו-`correlationId`.

## ארכיטקטורה

```
WatchStore.Core     Entities, DTOs, Interfaces, Enums, Exceptions - ללא תלויות חיצוניות
WatchStore.Data     DbContext, Fluent API Configurations, Repositories, Migrations, Seed   -> Core
WatchStore.Service  Business logic, AutoMapper Profiles, BCrypt                            -> Core
WatchStore.API      Controllers, Middleware, JWT, Program.cs                               -> Core, Service, Data (רק לרישום DI)
WatchStore.Tests    xUnit + Moq (Services), PostgreSQL (Concurrency)
```

- **קשרים:** Store → Orders ו-Category → Products ‏(many-to-one), ‏Product ↔ Tag דרך `ProductTag` ‏(many-to-many). טעינה עם `Include`/`ThenInclude`, בלי N+1: גם המוצרים של הזמנה נטענים בשאילתה אחת (`WHERE Id IN (...)`).
- **Pagination אמיתי:** `Skip`/`Take` בתוך ה-`IQueryable`, לפני `ToListAsync`. כל קריאה בלבד מסומנת `AsNoTracking`.
- **מחיקת מוצר היא מחיקה רכה** (`IsActive = false`), כי מוצר שכבר הוזמן חייב להישאר בהיסטוריית ההזמנות.
- **Middleware:** הסדר ב-pipeline הוא `ExceptionMiddleware` (ראשון) → `CorrelationIdMiddleware` → Authentication → Authorization. ה-CorrelationId חוזר ב-header `X-Correlation-ID` ומופיע בכל שורת לוג.
- **NLog** (`nlog.config`): הקוד כותב דרך `ILogger` הרגיל, ו-NLog מתחבר מתחת. הלוגים נכתבים ל-`bin/.../logs/`, וקובץ נפרד מרכז Warning ומעלה. לא נרשמים ללוג סיסמאות, tokens או גוף בקשות.
- **סודות:** `Jwt:Key` נשמר ב-User Secrets. ה-connection string המקומי (משתמש ברירת המחדל של PostgreSQL) נמצא ב-`appsettings.Development.json`. ב-Render הוא מגיע ממסד הנתונים שמוגדר ב-`render.yaml`, בפורמט `postgresql://`, ו-`PostgresConnectionString.Normalize` ממיר אותו לפורמט של Npgsql.
