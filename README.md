# Laser Cutting System — Backend API

Lazer kesim makineleri için **abonelik tabanlı hata kodu ve arıza çözüm kütüphanesi**nin backend'i.
Kullanıcılar makinelerinde çıkan hata kodlarını arar; hatanın açıklamasını, kaynağını ve çözüm adımlarını görür.
Hangi hata kodlarına erişebilecekleri abonelik planlarına ve seçtikleri makinelere bağlıdır.

| Depo | İçerik |
|---|---|
| **[lasercuttingsystem](https://github.com/YagizTurasan/lasercuttingsystem)** (bu depo) | Backend — .NET 9 Web API |
| [lasercuttingsystemfront](https://github.com/YagizTurasan/lasercuttingsystemfront) | Frontend — React + Vite |

---

## İçindekiler
- [Teknolojiler](#teknolojiler)
- [Çözüm yapısı](#çözüm-yapısı)
- [Kurulum ve çalıştırma](#kurulum-ve-çalıştırma)
- [Frontend ile birlikte çalıştırma](#frontend-ile-birlikte-çalıştırma)
- [Testler](#testler)
- [Yapılandırma](#yapılandırma)
- [E-posta](#e-posta)
- [Veri modeli](#veri-modeli)
- [Abonelik, erişim kuralları ve ödeme](#abonelik-erişim-kuralları-ve-ödeme)
- [Kimlik doğrulama](#kimlik-doğrulama)
- [API uçları](#api-uçları)
- [Excel ile toplu hata kodu yükleme](#excel-ile-toplu-hata-kodu-yükleme)
- [Canlı ortama çıkış](#canlı-ortama-çıkış)

---

## Teknolojiler

| Alan | Kullanılan |
|---|---|
| Framework | .NET 9, ASP.NET Core Web API |
| Veritabanı | SQL Server, Entity Framework Core 9 (code-first, migration) |
| Kimlik | ASP.NET Core Identity, JWT Bearer + httpOnly cookie'de refresh token |
| Eşleme | AutoMapper 12 |
| Excel | ClosedXML |
| E-posta | MailKit (SMTP) |
| Dokümantasyon | Swagger / Swashbuckle |
| Test | xUnit, Moq |

---

## Çözüm yapısı

`LaserCuttingSystem.sln` katmanlı bir mimariyle düzenlenmiştir:

```
lasercuttingsystem/
├── LaserCuttingSystem/              # Web API (giriş noktası) — LaserCuttingSystem.WebApi.csproj
│   ├── Controllers/                 # Kullanıcı controller'ları
│   │   └── Admin/                   # Admin paneli controller'ları
│   ├── Program.cs                   # DI, Swagger, CORS, migration, seed, geliştirme varsayılanları
│   └── appsettings.json             # Gizli bilgi içermez (bkz. Yapılandırma)
├── Business/                        # İş katmanı — LaserCuttingSystem.Business.csproj
│   ├── Services/                    # UserService, MachineService, ErrorCodeService, SubscriptionService, EmailService …
│   │   ├── AdminServices/           # Admin dashboard / kullanıcı / makine / hata kodu servisleri
│   │   ├── DomainService/           # MachineAccessService, SubscriptionDomainService (erişim kuralları)
│   │   └── DemoPaymentGateway.cs    # IPaymentGateway'in demo uygulaması
│   └── Mapping/                     # AutoMapper profilleri
├── LaserCuttingSystem.Data/         # EF Core DbContext, Repository + UnitOfWork, Migrations, Seeders
├── LaserCuttingSystem.Domain/       # Entity'ler, DTO'lar, Enum'lar, Domain Event'ler, Value Object'ler
├── LaserCuttingSystem.Identity/     # Identity yapılandırması, AuthService, JwtTokenService, yetki policy'leri
├── LaserCuttingSystem.Core/         # Ortak servisler (ICurrentUserService, IDateTimeProvider)
└── LaserCuttingSystem.Tests/        # xUnit testleri
```

Bağımlılık yönü: `WebApi → Business → Data → Domain`, `Identity` ve `Core` ortak olarak kullanılır.
Servis kayıtları her katmanın `Extensions/ServiceCollectionExtensions.cs` dosyasındadır ve
`Program.cs` içinde şu sırayla çağrılır: `AddCoreServices` → `AddDataLayer` → `AddIdentityServices` → `AddBusinessServices`.

---

## Kurulum ve çalıştırma

### Gereksinimler
- [.NET 9 SDK](https://dotnet.microsoft.com/download)
- SQL Server (LocalDB, Express veya tam sürüm)

### Adımlar

1. **Depoyu klonlayın.**
   ```bash
   git clone https://github.com/YagizTurasan/lasercuttingsystem.git
   cd lasercuttingsystem
   ```

2. **Bağlantı dizesini ayarlayın.** `LaserCuttingSystem/appsettings.json` içindeki `ConnectionStrings:DefaultConnection`
   değeri şu an geliştiricinin makinesine (`Server=YAGIZ`) işaret ediyor. Kendi sunucunuz için dosyayı değiştirmek yerine
   User Secrets ile geçersiz kılın:
   ```bash
   dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=(localdb)\\MSSQLLocalDB;Database=LaserCuttingDb;Trusted_Connection=True;TrustServerCertificate=True;" --project LaserCuttingSystem
   ```

3. **API'yi başlatın.**
   ```bash
   dotnet run --project LaserCuttingSystem --launch-profile https
   ```
   - HTTPS: `https://localhost:7206`
   - HTTP: `http://localhost:5238`
   - Swagger UI (yalnızca Development): kök adres, `https://localhost:7206/`

Başka bir adım gerekmez. İlk açılışta:
- **Veritabanı:** Yoksa oluşturulur ve bekleyen migration'lar otomatik uygulanır (`Database:AutoMigrate`).
- **Roller:** `Admin`, `Moderator`, `User`.
- **Abonelik planları:** Free, Basic, Professional, Enterprise.
- **Admin kullanıcısı:** `SeedAdmin:Email` / `SeedAdmin:Password`. Geliştirmede şifre verilmemişse **`admin@lasercutting.com` / `Admin123!`** kullanılır.

> Makine ve hata kodu verisi seed edilmez. Admin paneli veya [Excel import](#excel-ile-toplu-hata-kodu-yükleme) ile eklenir.

### Geliştirme varsayılanları
Gizli bilgiler `appsettings.json`'da tutulmaz. Projenin klonlanıp hemen çalışabilmesi için **yalnızca `Development` ortamında**, ayarlanmamışlarsa şu değerler kullanılır (`Program.cs` → `ApplyDevelopmentDefaults`):

| Ayar | Geliştirme varsayılanı |
|---|---|
| `JwtSettings:Key` | Yalnızca yerel kullanıma uygun sabit bir anahtar |
| `SeedAdmin:Password` | `Admin123!` |
| `JwtSettings:RequireHttpsMetadata` | `false` |
| `EmailSettings:LogWhenDisabled` | `true` (bkz. [E-posta](#e-posta)) |

Canlı ortamda bu varsayılanlar **uygulanmaz**. `JwtSettings:Key` verilmezse uygulama açıklayıcı bir hatayla başlamayı reddeder.

### Yeni migration eklemek
```bash
dotnet tool install --global dotnet-ef   # bir kez
dotnet ef migrations add <MigrationAdi> --project LaserCuttingSystem.Data --startup-project LaserCuttingSystem
```
Migration bir sonraki açılışta otomatik uygulanır.

---

## Frontend ile birlikte çalıştırma

İki depoyu yan yana klonlayın ve iki ayrı terminalde çalıştırın:

```bash
git clone https://github.com/YagizTurasan/lasercuttingsystem.git
git clone https://github.com/YagizTurasan/lasercuttingsystemfront.git

# Terminal 1 — backend (https://localhost:7206)
cd lasercuttingsystem
dotnet run --project LaserCuttingSystem --launch-profile https

# Terminal 2 — frontend (http://localhost:3000)
cd lasercuttingsystemfront
npm install
npm run dev
```

Tarayıcıda `http://localhost:3000` adresini açın ve varsayılan admin hesabıyla giriş yapın.

- Frontend, `/api` isteklerini Vite proxy'si üzerinden `https://localhost:7206` adresine iletir.
  Bu yüzden ek CORS veya sertifika ayarı gerekmez.
- Backend farklı bir adreste çalışıyorsa frontend'deki `VITE_API_PROXY_TARGET` değişkenini değiştirin
  (bkz. [frontend README](https://github.com/YagizTurasan/lasercuttingsystemfront#ortam-değişkenleri)).

---

## Testler

```bash
dotnet test
```

`LaserCuttingSystem.Tests` projesi veritabanı gerektirmez; repository'ler Moq ile taklit edilir. Kapsanan konular:

| Test sınıfı | Kapsam |
|---|---|
| `MachineAccessServiceTests` | Plan seviyelerine göre makine ve hata kodu erişimi, seçim limiti, parça hiyerarşisi üzerinden erişim |
| `ErrorCodeTests`, `SubscriptionPlanTests` | Entity iş kuralları ve hesaplanan alanlar |
| `AdminMachineServiceTests` | Silinen model / bölüm / parçayı geri getirme, silinmişleri de listeleme |
| `DemoPaymentGatewayTests` | Demo ödeme sonucu |
| `EmailServiceTests` | SMTP kapalıyken davranış, e-posta bağlantılarının frontend sayfalarına gitmesi |

---

## Yapılandırma

`appsettings.json` bölümleri. Gizli değerler dosyada **boştur**; [User Secrets](https://learn.microsoft.com/aspnet/core/security/app-secrets) (geliştirme) veya ortam değişkenleri (canlı) ile verilir.
Ortam değişkenlerinde `:` yerine `__` kullanılır, örneğin `JwtSettings__Key`.

| Bölüm | Anahtar | Varsayılan | Açıklama |
|---|---|---|---|
| `ConnectionStrings` | `DefaultConnection` | — | SQL Server bağlantı dizesi |
| `Database` | `AutoMigrate` | `true` | Açılışta bekleyen migration'ları uygula |
| `AppSettings` | `FrontendUrl` | `http://localhost:3000` | E-postalardaki bağlantıların gideceği frontend adresi |
| `Cors` | `AllowedOrigins` | `["http://localhost:3000"]` | API'ye tarayıcıdan erişebilecek frontend adresleri (credentials açık) |
| `Identity` | `RequireConfirmedEmail` | `false` | `true` ise e-postasını doğrulamayan kullanıcı giriş yapamaz. Açmadan önce SMTP'yi yapılandırın. |
| `SeedAdmin` | `Email` | `admin@lasercutting.com` | İlk açılışta oluşturulacak admin |
| | `Password` 🔒 | — (geliştirmede `Admin123!`) | Verilmezse admin oluşturulmaz, uyarı loglanır |
| `JwtSettings` | `Key` 🔒 | — (geliştirmede sabit anahtar) | Token imzalama anahtarı, en az 32 karakter |
| | `Issuer`, `Audience` | | Token doğrulama değerleri |
| | `AccessTokenExpiryMinutes` | `15` | Access token ömrü |
| | `RefreshTokenExpiryDays` | `7` | Refresh token ömrü |
| | `RequireHttpsMetadata` | `true` (geliştirmede `false`) | |
| `EmailSettings` | `IsEnabled` | `false` | SMTP ile gönderim |
| | `SmtpHost`, `SmtpPort`, `EnableSsl` | `smtp.gmail.com`, `587`, `true` | 465'te SSL, diğer portlarda STARTTLS kullanılır |
| | `SmtpUsername` 🔒, `SmtpPassword` 🔒 | — | SMTP kimlik bilgileri |
| | `FromEmail`, `FromName` | | Gönderen |
| | `LogWhenDisabled` | `false` (geliştirmede `true`) | Gönderim kapalıyken e-posta bağlantılarını loga yaz |

🔒 = gizli değer; dosyaya yazmayın.

```bash
# Geliştirmede gizli değer ayarlama örneği
dotnet user-secrets set "JwtSettings:Key" "<en-az-32-karakter-rastgele-anahtar>" --project LaserCuttingSystem
```

---

## E-posta

Şifre sıfırlama, e-posta doğrulama, hoş geldin ve abonelik onayı e-postaları gönderilir.
E-postalardaki bağlantılar frontend sayfalarına gider (`AppSettings:FrontendUrl`):

| E-posta | Bağlantı | Geçerlilik |
|---|---|---|
| Şifre sıfırlama | `/reset-password?email=…&token=…` | 1 saat, tek kullanımlık |
| E-posta doğrulama | `/verify-email?email=…&token=…` | 24 saat, tek kullanımlık |

Token'lar ASP.NET Identity'nin token sağlayıcılarıyla üretilir ve doğrulanır. Şifre değişince eski bağlantılar geçersiz olur.

### Geliştirmede SMTP olmadan
`EmailSettings:IsEnabled = false` iken geliştirme ortamında e-posta gönderilmez. Bunun yerine konu ve bağlantılar konsola loglanır:
```
warn: E-posta gönderimi kapalı, içerik loga yazıldı. Alıcı: … | Konu: Şifre Sıfırlama … | Bağlantılar: http://localhost:3000/reset-password?…
```
Bu bağlantıyı tarayıcıda açarak akışı uçtan uca deneyebilirsiniz.

### Gerçek gönderim (ör. Gmail)
Gmail hesabında 2 adımlı doğrulamayı açıp bir [uygulama şifresi](https://myaccount.google.com/apppasswords) oluşturun, sonra:
```bash
dotnet user-secrets set "EmailSettings:IsEnabled" "true" --project LaserCuttingSystem
dotnet user-secrets set "EmailSettings:SmtpUsername" "hesabiniz@gmail.com" --project LaserCuttingSystem
dotnet user-secrets set "EmailSettings:SmtpPassword" "<uygulama-şifresi>" --project LaserCuttingSystem
dotnet user-secrets set "EmailSettings:FromEmail" "hesabiniz@gmail.com" --project LaserCuttingSystem
```

---

## Veri modeli

### Makine hiyerarşisi
```
LaserMachine        (Marka, ör. "Trumpf")
 └── MachineModel   (Model kodu)
      └── MachineSection   (Bölüm, ör. "Kesim Kafası")
           └── MachinePart (Parça)
                └── ErrorCode
```

### Hata kodu (`ErrorCode`)
| Alan | Açıklama |
|---|---|
| `Code` | Hata kodu (ör. `E102`), en fazla 20 karakter |
| `Title` | Başlık |
| `Description` | Açıklama |
| `Solutions` | Çözüm adımları |
| `IsFreeAccess` | `true` ise herkes görebilir (ücretsiz kod) |
| `IsCommonAcrossAllMachines` | Tüm makinelerde ortak kod |
| `MachineId` / `MachinePartId` | Bağlı olduğu makine veya parça (ikisi de boşsa "Genel") |
| `ErrorSourceId` | Hata kaynağı (ör. Elektrik, Mekanik) |

### Diğer entity'ler
- **Kimlik:** `ApplicationUser`, `ApplicationRole`, `UserRefreshToken`
- **Abonelik:** `SubscriptionPlan`, `UserSubscription`, `UserMachineSelection`, `PaymentHistory`
- **Analitik:** `UserActivity` (admin dashboard'daki son aktiviteler)

Tüm entity'ler `BaseEntity` / `AuditableEntity` sınıfından türer (Id: `Guid`, `CreatedAt`, `UpdatedAt`, `IsActive`).
Silme işlemleri soft delete'tir (`IsActive = false`).

---

## Abonelik, erişim kuralları ve ödeme

Seed edilen planlar:

| Plan | Görünen ad | Fiyat | Level | Makine seçimi | Erişim |
|---|---|---|---|---|---|
| Free | Ücretsiz | 0 ₺ | 0 | 0 | Yalnızca `IsFreeAccess` kodlar |
| Basic | Temel | 299 ₺/ay | 1 | 1 | Seçilen makinenin kodları |
| Professional | Profesyonel | 799 ₺/ay | 2 | 3 | Seçilen makinelerin kodları |
| Enterprise | Kurumsal | 2999 ₺/ay | 3 | Sınırsız (`-1`) | Tüm makineler (`CanAccessAllMachines`) |

- Yeni kayıt olan her kullanıcıya otomatik olarak **Free** abonelik açılır.
- Kullanıcı planının izin verdiği sayıda makineyi `POST /api/Machine/{id}/select` ile seçer.
- Plan değiştiğinde (`SubscriptionService.HandleSubscriptionChangeAsync`) makine seçimleri yeni limite göre düzenlenir.
- Erişim kontrolü [`MachineAccessService`](Business/Services/DomainService/MachineAccessService.cs) içindedir:
  1. Hata kodu pasifse → erişim yok
  2. `IsFreeAccess` → herkes erişir
  3. Aktif abonelik yoksa → erişim yok
  4. Enterprise → her şeye erişir
  5. Diğer planlar → hata kodunun bağlı olduğu makine, kullanıcının seçtiği makineler arasındaysa erişir

### Ödeme
> **Proje demo ödeme ile çalışır; gerçek ödeme alınmaz.** Bu bilinçli bir tercihtir; gerçek bir ödeme sağlayıcısı şu an kapsam dışındadır.

Ödemeler [`IPaymentGateway`](Business/IServices/IPaymentGateway.cs) arayüzü üzerinden alınır:
- Ücretli bir plana abone olunduğunda ve abonelik yenilendiğinde `ChargeAsync` çağrılır.
- Başarılı ödeme `PaymentHistory`'ye yazılır.
- Ödeme başarısızsa abonelik oluşturulmaz ya da uzatılmaz.
- Ücretsiz plan ve adminin kullanıcıya plan ataması ödeme almaz.

Kayıtlı uygulama **`DemoPaymentGateway`**'dir. Her işlemi başarılı sayar ve `DEMO-…` önekli bir işlem numarası üretir.
Böylece abonelik, ödeme geçmişi ve admin gelir istatistikleri gerçek para hareketi olmadan çalışır.

İleride gerçek bir sağlayıcı (iyzico, Stripe vb.) eklenmek istenirse:
1. `IPaymentGateway`'i uygulayan bir sınıf yazılır.
2. `Business/Extensions/ServiceCollectionExtensions.cs` içinde `DemoPaymentGateway` yerine o sınıf kaydedilir.

---

## Kimlik doğrulama

- Giriş (`POST /api/Auth/login`) yanıt gövdesinde bir **access token** (15 dk) ve kullanıcı bilgisi döner.
- **Refresh token** (7 gün) yanıt gövdesinde **dönmez**; JavaScript'in okuyamayacağı bir cookie olarak ayarlanır:
  ```
  Set-Cookie: lcs_refresh_token=...; path=/api/Auth; secure; samesite=strict; httponly
  ```
  Yanıttaki `refreshToken` alanı her zaman boştur.
- İsteklerde `Authorization: Bearer <accessToken>` başlığı gönderilir. Swagger'daki **Authorize** butonu ile de test edilebilir.
- Süresi dolan token için yanıta `Token-Expired: true` başlığı eklenir; istemci `POST /api/Auth/refresh-token` ile yeniler.
  - Uç cookie'yi okur; gövde gerekmez.
  - Her kullanımda yeni bir refresh token üretir ve cookie'yi günceller (rotasyon). Kullanılmış token tekrar kabul edilmez.
  - Cookie yoksa (mobil istemci vb.) gövdedeki `{ "refreshToken": "..." }` değeri kabul edilir.
- `POST /api/Auth/logout` ve `POST /api/Auth/revoke-all-tokens` refresh token'ı iptal eder ve cookie'yi siler.
- Cookie `SameSite=Strict` olduğu için frontend ile API **aynı site** altında sunulmalıdır. Geliştirmede Vite proxy'si bunu sağlar.
- Token içindeki özel claim'ler: `subscription_level`, `subscription_plan`, `is_enterprise`, `subscription_expires`, `company` ve roller.
  Abonelik değiştiğinde bu claim'ler token yenilenene kadar eski değeri taşır.
- Şifre kuralları: en az 6 karakter ve en az 1 rakam (şifre sıfırlamada en az 8 karakter). 5 hatalı denemede 15 dk kilitlenir.
- Şifre sıfırlama ve e-posta doğrulama token'ları için bkz. [E-posta](#e-posta).

---

## API uçları

Tüm uçlar `/api` önekiyle başlar. 🔒 = giriş gerekli, 👑 = Admin rolü gerekli.

### Auth — `/api/Auth`
| Metot | Yol | Açıklama |
|---|---|---|
| POST | `/login` | Giriş |
| POST | `/register` | Kayıt (Free abonelik otomatik açılır) |
| POST | `/refresh-token` | Token yenileme (cookie ile; gövde isteğe bağlı) |
| POST | `/logout` | Çıkış (refresh token iptali, cookie silinir) |
| POST | `/forgot-password` | Şifre sıfırlama e-postası gönder |
| POST | `/reset-password` | E-postadaki token ile şifre sıfırla — body: `{ email, token, newPassword, confirmPassword }` |
| POST | `/send-email-verification` | Doğrulama e-postası gönder |
| POST | `/verify-email` | E-postadaki token ile doğrula — body: `{ email, token }` |
| POST | `/change-password` 🔒 | Şifre değiştir |
| POST | `/revoke-all-tokens` 🔒 | Tüm oturumları kapat |
| GET | `/profile` 🔒 | Token'daki kullanıcı bilgisi |

### Kullanıcı — `/api/Users`
| Metot | Yol | Açıklama |
|---|---|---|
| GET | `/profile` 🔒 | Profil (roller dahil) |
| PUT | `/profile` 🔒 | Profil güncelle |
| GET | `/` 👑 | Kullanıcı listesi |
| POST | `/{id}/activate`, `/{id}/deactivate` 👑 | Aktif/pasif yap |

### Makine — `/api/Machine` 🔒
| Metot | Yol | Açıklama |
|---|---|---|
| GET | `/` | Tüm makineler |
| GET | `/{id}` | Makine detayı |
| GET | `/search` | Makine arama (query string) |
| GET | `/selected` | Kullanıcının seçtiği makineler |
| POST | `/{id}/select` | Makine seç |
| DELETE | `/{id}/unselect` | Seçimi kaldır |
| PUT | `/reorder` | Seçilen makineleri sırala |
| GET | `/selection-status` | Seçim limiti durumu |
| GET | `/{id}/models` | Makinenin aktif modelleri |
| GET | `/models/{modelId}/sections` | Modelin aktif bölümleri |
| GET | `/sections/{sectionId}/parts` | Bölümün aktif parçaları |

### Hata kodları — `/api/ErrorCodes`
| Metot | Yol | Açıklama |
|---|---|---|
| GET | `/free` | Ücretsiz hata kodları (giriş gerekmez) |
| GET | `/sources` | Hata kaynakları (giriş gerekmez) |
| GET | `/{id}` 🔒 | Hata kodu detayı (erişim kontrollü) |
| GET | `/search` 🔒 | Arama: `?Query=&PageNumber=&PageSize=&SortBy=&SortDescending=` |
| POST | `/search` 🔒 | Aynı arama, JSON body ile. Filtreler: `{ "filters": { "accessLevel": "Free" \| "Premium", "machineId": "<guid>" } }` (frontend bunu kullanır) |
| GET | `/by-machine/{machineId}` 🔒 | Makineye göre kodlar |
| GET | `/access-info` 🔒 | Kullanıcının erişim özeti |
| GET | `/stats` 👑 | İstatistikler |

### Abonelik — `/api/Subscription` 🔒
| Metot | Yol | Açıklama |
|---|---|---|
| GET | `/plans` | Planlar |
| GET | `/current` | Aktif abonelik |
| GET | `/history` | Abonelik geçmişi |
| GET | `/payments` | Ödeme geçmişi (`PaymentHistoryDto[]`) |
| POST | `/subscribe` | Plana abone ol — body: `{ planId, autoRenew }` |
| POST | `/cancel` | İptal |
| POST | `/renew` | Yenile |
| GET | `/machine-selection-info` | Kalan makine seçim hakkı |
| GET | `/summary` | Dashboard özeti |
| GET | `/compare` | Plan karşılaştırma |

### Admin — 👑
| Controller | Yol öneki | Uçlar |
|---|---|---|
| Dashboard | `/api/admin/dashboard` | `GET summary`, `GET subscription-stats`, `GET revenue-stats`, `POST recent-activities` |
| Kullanıcılar | `/api/admin/users` | `POST search`, `GET/PUT/DELETE {id}`, `POST /`, `POST {id}/activate`, `POST {id}/deactivate`, `POST/DELETE {id}/roles/{role}`, `POST {id}/subscription`, `POST {id}/reset-password`, `GET roles` |
| Makineler | `/api/admin/machines` | `POST search`, `POST /`, `PUT/DELETE {id}` |
| Makine hiyerarşisi | `/api/admin/machines` | Listeleme, silinmişler dahil: `GET {machineId}/models`, `GET models/{id}/sections`, `GET sections/{id}/parts`. Ekleme: `POST {machineId}/models`, `POST models/{id}/sections`, `POST sections/{id}/parts`. Düzenleme ve silme: `PUT/DELETE models/{id}`, `sections/{id}`, `parts/{id}`. Geri getirme: `POST models/{id}/restore`, `sections/{id}/restore`, `parts/{id}/restore`. |
| Hata kodları | `/api/admin/error-codes` | `POST search`, `POST /`, `PUT/DELETE {id}`, `GET/POST sources`, `PUT/DELETE sources/{id}` |
| Excel | `/api/ExcelUpload` | `GET template` (boş şablon), `POST import` (multipart/form-data) |

Silme ve geri getirme alt seviyeyi de etkiler:

| Öğe | Silme / geri getirme şunu da etkiler |
|---|---|
| Model | Bölümleri |
| Bölüm | Parçaları |
| Parça | Parçaya bağlı hata kodları |

Admin uçları genellikle `ApiResponse<T>` (`{ success, message, data, errors }`) zarfıyla yanıt döner.
Sayfalı listeler `PagedResult<T>` (`{ items, totalCount, pageNumber, pageSize }`) biçimindedir.

---

## Excel ile toplu hata kodu yükleme

- `GET /api/ExcelUpload/template` 👑 doğru sütun sırasına ve örnek bir satıra sahip boş şablonu indirir (`hata-kodlari-sablon.xlsx`).
- `POST /api/ExcelUpload/import` 👑 dosyayı yükler — form alanı adı: `file` (`.xlsx` / `.xls`).

İlk satır başlıktır; veri 2. satırdan başlar. Sütun sırası:

| # | Sütun | Zorunlu | Not |
|---|---|---|---|
| 1 | Code | ✔ | Aynı koda sahip kayıt varsa **güncellenir** |
| 2 | Title | ✔ | |
| 3 | Description | | |
| 4 | Solutions | | |
| 5 | IsFreeAccess | | `1` = ücretsiz, diğer değerler = premium |
| 6 | Brand | ✔ | Yoksa yeni makine oluşturulur |
| 7 | Model | | Yoksa oluşturulur |
| 8 | Section | | Model verilmişse; yoksa oluşturulur |
| 9 | Part | | Section verilmişse; yoksa oluşturulur |
| 10 | ErrorSource | | Yoksa oluşturulur |

Yanıt; başarılı ve başarısız satır sayılarını ve satır bazlı hata mesajlarını içerir.

---

## Canlı ortama çıkış

Canlı ortamda (`ASPNETCORE_ENVIRONMENT=Production`) geliştirme varsayılanları uygulanmaz. Aşağıdaki değerleri ortam değişkeni olarak verin:

| Ortam değişkeni | Zorunlu | Not |
|---|---|---|
| `ConnectionStrings__DefaultConnection` | ✔ | |
| `JwtSettings__Key` | ✔ | En az 32 karakter, rastgele. Verilmezse uygulama başlamaz. |
| `SeedAdmin__Password` | İlk kurulumda | Verilmezse admin oluşturulmaz |
| `AppSettings__FrontendUrl` | ✔ | E-posta bağlantıları için, ör. `https://ornek.com` |
| `Cors__AllowedOrigins__0` | Frontend farklı origin'deyse | |
| `EmailSettings__IsEnabled`, `EmailSettings__SmtpUsername`, `EmailSettings__SmtpPassword`, `EmailSettings__FromEmail` | E-posta için | |
| `Identity__RequireConfirmedEmail` | İsteğe bağlı | E-posta çalışıyorsa `true` yapılabilir |

Ek olarak:
- **Ödeme:** Ödemeler demodur; abonelikler gerçek tahsilat yapılmadan aktifleşir (bkz. [Ödeme](#ödeme)).
- **Aynı site:** Frontend ile API aynı site altında sunulmalıdır (refresh token cookie'si `SameSite=Strict`).
