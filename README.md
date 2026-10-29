# OrtakRota – Arkadaşlarla Ortak Seyahat Planlayıcı

Bir grup arkadaşın aynı gezi planı üzerinde birlikte ve eş zamanlı çalışmasını sağlayan web portalı.
Depoda iki dersin dönem projesi birlikte bulunur (2026–2027 Güz):

- **BLM4581 Ağ Tabanlı Teknolojiler ve Uygulamaları:** ASP.NET Core MVC web portalı (`OrtakRota.Web/`)
- **BLM4587 iOS ile Mobil Uygulama Geliştirme I (Flutter):** ASP.NET Core Web API (`api/`) ve Flutter mobil uygulama (`mobile/`)

Üç parça da aynı PostgreSQL veritabanını ve aynı varlık sınıflarını kullanır.

**Grup üyeleri:** Simge Karsavuranoğlu ([@simgekar](https://github.com/simgekar)), Rana Berkdemir ([@Ranaberkdemir](https://github.com/Ranaberkdemir))

## Kullanılan teknolojiler

- ASP.NET Core MVC (.NET 10)
- Entity Framework Core 10 + PostgreSQL (Npgsql), Code-First migration
- ASP.NET Core Identity (kullanıcı ve roller)
- Bootstrap 5, Bootstrap Icons, jQuery
- Sonraki haftalarda: SignalR, Leaflet + OpenStreetMap, SortableJS, Swagger

## Proje yapısı

```
OrtakRota/
├── OrtakRota.sln
├── api/
│   └── OrtakRota.Api/   Flutter uygulamasının kullandığı Web API (Swagger ile test edilir)
├── mobile/              Flutter (Dart) mobil uygulama
└── OrtakRota.Web/
    ├── Controllers/     MVC controller'ları
    ├── Data/            AppDbContext (EF Core)
    ├── Models/          Varlık sınıfları (Trip, TripMember, Place, Vote, ...)
    ├── Migrations/      EF Core migration dosyaları
    ├── Views/           Razor görünümleri
    └── wwwroot/         CSS, JS ve diğer statik dosyalar
```

## Veritabanı tabloları

| Tablo | İçerik |
|---|---|
| AspNetUsers (AppUser) | Kullanıcılar |
| AspNetRoles | Site rolleri (Admin, User) |
| Trips | Geziler (ad, tarih, davet kodu, kurucu) |
| TripMembers | Gezi üyelikleri ve gezi içi rol (Owner / Editor / Viewer) |
| Places | Fikir havuzundaki mekanlar (konum, kategori) |
| Votes | Mekanlara verilen +1 / –1 oylar |
| ItineraryItems | Günlük plan kartları (gün, sıra, saat) |
| Expenses | Harcamalar |
| ExpenseShares | Harcamanın kişilere düşen payları |
| ChecklistItems | Ortak “Yanımıza Alınacaklar” listesi |

## Kurulum ve çalıştırma

### 1. Gerekenler

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) (kontrol: `dotnet --list-sdks`)
- [PostgreSQL](https://www.postgresql.org/download/) (kurulumda `postgres` kullanıcısı için bir şifre belirleyin)
- VS Code (C# Dev Kit eklentisiyle) veya Visual Studio 2026

### 2. Veritabanını oluşturun

pgAdmin'de ya da `psql` ile boş bir veritabanı açın:

```sql
CREATE DATABASE ortakrota;
```

### 3. Bağlantı dizesini User Secrets'a yazın

Veritabanı şifresi **depoya konmaz**. Her geliştirici kendi bilgisayarında User Secrets kullanır.
Proje klasöründe (`OrtakRota.Web`) şu komutu çalıştırın; `SIFRENIZ` yerine kendi PostgreSQL şifrenizi yazın:

```bash
cd OrtakRota.Web
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Port=5432;Database=ortakrota;Username=postgres;Password=SIFRENIZ"
```

Visual Studio kullanıyorsanız: projeye sağ tıklayın → **Manage User Secrets** → açılan `secrets.json` dosyasına yazın:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=ortakrota;Username=postgres;Password=SIFRENIZ"
  }
}
```

### 4. Migration'ları uygulayın

EF Core komut satırı aracını bir kez kurun:

```bash
dotnet tool install --global dotnet-ef --version 10.0.0
```

Ardından tabloları oluşturun:

```bash
cd OrtakRota.Web
dotnet ef database update
```

Modellerde değişiklik yaptığınızda yeni migration ekleyip veritabanını güncelleyin:

```bash
dotnet ef migrations add DegisiklikAdi
dotnet ef database update
```

### 5. Uygulamayı çalıştırın

```bash
cd OrtakRota.Web
dotnet run
```

Tarayıcıda `http://localhost:5080` adresini açın. Visual Studio'da `OrtakRota.sln` dosyasını açıp **F5** ile de çalıştırabilirsiniz.

### 6. Kullanıcı girişi ve roller

İlk çalıştırmada `Admin` ve `User` rolleri ile iki hazır hesap otomatik oluşturulur (web ve API aynı hesapları kullanır):

| Hesap | Şifre | Rol |
|---|---|---|
| `admin@ortakrota.com` | `Admin123` | Admin |
| `demo@ortakrota.com` | `Demo123` | User |

**Kayıt Ol** sayfasından açılan her yeni hesap `User` rolüyle başlar. Şifre en az 6 karakter olmalı ve en az bir büyük harf, bir küçük harf ve bir rakam içermelidir.

Gezi içinde ayrıca üç yetki vardır: geziyi kuran **Kurucu (Owner)**, davet koduyla katılan **Düzenleyici (Editor)** ve kurucunun yetkisini düşürdüğü **İzleyici (Viewer)**.

## Haftalık ilerleme

| Hafta | Lider | Yapılanlar |
|---|---|---|
| 1 | Simge Karsavuranoğlu | Depo, proje iskeleti, EF Core + PostgreSQL, varlık sınıfları, DbContext, ilk migration, Bootstrap sayfa düzeni |
| 2 | Simge Karsavuranoğlu | ASP.NET Core Identity ile kayıt, giriş ve çıkış; Admin ve User rolleri; gezi oluşturma, listeleme, düzenleme ve silme; davet koduyla geziye katılma; gezi içi roller (Kurucu, Düzenleyici, İzleyici) |

---

# Mobil uygulama (BLM4587 – Flutter)

## Web API'yi çalıştırma (`api/OrtakRota.Api`)

Web API, web projesiyle **aynı veritabanını** kullanır. Varlık sınıfları ve `AppDbContext`, `OrtakRota.Web` klasöründen bağlantı (link) olarak derlenir; tablolar web projesindeki migration'larla oluşturulur.

1. Yukarıdaki **2–4. adımları** uygulayın (veritabanı, User Secrets, `dotnet ef database update`). API, web projesiyle aynı `UserSecretsId` değerini kullandığı için bağlantı dizesini ayrıca girmenize gerek yoktur.
2. JWT imzalama anahtarını User Secrets'a ekleyin (en az 32 karakter; **depoya konmaz**). PowerShell'de rastgele bir anahtar üretip kaydetmek için:

```powershell
cd api/OrtakRota.Api
$key = [Convert]::ToBase64String((1..48 | ForEach-Object { Get-Random -Maximum 256 }) -as [byte[]])
dotnet user-secrets set "Jwt:Key" $key
```

3. API'yi başlatın:

```bash
cd api/OrtakRota.Api
dotnet run
```

4. Swagger arayüzü: `http://localhost:5180/swagger`

İlk çalıştırmada `Admin` ve `User` rolleri ile iki hazır hesap otomatik oluşturulur:

| Hesap | Şifre | Rol |
|---|---|---|
| `admin@ortakrota.com` | `Admin123` | Admin |
| `demo@ortakrota.com` | `Demo123` | User |

### Swagger'da token ile test

1. `POST /api/Auth/login` ile giriş yapın, yanıttaki `token` değerini kopyalayın.
2. Sayfanın üstündeki **Authorize** butonuna basıp token'ı yapıştırın.
3. Artık `Trips` ve (Admin ise) `Users` uç noktaları çağrılabilir. Token olmadan bu uç noktalar `401` döner.

| Metot | Uç nokta | Yetki | Açıklama |
|---|---|---|---|
| POST | `/api/auth/register` | Herkes | Kayıt olur (User rolü), token döner |
| POST | `/api/auth/login` | Herkes | Giriş yapar, token döner |
| GET | `/api/auth/me` | Giriş yapmış | Token sahibinin bilgileri |
| GET | `/api/trips` | Giriş yapmış | Üyesi olunan geziler (Admin: tümü) |
| GET | `/api/trips/{id}` | Giriş yapmış | Gezi detayı (üyeler, mekan ve harcama sayısı) |
| POST | `/api/trips` | Giriş yapmış | Yeni gezi ekler, ekleyen kişi kurucu (Owner) olur |
| PUT | `/api/trips/{id}` | Kurucu / Admin | Geziyi günceller |
| DELETE | `/api/trips/{id}` | Kurucu / Admin | Geziyi siler |
| GET | `/api/users` | Admin | Tüm kullanıcılar ve rolleri |

## Flutter uygulamasını çalıştırma (`mobile/`)

### Gerekenler

- Flutter 3.35 veya üzeri (kontrol: `flutter --version`, `flutter doctor`)
- Android Studio + Android emülatörü (ya da USB hata ayıklaması açık bir Android cihaz)

### Adımlar

```bash
cd mobile
flutter pub get
flutter run
```

### API adresinin ayarlanması

Varsayılan adres `http://10.0.2.2:5180` olarak `lib/config/api_config.dart` dosyasında tanımlıdır. Android emülatörü bilgisayardaki `localhost`'a `10.0.2.2` adresiyle ulaşır.

Gerçek bir cihazda çalıştırırken bilgisayarın yerel IP adresini verin (API'yi de bu adresi dinleyecek şekilde başlatın):

```bash
# API tarafı
dotnet run --urls http://0.0.0.0:5180
# Mobil taraf
flutter run --dart-define=API_URL=http://192.168.1.20:5180
```

## Mobil – haftalık ilerleme

| Hafta | Lider | Yapılanlar |
|---|---|---|
| 1 | Simge Karsavuranoğlu | Web API projesi, Swagger, Trips CRUD uç noktaları, rol ve demo kullanıcı seed; Flutter projesi, go_router ile gezinme, Giriş ekranı (form doğrulama), API'den beslenen Gezilerim ve Gezi Detayı ekranları |
| 2 | Simge Karsavuranoğlu | API: JWT ile kayıt/giriş, Admin ve User rolleri, yetkilendirilmiş Trips uç noktaları, Admin'e özel kullanıcı listesi, Swagger'da Authorize |
