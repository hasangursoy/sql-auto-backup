# SQL Auto Backup

SQL Auto Backup is a console app which backups Microsoft SQL databases on run. It can be scheduled in order to backup specified databases, or all databases excluding a list and then zip to single file to a custom location, which can be a cloud backup app etc.

App uses sentry.io in order to send error logs or notifications for successfull backups. You need to have an account on sentry.io if you want logging. Your key is something like: https://[some-id]@sentry.io/[another-id]

This app can be used only for local sql server backups.

For app settings please check the app.config (SQLAutoBackup.exe.config).

## Türkçe: SQL Otomatik Yedek

Çalıştırılınca, Microsoft SQL veritabanlarını yedekleyen bir konsol uygulamasıdır. Belirtilen veritabanlarını veya bir liste hariç tüm veritabanlarını yedeklemek ve sonra bir bulut yedekleme uygulaması vb. özel bir konuma tek bir zip dosyaya sıkıştırmak için zamanlanabilir.

Uygulama, başarılı yedeklemeler için bildirim veya hata bildirimleri göndermek için sentry.io kullanıyor. Bildirimleri almak  istiyorsanız sentry.io'da bir hesabınızın olması gerekir. Anahtarınız şuna benzer: https: // [bazı-kimlik] @ sentry.io / [başka bir kimlik]

Bu uygulama yalnızca yerel sql sunucusu yedeklemeleri için kullanılabilir.

Uygulama ayarları için lütfen app.config'i (SQLAutoBackup.exe.config) kontrol edin.
