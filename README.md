# SQL Auto Backup

SQL Auto Backup is a console app which backups Microsoft SQL databases on run. It can be scheduled in order to backup specified databases, or all databases excluding a list and then zip to single file to a custom location, which can be a cloud backup app etc.

App uses sentry.io in order to send error logs or notifications for successfull backups. You need to have an account on sentry.io if you want logging. Your key is something like: https://[some-id]@sentry.io/[another-id]

This app works only for local sql server backups.

For app settings please check the app.config (SQLAutoBackup.exe.config).
